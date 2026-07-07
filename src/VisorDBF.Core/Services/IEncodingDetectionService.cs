using System.Text;
namespace VisorDBF.Core.Services;

public interface IEncodingDetectionService
{
    /// <summary>
    /// Lee el Language Driver ID del header del archivo DBF (byte 0x1D).
    /// </summary>
    byte ReadLanguageDriverId(string filePath);

    /// <summary>
    /// Mapea el Language Driver ID a un Encoding conocido.
    /// Retorna null si el ID no es reconocido (0x00 o ID desconocido).
    /// </summary>
    Encoding? DetectEncoding(byte languageDriverId);

    /// <summary>
    /// Lee el Language Driver ID y retorna el Encoding detectado, o null si no reconocido.
    /// </summary>
    Encoding? DetectFromFile(string filePath);
}
