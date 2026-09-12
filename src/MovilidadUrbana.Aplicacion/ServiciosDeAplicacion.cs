using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.Aplicacion.Encuestas;
using MovilidadUrbana.Aplicacion.Localidades;

namespace MovilidadUrbana.Aplicacion;

/// <summary>Registra los casos de uso. Lo invoca cada punto de composición: la web y la API.</summary>
public static class ServiciosDeAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<ServicioDeLocalidades>();
        servicios.AddScoped<ServicioDeEncuestas>();
        return servicios;
    }
}
