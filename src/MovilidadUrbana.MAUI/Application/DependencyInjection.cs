using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.MAUI.Application.Encuestas;
using MovilidadUrbana.MAUI.Application.Localidades;

namespace MovilidadUrbana.MAUI.Application;

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
