using Core.Interfaces;
using Infrastructure.Http;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBrowserService, BrowserService>();
        services.AddScoped<IFilmSearchService, KinopoiskFilmSearchService>();
        services.AddScoped<IFilmSearchService, RutubeFilmSearchService>();

        services.AddScoped<ICaptchaDetectionService, CaptchaDetectionService>();

        services.AddBrowserClient();

        return services;
    }
}