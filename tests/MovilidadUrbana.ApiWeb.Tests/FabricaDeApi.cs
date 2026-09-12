using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace MovilidadUrbana.ApiWeb.Tests;

/// <summary>
/// Levanta la API en proceso sobre una base SQLite propia de la corrida, para que las pruebas no
/// toquen `datos/` ni se pisen entre corridas.
/// </summary>
public sealed class FabricaDeApi : WebApplicationFactory<Program>
{
    private readonly string _archivo = Path.Combine(Path.GetTempPath(), $"movilidad-api-{Guid.NewGuid():n}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:BaseDeDatos", $"Data Source={_archivo};Default Timeout=30");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var f in new[] { _archivo, _archivo + "-wal", _archivo + "-shm" }) File.Delete(f);
    }
}
