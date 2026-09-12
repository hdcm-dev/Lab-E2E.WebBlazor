namespace WebBlazor.E2E.Base.Login.Theme;

/// <summary>
/// Tonos semánticos del catálogo. Cada uno es un par texto + tint: el color nunca
/// es el único canal, así que el componente que lo usa imprime también su texto.
/// </summary>
public enum Tono
{
    /// <summary>Neutro o inactivo.</summary>
    Neutro,

    /// <summary>Éxito o activo.</summary>
    Exito,

    /// <summary>Atención.</summary>
    Atencion,

    /// <summary>Error o acción destructiva.</summary>
    Peligro,

    /// <summary>Informativo.</summary>
    Info
}
