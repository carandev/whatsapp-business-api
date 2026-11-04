using Microsoft.Extensions.Logging;

namespace Carandev.WhatsApp.Sdk.Tests.Fakes;

/// <summary>
/// A single captured <see cref="ILogger"/> call, recorded verbatim so tests can assert on the
/// rendered message and log level without depending on a specific logging provider.
/// </summary>
internal sealed record FakeLogRecord(LogLevel Level, string Message);

/// <summary>
/// A minimal <see cref="ILogger{T}"/> sink that records every log call it receives, so tests can
/// assert on Information/Debug/Trace output without wiring a real logging provider.
/// </summary>
internal sealed class FakeLogger<T> : ILogger<T>
{
    private readonly LogLevel _minLevel;

    public FakeLogger(LogLevel minLevel = LogLevel.Trace)
    {
        _minLevel = minLevel;
    }

    public List<FakeLogRecord> Records { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Records.Add(new FakeLogRecord(logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
