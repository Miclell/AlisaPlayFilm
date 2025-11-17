using System.Text.Json;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;

namespace Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });

        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.All;
            options.RequestBodyLogLimit = 4096;
            options.ResponseBodyLogLimit = 4096;
        });

        services.AddApplication();
        services.AddInfrastructure();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AlisaPlayFilm API",
                Version = "v1",
                Description = "API для взаимодействия с Яндекс Алисой для поиска и воспроизведения фильмов"
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        // Логируем старт приложения
        logger.LogInformation("Application starting...");
        logger.LogInformation("Environment: {Environment}", env.EnvironmentName);
        logger.LogInformation("Content Root: {ContentRoot}", env.ContentRootPath);
        
        // WebRoot используется только для статических файлов (wwwroot), в API приложении не требуется
        if (!string.IsNullOrEmpty(env.WebRootPath))
        {
            logger.LogInformation("Web Root: {WebRoot}", env.WebRootPath);
        }

        // Swagger доступен всегда (можно ограничить только для Development если нужно)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AlisaPlayFilm API v1");
            c.RoutePrefix = string.Empty; // Swagger будет доступен на корневом пути "/"
        });

        logger.LogInformation("Swagger UI available at: /");
        logger.LogInformation("Logs available at: /api/logs");

        app.UseHttpLogging();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        logger.LogInformation("Application configured successfully");
        logger.LogInformation("Server is ready to accept requests");
    }
}