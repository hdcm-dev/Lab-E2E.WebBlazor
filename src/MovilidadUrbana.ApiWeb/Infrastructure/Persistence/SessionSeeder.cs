using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.ApiWeb.Application.Abstractions;
using MovilidadUrbana.ApiWeb.Domain.Entities;

namespace MovilidadUrbana.ApiWeb.Infrastructure.Persistence;

/// <summary>
/// Deja la sesión con su juego de datos inicial la primera vez que se la toca. La marca en la
/// tabla `Sesiones` evita volver a sembrar cuando la persona borró todas las localidades a mano.
/// </summary>
public sealed class SessionSeeder(IDbContextFactory<AppDbContext> factory, ISessionContext session)
{
    private static readonly (string Nombre, string Provincia, string CodigoPostal, int Habitantes)[] LocalidadesIniciales =
    [
        ("Corrientes", "Corrientes", "3400", 346334),
        ("Resistencia", "Chaco", "3500", 291720)
    ];

    private bool _alreadySeeded;

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (_alreadySeeded) return;

        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        if (!await context.Sessions.AnyAsync(s => s.Id == session.Id, cancellationToken))
        {
            context.Sessions.Add(new Session { Id = session.Id, CreatedAt = DateTimeOffset.UtcNow });
            context.Localidades.AddRange(LocalidadesIniciales.Select(inicial => new Localidad
            {
                SessionId = session.Id,
                Nombre = inicial.Nombre,
                Provincia = inicial.Provincia,
                CodigoPostal = inicial.CodigoPostal,
                Habitantes = inicial.Habitantes
            }));

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Otra petición de la misma sesión ganó la carrera insertando la marca: los datos ya están.
            }
        }

        _alreadySeeded = true;
    }
}
