using System.Text;
namespace VisorDBF.Core.Models;

public sealed record ExportConfiguration
{
    public static readonly Encoding UTF8NoBOM = new UTF8Encoding(false);

    public string ColumnSeparator { get; init; } = ";";
    public string RowEndDelimiter { get; init; } = string.Empty;
    public bool IncludeHeader { get; init; } = true;
    public RowLimitMode RowLimitMode { get; init; } = RowLimitMode.All;
    public int MaxRows { get; init; } = 0;
    public Encoding OutputEncoding { get; init; } = UTF8NoBOM;
    public ColumnFormatConfiguration ColumnFormats { get; init; } = ColumnFormatConfiguration.Default;

    /// <summary>
    /// Separador decimal para valores numericos: "." (punto) o "," (coma).
    /// </summary>
    public string DecimalSeparator { get; init; } = ",";

    public static ExportConfiguration Default { get; } = new();
}
