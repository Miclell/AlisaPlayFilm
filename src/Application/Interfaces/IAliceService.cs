using Application.DTOs;

namespace Application.Interfaces;

public interface IAliceService
{
    Task<AliceResponse> ProcessRequestAsync(AliceRequest request, CancellationToken cancellationToken = default);
}