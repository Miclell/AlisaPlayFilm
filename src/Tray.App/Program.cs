using Core.Interfaces;
using Eto.Drawing;
using Eto.Forms;
using Infrastructure.Logging;
using Tray.App.Components.Tray;
using Tray.App.Components.Web;
using Tray.App.Models;
using ApplicationEF = Eto.Forms.Application;

namespace Tray.App;

public class Program
{
    private static WebServerManager _webServer = null!;
    private static TrayManager _trayManager = null!;
    private static NotificationService _notificationService = null!;
    private static IBrowserLogWriter _logWriter = null!;
    private static AppState _appState = null!;
    private static ApplicationEF _app = null!;

    [STAThread]
    public static async Task Main(string[] args)
    {
        try
        {
            _app = new ApplicationEF();
            _appState = new AppState();
            _logWriter = new BrowserLogWriter("System");

            _app.Initialized += (_, _) => InitializeApplication(args);

            _app.Run();

            await CleanupAsync();
        }
        catch (Exception ex)
        {
            HandleFatalError(ex);
        }
    }

    private static void InitializeApplication(string[] args)
    {
        var tray = new TrayIndicator
        {
            Title = Constants.AppName,
            Image = GetTrayIcon(),
            Visible = false
        };

        _notificationService = new NotificationService(tray, _appState);

        _webServer = new WebServerManager(_appState, _notificationService, _logWriter);

        _trayManager = new TrayManager(_appState, _notificationService, _webServer, tray);

        _ = Task.Run(async () => await _webServer.StartAsync(args));
    }

    private static Bitmap GetTrayIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.png");
        return new Bitmap(iconPath);
    }

    private static async Task CleanupAsync()
    {
        _notificationService.Dispose();
        _trayManager.Dispose();
        await _webServer.StopAsync();
        _logWriter.Dispose();
    }

    private static void HandleFatalError(Exception ex)
    {
        MessageBox.Show($"Fatal error: {ex.Message}", "Error", MessageBoxType.Error);
        Environment.Exit(1);
    }
}