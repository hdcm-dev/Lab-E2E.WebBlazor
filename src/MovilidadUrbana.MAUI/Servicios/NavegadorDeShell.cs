using MovilidadUrbana.MAUI.Presentacion.Abstracciones;
using MovilidadUrbana.MAUI.Presentacion.Localidades;

namespace MovilidadUrbana.MAUI.Servicios;

/// <summary>Navegación con Shell. La ruta del editor se registra en <see cref="AppShell"/>.</summary>
public sealed class NavegadorDeShell : INavegador
{
    public const string RutaDelEditor = "localidad";
    public const string ParametroDeLocalidad = "localidad";

    public Task IrAlEditorDeLocalidadAsync(LocalidadItem? localidad) =>
        Shell.Current.GoToAsync(RutaDelEditor, new ShellNavigationQueryParameters
        {
            // Siempre se pasa el parámetro: así el editor distingue el alta de la edición sin estado previo.
            [ParametroDeLocalidad] = (object?)localidad ?? string.Empty
        });

    public Task VolverAsync() => Shell.Current.GoToAsync("..");
}
