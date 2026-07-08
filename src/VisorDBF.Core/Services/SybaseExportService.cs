using System.Data;
using AdoNetCore.AseClient;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public class SybaseExportService : ISybaseExportService
{
    private const int BatchSize = 1000;

    public async Task TransferAsync(
        DbfFile file,
        SybaseConnectionConfig config,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        var connectionString = $"Data Source={config.Host}:{config.Port};Database={config.Database};User Id={config.Username};Password={config.Password};";

        await using var connection = new AseConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var columnNames = ValidateTable(connection, config.TableName, file.Fields);

        var insertSql = BuildInsertSql(config.TableName, columnNames);

        await using var command = new AseCommand(insertSql, connection);
        command.CommandType = CommandType.Text;

        var parameters = new AseParameter[columnNames.Count];
        for (int i = 0; i < columnNames.Count; i++)
        {
            parameters[i] = new AseParameter($"@p{i}", GetAseDbType(columnNames[i].Type));
            command.Parameters.Add(parameters[i]);
        }

        command.Prepare();

        var records = file.Records;
        int total = records.Count;
        int processed = 0;

        try
        {
            for (int batchStart = 0; batchStart < total; batchStart += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int batchEnd = Math.Min(batchStart + BatchSize, total);

                using var tx = connection.BeginTransaction();
                command.Transaction = tx;

                try
                {
                    for (int i = batchStart; i < batchEnd; i++)
                    {
                        var record = records[i];
                        if (record.IsDeleted)
                            continue;

                        for (int p = 0; p < columnNames.Count; p++)
                        {
                            var rawValue = record.Values.GetValueOrDefault(columnNames[p].Name);

                            if (rawValue == null || rawValue is DBNull)
                            {
                                parameters[p].Value = DBNull.Value;
                            }
                            else
                            {
                                parameters[p].Value = ConvertValue(rawValue, columnNames[p].Type);
                            }
                        }

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                processed = batchEnd;
                progress.Report(processed);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
                connection.Close();
        }
    }

    private static List<(string Name, DbfFieldType Type)> ValidateTable(
        AseConnection connection,
        string tableName,
        IReadOnlyList<DbfField> fields)
    {
        using var schemaTable = connection.GetSchema("Columns", new[] { null, null, tableName, null });

        if (schemaTable.Rows.Count == 0)
            throw new ExportException(
                $"La tabla '{tableName}' no existe o no tiene columnas en la base de datos '{connection.Database}'.",
                tableName);

        var dbColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < schemaTable.Rows.Count; i++)
        {
            var colName = schemaTable.Rows[i]["COLUMN_NAME"] as string;
            if (colName != null)
                dbColumns.Add(colName);
        }

        var result = new List<(string Name, DbfFieldType Type)>();

        foreach (var field in fields)
        {
            if (!dbColumns.Contains(field.Name))
                throw new ExportException(
                    $"La columna '{field.Name}' del DBF no existe en la tabla destino '{tableName}'. " +
                    $"El mapeo se hace por nombre de columna y deben coincidir exactamente.",
                    tableName);
            result.Add((field.Name, field.Type));
        }

        return result;
    }

    private static string BuildInsertSql(string tableName, List<(string Name, DbfFieldType Type)> columns)
    {
        var colNames = string.Join(", ", columns.Select(c => $"[{c.Name}]"));
        var paramNames = string.Join(", ", columns.Select((_, i) => $"@p{i}"));
        return $"INSERT INTO [{tableName}] ({colNames}) VALUES ({paramNames})";
    }

    private static AseDbType GetAseDbType(DbfFieldType type) => type switch
    {
        DbfFieldType.Character => AseDbType.VarChar,
        DbfFieldType.Numeric => AseDbType.Decimal,
        DbfFieldType.Float => AseDbType.Double,
        DbfFieldType.Date => AseDbType.DateTime,
        DbfFieldType.DateTime => AseDbType.DateTime,
        DbfFieldType.Logical => AseDbType.TinyInt,
        DbfFieldType.Integer => AseDbType.Integer,
        DbfFieldType.Memo => AseDbType.Text,
        _ => AseDbType.VarChar
    };

    private static object ConvertValue(object value, DbfFieldType type)
    {
        if (value is bool b && type == DbfFieldType.Logical)
            return b ? (byte)1 : (byte)0;

        if (value is DateTime dt)
        {
            if (type == DbfFieldType.Date)
                return dt.Date;
            return dt;
        }

        return value;
    }
}