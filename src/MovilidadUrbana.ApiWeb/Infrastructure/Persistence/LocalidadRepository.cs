using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.ApiWeb.Application.Abstractions;
using MovilidadUrbana.ApiWeb.Domain.Entities;

namespace MovilidadUrbana.ApiWeb.Infrastructure.Persistence;

/// <summary>
/// Acceso a localidades acotado a la sesión actual: ninguna consulta sale del espacio de datos
/// del visitante, ni siquiera si le llega el identificador de otra sesión.
///
/// Usa <see cref="IDbContextFactory{TContext}"/> y abre un contexto por operación, que es lo
/// recomendado en Blazor Server: un `DbContext` con alcance de ámbito viviría lo que dura el
/// circuito —minutos u horas— y no está pensado para eso.
/// </summary>
public sealed class LocalidadRepository(
    IDbContextFactory<AppDbContext> factory,
    ISessionContext session,
    SessionSeeder seeder) : ILocalidadRepository
{
    public async Task<IReadOnlyList<Localidad>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await seeder.EnsureSeededAsync(cancellationToken);
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context.Localidades
            .AsNoTracking()
            .Where(l => l.SessionId == session.Id)
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Localidad?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await seeder.EnsureSeededAsync(cancellationToken);
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        return await context.Localidades
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.SessionId == session.Id, cancellationToken);
    }

    public async Task<int> AddAsync(Localidad localidad, CancellationToken cancellationToken = default)
    {
        await seeder.EnsureSeededAsync(cancellationToken);
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        localidad.SessionId = session.Id;
        context.Localidades.Add(localidad);
        await context.SaveChangesAsync(cancellationToken);
        return localidad.Id;
    }

    public async Task UpdateAsync(Localidad localidad, CancellationToken cancellationToken = default)
    {
        // La entidad llegó desde `ObtenerAsync`, que ya filtró por sesión; la comprobación deja
        // igual la garantía escrita en el código y no en la memoria de quien lo lea.
        if (localidad.SessionId != session.Id) return;

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        context.Localidades.Update(localidad);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Localidades
            .Where(l => l.Id == id && l.SessionId == session.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
