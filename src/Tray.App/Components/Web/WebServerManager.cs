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

                    // 1. Вшитые конфиги (базовые значения)
                    config.AddEmbeddedJson("appsettings.json");
                    config.AddEmbeddedJson($"appsettings.{environmentName}.json", true);

                    // 2. Пользовательские конфиги из appdata (для продакшена)
                    var userConfigPath = UserConfigStore.EnsureDefaultConfig("appsettings.json");
                    config.AddJsonFile(userConfigPath, true, true);

                    var envUserFileName = $"appsettings.{environmentName}.json";
                    var envUserPath = UserConfigStore.EnsureEnvironmentConfig(envUserFileName);
                    if (File.Exists(envUserPath))
                        config.AddJsonFile(envUserPath, true, true);

                    // 3. Локальные конфиги рядом с exe (для разработки - имеют приоритет)
                    // В Debug режиме они загружаются ПОСЛЕ пользовательских, чтобы перекрывать их
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddJsonFile($"appsettings.{environmentName}.json", true, true);

                    // 4. Переменные окружения и командная строка (высший приоритет)
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
                        var existingService = services.FirstOrDefault(s => s.ServiceType == typeof(IBrowserLogWriter));
                        if (existingService != null) services.Remove(existingService);
                        services.AddSingleton(logWriter);
                    });
                })
                .Build();

            // Получаем URL сервера из конфигурации
            var configuration = _webHost.Services.GetRequiredService<IConfiguration>();
            
            // Пробуем получить из Kestrel:Endpoints:Https:Url
            var httpsUrl = configuration["Kestrel:Endpoints:Https:Url"];
            
            // Если не нашли, пробуем из Urls (может быть несколько через ;)
            if (string.IsNullOrEmpty(httpsUrl))
            {
                var urls = configuration["Urls"];
                if (!string.IsNullOrEmpty(urls))
                {
                    httpsUrl = urls.Split(';')
                        .FirstOrDefault(u => u.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase));
                }
            }
            
            // Если все еще не нашли, используем значение по умолчанию
            if (string.IsNullOrEmpty(httpsUrl))
            {
                httpsUrl = "https://localhost:8980";
            }
            
            // Заменяем 0.0.0.0 на localhost для открытия в браузере
            httpsUrl = httpsUrl.Replace("0.0.0.0", "localhost");
            
            // Сохраняем URL в AppState для использования в трее
            appState.ServerUrl = httpsUrl;

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