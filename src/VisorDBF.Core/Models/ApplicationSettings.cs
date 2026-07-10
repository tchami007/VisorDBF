namespace VisorDBF.Core.Models;

public sealed record ApplicationSettings
{
    public ExportConfiguration DefaultExportConfig { get; init; } = ExportConfiguration.Default;
    public ColumnFormatConfiguration DefaultColumnFormats { get; init; } = ColumnFormatConfiguration.Default;
    public List<ExportProfile> Profiles { get; init; } = new();
    public List<RecentFileEntry> RecentFiles { get; init; } = new();
    public WindowSettings WindowState { get; init; } = WindowSettings.Default;
    public string? LastProfileName { get; init; }

    public static ApplicationSettings Default { get; } = new();
}
