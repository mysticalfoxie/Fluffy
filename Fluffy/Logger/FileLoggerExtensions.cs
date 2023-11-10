using Microsoft.Extensions.Logging;

namespace Fluffy.Logger;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddLogFile(this ILoggingBuilder builder, DirectoryInfo directoryInfo = null)
    {
        if (directoryInfo is not null && !directoryInfo.Exists)
            directoryInfo.Create();
        
        var filename = DateTime.Now.ToString("dd.MM.yyyy-HH.mm.ss") + ".log";
        var directory = directoryInfo ?? new DirectoryInfo(Environment.CurrentDirectory);
        var filepath = Path.Combine(directory.FullName, filename);
        var logger = new FileLoggerProvider(filepath);
        return builder.AddProvider(logger);
    }
}