namespace MovilidadUrbana.Web.Domain.Asistente;

/// <summary>
/// Sinónimos versionados como datos (F-3): se aplican antes de clasificar y antes de buscar. La
/// frontera crece agregando filas acá, nunca tocando el prompt (F-6).
/// </summary>
public static class Sinonimos
{
    public const string Version = "sinonimos.v1";

    /// <summary>Clave canónica reservada para los medios que la encuesta no tiene.</summary>
    public const string FueraDeCatalogo = "fuera_de_catalogo";

    /// <summary>Forma de la persona (normalizada) → clave canónica de <see cref="Catalogos" /> o del glosario.</summary>
    public static readonly IReadOnlyList<(string Forma, string Canonico)> Tabla =
    [
        ("bondi", "colectivo"), ("bondis", "colectivo"), ("micro", "colectivo"), ("micros", "colectivo"),
        ("omnibus", "colectivo"), ("colectivos", "colectivo"), ("colectivo", "colectivo"),
        ("bici", "bicicleta"), ("bicis", "bicicleta"), ("bicicletas", "bicicleta"), ("bicicleta", "bicicleta"),
        ("caminando", "caminata"), ("a pata", "caminata"), ("a pie", "caminata"), ("caminata", "caminata"), ("camina", "caminata"),
        ("coche", "auto"), ("auto propio", "auto"), ("autos", "auto"), ("auto", "auto"),
        ("motos", "moto"), ("moto", "moto"), ("trenes", "tren"), ("tren", "tren"),
        ("facu", "estudio"), ("facultad", "estudio"), ("escuela", "estudio"), ("colegio", "estudio"), ("estudiar", "estudio"),
        ("laburo", "trabajo"), ("laburar", "trabajo"), ("trabajar", "trabajo"),
        ("medico", "salud"), ("hospital", "salud"), ("turno medico", "salud"),
        ("todos los dias", "diaria"),
        ("subte", FueraDeCatalogo), ("subterraneo", FueraDeCatalogo), ("avion", FueraDeCatalogo),
        ("taxi", FueraDeCatalogo), ("remis", FueraDeCatalogo), ("uber", FueraDeCatalogo), ("barco", FueraDeCatalogo),
        ("cp", "codigo postal"), ("postal", "codigo postal")
    ];

    /// <summary>Formas coloquiales que se explican la primera vez que aparecen (K5 §7.3).</summary>
    private static readonly HashSet<string> Coloquiales = ["bondi", "bondis", "micro", "micros", "bici", "bicis", "laburo", "facu", "coche", "a pata"];

    /// <summary>
    /// Aplica la tabla sobre una pregunta ya normalizada. Devuelve el texto con las formas
    /// reemplazadas por su canónico, los canónicos encontrados y los fuera de catálogo con su
    /// forma original, para nombrarlos en la respuesta.
    /// </summary>
    public static PreguntaNormalizada Aplicar(string pregunta)
    {
        var texto = " " + Texto.Normalizar(pregunta) + " ";
        var canonicos = new List<string>();
        var fuera = new List<string>();
        var explicadas = new List<(string Forma, string Canonico)>();

        foreach (var (forma, canonico) in Tabla.OrderByDescending(fila => fila.Forma.Length))
        {
            var buscado = " " + forma + " ";
            if (!texto.Contains(buscado, StringComparison.Ordinal)) continue;

            if (canonico == FueraDeCatalogo)
            {
                fuera.Add(forma);
                continue;
            }

            if (!canonicos.Contains(canonico)) canonicos.Add(canonico);
            if (Coloquiales.Contains(forma)) explicadas.Add((forma, canonico));
            texto = texto.Replace(buscado, " " + canonico + " ", StringComparison.Ordinal);
        }

        return new PreguntaNormalizada(texto.Trim(), canonicos, fuera, explicadas);
    }
}

/// <summary>Una pregunta después de aplicar <see cref="Sinonimos" />.</summary>
public sealed record PreguntaNormalizada(
    string Texto,
    IReadOnlyList<string> Canonicos,
    IReadOnlyList<string> FueraDeCatalogo,
    IReadOnlyList<(string Forma, string Canonico)> Explicadas)
{
    public bool Tiene(string palabraOFrase) =>
        (" " + Texto + " ").Contains(" " + palabraOFrase + " ", StringComparison.Ordinal);

    public bool TieneAlguna(params string[] opciones) => opciones.Any(Tiene);

    public bool Empieza(params string[] opciones) => opciones.Any(o => Texto.StartsWith(o + " ", StringComparison.Ordinal) || Texto == o);
}
