using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IFilmSearchService
{
    SearchSource Source { get; }
    Task<Film?> SearchAsync(string filmName, CancellationToken cancellationToken = default);
}