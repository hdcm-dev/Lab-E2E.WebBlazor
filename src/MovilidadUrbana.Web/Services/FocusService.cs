using Microsoft.JSInterop;

namespace MovilidadUrbana.Web.Services;

/// <inheritdoc cref="IFocusService" />
public sealed class FocusService(IJSRuntime js) : IFocusService
{
    public Task FocusMainContentAsync() =>
        js.InvokeVoidAsync("mqFoco.alContenidoPrincipal").AsTask();
}
