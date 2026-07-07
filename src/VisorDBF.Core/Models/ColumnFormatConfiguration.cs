namespace VisorDBF.Core.Models;

public sealed record ColumnFormatConfiguration
{
    public IReadOnlyDictionary<string, string?> Formats { get; init; } = new Dictionary<string, string?>();
    public bool IsActive { get; init; } = false;
    public static ColumnFormatConfiguration Default { get; } = new();
}
