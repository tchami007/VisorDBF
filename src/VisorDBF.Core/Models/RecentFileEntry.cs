namespace VisorDBF.Core.Models;

public sealed record RecentFileEntry
{
    public string FilePath { get; init; } = string.Empty;
    public DateTime LastOpened { get; init; }
    public string? DisplayName { get; init; }
}
