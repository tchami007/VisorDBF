using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public interface IColumnFormatService
{
    string? ApplyFormat(string fieldName, object? value, string? formatString);
    bool IsFormatValid(string? formatString, DbfFieldType fieldType);
    string? GetFormatOrDefault(string fieldName, IReadOnlyDictionary<string, string?> formats, string? defaultFormat = null);
}
