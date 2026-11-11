using Microsoft.Extensions.AI;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Opciones de las llamadas al modelo, en un solo lugar: temperatura 0 y semilla 7 salvo que la configuración diga otra cosa (<c>Asistente:Temperatura</c>, <c>Asistente:Semilla</c>), contexto
/// por variable y ninguna herramienta. Los valores que no son secretos (tiempo máximo) vienen de
/// <c>appsettings.json</c>, sección <c>Asistente</c>.
/// </summary>
public sealed class OpcionesDelModelo
{
    public TimeSpan TiempoMaximo { get; init; } = TimeSpan.FromSeconds(20);

    public int? NumCtx { get; init; }

    /// <summary>Semilla del muestreo; 7 en producción. La batería la varía entre corridas para medir estabilidad.</summary>
    public int Semilla { get; init; } = 7;

    /// <summary>Temperatura; 0 en producción. Con 0 la semilla no cambia nada: la batería sube las dos.</summary>
    public float Temperatura { get; init; }

    public ChatOptions ParaRedactar() => Base();

    public ChatOptions ParaExtraer()
    {
        var o = Base();
        o.ResponseFormat = ChatResponseFormat.Json;
        return o;
    }

    private ChatOptions Base()
    {
        var o = new ChatOptions { Temperature = Temperatura, Seed = Semilla, Tools = [] };
        // Sin razonamiento visible: el redactor y el extractor no lo necesitan, y en un modelo que
        // razona por defecto el tiempo se iría antes de la primera palabra útil. Es una opción propia
        // del proveedor, así que se fija acá, en el único lugar que lo conoce.
        o.RawRepresentationFactory = _ => new OllamaSharp.Models.Chat.ChatRequest { Think = false };
        if (NumCtx is int n) (o.AdditionalProperties ??= [])["num_ctx"] = n;
        return o;
    }
}
