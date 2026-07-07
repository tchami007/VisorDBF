using System.Text;

namespace VisorDBF.Core.Models;

/// <summary>
/// Representa un archivo DBF cargado en memoria.
/// Es el contrato central entre los servicios de Core y la UI.
/// </summary>
public class DbfFile
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public int RecordCount { get; init; }
    public Encoding SourceEncoding { get; init; } = Encoding.Default;
    public byte LanguageDriverId { get; init; }
    public DateTime LastModifiedDate { get; init; }
    public IReadOnlyList<DbfField> Fields { get; init; } = Array.Empty<DbfField>();
    public IReadOnlyList<DbfRecord> Records { get; init; } = Array.Empty<DbfRecord>();

    /// <summary>
    /// Verdadero si el archivo contiene al menos un registro con deleted flag activo.
    /// </summary>
    public bool HasDeletedRecords => Records.Any(r => r.IsDeleted);
}
