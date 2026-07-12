namespace VisorDBF.Core.Models;

public enum ExtraColumnType
{
    DateTime,
    Integer
}

public sealed record ExtraColumnConfig
{
    public string ColumnName { get; init; } = string.Empty;
    public ExtraColumnType Type { get; init; }
    public string RawValue { get; init; } = string.Empty;
}
