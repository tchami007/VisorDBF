namespace VisorDBF.Core.Exceptions;

public sealed class ExportException : Exception
{
    public string OutputPath { get; }

    public ExportException(string message, string outputPath, Exception? inner = null)
        : base(message, inner) => OutputPath = outputPath;
}
