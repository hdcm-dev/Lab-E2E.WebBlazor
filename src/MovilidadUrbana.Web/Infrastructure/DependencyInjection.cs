using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.Web.Application.Abstractions;
using MovilidadUrbana.Web.Infrastructure.Persistence;
using MovilidadUrbana.Web.Infrastructure.Sessions;

namespace MovilidadUrbana.Web.Infrastructure;

/// <summary>
/// Registra la persistencia y la sesión. La cadena de conexión la decide quien compone —web o
/// API—; el cómo se implementa cada abstracción queda acá.
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionString = "Data Source=datos/movilidad.db;Default Timeout=30";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Factory y no contexto de ámbito: un circuito de Blazor vive más que una petición.
        services.AddDbContextFactory<AppDbContext>(opciones => opciones.UseSqlite(connectionString));

        // La misma instancia sirve a la implementación concreta y a la abstracción.
        services.AddScoped<SessionContext>();
        services.AddScoped<ISessionContext>(sp => sp.GetRequiredService<SessionContext>());
        services.AddScoped<SessionSeeder>();
        services.AddScoped<ILocalidadRepository, LocalidadRepository>();
        services.AddScoped<IEncuestaRepository, EncuestaRepository>();
        return services;
    }
}
