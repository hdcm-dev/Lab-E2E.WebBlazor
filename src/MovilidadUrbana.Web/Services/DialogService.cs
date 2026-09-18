namespace MovilidadUrbana.Web.Services;

/// <summary>
/// Realización del servicio de diálogos sobre el circuito: el estado vive en el ámbito del
/// circuito y no en almacenamiento del navegador.
/// </summary>
public sealed class DialogService : IDialogService
{
    private TaskCompletionSource<bool>? _pending;

    public ConfirmationRequest? Current { get; private set; }

    public event Action? OnChange;

    public Task<bool> ConfirmAsync(ConfirmationRequest request)
    {
        // Una confirmación nueva cancela la anterior: dejarla colgada dejaría al llamador previo
        // esperando para siempre.
        _pending?.TrySetResult(false);

        Current = request;
        _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnChange?.Invoke();

        return _pending.Task;
    }

    public void Resolve(bool confirmado)
    {
        var espera = _pending;
        Current = null;
        _pending = null;
        OnChange?.Invoke();
        espera?.TrySetResult(confirmado);
    }
}
