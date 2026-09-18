namespace MovilidadUrbana.Web.Services;

/// <summary>
/// Traslado del foco a las zonas del shell que no pertenecen al componente que lo pide. Existe
/// para que ninguna página invoque interoperabilidad por su cuenta.
/// </summary>
public interface IFocusService
{
    /// <summary>Manda el foco al contenido principal del shell.</summary>
    Task FocusMainContentAsync();
}
