using System.Diagnostics;
using Eto.Forms;
using Tray.App.Models;

namespace Tray.App.Components.Tray;

public class NotificationService(TrayIndicator tray, AppState appState) : IDisposable
{
    private bool _isDisposed;

    public void Dispose()
    {
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    public void Show(string title, string message)
    {
        InvokeSafe(() =>
        {
            tray.Title = $"{title}: {message}";
            ShowTemporaryNotification(title, message);
            Debug.WriteLine($"NOTIFICATION: {title} - {message}");
        });
    }

    public void ShowError(string message)
    {
        InvokeSafe(() =>
        {
            const string title = "Error";
            tray.Title = $"{title}: {message}";
            ShowTemporaryNotification(title, message);

            if (IsCriticalError(message)) MessageBox.Show(message, "Server Error", MessageBoxType.Error);

            appState.LastError = message;
            appState.ErrorCount++;
        });
    }

    public void ShowSuccess(string message)
    {
        InvokeSafe(() =>
        {
            const string title = "Success";
            tray.Title = $"{title}: {message}";
            ShowTemporaryNotification(title, message);
            appState.LastError = null;
        });
    }

    public void ShowStatus(string message)
    {
        InvokeSafe(() => { tray.Title = $"{Constants.AppName} - {message}"; });
    }

    private void ShowTemporaryNotification(string title, string message)
    {
        if (_isDisposed) return;

        tray.Title = $"{title}: {message}";

        Task.Delay(5000)
            .ContinueWith(_ => { InvokeSafe(() => { tray.Title = $"{Constants.AppName} - {appState.Status}"; }); },
                TaskScheduler.Default);
    }

    private static bool IsCriticalError(string message)
    {
        var criticalErrors = new[]
        {
            "port already in use",
            "access denied",
            "failed to start",
            "cannot bind"
        };

        return criticalErrors.Any(error =>
            message.Contains(error, StringComparison.OrdinalIgnoreCase));
    }

    private void InvokeSafe(Action action)
    {
        if (_isDisposed) return;

        var app = Eto.Forms.Application.Instance;

        app?.Invoke(() =>
        {
            if (_isDisposed) return;

            try
            {
                action();
            }
            catch (ObjectDisposedException)
            {
                // TrayIndicator уже уничтожен
            }
        });
    }
}