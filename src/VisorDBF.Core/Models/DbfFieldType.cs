namespace VisorDBF.Core.Models;

/// <summary>
/// Tipos de campo del formato DBF segun la especificacion dBASE / FoxPro.
/// Referencia: docs/TECH.md §7.2
/// </summary>
public enum DbfFieldType
{
    Character,    // C — string de longitud fija
    Numeric,      // N — numero almacenado como texto
    Float,        // F — punto flotante
    Date,         // D — fecha YYYYMMDD
    DateTime,     // T — fecha y hora (FoxPro)
    Logical,      // L — booleano T/F/Y/N
    Memo,         // M — puntero a bloque memo
    Integer,      // I — entero binario (FoxPro)
    Unknown       // fallback para tipos no reconocidos
}

/// <summary>
/// Extension methods para DbfFieldType.
/// </summary>
public static class DbfFieldTypeExtensions
{
    /// <summary>
    /// Retorna el codigo de una letra para mostrar en el header de la grilla.
    /// </summary>
    public static string ToDisplayString(this DbfFieldType type) => type switch
    {
        DbfFieldType.Character => "C",
        DbfFieldType.Numeric   => "N",
        DbfFieldType.Float     => "F",
        DbfFieldType.Date      => "D",
        DbfFieldType.DateTime  => "DT",
        DbfFieldType.Logical   => "L",
        DbfFieldType.Memo      => "M",
        DbfFieldType.Integer   => "I",
        _                      => "?"
    };

    /// <summary>
    /// Retorna true para tipos que tienen formato configurable (Phase 3).
    /// </summary>
    public static bool HasConfigurableFormat(this DbfFieldType type) => type switch
    {
        DbfFieldType.Numeric  => true,
        DbfFieldType.Float    => true,
        DbfFieldType.Date     => true,
        DbfFieldType.DateTime => true,
        _                     => false
    };
}
