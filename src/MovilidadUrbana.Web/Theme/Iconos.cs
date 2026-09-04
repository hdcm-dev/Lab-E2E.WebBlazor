namespace MovilidadUrbana.Web.Theme;

/// <summary>
/// Diccionario de íconos del producto: el trazo interior de cada SVG, sin el elemento
/// <c>&lt;svg&gt;</c> que lo envuelve —ese lo aporta <c>Icono.razor</c>, que fija la grilla de 24,
/// el trazo de 1.75 y el tratamiento ARIA—.
///
/// Set único, de trazo coherente y sobre la misma grilla, según §6.1 del catálogo de diseño. El
/// color no se declara acá: lo hereda del contenedor por <c>currentColor</c>, que es lo que hace
/// que el token de color siga siendo el que manda.
/// </summary>
public static class Iconos
{
    /// <summary>Identidad del producto: la movilidad urbana, dibujada como un colectivo.</summary>
    public const string Marca =
        """<rect x="4" y="4" width="16" height="12" rx="2" /><path d="M4 11h16" /><path d="M7 16v2" /><path d="M17 16v2" /><circle cx="8" cy="19" r="1.5" /><circle cx="16" cy="19" r="1.5" />""";

    public const string Inicio =
        """<path d="M4 11 12 4l8 7v8a1 1 0 0 1-1 1h-4v-6H9v6H5a1 1 0 0 1-1-1z" />""";

    public const string Localidad =
        """<path d="M12 21s-6-5.686-6-10a6 6 0 1 1 12 0c0 4.314-6 10-6 10z" /><circle cx="12" cy="10" r="2" />""";

    public const string Encuesta =
        """<path d="M9 4h6v2H9z" /><path d="M8 5H6a1 1 0 0 0-1 1v13a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1h-2" /><path d="M9 11h6" /><path d="M9 15h4" />""";

    public const string Editar =
        """<path d="M4 20h4L18 10l-4-4L4 16z" /><path d="M14 6l4 4" />""";

    public const string Eliminar =
        """<path d="M4 7h16" /><path d="M9 7V5h6v2" /><path d="M6 7l1 13h10l1-13" />""";

    public const string Buscar =
        """<circle cx="11" cy="11" r="6" /><path d="M15.5 15.5 20 20" />""";

    public const string Alerta =
        """<path d="M12 4l9 16H3z" /><path d="M12 10v4" /><path d="M12 17h.01" />""";

    public const string Vacio =
        """<path d="M5 13 7 5h10l2 8v5a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1z" /><path d="M5 13h4l1 3h4l1-3h4" />""";

    public const string Tilde =
        """<path d="M5 13l4 4L19 7" />""";

    public const string Anterior =
        """<path d="M14 6l-6 6 6 6" />""";

    public const string Siguiente =
        """<path d="M10 6l6 6-6 6" />""";
}
