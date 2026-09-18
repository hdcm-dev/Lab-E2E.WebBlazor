using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.Web.Application.Encuestas;
using MovilidadUrbana.Web.Application.Localidades;

namespace MovilidadUrbana.Web.Application;

/// <summary>Registra los casos de uso. Lo invoca cada punto de composición: la web y la API.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LocalidadService>();
        services.AddScoped<EncuestaService>();
        return services;
    }
}
