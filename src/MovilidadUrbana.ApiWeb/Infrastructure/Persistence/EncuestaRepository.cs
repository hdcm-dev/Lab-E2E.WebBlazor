using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.ApiWeb.Application.Abstractions;
using MovilidadUrbana.ApiWeb.Domain.Entities;

namespace MovilidadUrbana.ApiWeb.Infrastructure.Persistence;

public sealed class EncuestaRepository(
    IDbContextFactory<AppDbContext> factory,
    ISessionContext session) : IEncuestaRepository
{
    public async Task<int> AddAsync(RespuestaDeEncuesta respuesta, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        respuesta.SessionId = session.Id;
        context.Encuestas.Add(respuesta);
        await context.SaveChangesAsync(cancellationToken);
        return respuesta.Id;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context.Encuestas.CountAsync(e => e.SessionId == session.Id, cancellationToken);
    }
}
