using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public partial class KinopoiskFilmSearchService(
    IHttpClientFactory httpClientFactory,
    ICaptchaDetectionService captchaDetectionService,
    ILogger<KinopoiskFilmSearchService> logger) : IFilmSearchService
{
    public SearchSource Source { get; } = SearchSource.Kinopoisk;
    
    public async Task<Film?> SearchAsync(string filmName, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("BrowserClient");
            
            var encodedFilmName = Uri.EscapeDataString(filmName);
            var searchUrl = $"https://www.kinopoisk.ru/index.php?kp_query={encodedFilmName}";
            
            logger.LogInformation("Kinopoisk: Sending search request");
            logger.LogDebug("Search URL: {Url}", searchUrl);
            
            var response = await httpClient.GetStringAsync(searchUrl, cancellationToken);
            
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);
            if (captchaDetectionService.HasCaptcha(htmlDoc))
            {
                logger.LogWarning("Captcha detected on {Url} (1)", searchUrl);
                return null;
            }
                
            var filmLink = htmlDoc.DocumentNode
                .SelectNodes("//a[contains(@href, '/film/')]").FirstOrDefault();
            
            var filmUrl = filmLink!.GetAttributeValue("href", "");

            var match = GetFilmIdEx().Match(filmUrl);
            if (!match.Success) return null;
            
            var filmPageUrl = $"https://www.kinopoisk.ru/film/{match.Groups[1].Value}/";

            var filmResponse = await httpClient.GetStringAsync(filmPageUrl, cancellationToken);

            var filmDoc = new HtmlDocument();
            filmDoc.LoadHtml(filmResponse);
            
            if (captchaDetectionService.HasCaptcha(filmDoc))
            {
                logger.LogWarning("Captcha detected on {Url} (2)", filmPageUrl);
                return null;
            }
            
            TryGetVideoUrl(filmDoc, out filmUrl);

            if (filmUrl is not null)
                return new Film()
                {
                    Title = filmName,
                    Url = filmUrl,
                    Source = SearchSource.Kinopoisk
                };
            
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static void TryGetVideoUrl(HtmlDocument filmDoc, out string? filmUrl)
    {
        var watchElements = filmDoc.DocumentNode.SelectNodes("//*[contains(normalize-space(text()), 'Смотреть фильм')]");
        var hasWatchOption = watchElements is { Count: > 0 };

        filmUrl = null;
        if (!hasWatchOption)
            return;

        foreach (var element in watchElements)
        {
            var interactiveParent = element.AncestorsAndSelf()
                .FirstOrDefault(n => n.Name == "a");

            if (interactiveParent == null) continue;
            
            filmUrl = interactiveParent.GetAttributeValue("href", "");
                    
            if (!string.IsNullOrEmpty(filmUrl))
                break;
        }

        filmUrl = System.Net.WebUtility.HtmlDecode(filmUrl);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"/film/(\d+)")]
    private static partial System.Text.RegularExpressions.Regex GetFilmIdEx();
}