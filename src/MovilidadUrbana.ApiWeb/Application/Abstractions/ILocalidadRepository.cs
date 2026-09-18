using MovilidadUrbana.ApiWeb.Domain.Entities;

namespace MovilidadUrbana.ApiWeb.Application.Abstractions;

public interface ILocalidadRepository
{
    Task<IReadOnlyList<Localidad>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Localidad?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> AddAsync(Localidad localidad, CancellationToken cancellationToken = default);

    Task UpdateAsync(Localidad localidad, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
