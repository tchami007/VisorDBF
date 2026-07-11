using System.Collections.Frozen;
using System.Text;
using VisorDBF.Core.Exceptions;

namespace VisorDBF.Core.Services;

/// <summary>
/// Detecta la codificacion de un archivo DBF leyendo el Language Driver ID
/// del header (byte en offset 0x1D) y mapeandolo a un System.Text.Encoding conocido.
/// Referencia: docs/TECH.md §7.3
/// </summary>
public sealed class EncodingDetectionService : IEncodingDetectionService
{
    // Tabla completa de Language Driver IDs → encoding name
    // Fuente: docs/TECH.md §7.3 + 01-RESEARCH.md (tabla extendida)
    private static readonly FrozenDictionary<byte, string> LanguageDriverMap =
        new Dictionary<byte, string>
    {
        { 0x01, "IBM437" },
        { 0x02, "IBM850" },
        { 0x03, "windows-1252" },
        { 0x08, "IBM865" },
        { 0x09, "IBM437" },
        { 0x0A, "IBM850" },
        { 0x0B, "IBM437" },
        { 0x0D, "IBM437" },
        { 0x0E, "IBM850" },
        { 0x0F, "IBM437" },
        { 0x10, "IBM850" },
        { 0x11, "IBM437" },
        { 0x13, "IBM850" },
        { 0x1A, "IBM932" },       // Japanese
        { 0x1B, "IBM850" },
        { 0x1C, "IBM437" },
        { 0x1D, "IBM437" },
        { 0x1F, "IBM850" },
        { 0x22, "IBM850" },
        { 0x23, "IBM437" },
        { 0x24, "IBM860" },
        { 0x25, "IBM850" },
        { 0x26, "IBM866" },
        { 0x37, "IBM850" },
        { 0x40, "IBM852" },
        { 0x4D, "IBM936" },       // Chinese simplified
        { 0x4E, "IBM949" },       // Korean
        { 0x4F, "IBM950" },       // Chinese traditional
        { 0x50, "IBM874" },       // Thai
        { 0x57, "windows-1252" },
        { 0x58, "windows-1252" },
        { 0x59, "windows-1252" },
        { 0x64, "IBM852" },
        { 0x65, "IBM866" },
        { 0x66, "IBM865" },
        { 0x67, "IBM861" },
        { 0x6A, "IBM737" },
        { 0x6B, "IBM857" },
        { 0x78, "IBM950" },
        { 0x79, "IBM949" },
        { 0x7A, "IBM936" },
        { 0x7B, "IBM932" },
        { 0x7C, "IBM874" },
        { 0x86, "IBM737" },
        { 0x87, "IBM852" },
        { 0x88, "IBM857" },
        { 0xC8, "windows-1250" },
        { 0xC9, "windows-1251" },
        { 0xCA, "windows-1254" },
        { 0xCB, "windows-1253" },
        { 0xCC, "windows-1257" },
    }.ToFrozenDictionary();

    /// <inheritdoc/>
    public byte ReadLanguageDriverId(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(0x1D, SeekOrigin.Begin);
            int b = fs.ReadByte();
            return b < 0 ? (byte)0x00 : (byte)b;
        }
        catch (FileNotFoundException ex)
        {
            throw new DbfReadException($"Archivo DBF no encontrado: {filePath}", filePath, ex);
        }
        catch (IOException ex)
        {
            throw new DbfReadException($"Error al leer el archivo DBF: {filePath}", filePath, ex);
        }
    }

    /// <inheritdoc/>
    public Encoding? DetectEncoding(byte languageDriverId)
    {
        if (languageDriverId == 0x00)
            return null;

        if (!LanguageDriverMap.TryGetValue(languageDriverId, out var encodingName))
            return null;

        return Encoding.GetEncoding(encodingName);
    }

    /// <inheritdoc/>
    public Encoding? DetectFromFile(string filePath)
    {
        var id = ReadLanguageDriverId(filePath);
        return DetectEncoding(id);
    }
}
