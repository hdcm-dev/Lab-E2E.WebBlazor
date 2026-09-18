using MovilidadUrbana.MAUI.Presentation.Localidades;

namespace MovilidadUrbana.MAUI.Presentation.Abstractions;

/// <summary>
/// Navegación entre pantallas, vista desde el ViewModel. La implementa la aplicación con Shell; en
/// las pruebas, un doble que registra lo que se pidió.
/// </summary>
public interface INavigationService
{
    /// <summary>Abre el editor: vacío para dar de alta, o con la localidad para modificarla.</summary>
    Task NavigateToLocalidadEditorAsync(LocalidadItem? localidad);

    /// <summary>Vuelve a la pantalla anterior.</summary>
    Task GoBackAsync();
}
