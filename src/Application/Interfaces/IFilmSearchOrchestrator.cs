using Core.Entities;

namespace Application.Interfaces;

public interface IFilmSearchOrchestrator
{
    Task<Film?> SearchFilmAsync(string filmName, CancellationToken cancellationToken = default);
}