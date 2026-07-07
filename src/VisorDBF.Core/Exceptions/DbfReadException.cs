namespace VisorDBF.Core.Exceptions;

public class DbfReadException : Exception
{
    public string FilePath { get; }

    public DbfReadException(string message, string filePath, Exception? inner = null)
        : base(message, inner) => FilePath = filePath;
}
