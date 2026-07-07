using System.Text;
namespace VisorDBF.Core.Models;

public sealed record ExportConfiguration
{
    public string ColumnSeparator { get; init; } = ";";
    public string RowEndDelimiter { get; init; } = string.Empty;
    public bool IncludeHeader { get; init; } = true;
    public RowLimitMode RowLimitMode { get; init; } = RowLimitMode.All;
    public int MaxRows { get; init; } = 0;
    public Encoding OutputEncoding { get; init; } = Encoding.UTF8;

    public static ExportConfiguration Default { get; } = new();
}
