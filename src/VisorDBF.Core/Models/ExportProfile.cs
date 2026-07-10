namespace VisorDBF.Core.Models;

public sealed record ExportProfile
{
    public string Name { get; init; } = string.Empty;
    public ExportConfiguration Config { get; init; } = ExportConfiguration.Default;
    public ColumnFormatConfiguration ColumnFormats { get; init; } = ColumnFormatConfiguration.Default;
}
