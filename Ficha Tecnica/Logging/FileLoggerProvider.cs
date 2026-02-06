using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Ficha_Tecnica.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    internal static readonly object FileLock = new();

    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public FileLoggerProvider(string directoryPath, LogLevel minimumLevel = LogLevel.Error)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("The log directory path must be provided.", nameof(directoryPath));
        }

        DirectoryPath = directoryPath;
        MinimumLevel = minimumLevel;

        Directory.CreateDirectory(DirectoryPath);
    }

    internal string DirectoryPath { get; }

    internal LogLevel MinimumLevel { get; }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggerProvider _provider;

        public FileLogger(string categoryName, FileLoggerProvider provider)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _provider.MinimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
            var builder = new StringBuilder();
            builder.Append(timestamp)
                .Append(" [")
                .Append(logLevel)
                .Append("] ")
                .Append(_categoryName)
                .Append(':');

            if (!string.IsNullOrWhiteSpace(message))
            {
                builder.Append(' ').Append(message);
            }

            if (exception != null)
            {
                builder.AppendLine();
                builder.Append(exception);
            }

            var logFilePath = Path.Combine(_provider.DirectoryPath, $"app-{DateTime.UtcNow:yyyyMMdd}.log");
            var entry = builder.ToString();

            try
            {
                lock (FileLock)
                {
                    File.AppendAllText(logFilePath, entry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Ignore file logging failures to avoid crashing the application while handling errors.
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
