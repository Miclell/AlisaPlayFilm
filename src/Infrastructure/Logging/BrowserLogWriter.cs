using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

public class BrowserLogWriter : IBrowserLogWriter
{
    // Статические поля для общих ресурсов
    private static readonly ConcurrentDictionary<string, BrowserLogWriter> _loggers = new();
    private static readonly ConcurrentQueue<BrowserLogEntry> _logs = new();
    private static readonly List<WeakReference<Action<BrowserLogEntry>>> _staticSubscribers = new();
    private static readonly Timer _cleanupTimer;
    private static TimeSpan _logLifetime = TimeSpan.FromHours(1);

    // Экземплярные поля
    private readonly string _categoryName;

    static BrowserLogWriter()
    {
        _cleanupTimer = new Timer(_ => RemoveExpiredLogs(), null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public BrowserLogWriter(string categoryName)
    {
        _categoryName = categoryName;
        _loggers[categoryName] = this;
        // Убрали автоматическое логирование инициализации - это лишний шум
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logEntry = new BrowserLogEntry(
            DateTime.UtcNow,
            logLevel,
            _categoryName,
            message ?? string.Empty,
            exception?.ToString()
        );

        Enqueue(logEntry);
    }

    public string GetLogsAsHtml()
    {
        var recentLogs = GetRecentLogs(500);

        var htmlTemplate = LoadHtmlTemplate();
        var logsJson = SerializeLogsToJson(recentLogs);
        var safeJson = WebUtility.HtmlEncode(logsJson);

        return htmlTemplate.Replace("{{LOGS_DATA}}", safeJson);
    }

    public IReadOnlyList<BrowserLogEntry> GetRecentLogs(int maxCount = 1000)
    {
        return _logs
            .Where(log => DateTime.UtcNow - log.Timestamp <= _logLifetime)
            .TakeLast(maxCount)
            .ToList();
    }

    public void Subscribe(Action<BrowserLogEntry> action)
    {
        lock (_staticSubscribers)
        {
            _staticSubscribers.Add(new WeakReference<Action<BrowserLogEntry>>(action));
        }

        CleanupSubscribers();
    }

    public void Unsubscribe(Action<BrowserLogEntry> action)
    {
        lock (_staticSubscribers)
        {
            _staticSubscribers.RemoveAll(wr =>
                wr.TryGetTarget(out var target) && target == action);
        }

        _onLogEntry -= action;
    }

    public event Action<BrowserLogEntry>? OnLogEntry
    {
        add => _onLogEntry += value;
        remove => _onLogEntry -= value;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new BrowserLogWriter(name));
    }

    public void Dispose()
    {
        _loggers.TryRemove(_categoryName, out _);
        _onLogEntry = null;
    }

    private event Action<BrowserLogEntry>? _onLogEntry;

    public static void Configure(TimeSpan logLifetime)
    {
        _logLifetime = logLifetime;
    }

    private static void Enqueue(BrowserLogEntry logEntry)
    {
        _logs.Enqueue(logEntry);

        // Ограничение по количеству + очистка старых
        while (_logs.Count > 2000 && _logs.TryDequeue(out _))
        {
        }

        NotifySubscribers(logEntry);
    }

    private static void NotifySubscribers(BrowserLogEntry logEntry)
    {
        foreach (var logger in _loggers.Values) logger.NotifyInstanceSubscribers(logEntry);

        NotifyStaticSubscribers(logEntry);
    }

    private void NotifyInstanceSubscribers(BrowserLogEntry logEntry)
    {
        try
        {
            _onLogEntry?.Invoke(logEntry);
        }
        catch
        {
            // Игнорируем ошибки в подписчиках
        }
    }

    private static void NotifyStaticSubscribers(BrowserLogEntry logEntry)
    {
        lock (_staticSubscribers)
        {
            foreach (var weakRef in _staticSubscribers.ToList())
                if (weakRef.TryGetTarget(out var action))
                    try
                    {
                        action(logEntry);
                    }
                    catch
                    {
                        // Игнорируем ошибки в подписчиках
                    }
        }
    }

    private static void RemoveExpiredLogs()
    {
        var cutoff = DateTime.UtcNow - _logLifetime;
        var expiredCount = 0;

        while (_logs.TryPeek(out var oldest) && oldest.Timestamp < cutoff)
            if (_logs.TryDequeue(out _))
                expiredCount++;

        if (expiredCount > 0) CleanupSubscribers();
    }

    private static void CleanupSubscribers()
    {
        lock (_staticSubscribers)
        {
            _staticSubscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
        }
    }

    private static string LoadHtmlTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Infrastructure.Resources.logs-template.html";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) throw new InvalidOperationException($"Resource {resourceName} not found");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string SerializeLogsToJson(IEnumerable<BrowserLogEntry> logs)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(logs, options);
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}