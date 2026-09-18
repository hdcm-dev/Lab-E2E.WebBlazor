using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MovilidadUrbana.ApiWeb.Infrastructure.Persistence;

/// <summary>
/// Crea el archivo SQLite y su esquema al arrancar.
///
/// Se usa <c>EnsureCreated</c> y no migraciones a propósito: el laboratorio no versiona el
/// esquema, y así el binario publicado arranca en cualquier máquina sin pasos previos.
/// </summary>
public static class DatabaseInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var alcance = services.CreateScope();
        var factory = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = factory.CreateDbContext();

        var origen = new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;
        var carpeta = Path.GetDirectoryName(Path.GetFullPath(origen));
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

        context.Database.EnsureCreated();

        // WAL permite leer mientras otra conexión escribe. Con las pruebas E2E en paralelo,
        // varias sesiones tocan el mismo archivo al mismo tiempo.
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
}
