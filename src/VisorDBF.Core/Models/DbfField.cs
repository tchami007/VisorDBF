namespace VisorDBF.Core.Models;

/// <summary>
/// Definicion inmutable de una columna en un archivo DBF.
/// Implementada como record para igualdad estructural y inmutabilidad.
/// </summary>
public sealed record DbfField(
    string Name,
    DbfFieldType Type,
    int Length,
    int DecimalCount,
    int OrdinalPosition
)
{
    /// <summary>
    /// Nombre del campo tal como aparece en el encabezado DBF (puede tener espacios o caracteres especiales).
    /// </summary>
    public string Name { get; init; } = Name?.Trim() ?? throw new ArgumentNullException(nameof(Name));

    /// <summary>
    /// Nombre seguro para usar como binding key — reemplaza caracteres no validos.
    /// Nota: DbfRecord.Values usa Name original como key; este campo es para display.
    /// </summary>
    public string DisplayName => Name;
}
