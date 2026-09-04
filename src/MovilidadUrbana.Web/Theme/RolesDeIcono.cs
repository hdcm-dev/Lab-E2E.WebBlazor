namespace MovilidadUrbana.Web.Theme;

/// <summary>
/// Tamaño del ícono según el rol que cumple en la superficie, tomado de §6.1 del catálogo de
/// diseño. El tamaño no se escribe en la vista: se nombra por su rol.
/// </summary>
public static class RolesDeIcono
{
    /// <summary>Identidad del producto y navegación principal.</summary>
    public const int Navegacion = 24;

    /// <summary>Ícono de tarjeta de acceso.</summary>
    public const int Tarjeta = 20;

    /// <summary>Ícono en línea con el texto y dentro de un botón.</summary>
    public const int Inline = 16;

    /// <summary>Ícono en las acciones de una fila de la grilla.</summary>
    public const int Fila = 15;
}
