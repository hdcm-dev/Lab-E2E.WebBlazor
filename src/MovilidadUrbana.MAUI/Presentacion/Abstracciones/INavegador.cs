using MovilidadUrbana.MAUI.Presentacion.Localidades;

namespace MovilidadUrbana.MAUI.Presentacion.Abstracciones;

/// <summary>
/// Navegación entre pantallas, vista desde el ViewModel. La implementa la aplicación con Shell; en
/// las pruebas, un doble que registra lo que se pidió.
/// </summary>
public interface INavegador
{
    /// <summary>Abre el editor: vacío para dar de alta, o con la localidad para modificarla.</summary>
    Task IrAlEditorDeLocalidadAsync(LocalidadItem? localidad);

    /// <summary>Vuelve a la pantalla anterior.</summary>
    Task VolverAsync();
}
