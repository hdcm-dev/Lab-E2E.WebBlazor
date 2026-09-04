namespace MovilidadUrbana.Web.Servicios;

/// <summary>
/// Identidad de la versión desplegada. La resuelve el punto de composición del host y la exhibe
/// el sello de versión: el componente no la compone ni la lee de una constante de la vista, para
/// que la cadena que ve la persona sea la misma que se registra en el diagnóstico.
/// </summary>
public interface IIdentidadDeVersion
{
    /// <summary>Versión legible, sin los metadatos de construcción.</summary>
    string VersionLegible { get; }

    /// <summary>La construcción es preliminar (versión con etiqueta de prelanzamiento).</summary>
    bool EsPreliminar { get; }

    /// <summary>No se pudo atar el binario a una construcción concreta.</summary>
    bool OrigenIndeterminado { get; }
}
