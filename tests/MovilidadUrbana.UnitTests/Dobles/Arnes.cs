using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MovilidadUrbana.Web.Application;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Infrastructure;
using MovilidadUrbana.Web.Infrastructure.AI;
using MovilidadUrbana.Web.Infrastructure.Persistence;
using MovilidadUrbana.Web.Infrastructure.Sessions;

namespace MovilidadUrbana.UnitTests.Dobles;

/// <summary>
/// Compone la aplicación como lo hace <c>Program.cs</c> —infraestructura, aplicación y capa de
/// IA— sobre una base SQLite en memoria propia de la prueba, con la sesión fijada por el código y
/// el modelo reemplazado por un doble. Sin red, sin navegador.
/// </summary>
public sealed class Arnes : IDisposable
{
    private readonly SqliteConnection _conexion;

    public ServiceProvider Servicios { get; }

    public ClienteDeChatGuionado Modelo { get; }

    public Arnes(ClienteDeChatGuionado? modelo = null, bool habilitada = true, IDictionary<string, string?>? configuracion = null, bool redaccion = true)
    {
        var nombre = "arnes-" + Guid.NewGuid().ToString("n");
        var cadena = $"Data Source=file:{nombre}?mode=memory&cache=shared";
        _conexion = new SqliteConnection(cadena);
        _conexion.Open();

        Modelo = modelo ?? new ClienteDeChatGuionado(_ => null);
        var valores = configuracion ?? (habilitada
            // La bandera de redacción va explícita: en producción está apagada por omisión (ADR 0008).
            ? new Dictionary<string, string?> { ["OLLAMA_ENDPOINT"] = "http://modelo.invalid", ["OLLAMA_MODEL"] = "doble", [Habilitacion.ClaveRedaccion] = redaccion ? "true" : "false" }
            : new Dictionary<string, string?>());
        var config = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging(l => l.AddProvider(NullLoggerProvider.Instance));
        servicios.AddInfrastructure(cadena);
        servicios.AddApplication();
        servicios.AddAI(config, contenidoEnTrazas: false);
        // El proveedor real se reemplaza por el doble: el resto de la composición es la de producción.
        servicios.RemoveAll<IChatClient>();
        servicios.AddSingleton<IChatClient>(Modelo);
        if (Habilitacion.Evaluar(config).Motivo == MotivoDeHabilitacion.Habilitada)
        {
            servicios.RemoveAll<IRedactorDeAyuda>();
            servicios.RemoveAll<IExtractorDeLocalidad>();
            servicios.AddSingleton<IRedactorDeAyuda, RedactorConModelo>();
            servicios.AddSingleton<IExtractorDeLocalidad, ExtractorDeLocalidad>();
        }
        Servicios = servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        DatabaseInitializer.Initialize(Servicios);
    }

    /// <summary>Un alcance con su sesión: equivale a un circuito de Blazor de un navegador.</summary>
    public IServiceScope Sesion(string id)
    {
        var alcance = Servicios.CreateScope();
        alcance.ServiceProvider.GetRequiredService<SessionContext>().Set(id);
        return alcance;
    }

    public async Task<RespuestaDelAsistente> PreguntarAsync(string sesion, string pregunta, string? ruta = null)
    {
        using var alcance = Sesion(sesion);
        return await alcance.ServiceProvider.GetRequiredService<AsistenteService>().ResponderAsync(pregunta, ruta);
    }

    public async Task SembrarEncuestasAsync(string sesion, IEnumerable<RespuestaDeEncuesta> respuestas)
    {
        using var alcance = Sesion(sesion);
        // La siembra de localidades de la sesión corre igual que en producción.
        await alcance.ServiceProvider.GetRequiredService<SessionSeeder>().EnsureSeededAsync();
        var factory = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        foreach (var r in respuestas)
        {
            r.Id = 0;
            r.SessionId = sesion;
            context.Encuestas.Add(r);
        }
        await context.SaveChangesAsync();
    }

    public async Task VaciarLocalidadesAsync(string sesion)
    {
        using var alcance = Sesion(sesion);
        await alcance.ServiceProvider.GetRequiredService<SessionSeeder>().EnsureSeededAsync();
        var factory = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Localidades.Where(l => l.SessionId == sesion).ExecuteDeleteAsync();
    }

    public async Task<int> AgregarLocalidadAsync(string sesion, string nombre, string provincia, string cp, int habitantes)
    {
        using var alcance = Sesion(sesion);
        await alcance.ServiceProvider.GetRequiredService<SessionSeeder>().EnsureSeededAsync();
        var factory = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        var l = new Localidad { SessionId = sesion, Nombre = nombre, Provincia = provincia, CodigoPostal = cp, Habitantes = habitantes };
        context.Localidades.Add(l);
        await context.SaveChangesAsync();
        return l.Id;
    }

    public void Dispose()
    {
        Servicios.Dispose();
        _conexion.Dispose();
    }
}
