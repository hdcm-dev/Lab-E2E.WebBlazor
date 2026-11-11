using Microsoft.Extensions.Configuration;
using MovilidadUrbana.Web.Application.Asistente;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Habilitación de la capa de IA con motivo exacto (REQ-IA-29). Se evalúa una vez al arrancar,
/// sólo con los nombres de las claves: el valor del endpoint o del modelo nunca se registra ni se
/// muestra. Sin valores por defecto: si falta una clave, la IA queda apagada y lo dice.
/// </summary>
public sealed class Habilitacion : IEstadoDelAsistente
{
    public const string ClaveEndpoint = "OLLAMA_ENDPOINT";
    public const string ClaveModelo = "OLLAMA_MODEL";
    public const string ClaveEmbedEndpoint = "OLLAMA_EMBED_ENDPOINT";
    public const string ClaveEmbedModelo = "OLLAMA_EMBED_MODEL";
    public const string ClaveNumCtx = "OLLAMA_NUM_CTX";
    public const string ClaveHabilitado = "Asistente:Habilitado";

    public static readonly IReadOnlySet<string> ClavesConocidas =
        new HashSet<string>([ClaveEndpoint, ClaveModelo, ClaveEmbedEndpoint, ClaveEmbedModelo, ClaveNumCtx], StringComparer.Ordinal);

    public bool PanelVisible { get; init; }

    public MotivoDeHabilitacion Motivo { get; init; }

    public string? Clave { get; init; }

    public bool SoloLexico { get; init; } = true;

    public string? Endpoint { get; init; }

    public string? Modelo { get; init; }

    public int? NumCtx { get; init; }

    public static Habilitacion Evaluar(IConfiguration configuracion)
    {
        var habilitado = !string.Equals(configuracion[ClaveHabilitado], "false", StringComparison.OrdinalIgnoreCase);
        if (!habilitado) return new Habilitacion { PanelVisible = false, Motivo = MotivoDeHabilitacion.ApagadaPorParametro, Clave = "Asistente__Habilitado" };

        // Una variante mal escrita («OLLAMA_MODELO») apaga y se nombra: es mejor que ignorarla en silencio.
        var desconocida = configuracion.AsEnumerable()
            .Select(par => par.Key)
            .Where(clave => !clave.Contains(':') && clave.StartsWith("OLLAMA_", StringComparison.OrdinalIgnoreCase) && !ClavesConocidas.Contains(clave))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
        if (desconocida is not null) return new Habilitacion { PanelVisible = true, Motivo = MotivoDeHabilitacion.ClaveDesconocida, Clave = desconocida };

        var endpoint = configuracion[ClaveEndpoint];
        if (string.IsNullOrWhiteSpace(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            return new Habilitacion { PanelVisible = true, Motivo = MotivoDeHabilitacion.FaltaEndpoint, Clave = ClaveEndpoint };

        var modelo = configuracion[ClaveModelo];
        if (string.IsNullOrWhiteSpace(modelo))
            return new Habilitacion { PanelVisible = true, Motivo = MotivoDeHabilitacion.FaltaModelo, Clave = ClaveModelo };

        var numCtx = int.TryParse(configuracion[ClaveNumCtx], out var n) && n > 0 ? n : (int?)null;
        return new Habilitacion
        {
            PanelVisible = true,
            Motivo = MotivoDeHabilitacion.Habilitada,
            Endpoint = endpoint,
            Modelo = modelo,
            NumCtx = numCtx,
            SoloLexico = true
        };
    }
}
