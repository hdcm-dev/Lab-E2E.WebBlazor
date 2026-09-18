using MovilidadUrbana.MAUI.Domain.Entities;

namespace MovilidadUrbana.MAUI.Application.Abstractions;

public interface IEncuestaRepository
{
    Task<int> AddAsync(RespuestaDeEncuesta respuesta, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
