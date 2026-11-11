using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovilidadUrbana.Web.Application.Asistente;
using OllamaSharp;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Composición de la capa de IA (ADR 0002): el proveedor se elige acá y sólo acá. Todo sale del
/// entorno —OLLAMA_ENDPOINT, OLLAMA_MODEL, OLLAMA_NUM_CTX—, sin valores por defecto en el código.
/// Si falta algo, la IA queda apagada con su motivo y el asistente responde con plantillas.
/// </summary>
public static class DependencyInjection
{
    public const string FuenteDeTrazas = "MovilidadUrbana.Asistente";

    public static IServiceCollection AddAI(this IServiceCollection services, IConfiguration configuracion, bool contenidoEnTrazas)
    {
        var habilitacion = Habilitacion.Evaluar(configuracion);
        services.AddSingleton(habilitacion);
        services.AddSingleton<IEstadoDelAsistente>(habilitacion);

        var segundos = int.TryParse(configuracion["Asistente:TiempoMaximoSegundos"], out var s) && s > 0 ? s : 20;
        services.AddSingleton(new OpcionesDelModelo { TiempoMaximo = TimeSpan.FromSeconds(segundos), NumCtx = habilitacion.NumCtx });
        services.AddSingleton<LimitadorDelModelo>();

        if (habilitacion.Motivo != MotivoDeHabilitacion.Habilitada)
        {
            services.AddSingleton<IRedactorDeAyuda, RedactorSinModelo>();
            services.AddSingleton<IExtractorDeLocalidad, ExtractorSinModelo>();
            return services;
        }

        services.AddSingleton<IChatClient>(sp =>
            new ChatClientBuilder(new OllamaApiClient(new Uri(habilitacion.Endpoint!), habilitacion.Modelo!))
                // Sin contenido en las trazas fuera de Development (REQ-IA-09).
                .UseOpenTelemetry(sp.GetService<ILoggerFactory>(), FuenteDeTrazas, c => c.EnableSensitiveData = contenidoEnTrazas)
                .Build(sp));
        services.AddSingleton<IRedactorDeAyuda, RedactorConModelo>();
        services.AddSingleton<IExtractorDeLocalidad, ExtractorDeLocalidad>();
        return services;
    }
}
