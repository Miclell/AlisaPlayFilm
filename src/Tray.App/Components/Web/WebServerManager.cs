using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server;
using Tray.App.Components.Tray;
using Tray.App.Configuration;
using Tray.App.Models;

namespace Tray.App.Components.Web;

public class WebServerManager(
    AppState appState,
    NotificationService notificationService,
    IBrowserLogWriter logWriter)
{
    private CancellationTokenSource _cts = new();
    private IHost? _webHost;

    public bool IsRunning { get; private set; }

    public async Task StartAsync(string[] args)
    {
        try
        {
            var commandLineArgs = args ?? Array.Empty<string>();

            _webHost = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var environmentName = context.HostingEnvironment.EnvironmentName;

                    config.Sources.Clear();

                    config.AddEmbeddedJson("appsettings.json");
                    config.AddEmbeddedJson($"appsettings.{environmentName}.json", true);

                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddJsonFile($"appsettings.{environmentName}.json", true, true);

                    var userConfigPath = UserConfigStore.EnsureDefaultConfig("appsettings.json");
                    config.AddJsonFile(userConfigPath, true, true);

                    var envUserFileName = $"appsettings.{environmentName}.json";
                    var envUserPath = UserConfigStore.EnsureEnvironmentConfig(envUserFileName);
                    if (File.Exists(envUserPath))
                        config.AddJsonFile(envUserPath, true, true);

                    config.AddEnvironmentVariables();

                    if (commandLineArgs.Length > 0)
                        config.AddCommandLine(commandLineArgs);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(logWriter);
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureServices((_, services) =>
                    {
                        // Заменяем IBrowserLogWriter в DI на наш экземпляр
                        var existingService = services.FirstOrDefault(s => s.ServiceType == typeof(IBrowserLogWriter));
                        if (existingService != null) services.Remove(existingService);
                        services.AddSingleton(logWriter);
                    });
                })
                .Build();

            IsRunning = true;
            appState.IsRunning = true;
            appState.StartTime = DateTime.Now;

            notificationService.ShowSuccess($"{Constants.AppName} started successfully!");

            await _webHost.RunAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            IsRunning = false;
            appState.IsRunning = false;
            notificationService.ShowError($"Server failed: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            appState.IsRunning = false;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            notificationService.Show("Stopping", "Server is shutting down...");

            IsRunning = false;
            appState.IsRunning = false;

            await _cts.CancelAsync();

            if (_webHost != null)
            {
                await _webHost.StopAsync(TimeSpan.FromSeconds(5));
                _webHost.Dispose();
                _webHost = null!;
            }

            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
        catch (Exception ex)
        {
            notificationService.ShowError($"Error during shutdown: {ex.Message}");
        }
    }

    public void Restart()
    {
        _ = Task.Run(async () =>
        {
            await StopAsync();
            await Task.Delay(1000);
            await StartAsync([]);
        });
    }
}