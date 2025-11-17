using System.Text.Json;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class RutubeFilmSearchService(
    IHttpClientFactory httpClientFactory,
    ILogger<RutubeFilmSearchService> logger)
    : IFilmSearchService
{
    public SearchSource Source => SearchSource.Rutube;

    public async Task<Film?> SearchAsync(string filmName, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("BrowserClient");

            var encodedFilmName = Uri.EscapeDataString(filmName);
            var searchUrl =
                $"https://rutube.ru/api/search/video/?query={encodedFilmName}&fields=id,title,description,duration,category&format=json";

            logger.LogInformation("Rutube: Sending API search request for {FilmName}", filmName);
            logger.LogDebug("Search URL: {Url}", searchUrl);

            var response = await httpClient.GetStringAsync(searchUrl, cancellationToken);

            using var jsonDoc = JsonDocument.Parse(response);

            if (!jsonDoc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array)
            {
                logger.LogWarning("No results found in API response for {FilmName}", filmName);
                return null;
            }

            var films = new List<Film>();

            foreach (var video in results.EnumerateArray())
            {
                if (!video.TryGetProperty("id", out var idElement) ||
                    !video.TryGetProperty("title", out var titleElement))
                    continue;

                var id = idElement.GetString();
                var title = titleElement.GetString();

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title))
                    continue;

                var description = video.TryGetProperty("description", out var descElement)
                    ? descElement.GetString()
                    : string.Empty;

                var duration = video.TryGetProperty("duration", out var durationElement)
                    ? durationElement.GetInt32()
                    : 0;

                var category = "Unknown";
                if (video.TryGetProperty("category", out var categoryElement) &&
                    categoryElement.ValueKind == JsonValueKind.Object)
                    category = categoryElement.TryGetProperty("name", out var categoryNameElement)
                        ? categoryNameElement.GetString()
                        : "Unknown";

                logger.LogDebug("Found video: {Title} (ID: {Id}, Category: {Category}, Duration: {Duration}s)",
                    title, id, category, duration);

                if (!IsRelevantFilm(filmName, title, description!, duration))
                    continue;

                var filmUrl = $"https://rutube.ru/video/{id}/";

                logger.LogInformation("Found relevant film: {Title} at {Url}", title, filmUrl);

                films.Add(new Film
                {
                    Title = title,
                    Url = filmUrl,
                    Source = SearchSource.Rutube,
                    Description = description
                });
            }

            return GetMostRelevantFilm(filmName, films);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching film on Rutube via API: {FilmName}", filmName);
            return null;
        }
    }

    private bool IsRelevantFilm(string searchQuery, string title, string description, int duration)
    {
        if (string.IsNullOrEmpty(title)) return false;

        var searchLower = searchQuery.ToLowerInvariant();
        var titleLower = title.ToLowerInvariant();
        var descLower = description.ToLowerInvariant();

        if (IsTrailer(titleLower, descLower, duration))
        {
            logger.LogDebug("Filtered out as trailer: {Title}", title);
            return false;
        }

        if (IsShortVideo(duration))
        {
            logger.LogDebug("Filtered out as short video: {Title} ({Duration}s)", title, duration);
            return false;
        }

        var cleanTitle = CleanTitle(titleLower);

        var searchWords = searchLower.Split([' ', ',', '.', '!', '?'],
            StringSplitOptions.RemoveEmptyEntries);

        if (searchWords.Length == 0) return false;

        var titleMatchCount = searchWords.Count(word =>
            word.Length > 2 && cleanTitle.Contains(word));

        var descMatchCount = searchWords.Count(word =>
            word.Length > 2 && descLower.Contains(word));

        var totalRelevance = titleMatchCount * 2 + descMatchCount;

        var relevanceThreshold = Math.Max(1, searchWords.Length * 0.6);

        var isRelevant = totalRelevance >= relevanceThreshold;

        if (isRelevant)
            logger.LogDebug("Film {Title} is relevant: title matches {TitleMatches}, desc matches {DescMatches}",
                title, titleMatchCount, descMatchCount);

        return isRelevant;
    }

    private Film? GetMostRelevantFilm(string searchQuery, List<Film> films)
    {
        switch (films.Count)
        {
            case 0:
                return null;
            case 1:
                return films[0];
        }

        var searchLower = searchQuery.ToLowerInvariant();
        var searchWords = searchLower.Split([' ', ',', '.', '!', '?'],
            StringSplitOptions.RemoveEmptyEntries);

        return films.OrderByDescending(f =>
        {
            var titleLower = f.Title.ToLowerInvariant();
            var cleanTitle = CleanTitle(titleLower);

            var exactMatches = searchWords.Count(word =>
                cleanTitle.Contains(word));

            var partialMatches = searchWords.Count(word =>
                titleLower.Contains(word));

            return exactMatches * 3 + partialMatches;
        }).First();
    }

    private static string CleanTitle(string title)
    {
        var cleanTitle = title
            .Replace("(фильм)", "")
            .Replace("(кино)", "")
            .Replace("(film)", "")
            .Replace("(movie)", "")
            .Replace("смотреть онлайн", "")
            .Replace("полный фильм", "")
            .Replace("трейлер", "")
            .Replace("trailer", "")
            .Replace("озвучка", "")
            .Replace("субтитры", "")
            .Replace("hd", "")
            .Replace("full hd", "")
            .Replace("1080p", "")
            .Replace("720p", "")
            .Trim();

        return string.Join(" ", cleanTitle.Split([' '], StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsTrailer(string title, string description, int duration)
    {
        var trailerIndicators = new[] { "трейлер", "trailer", "тизер", "teaser" };
        return trailerIndicators.Any(indicator =>
            title.Contains(indicator) || description.Contains(indicator)) || duration < 1800; // Меньше 30 минут
    }

    private static bool IsShortVideo(int duration)
    {
        return duration is > 0 and < 1200; // Меньше 20 минут
    }
}