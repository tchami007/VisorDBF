namespace VisorDBF.Core.Models;

public sealed record WindowSettings
{
    public double Left { get; init; } = double.NaN;
    public double Top { get; init; } = double.NaN;
    public double Width { get; init; } = double.NaN;
    public double Height { get; init; } = double.NaN;
    public bool IsMaximized { get; init; }

    public static WindowSettings Default { get; } = new();
}
