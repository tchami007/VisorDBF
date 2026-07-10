using System.Globalization;
using VisorDBF.Core.Exceptions;

namespace VisorDBF.Core.Services;

public static class SybaseValueConverter
{
    public static Func<object?, object?> BuildConvertFunction(string typeName, string columnName, byte precision = 0, byte scale = 0)
    {
        return typeName.ToLowerInvariant() switch
        {
            "int" or "integer" => ConvertToInt(columnName),
            "smallint" => ConvertToSmallInt(columnName),
            "tinyint" => ConvertToTinyInt(columnName),
            "bigint" => ConvertToBigInt(columnName),
            "numeric" or "decimal" or "money" or "smallmoney" => ConvertToNumeric(columnName, precision, scale),
            "float" => ConvertToFloat(columnName),
            "real" => ConvertToReal(columnName),
            "datetime" or "smalldatetime" => ConvertToDateTime(columnName),
            "date" => ConvertToDate(columnName),
            "bit" => ConvertToBit(columnName),
            _ => ConvertToStringValue
        };
    }

    public static Func<object?, object?> ConvertToInt(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return BuildConverter<int>(columnName,
            s => int.Parse(s, NumberStyles.Any, ci),
            v => Convert.ToInt32(v, ci),
            "Int");
    }

    public static Func<object?, object?> ConvertToSmallInt(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return BuildConverter<short>(columnName,
            s => short.Parse(s, NumberStyles.Any, ci),
            v => Convert.ToInt16(v, ci),
            "SmallInt");
    }

    public static Func<object?, object?> ConvertToTinyInt(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return BuildConverter<byte>(columnName,
            s => byte.Parse(s, NumberStyles.Any, ci),
            v => Convert.ToByte(v, ci),
            "TinyInt");
    }

    public static Func<object?, object?> ConvertToBigInt(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return BuildConverter<long>(columnName,
            s => long.Parse(s, NumberStyles.Any, ci),
            v => Convert.ToInt64(v, ci),
            "BigInt");
    }

    public static Func<object?, object?> ConvertToNumeric(string columnName, byte precision, byte scale)
    {
        var ci = CultureInfo.InvariantCulture;
        var typeLabel = "NUMERIC";
        return v =>
        {
            if (v == null || v is DBNull) return DBNull.Value;
            try
            {
                if (v is string s)
                {
                    if (decimal.TryParse(s.Trim(), NumberStyles.Any, ci, out var dec))
                    {
                        ValidatePrecision(dec, columnName, precision, scale, typeLabel);
                        return s.TrimEnd();
                    }
                    throw new ExportException(
                        $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", "");
                }
                var decVal = Convert.ToDecimal(v, ci);
                ValidatePrecision(decVal, columnName, precision, scale, typeLabel);
                return decVal.ToString(ci);
            }
            catch (ExportException) { throw; }
            catch
            {
                throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", "");
            }
        };
    }

    public static Func<object?, object?> ConvertToFloat(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return BuildConverter<double>(columnName,
            s => double.Parse(s, NumberStyles.Any, ci),
            v => Convert.ToDouble(v, ci),
            "Float");
    }

    public static Func<object?, object?> ConvertToReal(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return BuildConverter<float>(columnName,
            s => float.Parse(s, NumberStyles.Any, ci),
            v => Convert.ToSingle(v, ci),
            "Real");
    }

    public static Func<object?, object?> ConvertToDateTime(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return v =>
        {
            if (v == null || v is DBNull) return DBNull.Value;
            try
            {
                if (v is DateTime dt)
                    return dt.ToString("yyyy-MM-dd HH:mm:ss", ci);
                if (v is string s)
                {
                    var dateStr = s.Trim();
                    if (DateTime.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dtp)
                        || DateTime.TryParse(dateStr, ci, DateTimeStyles.None, out dtp)
                        || DateTime.TryParseExact(dateStr,
                            ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss"],
                            ci, DateTimeStyles.None, out dtp))
                        return dtp.ToString("yyyy-MM-dd HH:mm:ss", ci);
                    throw new ExportException(
                        $"Columna '{columnName}': el valor '{v}' no se puede convertir a DateTime.", "");
                }
                return Convert.ToDateTime(v, ci).ToString("yyyy-MM-dd HH:mm:ss", ci);
            }
            catch (ExportException) { throw; }
            catch
            {
                throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a DateTime.", "");
            }
        };
    }

    public static Func<object?, object?> ConvertToDate(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return v =>
        {
            if (v == null || v is DBNull) return DBNull.Value;
            try
            {
                if (v is DateTime dt)
                    return dt.ToString("yyyy-MM-dd", ci);
                if (v is string s)
                {
                    var dateStr = s.Trim();
                    if (DateTime.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dtp)
                        || DateTime.TryParse(dateStr, ci, DateTimeStyles.None, out dtp)
                        || DateTime.TryParseExact(dateStr,
                            ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss"],
                            ci, DateTimeStyles.None, out dtp))
                        return dtp.ToString("yyyy-MM-dd", ci);
                    throw new ExportException(
                        $"Columna '{columnName}': el valor '{v}' no se puede convertir a Date.", "");
                }
                return Convert.ToDateTime(v, ci).ToString("yyyy-MM-dd", ci);
            }
            catch (ExportException) { throw; }
            catch
            {
                throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a Date.", "");
            }
        };
    }

    public static Func<object?, object?> ConvertToBit(string columnName)
    {
        var ci = CultureInfo.InvariantCulture;
        return v =>
        {
            if (v == null || v is DBNull) return DBNull.Value;
            try
            {
                if (v is bool b) return b ? "1" : "0";
                if (v is string s) return s.Trim().ToUpperInvariant() is "T" or "Y" or "1" or "TRUE" or "YES" ? "1" : "0";
                return Convert.ToBoolean(v, ci) ? "1" : "0";
            }
            catch { return "0"; }
        };
    }

    public static object? ConvertToStringValue(object? value)
    {
        if (value == null || value is DBNull) return DBNull.Value;
        if (value is string s) return s.TrimEnd();
        return (value.ToString() ?? "").TrimEnd();
    }

    private static Func<object?, object?> BuildConverter<T>(
        string columnName,
        Func<string, T> parseValue,
        Func<object, T> convertValue,
        string typeLabel) where T : IConvertible
    {
        var ci = CultureInfo.InvariantCulture;
        return v =>
        {
            if (v == null || v is DBNull) return DBNull.Value;
            try
            {
                if (v is string s)
                {
                    parseValue(s.Trim());
                    return s.TrimEnd();
                }
                return ((IConvertible)convertValue(v)).ToString(ci);
            }
            catch (ExportException) { throw; }
            catch
            {
                throw new ExportException(
                    $"Columna '{columnName}': el valor '{v}' no se puede convertir a {typeLabel}.", "");
            }
        };
    }

    private static void ValidatePrecision(decimal value, string columnName, byte precision, byte scale, string typeLabel)
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
            $"{typeLabel}({precision},{scale}). " +
            $"El maximo entero permitido tiene {intDigits} digito(s).", "");
    }

    public static string? GetConvertTypeForProbe(string dbTypeName, byte precision, byte scale)
    {
        var lower = dbTypeName.ToLowerInvariant();
        return lower switch
        {
            "int" or "integer"        => "INT",
            "smallint"                => "SMALLINT",
            "tinyint"                 => "TINYINT",
            "bigint"                  => "BIGINT",
            "numeric" or "decimal"    => precision > 0
                ? $"NUMERIC({precision},{scale})"
                : "NUMERIC(18,2)",
            "float"                   => "FLOAT",
            "real"                    => "REAL",
            "money" or "smallmoney"   => precision > 0
                ? $"NUMERIC({precision},{scale})"
                : "NUMERIC(18,2)",
            "date"                    => "DATE",
            "datetime" or "smalldatetime" => "DATETIME",
            _                         => null
        };
    }
}
