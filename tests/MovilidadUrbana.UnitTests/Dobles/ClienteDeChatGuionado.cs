using Microsoft.Extensions.AI;

namespace MovilidadUrbana.UnitTests.Dobles;

/// <summary>
/// Doble de <see cref="IChatClient" />: responde lo que se le guiona y registra todo lo que se
/// le envió, mensajes y opciones, para que las pruebas afirmen qué viajó al modelo.
/// </summary>
public sealed class ClienteDeChatGuionado : IChatClient
{
    private readonly Func<IReadOnlyList<ChatMessage>, string?> _guion;

    public ClienteDeChatGuionado(Func<IReadOnlyList<ChatMessage>, string?> guion) => _guion = guion;

    public ClienteDeChatGuionado(string respuesta) : this(_ => respuesta) { }

    public List<(IReadOnlyList<ChatMessage> Mensajes, ChatOptions? Opciones)> Pedidos { get; } = [];

    public TimeSpan Demora { get; set; } = TimeSpan.Zero;

    public bool Falla { get; set; }

    public string TodoLoEnviado() =>
        string.Join("\n---\n", Pedidos.SelectMany(p => p.Mensajes.Where(m => m.Role != ChatRole.System).Select(m => m.Text)));

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var lista = messages.ToList();
        lock (Pedidos) Pedidos.Add((lista, options));
        if (Demora > TimeSpan.Zero) await Task.Delay(Demora, cancellationToken);
        if (Falla) throw new HttpRequestException("sin servidor");
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, _guion(lista) ?? string.Empty));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
