using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.Web.Aplicacion.Encuestas;
using MovilidadUrbana.Web.Aplicacion.Localidades;

namespace MovilidadUrbana.Web.Aplicacion;

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
