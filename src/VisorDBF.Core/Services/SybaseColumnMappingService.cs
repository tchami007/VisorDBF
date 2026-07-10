using System.Data.Odbc;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Logging;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

public sealed record ColumnInfo(
    string Name,
    OdbcType OdbcType,
    string DbTypeName,
    byte Precision,
    byte Scale,
    Func<object?, object?> Convert);

public sealed class SybaseColumnMappingService
{
    private const int CommandTimeoutSec = 300;

    public async Task<List<ColumnInfo>> LoadColumnTypesAsync(
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

            var dbfField = fields.First(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            var odbcType = MapSybaseTypeToOdbc(typeName);
            var convertFunc = SybaseValueConverter.BuildConvertFunction(typeName, dbfField.Name, prec, scale);
            result.Add(new ColumnInfo(dbfField.Name, odbcType, typeName, prec, scale, convertFunc));

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
}
