namespace VisorDBF.Core.Exceptions;

public sealed class DbfReadException : Exception
{
    public string FilePath { get; }

    public DbfReadException(string message, string filePath, Exception? inner = null)
        : base(message, inner) => FilePath = filePath;
}
