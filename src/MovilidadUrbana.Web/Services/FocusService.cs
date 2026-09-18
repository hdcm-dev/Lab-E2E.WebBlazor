using Microsoft.JSInterop;

namespace MovilidadUrbana.Web.Services;

/// <inheritdoc cref="IServicioDeFoco" />
public sealed class FocusService(IJSRuntime js) : IFocusService
{
    public Task FocusMainContentAsync() =>
        js.InvokeVoidAsync("mqFoco.alContenidoPrincipal").AsTask();
}
