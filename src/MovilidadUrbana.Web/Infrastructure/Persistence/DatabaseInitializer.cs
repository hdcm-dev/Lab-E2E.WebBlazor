using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MovilidadUrbana.Web.Infrastructure.Persistence;

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
        AsegurarTablaDeVotos(context);

        // WAL permite leer mientras otra conexión escribe. Con las pruebas E2E en paralelo,
        // varias sesiones tocan el mismo archivo al mismo tiempo.
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }

    /// <summary>
    /// El repo crea el esquema con <c>EnsureCreated</c>, sin migraciones (ADR 0007). Sobre un
    /// archivo de una versión anterior, <c>EnsureCreated</c> no agrega tablas: la de votos se crea
    /// con la misma sentencia que genera EF, con <c>IF NOT EXISTS</c>.
    /// </summary>
    private static void AsegurarTablaDeVotos(AppDbContext context)
    {
        var script = context.Database.GenerateCreateScript();
        foreach (var sentencia in script.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (sentencia.StartsWith("CREATE TABLE \"Votos\"", StringComparison.Ordinal))
                context.Database.ExecuteSqlRaw(sentencia.Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS "));
            else if (sentencia.StartsWith("CREATE INDEX \"IX_Votos_", StringComparison.Ordinal))
                context.Database.ExecuteSqlRaw(sentencia.Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS "));
        }
    }
}
