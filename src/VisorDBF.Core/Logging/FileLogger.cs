using System.Runtime.CompilerServices;

namespace VisorDBF.Core.Logging;

public sealed class FileLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private readonly string _logPath;

    public string LogPath => _logPath;

    public FileLogger(string filePath)
    {
        _logPath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        _writer = new StreamWriter(filePath, append: false)
        {
            AutoFlush = true
        };
        WriteLine("=== Log started ===");
    }

    public void WriteLine(string message, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var lineStr = $"[{timestamp}] [{member}:{line}] {message}";
        lock (_lock)
        {
            _writer.WriteLine(lineStr);
        }
    }

    public void WriteLine(string message, TimeSpan elapsed, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var lineStr = $"[{timestamp}] [{member}:{line}] {message} (elapsed: {elapsed.TotalSeconds:F3}s)";
        lock (_lock)
        {
            _writer.WriteLine(lineStr);
        }
    }

    public void Dispose()
    {
        WriteLine("=== Log ended ===");
        lock (_lock)
        {
            _writer.Dispose();
        }
    }
}
