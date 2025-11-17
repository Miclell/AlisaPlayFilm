namespace Core.Interfaces;

public interface IBrowserService
{
    Task OpenUrlAsync(string url, CancellationToken cancellationToken = default);
}