using System.Data;
using System.Data.Odbc;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Logging;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public sealed class SybaseProbeService
{
    private const int OpenAsyncTimeoutSec = 60;
    private const int CommandTimeoutSec = 300;

    private readonly SybaseColumnMappingService _columnMappingService;

    public SybaseProbeService(SybaseColumnMappingService columnMappingService)
    {
        _columnMappingService = columnMappingService ?? throw new ArgumentNullException(nameof(columnMappingService));
    }

    public async Task ProbeConversionsViaSybaseAsync(
        OdbcConnection connection,
        List<ColumnInfo> columns,
        DbfRecord firstRecord,
        FileLogger log,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        foreach (var col in columns)
        {
            var rawValue = firstRecord.Values.GetValueOrDefault(col.Name);
            object? converted;

            try
            {
                converted = col.Convert(rawValue);
            }
            catch (Exception ex)
            {
                errors.Add($"Columna '{col.Name}' ({col.DbTypeName}): C# falla con valor '{rawValue}': {ex.Message}");
                log.WriteLine($"  FAIL col '{col.Name}': C# conversion error: {ex.Message}");
                continue;
            }

            if (converted == null || converted is DBNull)
            {
                log.WriteLine($"  SKIP col '{col.Name}': valor nulo");
                continue;
            }

            var convertType = SybaseValueConverter.GetConvertTypeForProbe(col.DbTypeName, col.Precision, col.Scale);
            if (convertType == null)
            {
                log.WriteLine($"  SKIP col '{col.Name}' ({col.DbTypeName}): sin CONVERT necesario");
                continue;
            }

            var sql = $"SELECT CONVERT({convertType}, ?)";
            try
            {
                await using var cmd = new OdbcCommand(sql, connection);
                cmd.CommandTimeout = CommandTimeoutSec;
                cmd.Parameters.Add(new OdbcParameter("@val", OdbcType.VarChar, 8000)
                {
                    Value = converted
                });

                await cmd.ExecuteScalarAsync(cancellationToken);
                log.WriteLine($"  OK  col '{col.Name}' ({col.DbTypeName}): CONVERT({convertType}, '{converted}')");
            }
            catch (Exception ex)
            {
                var msg = $"Columna '{col.Name}' ({col.DbTypeName}): CONVERT({convertType}, '{converted}') falla: {ex.Message}";
                errors.Add(msg);
                log.WriteLine($"  FAIL col '{col.Name}': {msg}");
            }
        }

        if (errors.Count > 0)
        {
            var full = string.Join(Environment.NewLine, errors);
            throw new ExportException(
                $"Errores de conversion Sybase en {errors.Count} columna(s):{Environment.NewLine}{full}", "");
        }
    }

    public async Task<ProbeResult> ProbeFirstRecordAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"VisorDBF_Probe_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        using var log = new FileLogger(logPath);

        log.WriteLine($"=== Sybase Probe Start ===");
        log.WriteLine($"Host: {config.Host}:{config.Port}");
        log.WriteLine($"Database: {config.Database}");
        log.WriteLine($"Table: {config.TableName}");
        log.WriteLine($"Log file: {logPath}");

        var connectionString = config.BuildConnectionString();

        await using var connection = new OdbcConnection(connectionString);

        try
        {
            log.WriteLine("Opening probe connection...");
            var openTask = connection.OpenAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(OpenAsyncTimeoutSec), cancellationToken);
            var done = await Task.WhenAny(openTask, timeoutTask);

            if (done == timeoutTask)
            {
                log.WriteLine($"Probe connection timed out after {OpenAsyncTimeoutSec}s");
                return new ProbeResult(false, $"No se pudo conectar a {config.Host}:{config.Port} en {OpenAsyncTimeoutSec}s.");
            }
            await openTask;
        }
        catch (Exception ex)
        {
            log.WriteLine($"Probe connection failed: {ex.Message}");
            return new ProbeResult(false, $"Conexion fallida: {ex.Message}");
        }

        try
        {
            var columns = await _columnMappingService.LoadColumnTypesAsync(connection, config.TableName, file.Fields, log, cancellationToken);
            var extraInfos = SybaseExportService.CreateExtraColumnInfos(config.ExtraColumns);
            var allColumns = columns.Concat(extraInfos).ToList();

            var first = file.Records.FirstOrDefault(r => !r.IsDeleted);
            if (first == null)
            {
                log.WriteLine("No records to probe");
                return new ProbeResult(true, null);
            }

            log.WriteLine("Probing conversions against Sybase with first record...");
            if (extraInfos.Count > 0)
            {
                log.WriteLine($"Extra columns: {string.Join(", ", config.ExtraColumns.Select(e => $"{e.ColumnName} ({e.Type}) = '{e.RawValue}'"))}");
            }

            await ProbeConversionsViaSybaseAsync(connection, allColumns, first, log, cancellationToken);
            log.WriteLine("All Sybase conversions passed");
            return new ProbeResult(true, null);
        }
        catch (ExportException ex)
        {
            log.WriteLine($"Probe failed: {ex.Message}");
            return new ProbeResult(false, $"Probe fallido: {ex.Message}");
        }
        catch (Exception ex)
        {
            log.WriteLine($"Probe error: {ex.Message}");
            return new ProbeResult(false, $"Error durante probe: {ex.Message}");
        }
    }
}
