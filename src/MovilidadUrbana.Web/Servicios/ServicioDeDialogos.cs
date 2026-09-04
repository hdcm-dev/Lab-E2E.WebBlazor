namespace MovilidadUrbana.Web.Servicios;

/// <summary>
/// Realización del servicio de diálogos sobre el circuito: el estado vive en el ámbito del
/// circuito y no en almacenamiento del navegador.
/// </summary>
public sealed class ServicioDeDialogos : IServicioDeDialogos
{
    private TaskCompletionSource<bool>? _espera;

    public PedidoDeConfirmacion? Vigente { get; private set; }

    public event Action? AlCambiar;

    public Task<bool> ConfirmarAsync(PedidoDeConfirmacion pedido)
    {
        // Una confirmación nueva cancela la anterior: dejarla colgada dejaría al llamador previo
        // esperando para siempre.
        _espera?.TrySetResult(false);

        Vigente = pedido;
        _espera = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        AlCambiar?.Invoke();

        return _espera.Task;
    }

    public void Resolver(bool confirmado)
    {
        var espera = _espera;
        Vigente = null;
        _espera = null;
        AlCambiar?.Invoke();
        espera?.TrySetResult(confirmado);
    }
}
