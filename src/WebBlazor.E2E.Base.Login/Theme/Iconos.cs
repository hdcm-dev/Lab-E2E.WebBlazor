namespace WebBlazor.E2E.Base.Login.Theme;

/// <summary>
/// Diccionario de trazos SVG del producto. Cada constante es el interior de un
/// <c>&lt;svg&gt;</c> de grilla 24 y trazo 1.75, que el componente <c>Icono</c>
/// envuelve: hereda el color por <c>currentColor</c>, escala con el tamaño de su
/// rol y no depende de ninguna red.
/// </summary>
/// <remarks>
/// Un ícono nuevo se agrega acá y en ningún otro lado: un trazo escrito en línea
/// dentro de una superficie deja de ser reutilizable y se desalinea del resto.
/// </remarks>
public static class Iconos
{
    /// <summary>Marca del producto, para la identidad de los dos shells.</summary>
    public const string Marca =
        """<rect x="3" y="3" width="18" height="18" rx="4" /><path d="M8 12h8" /><path d="M12 8v8" />""";

    /// <summary>Inicio.</summary>
    public const string Inicio =
        """<path d="M5 12 12 5l7 7" /><path d="M7 10v9h10v-9" />""";

    /// <summary>Mensaje: la superficie que muestra una frase.</summary>
    public const string Mensaje =
        """<path d="M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H9l-5 4z" />""";

    /// <summary>Búsqueda.</summary>
    public const string Buscar =
        """<circle cx="10.5" cy="10.5" r="5.5" /><path d="M20 20l-5.2-5.2" />""";

    /// <summary>Abrir el detalle de una fila.</summary>
    public const string Abrir =
        """<path d="M5 12h13" /><path d="M13 7l5 5-5 5" />""";

    /// <summary>Baja de un registro.</summary>
    public const string Eliminar =
        """<path d="M4 7h16" /><path d="M9 7V5h6v2" /><path d="M6 7l1 12h10l1-12" />""";

    /// <summary>Alerta: acompaña a los estados de error y de indisponibilidad.</summary>
    public const string Alerta =
        """<path d="M12 4 2.5 20h19z" /><path d="M12 10v4" /><path d="M12 17h.01" />""";

    /// <summary>Colección sin elementos.</summary>
    public const string Vacio =
        """<path d="M4 13h4l2 3h4l2-3h4" /><path d="M6 5h12l2 8v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-5z" />""";

    /// <summary>Ingreso al sistema.</summary>
    public const string Ingresar =
        """<path d="M14 4h4a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-4" /><path d="M3 12h12" /><path d="M11 8l4 4-4 4" />""";

    /// <summary>Cierre de sesión.</summary>
    public const string Salir =
        """<path d="M10 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4" /><path d="M9 12h12" /><path d="M17 8l4 4-4 4" />""";

    /// <summary>Confirmación de una acción completada.</summary>
    public const string Exito =
        """<path d="M5 12.5 10 17 19 7" />""";
}
