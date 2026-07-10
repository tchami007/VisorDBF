using System.Globalization;
using VisorDBF.Core.Exceptions;

namespace VisorDBF.Core.Services;

public static class SybaseValueConverter
{
    public static Func<object?, object?> BuildConvertFunction(string typeName, string columnName, byte precision = 0, byte scale = 0)
    {
        var lower = typeName.ToLowerInvariant();
        var ci = CultureInfo.InvariantCulture;

        return lower switch
        {
            "int" or "integer" => v =>
            {
                if (v == null || v is DBNull) return DBNull.Value;
                try
                {
                    if (v is string s)
                    {
                        if (int.TryParse(s.Trim(), NumberStyles.Any, ci, out _))
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
                        if (short.TryParse(s.Trim(), NumberStyles.Any, ci, out _))
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
                        if (byte.TryParse(s.Trim(), NumberStyles.Any, ci, out _))
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
                        if (long.TryParse(s.Trim(), NumberStyles.Any, ci, out _))
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
                        if (decimal.TryParse(s.Trim(), NumberStyles.Any, ci, out var dec))
                        {
                            ValidatePrecision(dec, columnName, precision, scale, typeName);
                            return s.TrimEnd();
                        }
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", "");
                    }
                    var decVal = Convert.ToDecimal(v, ci);
                    ValidatePrecision(decVal, columnName, precision, scale, typeName);
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
                        if (double.TryParse(s.Trim(), NumberStyles.Any, ci, out _))
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
                        if (float.TryParse(s.Trim(), NumberStyles.Any, ci, out _))
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
                        if (decimal.TryParse(s.Trim(), NumberStyles.Any, ci, out var dec))
                        {
                            ValidatePrecision(dec, columnName, precision, scale, typeName);
                            return s.TrimEnd();
                        }
                        throw new ExportException(
                            $"Columna '{columnName}': el valor '{v}' no se puede convertir a Decimal.", "");
                    }
                    var decVal = Convert.ToDecimal(v, ci);
                    ValidatePrecision(decVal, columnName, precision, scale, typeName);
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

    private static void ValidatePrecision(decimal value, string columnName, byte precision, byte scale, string typeName)
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
