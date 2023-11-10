using Microsoft.Extensions.Logging;

namespace Fluffy.Logger;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;

    public FileLoggerProvider(string logFilePath)
    {
        _logFilePath = logFilePath;
    }
    
    public void Dispose()
    { 
        GC.SuppressFinalize(this);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_logFilePath);
    }
}