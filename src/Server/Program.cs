using Application;
using Infrastructure;
using Microsoft.OpenApi.Models;

namespace Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Настройка kebab-case для JSON
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.KebabCaseLower;
            });

        // HTTP логирование для отладки
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
            options.RequestBodyLogLimit = 4096;
            options.ResponseBodyLogLimit = 4096;
        });

        // Добавляем сервисы из слоев
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure();

        // Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AlisaPlayFilm API",
                Version = "v1",
                Description = "API для взаимодействия с Яндекс Алисой для поиска и воспроизведения фильмов"
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AlisaPlayFilm API v1");
                c.RoutePrefix = string.Empty; // Swagger UI на корневом пути
            });
        }

        // HTTP логирование
        app.UseHttpLogging();

        // Для работы с Алисой может потребоваться HTTP, поэтому разрешаем оба протокола
        // В production можно использовать только HTTPS
        app.UseHttpsRedirection();
        
        app.MapControllers();

        app.Run();
    }
}