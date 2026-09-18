using MovilidadUrbana.MAUI.Presentation.Abstractions;
using MovilidadUrbana.MAUI.Presentation.Localidades;

namespace MovilidadUrbana.MAUI.Services;

/// <summary>Navegación con Shell. La ruta del editor se registra en <see cref="AppShell"/>.</summary>
public sealed class ShellNavigationService : INavigationService
{
    public const string EditorRoute = "localidad";
    public const string LocalidadParameter = "localidad";

    public Task NavigateToLocalidadEditorAsync(LocalidadItem? localidad) =>
        Shell.Current.GoToAsync(EditorRoute, new ShellNavigationQueryParameters
        {
            // Siempre se pasa el parámetro: así el editor distingue el alta de la edición sin estado previo.
            [LocalidadParameter] = (object?)localidad ?? string.Empty
        });

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
