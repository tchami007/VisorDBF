using System.Data;
using System.Data.Odbc;
using System.Globalization;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Logging;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public sealed class SybaseExportService : ISybaseExportService
{
    private const int BatchSize = 1000;
    private const int ConnectionTimeoutSec = 30;
    private const int CommandTimeoutSec = 300;
    private const int OpenAsyncTimeoutSec = 60;
    private const int MaxConsecutiveErrors = 1;

    private readonly SybaseColumnMappingService _columnMappingService;
    private readonly SybaseProbeService _probeService;

    public SybaseExportService()
    {
        var mappingService = new SybaseColumnMappingService();
        _columnMappingService = mappingService;
        _probeService = new SybaseProbeService(mappingService);
    }

    public SybaseExportService(SybaseColumnMappingService columnMappingService, SybaseProbeService probeService)
    {
        _columnMappingService = columnMappingService ?? throw new ArgumentNullException(nameof(columnMappingService));
        _probeService = probeService ?? throw new ArgumentNullException(nameof(probeService));
    }

    public async Task TransferAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"VisorDBF_Sybase_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        using var log = new FileLogger(logPath);

        log.WriteLine($"=== Sybase Transfer Start ===");
        log.WriteLine($"Host: {config.Host}:{config.Port}");
        log.WriteLine($"Database: {config.Database}");
        log.WriteLine($"Table: {config.TableName}");
        log.WriteLine($"Records: {file.Records.Count}");
        log.WriteLine($"MaxConsecutiveErrors: {MaxConsecutiveErrors}");
        log.WriteLine($"Log file: {logPath}");

        var connectionString = BuildConnectionString(config);

        log.WriteLine("Creating OdbcConnection...");
        await using var connection = new OdbcConnection(connectionString);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            try
            {
                log.WriteLine("Opening connection...");
                var openTask = connection.OpenAsync(cancellationToken);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(OpenAsyncTimeoutSec), cancellationToken);
                var done = await Task.WhenAny(openTask, timeoutTask);

                if (done == timeoutTask)
                {
                    log.WriteLine($"OpenAsync timed out after {OpenAsyncTimeoutSec}s");
                    throw new ExportException(
                        $"No se pudo conectar a {config.Host}:{config.Port} en {OpenAsyncTimeoutSec}s. " +
                        "Verifique que el servidor Sybase este accesible.",
                        config.TableName);
                }
                await openTask;
            }
            catch (OperationCanceledException)
            {
                log.WriteLine("OpenAsync cancelled by user");
                throw;
            }
            catch (ExportException) { throw; }
            catch (Exception ex)
            {
                log.WriteLine($"OpenAsync failed: {ex.Message}");
                throw new ExportException($"Error al conectar a Sybase: {ex.Message}", config.TableName, ex);
            }

            sw.Stop();
            log.WriteLine($"Connection opened successfully", sw.Elapsed);

            var columns = await _columnMappingService.LoadColumnTypesAsync(connection, config.TableName, file.Fields, log, cancellationToken);

            log.WriteLine($"Building INSERT command for {columns.Count} columns...");
            var insertSql = BuildInsertSql(config.TableName, columns);
            log.WriteLine($"INSERT SQL: {insertSql}");

            await using var command = new OdbcCommand(insertSql, connection);
            command.CommandType = CommandType.Text;
            command.CommandTimeout = CommandTimeoutSec;

            var parameters = new OdbcParameter[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                parameters[i] = new OdbcParameter($"@p{i}", OdbcType.VarChar);
                command.Parameters.Add(parameters[i]);
            }

            var records = file.Records;
            int total = records.Count;
            int processed = 0;
            int skipped = 0;

            log.WriteLine($"Starting transfer: {total} records in batches of {BatchSize}");

            sw.Restart();

            try
            {
                for (int batchStart = 0; batchStart < total; batchStart += BatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int batchEnd = Math.Min(batchStart + BatchSize, total);
                    int batchCount = batchEnd - batchStart;

                    log.WriteLine($"Batch: records {batchStart}-{batchEnd - 1}");

                    var (batchProcessed, batchSkipped, batchFailed) = await TryProcessBatchAsync(
                        connection, command, parameters, columns,
                        records, batchStart, batchEnd, log, cancellationToken);

                    processed += batchProcessed;
                    skipped += batchSkipped;

                    if (batchFailed)
                    {
                        log.WriteLine($"Batch {batchStart}-{batchEnd - 1} failed after retry, falling back to individual inserts");
                        var (indivProcessed, indivSkipped) = await ProcessIndividualWithFallbackAsync(
                            connection, command, parameters, columns,
                            records, batchStart, batchEnd, log, cancellationToken);

                        processed += indivProcessed;
                        skipped += indivSkipped;
                    }

                    progress.Report(processed);

                    if (processed % (BatchSize * 10) == 0 || processed >= total)
                        log.WriteLine($"Progress: {processed}/{total}, skipped: {skipped}", sw.Elapsed);
                }

                sw.Stop();
                log.WriteLine($"Transfer completed. Processed: {processed}, Skipped: {skipped}", sw.Elapsed);
            }
            catch (OperationCanceledException)
            {
                log.WriteLine($"Transfer cancelled by user. Processed: {processed}, Skipped: {skipped}");
                throw;
            }
            catch (ExportException)
            {
                log.WriteLine($"Transfer aborted. Processed: {processed}, Skipped: {skipped}");
                throw;
            }
            catch (Exception ex)
            {
                log.WriteLine($"Transfer failed at record {processed}/{total}: {ex.Message}");
                log.WriteLine($"Exception: {ex}");
                throw;
            }
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
            {
                log.WriteLine("Closing connection...");
                connection.Close();
                log.WriteLine("Connection closed");
            }
            log.WriteLine("=== Sybase Transfer End ===");
        }
    }

    private sealed class BatchResult
    {
        public int Processed { get; set; }
        public int Skipped { get; set; }
        public bool Failed { get; set; }
    }

    private static async Task<(int processed, int skipped, bool failed)> TryProcessBatchAsync(
        OdbcConnection connection,
        OdbcCommand command,
        OdbcParameter[] parameters,
        List<ColumnInfo> columns,
        IReadOnlyList<DbfRecord> records,
        int batchStart,
        int batchEnd,
        FileLogger log,
        CancellationToken cancellationToken)
    {
        int processed = 0;
        int skipped = 0;

        using var tx = connection.BeginTransaction();
        command.Transaction = tx;

        try
        {
            for (int i = batchStart; i < batchEnd; i++)
            {
                var record = records[i];
                if (record.IsDeleted)
                {
                    skipped++;
                    continue;
                }

                SetParameterValues(parameters, columns, record, log, i);
                await command.ExecuteNonQueryAsync(cancellationToken);
                processed++;
            }

            tx.Commit();
            return (processed, skipped, false);
        }
        catch
        {
            try { tx.Rollback(); } catch (Exception rollbackEx) { log.WriteLine($"Rollback failed: {rollbackEx.Message}"); }
            return (0, 0, true);
        }
    }

    private static async Task<(int processed, int skipped)> ProcessIndividualWithFallbackAsync(
        OdbcConnection connection,
        OdbcCommand command,
        OdbcParameter[] parameters,
        List<ColumnInfo> columns,
        IReadOnlyList<DbfRecord> records,
        int batchStart,
        int batchEnd,
        FileLogger log,
        CancellationToken cancellationToken)
    {
        int processed = 0;
        int skipped = 0;
        int consecutiveErrors = 0;

        command.Transaction = null;

        for (int i = batchStart; i < batchEnd; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = records[i];
            if (record.IsDeleted)
            {
                skipped++;
                continue;
            }

            try
            {
                SetParameterValues(parameters, columns, record, log, i);
                await command.ExecuteNonQueryAsync(cancellationToken);
                processed++;
                consecutiveErrors = 0;
            }
            catch (Exception ex)
            {
                consecutiveErrors++;

                var fieldValues = string.Join(", ",
                    columns.Select(c => $"{c.Name}={record.Values.GetValueOrDefault(c.Name) ?? "NULL"}"));

                log.WriteLine($"Error en registro {i}: {ex.Message}");
                log.WriteLine($"  Valores: {fieldValues}");

                if (consecutiveErrors >= MaxConsecutiveErrors)
                {
                    log.WriteLine($"{MaxConsecutiveErrors} errores consecutivos, abortando");
                    throw new ExportException(
                        $"Se cancelo el traspaso por {MaxConsecutiveErrors} errores consecutivos. " +
                        $"Ultimo error en registro {i}: {ex.Message}",
                        "", ex);
                }
            }
        }

        return (processed, skipped);
    }

    private static void SetParameterValues(
        OdbcParameter[] parameters,
        List<ColumnInfo> columns,
        DbfRecord record,
        FileLogger? log = null,
        int recordIndex = -1)
    {
        for (int p = 0; p < columns.Count; p++)
        {
            var rawValue = record.Values.GetValueOrDefault(columns[p].Name);
            var converted = columns[p].Convert(rawValue);
            parameters[p].Value = converted;

            if (log != null && recordIndex >= 0 && converted is decimal d)
            {
                var info = columns[p];
                var intPartStr = decimal.Truncate(Math.Abs(d)).ToString("F0", CultureInfo.InvariantCulture);
                int intDigits = intPartStr.Length;
                int maxInt = info.Precision - info.Scale;
                if (intDigits > maxInt)
                    log.WriteLine($"  WARN registro {recordIndex} col '{info.Name}': {d} tiene {intDigits} enteros pero numeric({info.Precision},{info.Scale}) solo admite {maxInt}");
            }
        }
    }

    private static string BuildConnectionString(SybaseConnectionConfig config) =>
        config.BuildConnectionString();

    public async Task<ProbeResult> ProbeFirstRecordAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        CancellationToken cancellationToken)
    {
        return await _probeService.ProbeFirstRecordAsync(file, config, cancellationToken);
    }

    private static string BuildInsertSql(string tableName, List<ColumnInfo> columns)
    {
        var colNames = string.Join(", ", columns.Select(c => c.Name));
        var paramNames = string.Join(", ", columns.Select((c, i) =>
        {
            var lower = c.DbTypeName.ToLowerInvariant();
            if (lower is "numeric" or "decimal" or "money" or "smallmoney")
            {
                var p = c.Precision > 0 ? c.Precision : 18;
                var s = c.Scale > 0 ? c.Scale : 0;
                return $"CONVERT(NUMERIC({p},{s}), @p{i})";
            }
            return $"@p{i}";
        }));
        return $"INSERT INTO {tableName} ({colNames}) VALUES ({paramNames})";
    }
}
