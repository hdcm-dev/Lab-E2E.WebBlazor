namespace MovilidadUrbana.Web.Dominio;

/// <summary>Valores fijos que comparten las pantallas y las reglas.</summary>
public static class Catalogos
{
    public static readonly IReadOnlyList<string> Provincias =
    [
        "Buenos Aires",
        "Chaco",
        "Córdoba",
        "Corrientes",
        "Entre Ríos",
        "Mendoza",
        "Santa Fe"
    ];

    /// <summary>Medios de transporte de la encuesta: clave persistida y etiqueta que se muestra.</summary>
    public static readonly IReadOnlyList<(string Clave, string Etiqueta)> Medios =
    [
        ("colectivo", "Colectivo"),
        ("auto", "Auto particular"),
        ("bicicleta", "Bicicleta"),
        ("moto", "Moto"),
        ("caminata", "A pie"),
        ("tren", "Tren")
    ];

    public static readonly IReadOnlyList<(string Clave, string Etiqueta)> Frecuencias =
    [
        ("diaria", "Todos los días"),
        ("semanal", "Algunos días por semana"),
        ("ocasional", "Ocasionalmente")
    ];

    public static readonly IReadOnlyList<(string Clave, string Etiqueta)> Motivos =
    [
        ("trabajo", "Trabajo"),
        ("estudio", "Estudio"),
        ("salud", "Salud"),
        ("otros", "Otros")
    ];

    public static string EtiquetaDeMedio(string clave) =>
        Medios.FirstOrDefault(m => m.Clave == clave).Etiqueta ?? clave;

    public static string EtiquetaDeFrecuencia(string clave) =>
        Frecuencias.FirstOrDefault(f => f.Clave == clave).Etiqueta ?? clave;

    public static string EtiquetaDeMotivo(string clave) =>
        Motivos.FirstOrDefault(m => m.Clave == clave).Etiqueta ?? clave;
}
