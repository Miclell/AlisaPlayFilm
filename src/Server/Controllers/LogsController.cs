using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController(IBrowserLogWriter logWriter, IWebHostEnvironment env) : ControllerBase
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [HttpGet]
    public IActionResult GetLogs()
    {
        var html = logWriter.GetLogsAsHtml();
        return Content(html, "text/html");
    }

    [HttpGet("stream")]
    public async Task GetLogStream()
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var tcs = new TaskCompletionSource();
        var cancellationToken = HttpContext.RequestAborted;

        Action<BrowserLogEntry> onLog = null!;
        onLog = entry =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logWriter.Unsubscribe(onLog);
                return;
            }

            _ = TrySendLogEntryAsync(entry);
        };

        logWriter.Subscribe(onLog);

        await SendLogEntryAsync(new BrowserLogEntry(
            DateTime.UtcNow, LogLevel.Information, "LogViewer",
            "Log stream connected"
        ));

        // Keep-alive только в Debug режиме - в проде не нужен
        if (env.IsDevelopment()) _ = KeepAliveLoopAsync(cancellationToken);

        cancellationToken.Register(() => tcs.TrySetResult());
        await tcs.Task;

        logWriter.Unsubscribe(onLog);
    }

    [HttpPost("config")]
    public IActionResult ConfigureLogs([FromBody] LogConfig config)
    {
        if (config.LogLifetimeHours.HasValue)
            BrowserLogWriter.Configure(TimeSpan.FromHours(config.LogLifetimeHours.Value));

        return Ok(new { message = "Log configuration updated" });
    }

    [HttpGet("export")]
    public IActionResult ExportLogs([FromQuery] int? maxCount = 1000)
    {
        var logs = logWriter.GetRecentLogs(maxCount ?? 1000);
        var logText = string.Join("\n", logs.Select(log =>
            $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {log.LogLevel} {log.Category}: {log.Message}" +
            (log.Exception != null ? $"\nEXCEPTION: {log.Exception}" : "")
        ));

        var bytes = Encoding.UTF8.GetBytes(logText);
        var fileName = $"logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";

        return File(bytes, "text/plain", fileName);
    }

    private async Task SendLogEntryAsync(BrowserLogEntry entry)
    {
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        var data = $"data: {json}\n\n";
        await Response.WriteAsync(data, HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
    }

    private async Task TrySendLogEntryAsync(BrowserLogEntry entry)
    {
        try
        {
            await SendLogEntryAsync(entry);
        }
        catch
        {
            // Ignore errors for individual log entries
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        var counter = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

            try
            {
                await SendLogEntryAsync(new BrowserLogEntry(
                    DateTime.UtcNow, LogLevel.Debug, "LogViewer",
                    $"Keep-alive #{++counter}"
                ));
            }
            catch
            {
                break;
            }
        }
    }

    public record LogConfig(int? LogLifetimeHours = null);
}