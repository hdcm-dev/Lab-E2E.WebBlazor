using Microsoft.Extensions.AI;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Opciones de las llamadas al modelo, en un solo lugar: temperatura 0, semilla fija, contexto
/// por variable y ninguna herramienta. Los valores que no son secretos (tiempo máximo) vienen de
/// <c>appsettings.json</c>, sección <c>Asistente</c>.
/// </summary>
public sealed class OpcionesDelModelo
{
    public TimeSpan TiempoMaximo { get; init; } = TimeSpan.FromSeconds(20);

    public int? NumCtx { get; init; }

    public ChatOptions ParaRedactar() => Base();

    public ChatOptions ParaExtraer()
    {
        var o = Base();
        o.ResponseFormat = ChatResponseFormat.Json;
        return o;
    }

    private ChatOptions Base()
    {
        var o = new ChatOptions { Temperature = 0, Seed = 7, Tools = [] };
        // Sin razonamiento visible: el redactor y el extractor no lo necesitan, y en un modelo que
        // razona por defecto el tiempo se iría antes de la primera palabra útil. Es una opción propia
        // del proveedor, así que se fija acá, en el único lugar que lo conoce.
        o.RawRepresentationFactory = _ => new OllamaSharp.Models.Chat.ChatRequest { Think = false };
        if (NumCtx is int n) (o.AdditionalProperties ??= [])["num_ctx"] = n;
        return o;
    }
}
