using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.Web.Infrastructure.Persistence;

/// <summary>Escribe votos y señales. No filtra por sesión porque el voto no la guarda.</summary>
public sealed class RegistroDeVotos(IDbContextFactory<AppDbContext> factory, CatalogoDeDestinos catalogo) : IRegistroDeSenales
{
    public async Task RegistrarAsync(Voto voto, CancellationToken cancellationToken = default)
    {
        // Los únicos textos del voto son ids de catálogos cerrados: lo demás no entra.
        if (!Intenciones.Todas.Contains(voto.Intencion)) throw new ArgumentException("Intención fuera del catálogo.", nameof(voto));
        if (voto.Destino is not null && !catalogo.Existe(voto.Destino)) throw new ArgumentException("Destino fuera del catálogo.", nameof(voto));

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        voto.Id = 0;
        context.Votos.Add(voto);
        await context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Lee los votos para el tablero de medición. El pipeline del asistente no lo conoce.</summary>
public sealed class LectorDeVotos(IDbContextFactory<AppDbContext> factory) : ILectorDeVotos
{
    public async Task<IReadOnlyList<Voto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context.Votos.AsNoTracking().OrderBy(v => v.Id).ToListAsync(cancellationToken);
    }
}
