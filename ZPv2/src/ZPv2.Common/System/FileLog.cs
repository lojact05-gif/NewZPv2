using System.Text;

namespace ZPv2.Common.System;

public sealed class FileLog
{
    private readonly object _sync = new();
    private readonly string _logDir;

    public FileLog()
    {
        _logDir = AppPaths.ResolveWritableLogDir();
    }

    public string LogDir => _logDir;

    public void Info(string message) => Write("INFO", message, null);
    public void Warn(string message) => Write("WARN", message, null);
    public void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private void Write(string level, string message, Exception? ex)
    {
        var file = Path.Combine(_logDir, "zpv2-" + DateTimeOffset.Now.ToString("yyyyMMdd") + ".log");
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] {level} {message}";
        if (ex is not null)
        {
            line += Environment.NewLine + ex;
        }

        lock (_sync)
        {
            File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
        }
    }
}
