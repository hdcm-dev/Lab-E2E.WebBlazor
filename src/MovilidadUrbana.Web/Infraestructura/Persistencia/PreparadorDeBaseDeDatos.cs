using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MovilidadUrbana.Web.Infraestructura.Persistencia;

/// <summary>
/// Crea el archivo SQLite y su esquema al arrancar.
///
/// Se usa <c>EnsureCreated</c> y no migraciones a propósito: el laboratorio no versiona el
/// esquema, y así el binario publicado arranca en cualquier máquina sin pasos previos.
/// </summary>
public static class PreparadorDeBaseDeDatos
{
    public static void Preparar(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var fabrica = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<ContextoDeDatos>>();
        using var contexto = fabrica.CreateDbContext();

        var origen = new SqliteConnectionStringBuilder(contexto.Database.GetConnectionString()).DataSource;
        var carpeta = Path.GetDirectoryName(Path.GetFullPath(origen));
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

        contexto.Database.EnsureCreated();

        // WAL permite leer mientras otra conexión escribe. Con las pruebas E2E en paralelo,
        // varias sesiones tocan el mismo archivo al mismo tiempo.
        contexto.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }
}
