using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Application.Encuestas;
using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Application.Resultados;

namespace MovilidadUrbana.Web.Application;

/// <summary>Registra los casos de uso. Lo invoca cada punto de composición: la web y la API.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LocalidadService>();
        services.AddScoped<EncuestaService>();
        services.AddScoped<ResultadosDeEncuestaService>();

        // El asistente: catálogo y corpus son datos inmutables (singleton); las funciones y el
        // servicio son del circuito, cerrados sobre la sesión que puso el código.
        services.AddSingleton(_ => new CatalogoDeDestinos());
        services.AddSingleton(_ => new CorpusDeAyuda());
        services.AddSingleton<RecuperadorDeAyuda>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<FuncionesDeLectura>();
        services.AddScoped<AsistenteService>();
        return services;
    }
}
