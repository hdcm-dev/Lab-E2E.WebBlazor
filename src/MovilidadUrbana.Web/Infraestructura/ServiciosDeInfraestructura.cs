using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Infraestructura.Persistencia;
using MovilidadUrbana.Web.Infraestructura.Sesiones;

namespace MovilidadUrbana.Web.Infraestructura;

/// <summary>
/// Registra la persistencia y la sesión. La cadena de conexión la decide quien compone —web o
/// API—; el cómo se implementa cada abstracción queda acá.
/// </summary>
public static class ServiciosDeInfraestructura
{
    public const string CadenaDeConexionPorDefecto = "Data Source=datos/movilidad.db;Default Timeout=30";

    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios, string cadenaDeConexion)
    {
        // Factory y no contexto de ámbito: un circuito de Blazor vive más que una petición.
        servicios.AddDbContextFactory<ContextoDeDatos>(opciones => opciones.UseSqlite(cadenaDeConexion));

        // La misma instancia sirve a la implementación concreta y a la abstracción.
        servicios.AddScoped<ContextoDeSesion>();
        servicios.AddScoped<IContextoDeSesion>(sp => sp.GetRequiredService<ContextoDeSesion>());
        servicios.AddScoped<SembradorDeSesion>();
        servicios.AddScoped<IRepositorioDeLocalidades, RepositorioDeLocalidades>();
        servicios.AddScoped<IRepositorioDeEncuestas, RepositorioDeEncuestas>();
        return servicios;
    }
}
