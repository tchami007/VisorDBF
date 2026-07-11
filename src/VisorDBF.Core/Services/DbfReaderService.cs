using System.Globalization;
using System.Text;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;

namespace VisorDBF.Core.Services;

/// <summary>
/// Lee archivos DBF completos en memoria.
/// Implementa D-04: carga completa en memoria al abrir el archivo.
/// Implementa D-06: signature Task&lt;DbfFile&gt; ReadAsync(string, Encoding, CancellationToken).
/// </summary>
public sealed class DbfReaderService : IDbfReaderService
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
    /// Lee campos y registros directamente desde los bytes del archivo DBF.
    /// </summary>
    private static (List<DbfField> fields, List<Models.DbfRecord> records) ReadDbfData(
        string filePath,
        Encoding encoding,
        CancellationToken cancellationToken)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var header = new byte[32];
        fs.Read(header, 0, 32);

        int headerLen = BitConverter.ToInt16(header, 8);
        int recLen = BitConverter.ToInt16(header, 10);

        var fields = ReadFieldDefinitions(fs, encoding);
        int recordCount = BitConverter.ToInt32(header, 4);
        var records = new List<Models.DbfRecord>(recordCount);

        var recordBuffer = new byte[recLen];
        fs.Seek(headerLen, SeekOrigin.Begin);

        int rowIndex = 0;
        while (fs.Read(recordBuffer, 0, recLen) == recLen)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool isDeleted = recordBuffer[0] == 0x2A;
            var values = new Dictionary<string, object?>(fields.Count);

            int offset = 1;
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                values[field.Name] = ParseFieldValue(recordBuffer, offset, field, encoding, filePath);
                offset += field.Length;
            }

            records.Add(new Models.DbfRecord(rowIndex, isDeleted, values));
            rowIndex++;
        }

        return (fields, records);
    }

    /// <summary>
    /// Lee las definiciones de los campos desde el header del DBF.
    /// </summary>
    private static List<DbfField> ReadFieldDefinitions(FileStream fs, Encoding encoding)
    {
        var header = new byte[32];
        fs.Seek(0, SeekOrigin.Begin);
        fs.Read(header, 0, 32);

        int headerLen = BitConverter.ToInt16(header, 8);
        int numFields = (headerLen - 32 - 1) / 32;

        var fields = new List<DbfField>(numFields);

        for (int i = 0; i < numFields; i++)
        {
            byte[] fieldDef = new byte[32];
            fs.Seek(32 + i * 32, SeekOrigin.Begin);
            fs.Read(fieldDef, 0, 32);

            string rawName = encoding.GetString(fieldDef, 0, 11);
            string name = rawName.TrimEnd('\0');
            if (string.IsNullOrEmpty(name))
                continue;

            char rawType = (char)fieldDef[11];
            int length = fieldDef[16];
            int decimalCount = fieldDef[17];

            var type = rawType switch
            {
                'C' or 'c' => DbfFieldType.Character,
                'N' or 'n' => DbfFieldType.Numeric,
                'F' or 'f' => DbfFieldType.Float,
                'D' or 'd' => DbfFieldType.Date,
                'T' or 't' => DbfFieldType.DateTime,
                'L' or 'l' => DbfFieldType.Logical,
                'M' or 'm' => DbfFieldType.Memo,
                'I' or 'i' => DbfFieldType.Integer,
                'B' or 'b' => DbfFieldType.Float,
                _ => DbfFieldType.Unknown
            };

            fields.Add(new DbfField(name, type, length, decimalCount, i));
        }

        return fields;
    }

    /// <summary>
    /// Parsea el valor de un campo desde los bytes del registro.
    /// </summary>
    private static object? ParseFieldValue(
        byte[] recordBuffer, int offset, DbfField field, Encoding encoding, string filePath)
    {
        int length = field.Length;
        if (offset + length > recordBuffer.Length)
            return null;

        switch (field.Type)
        {
            case DbfFieldType.Numeric:
                return ParseNumericValue(recordBuffer, offset, length, field.DecimalCount);

            case DbfFieldType.Float:
                return ParseNumericValue(recordBuffer, offset, length, field.DecimalCount);

            case DbfFieldType.Integer:
                return ParseNumericValue(recordBuffer, offset, length, 0);

            case DbfFieldType.Character:
            {
                string raw = encoding.GetString(recordBuffer, offset, length);
                int nullIdx = raw.IndexOf('\0');
                if (nullIdx >= 0)
                    raw = raw[..nullIdx];
                return raw;
            }

            case DbfFieldType.Date:
                return ParseDateValue(recordBuffer, offset, length);

            case DbfFieldType.DateTime:
                return ParseDateValue(recordBuffer, offset, length);

            case DbfFieldType.Logical:
            {
                if (length < 1) return null;
                char c = (char)recordBuffer[offset];
                return c is 'Y' or 'y' or 'T' or 't';
            }

            case DbfFieldType.Memo:
            {
                string raw = encoding.GetString(recordBuffer, offset, length).TrimEnd('\0');
                return raw;
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// Parsea un valor numerico desde ASCII.
    /// Usa decimal/long segun corresponda para evitar perdida de precision.
    /// </summary>
    private static object? ParseNumericValue(byte[] buffer, int offset, int length, int decimalCount)
    {
        int end = offset + length;
        while (end > offset && buffer[end - 1] == ' ')
            end--;

        if (end <= offset)
            return null;

        // Construir string con los bytes significativos
        int start = offset;
        while (start < end && buffer[start] == ' ')
            start++;

        if (start >= end)
            return null;

        // Si tiene decimales, leer como decimal
        if (decimalCount > 0)
        {
            // Construir string incluyendo punto decimal si es necesario
            int rawLength = end - start;
            Span<char> chars = stackalloc char[rawLength];
            for (int i = 0; i < rawLength; i++)
                chars[i] = (char)buffer[start + i];

            string numStr = new(chars);

            if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decVal))
                return decVal;

            return null;
        }

        // Sin decimales: intentar como long primero, luego decimal
        int sigLength = end - start;
        Span<char> chars2 = stackalloc char[sigLength];
        for (int i = 0; i < sigLength; i++)
            chars2[i] = (char)buffer[start + i];

        string intStr = new(chars2);

        if (long.TryParse(intStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
            return longVal;

        if (decimal.TryParse(intStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decVal2))
            return decVal2;

        return null;
    }

    /// <summary>
    /// Parsea una fecha desde formato DBF (YYYYMMDD).
    /// </summary>
    private static object? ParseDateValue(byte[] buffer, int offset, int length)
    {
        if (length < 8) return null;
        if (offset + 8 > buffer.Length) return null;

        bool allSpaces = true;
        for (int i = 0; i < 8; i++)
        {
            if (buffer[offset + i] != ' ' && buffer[offset + i] != '0')
            {
                allSpaces = false;
                break;
            }
        }
        if (allSpaces)
            return null;

        Span<char> chars = stackalloc char[8];
        for (int i = 0; i < 8; i++)
            chars[i] = (char)buffer[offset + i];

        string dateStr = new(chars);

        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result;

        return null;
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


}
