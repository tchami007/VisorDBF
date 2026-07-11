namespace VisorDBF.Core.Models;

/// <summary>
/// Representa una fila de datos de un archivo DBF.
/// Implementada como class (mutable durante la carga desde DbfReaderService).
/// Values usa el nombre del campo como key (case-sensitive, tal como viene del DBF).
/// </summary>
public sealed class DbfRecord
{
    public Dictionary<string, object?> Values { get; init; } = new();
    public bool IsDeleted { get; init; }
    public int RowIndex { get; init; }

    public DbfRecord() { }

    public DbfRecord(int rowIndex, bool isDeleted, Dictionary<string, object?> values)
    {
        RowIndex = rowIndex;
        IsDeleted = isDeleted;
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }
}
