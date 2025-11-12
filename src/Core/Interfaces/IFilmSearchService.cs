using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IFilmSearchService
{
    Task<Film?> SearchAsync(string filmName, CancellationToken cancellationToken = default);
    SearchSource Source { get; }
}

