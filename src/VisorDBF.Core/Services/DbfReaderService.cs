using System.Text;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

/// <summary>
/// Lee archivos DBF completos en memoria usando DbfDataReader.
/// Implementa D-04: carga completa en memoria al abrir el archivo.
/// Implementa D-06: signature Task&lt;DbfFile&gt; ReadAsync(string, Encoding, CancellationToken).
/// </summary>
public class DbfReaderService : IDbfReaderService
{
    /// <inheritdoc/>
    public async Task<DbfFile> ReadAsync(
        string filePath,
        Encoding encoding,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new DbfReadException("La ruta del archivo no puede ser nula o vacia.", filePath ?? string.Empty);

        if (!File.Exists(filePath))
            throw new DbfReadException($"El archivo DBF no existe: {filePath}", filePath);

        try
        {
            // Leer header manualmente para LanguageDriverId y LastModifiedDate
            // (DbfDataReader no expone estos via API publica)
            var (languageDriverId, lastModifiedDate) = ReadHeaderInfo(filePath);

            // Construir campos y registros en background thread para no bloquear UI
            var (fields, records) = await Task.Run(
                () => ReadDbfData(filePath, encoding, cancellationToken),
                cancellationToken);

            return new DbfFile
            {
                FilePath = filePath,
                RecordCount = records.Count,
                SourceEncoding = encoding,
                LanguageDriverId = languageDriverId,
                LastModifiedDate = lastModifiedDate,
                Fields = fields,
                Records = records
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbfReadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DbfReadException($"Error al leer el archivo DBF: {ex.Message}", filePath, ex);
        }
    }

    /// <summary>
    /// Lee los campos y registros del DBF usando DbfDataReader.
    /// Ejecutado en un Task.Run para no bloquear el hilo de UI.
    /// </summary>
    private static (List<DbfField> fields, List<Models.DbfRecord> records) ReadDbfData(
        string filePath,
        Encoding encoding,
        CancellationToken cancellationToken)
    {
        using var dbfReader = new DbfDataReader.DbfDataReader(filePath, encoding);

        // Construir DbfField desde las columnas del DbfTable
        var columns = dbfReader.DbfTable.Columns;
        var fields = columns.Select(col => new DbfField(
            Name: col.Name,
            Type: MapColumnType(col.ColumnType),
            Length: col.Length,
            DecimalCount: col.DecimalCount,
            OrdinalPosition: col.Index
        )).ToList();

        // Iterar registros
        var records = new List<Models.DbfRecord>();
        int rowIndex = 0;

        while (dbfReader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool isDeleted = dbfReader.DbfRecord.IsDeleted;
            var values = new Dictionary<string, object?>(fields.Count);

            for (int i = 0; i < fields.Count; i++)
            {
                object? value;
                if (dbfReader.IsDBNull(i))
                {
                    value = null;
                }
                else if (fields[i].Type == DbfFieldType.Memo)
                {
                    // MEMO: capturar excepcion si el archivo .FPT/.DBT no existe
                    try
                    {
                        value = dbfReader.GetValue(i);
                    }
                    catch (Exception)
                    {
                        value = string.Empty;
                    }
                }
                else
                {
                    value = dbfReader.GetValue(i);
                }

                values[fields[i].Name] = value;
            }

            records.Add(new Models.DbfRecord(rowIndex, isDeleted, values));
            rowIndex++;
        }

        return (fields, records);
    }

    /// <summary>
    /// Lee el Language Driver ID (byte 0x1D) y la fecha de ultima modificacion
    /// (bytes 1-3) directamente del header del archivo DBF.
    /// </summary>
    private static (byte languageDriverId, DateTime lastModifiedDate) ReadHeaderInfo(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var header = new byte[32];
        int bytesRead = fs.Read(header, 0, 32);
        if (bytesRead < 32)
            return (0x00, DateTime.MinValue);

        // Bytes 1-3: last modified date (year+1900, month, day)
        DateTime lastModified;
        try
        {
            int year = 1900 + header[1];
            int month = header[2];
            int day = header[3];
            if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                lastModified = new DateTime(year, month, day);
            else
                lastModified = DateTime.MinValue;
        }
        catch
        {
            lastModified = DateTime.MinValue;
        }

        // Byte 29 (0x1D): Language Driver ID
        byte languageDriverId = header[29];

        return (languageDriverId, lastModified);
    }

    /// <summary>
    /// Mapea DbfColumnType al enum DbfFieldType del dominio.
    /// </summary>
    private static DbfFieldType MapColumnType(DbfDataReader.DbfColumnType columnType) => columnType switch
    {
        DbfDataReader.DbfColumnType.Character  => DbfFieldType.Character,
        DbfDataReader.DbfColumnType.Number     => DbfFieldType.Numeric,
        DbfDataReader.DbfColumnType.Float      => DbfFieldType.Float,
        DbfDataReader.DbfColumnType.Date       => DbfFieldType.Date,
        DbfDataReader.DbfColumnType.DateTime   => DbfFieldType.DateTime,
        DbfDataReader.DbfColumnType.Boolean    => DbfFieldType.Logical,
        DbfDataReader.DbfColumnType.Memo       => DbfFieldType.Memo,
        DbfDataReader.DbfColumnType.Signedlong => DbfFieldType.Integer,
        DbfDataReader.DbfColumnType.Double     => DbfFieldType.Float,
        _                                      => DbfFieldType.Unknown
    };
}
