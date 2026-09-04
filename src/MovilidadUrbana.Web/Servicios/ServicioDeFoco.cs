using Microsoft.JSInterop;

namespace MovilidadUrbana.Web.Servicios;

/// <inheritdoc cref="IServicioDeFoco" />
public sealed class ServicioDeFoco(IJSRuntime js) : IServicioDeFoco
{
    public Task AlContenidoPrincipalAsync() =>
        js.InvokeVoidAsync("mqFoco.alContenidoPrincipal").AsTask();
}
