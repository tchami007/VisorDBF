using System.Globalization;
using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public sealed class ColumnFormatService : IColumnFormatService
{
    public string? ApplyFormat(string fieldName, object? value, string? formatString)
    {
        if (value is null)
            return null;

        if (string.IsNullOrEmpty(formatString))
            return value.ToString();

        if (value is IFormattable formattable)
        {
            try
            {
                return formattable.ToString(formatString, CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                return "ERROR";
            }
        }

        return value.ToString();
    }

    public bool IsFormatValid(string? formatString, DbfFieldType fieldType)
    {
        if (string.IsNullOrEmpty(formatString))
            return true;

        object? sample = fieldType switch
        {
            DbfFieldType.Numeric => 123.45m,
            DbfFieldType.Float => 123.45,
            DbfFieldType.Date => new DateOnly(2024, 1, 15),
            DbfFieldType.DateTime => new DateTime(2024, 1, 15, 10, 30, 0),
            _ => "test"
        };

        if (sample is IFormattable formattable)
        {
            try
            {
                formattable.ToString(formatString, CultureInfo.CurrentCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return true;
    }

    public string? GetFormatOrDefault(string fieldName, IReadOnlyDictionary<string, string?> formats, string? defaultFormat = null)
    {
        if (formats.TryGetValue(fieldName, out var format))
            return format;

        return defaultFormat;
    }
}
