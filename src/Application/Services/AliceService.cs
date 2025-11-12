using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public partial class AliceService(
    IFilmSearchOrchestrator filmSearchOrchestrator,
    IBrowserService browserService,
    ILogger<AliceService> logger)
    : IAliceService
{
    public async Task<AliceResponse> ProcessRequestAsync(AliceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalCommand = request.Request.OriginalUtterance ?? request.Request.Command;
            var command = originalCommand.ToLowerInvariant();

            var filmName = ExtractFilmName(command);

            logger.LogDebug("Extracted film name: '{FilmName}'", filmName);

            if (string.IsNullOrWhiteSpace(filmName))
            {
                logger.LogWarning("Film name is empty after extraction");
                var response = CreateResponse("Пожалуйста, укажите название фильма.");
                response.Session = request.Session;
                return response;
            }

            logger.LogInformation("Starting film search for: '{FilmName}'", filmName);

            var film = await filmSearchOrchestrator.SearchFilmAsync(filmName, cancellationToken);

            if (film == null)
            {
                var response = CreateResponse("Фильм не найден.");
                response.Session = request.Session;
                return response;
            }

            await browserService.OpenUrlAsync(film.Url, cancellationToken);

            var successResponse = CreateResponse($"Открываю фильм: {film.Title}");
            successResponse.Session = request.Session;
            return successResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Alice request");
            var response = CreateResponse("Произошла ошибка при обработке запроса.");
            response.Session = request.Session;
            return response;
        }
    }

    private string ExtractFilmName(string command)
    {
        logger.LogDebug("=== ExtractFilmName ===");
        logger.LogDebug("Input command: '{Command}'", command);

        var match = GetFilmNameEx().Match(command);

        if (match.Success)
        {
            var filmName = match.Groups[1].Value.Trim();
            logger.LogDebug("Extracted film name: '{FilmName}'", filmName);

            filmName = CleanFilmNameEx().Replace(filmName, "");

            logger.LogDebug("Final film name: '{FilmName}'", filmName);
            return filmName;
        }

        logger.LogDebug("No match found, returning original command: '{Command}'", command);
        return command.Trim();
    }

    private static AliceResponse CreateResponse(string text)
    {
        return new AliceResponse
        {
            Response = new Response
            {
                Text = text,
                EndSession = true
            },
            Session = new Session()
        };
    }

    [GeneratedRegex(@"(?:(?:алиса\s*,?\s*)|(?:включи|покажи|найди|открой)\s+(?:фильм\s+)?)(.*)",
        RegexOptions.IgnoreCase, "ru-RU")]
    private static partial Regex GetFilmNameEx();

    [GeneratedRegex(@"[.,!?;:]$")]
    private static partial Regex CleanFilmNameEx();
}