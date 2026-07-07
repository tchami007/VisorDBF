namespace VisorDBF.Core.Exceptions;

public class UnknownEncodingException : Exception
{
    public byte LanguageDriverId { get; }
    public string FilePath { get; }

    public UnknownEncodingException(byte languageDriverId, string filePath)
        : base($"Language Driver ID 0x{languageDriverId:X2} no reconocido en '{filePath}'.")
    {
        LanguageDriverId = languageDriverId;
        FilePath = filePath;
    }
}
