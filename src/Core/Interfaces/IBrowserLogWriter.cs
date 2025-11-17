using Core.Models;
using Microsoft.Extensions.Logging;

namespace Core.Interfaces;

public interface IBrowserLogWriter : ILogger, ILoggerProvider, IDisposable
{
    string GetLogsAsHtml();
    IReadOnlyList<BrowserLogEntry> GetRecentLogs(int maxCount = 1000);
    void Subscribe(Action<BrowserLogEntry> action);
    void Unsubscribe(Action<BrowserLogEntry> action);
    event Action<BrowserLogEntry>? OnLogEntry;
}