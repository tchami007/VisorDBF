using System.Data;
using System.Data.Odbc;
using System.Globalization;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Logging;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public class SybaseExportService : ISybaseExportService
{
    private const int BatchSize = 1000;
    private const int ConnectionTimeoutSec = 30;
    private const int CommandTimeoutSec = 300;
    private const int OpenAsyncTimeoutSec = 60;
    private const int MaxConsecutiveErrors = 1;

    private sealed record ColumnInfo(
        string Name,
        OdbcType OdbcType,
        string DbTypeName,
        byte Precision,
        byte Scale,
        Func<object?, object?> Convert);

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
                throw new ExportException($"Error al conectar a Sybase: {ex.Message}", config.TableName);
            }

            sw.Stop();
            log.WriteLine($"Connection opened successfully", sw.Elapsed);

            var columns = await LoadColumnTypesAsync(connection, config.TableName, file.Fields, log, cancellationToken);

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
            try { tx.Rollback(); } catch { }
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
                        "");
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
                var intPartStr = decimal.Truncate(Math.Abs(d)).ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                int intDigits = intPartStr.Length;
                int maxInt = info.Precision - info.Scale;
                if (intDigits > maxInt)
                    log.WriteLine($"  WARN registro {recordIndex} col '{info.Name}': {d} tiene {intDigits} enteros pero numeric({info.Precision},{info.Scale}) solo admite {maxInt}");
            }
        }
    }

    private static async Task ProbeConversionsViaSybaseAsync(
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

            var convertType = GetConvertTypeForProbe(col);
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

    private static string? GetConvertTypeForProbe(ColumnInfo col)
    {
        var lower = col.DbTypeName.ToLowerInvariant();
        return lower switch
        {
            "int" or "integer"        => "INT",
            "smallint"                => "SMALLINT",
            "tinyint"                 => "TINYINT",
            "bigint"                  => "BIGINT",
            "numeric" or "decimal"    => col.Precision > 0
                ? $"NUMERIC({col.Precision},{col.Scale})"
                : "NUMERIC(18,2)",
            "float"                   => "FLOAT",
            "real"                    => "REAL",
            "money" or "smallmoney"   => col.Precision > 0
                ? $"NUMERIC({col.Precision},{col.Scale})"
                : "NUMERIC(18,2)",
            "date"                    => "DATE",
            "datetime" or "smalldatetime" => "DATETIME",
            _                         => null
        };
    }

    private static string BuildConnectionString(SybaseConnectionConfig config)
    {
        return
            "DRIVER={Adaptive Server Enterprise};" +
            $"Server={config.Host};" +
            $"Port={config.Port};" +
            $"Database={config.Database};" +
            $"UID={config.Username};" +
            $"PWD={config.Password};" +
            $"Connection Timeout={ConnectionTimeoutSec};";
    }

    public async Task<bool> ProbeFirstRecordAsync(
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

        var connectionString = BuildConnectionString(config);

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
                return false;
            }
            await openTask;
        }
        catch (Exception ex)
        {
            log.WriteLine($"Probe connection failed: {ex.Message}");
            return false;
        }

        try
        {
            var columns = await LoadColumnTypesAsync(connection, config.TableName, file.Fields, log, cancellationToken);

            var first = file.Records.FirstOrDefault(r => !r.IsDeleted);
            if (first == null)
            {
                log.WriteLine("No records to probe");
                return true;
            }

            log.WriteLine("Probing conversions against Sybase with first record...");
            await ProbeConversionsViaSybaseAsync(connection, columns, first, log, cancellationToken);
            log.WriteLine("All Sybase conversions passed");
            return true;
        }
        catch (ExportException ex)
        {
            log.WriteLine($"Probe failed: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            log.WriteLine($"Probe error: {ex.Message}");
            return false;
        }
    }

    private static async Task<List<ColumnInfo>> LoadColumnTypesAsync(
        OdbcConnection connection,
        string tableName,
        IReadOnlyList<DbfField> fields,
        FileLogger log,
        CancellationToken cancellationToken)
    {
        log.WriteLine($"Loading column types from Sybase table '{tableName}'...");

        var sql = "SELECT c.name, t.name, c.prec, c.scale " +
                  "FROM syscolumns c, sysobjects o, systypes t " +
                  "WHERE c.id = o.id AND o.name = ? AND c.usertype = t.usertype " +
                  "ORDER BY c.colid";

        log.WriteLine($"SQL: {sql}");

        var dbCols = new List<(string name, string typeName, byte prec, byte scale)>();

        await using (var cmd = new OdbcCommand(sql, connection))
        {
            cmd.CommandTimeout = CommandTimeoutSec;
            cmd.Parameters.Add(new OdbcParameter("@table", OdbcType.NVarChar, 255) { Value = tableName });

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var name = reader.GetString(0);
                var typeName = reader.GetString(1);
                var prec = reader.IsDBNull(2) ? (byte)0 : reader.GetByte(2);
                var scale = reader.IsDBNull(3) ? (byte)0 : reader.GetByte(3);
                dbCols.Add((name, typeName, prec, scale));
            }
        }

        log.WriteLine($"Found {dbCols.Count} columns in Sybase table");

        if (dbCols.Count == 0)
            throw new ExportException(
                $"La tabla '{tableName}' no existe o no tiene columnas.", tableName);

        var dbfFieldMap = fields.ToDictionary(f => f.Name, f => f.Type, StringComparer.OrdinalIgnoreCase);

        var result = new List<ColumnInfo>();

        foreach (var (name, typeName, prec, scale) in dbCols)
        {
            if (!dbfFieldMap.ContainsKey(name))
                continue;

            var odbcType = MapSybaseTypeToOdbc(typeName);
            var convertFunc = BuildConvertFunction(typeName, name, prec, scale);
            result.Add(new ColumnInfo(name, odbcType, typeName, prec, scale, convertFunc));

            log.WriteLine($"  Col: {name,-30} SybaseType: {typeName,-12} OdbcType: {odbcType,-10} Prec: {prec} Scale: {scale}");
        }

        if (result.Count == 0)
            throw new ExportException(
                $"Ninguna columna del DBF coincide con las columnas de la tabla '{tableName}'. " +
                "Los nombres deben coincidir exactamente.", tableName);

        log.WriteLine($"Final column count: {result.Count} (matched from {dbCols.Count} DB columns)");
        return result;
    }

    private static OdbcType MapSybaseTypeToOdbc(string typeName)
    {
        return OdbcType.VarChar;
    }

    private static Func<object?, object?> BuildConvertFunction(string typeName, string columnName, byte precision = 0, byte scale = 0)
    {
        var lower = typeName.ToLowerInvariant();
        var ci = System.Globalization.CultureInfo.InvariantCulture;

        void ValidatePrecision(decimal value)
        {
            if (precision == 0) return;
            var intDigits = precision - scale;
            if (intDigits <= 0) return;
            if (value == 0) return;

            var absValue = Math.Abs(value);
            var digits = (int)Math.Floor(Math.Log10((double)absValue)) + 1;
            if (digits <= intDigits) return;

            throw new ExportException(
                $"Columna '{columnName}': el valor '{value}' excede " +
                $"{typeName.ToUpperInvariant()}({precision},{scale}). " +
                $"El maximo entero permitido tiene {intDigits} digito(s).", "");
        }

        return lower switch
        {
            "int" or "integer" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (int.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out _))
                            return s.TrimEnd();
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Int.", "");
                    }
                    return Convert.ToInt32(v, ci).ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Int.", ""); }
            },

            "smallint" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (short.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out _))
                            return s.TrimEnd();
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a SmallInt.", "");
                    }
                    return Convert.ToInt16(v, ci).ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a SmallInt.", ""); }
            },

            "tinyint" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (byte.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out _))
                            return s.TrimEnd();
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a TinyInt.", "");
                    }
                    return Convert.ToByte(v, ci).ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a TinyInt.", ""); }
            },

            "bigint" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (long.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out _))
                            return s.TrimEnd();
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a BigInt.", "");
                    }
                    return Convert.ToInt64(v, ci).ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a BigInt.", ""); }
            },

            "numeric" or "decimal" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (decimal.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out var dec))
                        {
                            ValidatePrecision(dec);
                            return s.TrimEnd();
                        }
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", "");
                    }
                    var decVal = Convert.ToDecimal(v, ci);
                    ValidatePrecision(decVal);
                    return decVal.ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", ""); }
            },

            "float" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out _))
                            return s.TrimEnd();
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Float.", "");
                    }
                    return Convert.ToDouble(v, ci).ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Float.", ""); }
            },

            "real" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (float.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out _))
                            return s.TrimEnd();
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Real.", "");
                    }
                    return Convert.ToSingle(v, ci).ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Real.", ""); }
            },

            "money" or "smallmoney" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (decimal.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, ci, out var dec))
                        {
                            ValidatePrecision(dec);
                            return s.TrimEnd();
                        }
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", "");
                    }
                    var decVal = Convert.ToDecimal(v, ci);
                    ValidatePrecision(decVal);
                    return decVal.ToString(ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", ""); }
            },

            "datetime" or "smalldatetime" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is DateTime dt)
                        return dt.ToString("yyyy-MM-dd HH:mm:ss", ci);
                    if (v is string s)
                    {
                        var dateStr = s.Trim();
                        DateTime dtp;
                        if (DateTime.TryParse(dateStr, CultureInfo.CurrentCulture,
                                DateTimeStyles.None, out dtp)
                            || DateTime.TryParse(dateStr, ci, DateTimeStyles.None, out dtp)
                            || DateTime.TryParseExact(dateStr,
                                ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss",
                                 "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss"],
                                ci, DateTimeStyles.None, out dtp))
                            return dtp.ToString("yyyy-MM-dd HH:mm:ss", ci);
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a DateTime.", "");
                    }
                    return Convert.ToDateTime(v, ci).ToString("yyyy-MM-dd HH:mm:ss", ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a DateTime.", ""); }
            },

            "date" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is DateTime dt)
                        return dt.ToString("yyyy-MM-dd", ci);
                    if (v is string s)
                    {
                        var dateStr = s.Trim();
                        DateTime dtp;
                        if (DateTime.TryParse(dateStr, CultureInfo.CurrentCulture,
                                DateTimeStyles.None, out dtp)
                            || DateTime.TryParse(dateStr, ci, DateTimeStyles.None, out dtp)
                            || DateTime.TryParseExact(dateStr,
                                ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss",
                                 "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss"],
                                ci, DateTimeStyles.None, out dtp))
                            return dtp.ToString("yyyy-MM-dd", ci);
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Date.", "");
                    }
                    return Convert.ToDateTime(v, ci).ToString("yyyy-MM-dd", ci);
                }
                catch (ExportException) { throw; }
                catch { throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Date.", ""); }
            },

            "bit" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is bool b) return b ? "1" : "0";
                    if (v is string s) return s.Trim().ToUpperInvariant() is "T" or "Y" or "1" or "TRUE" or "YES" ? "1" : "0";
                    return Convert.ToBoolean(v, ci) ? "1" : "0";
                }
                catch { return "0"; }
            },

            "char" or "varchar" or "nchar" or "nvarchar" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                if (v is string s) return s.TrimEnd();
                return (v.ToString() ?? "").TrimEnd();
            },

            _ => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                if (v is string s) return s.TrimEnd();
                return (v.ToString() ?? "").TrimEnd();
            }
        };
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
