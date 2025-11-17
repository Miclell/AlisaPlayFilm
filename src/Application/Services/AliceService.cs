using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class AliceService(
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

            logger.LogDebug("=== ExtractFilmName ===");
            logger.LogDebug("Input command: '{Command}'", originalCommand);

            var filmName = ExtractFilmName(originalCommand);

            logger.LogDebug("Final film name: '{FilmName}'", filmName);

            if (string.IsNullOrWhiteSpace(filmName))
            {
                logger.LogWarning("Film name is empty after extraction");
                var response =
                    CreateResponse(
                        "Пожалуйста, укажите название фильма. Например: 'Включи Матрицу' или 'Найди фильм Интерстеллар'",
                        request.Session);
                response.Session = request.Session;
                return response;
            }

            logger.LogInformation("Starting film search for: '{FilmName}'", filmName);

            var film = await filmSearchOrchestrator.SearchFilmAsync(filmName, cancellationToken);

            if (film == null)
            {
                var response =
                    CreateResponse($"К сожалению, не удалось найти фильм '{filmName}'. Попробуйте другое название.",
                        request.Session);
                response.Session = request.Session;
                return response;
            }

            await browserService.OpenUrlAsync(film.Url, cancellationToken);

            var successResponse = CreateResponse($"Открываю фильм: {film.Title}", request.Session);
            successResponse.Session = request.Session;
            return successResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Alice request");
            var response = CreateResponse("Произошла ошибка при обработке запроса. Попробуйте еще раз.",
                request.Session);
            response.Session = request.Session;
            return response;
        }
    }

    private static string ExtractFilmName(string command)
    {
        var patterns = new[]
        {
            @"включи фильм (.+)",
            @"покажи фильм (.+)",
            @"найди фильм (.+)",
            @"открой фильм (.+)",
            @"включи (.+)",
            @"покажи (.+)",
            @"найди (.+)",
            @"открой (.+)",
            @"алиса включи (.+)",
            @"алиса найди (.+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(command, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var filmName = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(filmName)) return filmName;
        }

        return "";
    }

    private static AliceResponse CreateResponse(string text, Session requestSession)
    {
        return new AliceResponse
        {
            Response = new Response
            {
                Text = text,
                EndSession = true
            },
            Session = requestSession
        };
    }
}