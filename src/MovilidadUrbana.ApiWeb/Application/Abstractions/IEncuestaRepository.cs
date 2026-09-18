using MovilidadUrbana.ApiWeb.Domain.Entities;

namespace MovilidadUrbana.ApiWeb.Application.Abstractions;

public interface IEncuestaRepository
{
    Task<int> AddAsync(RespuestaDeEncuesta respuesta, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
