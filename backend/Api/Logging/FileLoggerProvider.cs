using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TeachAssist.Api.Logging;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _fileLock = new();

    public FileLoggerProvider(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    public void Write(string message)
    {
        lock (_fileLock)
        {
            File.AppendAllText(_logFilePath, message);
        }
    }

    private class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggerProvider _provider;

        public FileLogger(string categoryName, FileLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel < LogLevel.Information) return false;
            return _categoryName.Contains("GradeNotification", StringComparison.Ordinal);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName} - {formatter(state, exception)}";
            if (exception != null)
            {
                message += $"\nException: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}";
            }
            message += Environment.NewLine;
            _provider.Write(message);
        }
    }
}
