using System.Text;
using Microsoft.Extensions.Logging;

namespace Fluffy.Logger;

public class FileLogger : ILogger
{
    private readonly string _logFilePath;

    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var logEntry = $"{DateTime.Now:dd.MM.yyyy - HH:mm:ss.ffff} [{logLevel.ToString().PadRight("Information".Length, ' ')}]: {formatter(state, exception)}\n";
        File.AppendAllText(_logFilePath, logEntry, Encoding.Unicode);
    }
}