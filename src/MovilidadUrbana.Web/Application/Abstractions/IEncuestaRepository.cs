using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.Web.Application.Abstractions;

public interface IEncuestaRepository
{
    Task<int> AddAsync(RespuestaDeEncuesta respuesta, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Las respuestas de la sesión. Sólo las lee el servicio de resultados, que las agrega.</summary>
    Task<IReadOnlyList<RespuestaDeEncuesta>> GetAllAsync(CancellationToken cancellationToken = default);
}
