using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;
using Tray.App.Components.Web;
using Tray.App.Models;

namespace Tray.App.Components.Tray;

public class TrayManager : IDisposable
{
    private readonly AppState _appState;
    private readonly NotificationService? _notifications;
    private readonly WebServerManager? _webServer;
    private ButtonMenuItem? _quitItem;
    private ButtonMenuItem? _restartItem;
    private ButtonMenuItem? _showLogsItem;
    private ButtonMenuItem? _statusItem;

    public TrayManager(AppState appState, NotificationService? notifications, WebServerManager? webServer,
        TrayIndicator? tray = null)
    {
        _appState = appState;
        _notifications = notifications;
        _webServer = webServer;
        _appState.StatusChanged += OnStatusChanged;

        TrayIndicator = tray ?? new TrayIndicator
        {
            Title = Constants.AppName,
            Image = CreateBeautifulIcon(),
            Visible = false
        };

        InitializeTray();
        UpdateStatus();
    }

    private TrayIndicator TrayIndicator { get; }

    public void Dispose()
    {
        _appState.StatusChanged -= OnStatusChanged;
        TrayIndicator.Dispose();
        GC.SuppressFinalize(this);
    }

    private void InitializeTray()
    {
        var menuBuilder = new TrayMenuBuilder(_appState);
        var menu = menuBuilder.BuildMenu();

        _statusItem = menu.Items.OfType<ButtonMenuItem>()
            .First(x => x.Text.Contains("Server"));
        _showLogsItem = menu.Items.OfType<ButtonMenuItem>()
            .First(x => x.Text.Contains("Logs"));
        _restartItem = menu.Items.OfType<ButtonMenuItem>()
            .First(x => x.Text.Contains("Restart"));
        _quitItem = menu.Items.OfType<ButtonMenuItem>()
            .First(x => x.Text.Contains("Quit"));

        _showLogsItem.Click += (_, _) => ShowLogs();
        _restartItem.Click += (_, _) => RestartServer();
        _quitItem.Click += (_, _) => QuitApplication();

        TrayIndicator.Menu = menu;
        TrayIndicator.Activated += (_, _) => ShowLogs();
        TrayIndicator.Visible = true;

        _notifications?.Show("Server Started", $"{Constants.AppName} is running in background");
    }

    private void UpdateStatus()
    {
        Eto.Forms.Application.Instance.Invoke(() =>
        {
            if (_statusItem != null) _statusItem.Text = _appState.Status;
            TrayIndicator.Title = $"{Constants.AppName} - {_appState.Status}";
        });
    }

    private Bitmap CreateBeautifulIcon()
    {
        try
        {
            // Пытаемся загрузить иконку из ресурсов
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.png");
            if (File.Exists(iconPath)) return new Bitmap(iconPath);

            // Если файл не найден, пытаемся загрузить из папки проекта (для разработки)
            var projectIconPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "Tray.App", "Resources", "icon.png");
            if (File.Exists(projectIconPath)) return new Bitmap(projectIconPath);

            // Fallback: создаем программную иконку
            var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppRgba);
            using var graphics = new Graphics(bitmap);

            graphics.FillEllipse(Colors.White, 16, 16, 32, 32);
            graphics.FillRectangle(Colors.Navy, 24, 20, 16, 12);
            graphics.FillRectangle(Colors.Navy, 28, 32, 8, 8);

            return bitmap;
        }
        catch (Exception)
        {
            // В случае ошибки создаем программную иконку
            var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppRgba);
            using var graphics = new Graphics(bitmap);

            graphics.FillEllipse(Colors.White, 16, 16, 32, 32);
            graphics.FillRectangle(Colors.Navy, 24, 20, 16, 12);
            graphics.FillRectangle(Colors.Navy, 28, 32, 8, 8);

            return bitmap;
        }
    }

    private void ShowLogs()
    {
        try
        {
            // Используем HTTPS URL для логов
            var url = "https://localhost:8980/api/logs";
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else if (OperatingSystem.IsLinux()) Process.Start("xdg-open", url);
        }
        catch (Exception ex)
        {
            _notifications?.ShowError($"Failed to open logs: {ex.Message}");
        }
    }

    private void RestartServer()
    {
        _notifications?.Show("Restarting", "Server is restarting...");
        _webServer?.Restart();
    }

    private void QuitApplication()
    {
        Eto.Forms.Application.Instance.Quit();
    }

    private void OnStatusChanged()
    {
        UpdateStatus();
    }
}