using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Core.Models;

public record BrowserLogEntry(
    DateTime Timestamp,
    LogLevel LogLevel,
    string Category,
    string Message,
    string? Exception = null)
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel LogLevel { get; } = LogLevel;

    public string Id { get; } = Guid.NewGuid().ToString("N");
}

public class LogEntry<TState>
{
    private readonly Func<TState, Exception?, string> _formatter;

    public LogEntry(LogLevel logLevel, string category, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogLevel = logLevel;
        Category = category;
        EventId = eventId;
        State = state;
        Exception = exception;
        _formatter = formatter;
    }

    public LogLevel LogLevel { get; }
    public string Category { get; }
    public EventId EventId { get; }
    public TState State { get; }
    public Exception? Exception { get; }

    public string FormatMessage()
    {
        return _formatter(State, Exception);
    }
}