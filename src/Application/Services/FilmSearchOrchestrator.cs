using Application.Interfaces;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class FilmSearchOrchestrator : IFilmSearchOrchestrator
{
    private readonly ILogger<FilmSearchOrchestrator> _logger;
    private readonly IReadOnlyList<IFilmSearchService> _searchServices;

    public FilmSearchOrchestrator(
        IEnumerable<IFilmSearchService> searchServices,
        ILogger<FilmSearchOrchestrator> logger)
    {
        _searchServices = searchServices.OrderBy(s => s.Source).ToList().AsReadOnly();
        _logger = logger;
    }

    public async Task<Film?> SearchFilmAsync(string filmName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filmName))
        {
            _logger.LogWarning("FilmSearchOrchestrator: Empty film name provided");
            return null;
        }

        using var scope = _logger.BeginScope("Film search for '{FilmName}'", filmName);
        _logger.LogInformation("Starting film search across {ServiceCount} services", _searchServices.Count);

        LogAvailableServices();

        foreach (var searchService in _searchServices)
        {
            var result = await TrySearchOnServiceAsync(searchService, filmName, cancellationToken);
            if (result != null)
                return result;
        }

        _logger.LogWarning("Film not found on any of {ServiceCount} services", _searchServices.Count);
        return null;
    }

    private async Task<Film?> TrySearchOnServiceAsync(IFilmSearchService service, string filmName,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Searching on {Source}", service.Source);
            var film = await service.SearchAsync(filmName, cancellationToken);

            if (film != null)
            {
                _logger.LogInformation("Found on {Source}: '{Title}'", service.Source, film.Title);
                return film;
            }

            _logger.LogDebug("Not found on {Source}", service.Source);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Search cancelled on {Source}", service.Source);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching on {Source}", service.Source);
            return null;
        }
    }

    private void LogAvailableServices()
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;

        var services = _searchServices
            .Select((s, i) => $"{i + 1}. {s.Source}")
            .ToArray();

        _logger.LogDebug("Available services:\n{Services}", string.Join("\n", services));
    }
}