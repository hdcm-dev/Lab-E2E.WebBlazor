using System.Text.RegularExpressions;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>
/// Forma de las citas en un texto ya validado (K5 CD-8). El validador exige que cada oración del
/// modelo cierre con su cita; en pantalla, con una sola fuente, alcanza una sola cita al final. Y
/// un texto que, quitadas las citas, es la plantilla no es una redacción: no cuenta como tal.
/// </summary>
public static partial class Citas
{
    /// <summary>
    /// Un solo espacio antes de cada «[n]» y, si hay una sola fuente, una sola cita: la última. El
    /// texto sin citas queda igual.
    /// </summary>
    public static string Normalizar(string texto, int fuentes)
    {
        var t = EspacioAntes().Replace(texto, " [$1]");
        if (fuentes != 1) return t.Trim();
        var citas = CitaConEspacio().Matches(t);
        if (citas.Count <= 1) return t.Trim();
        var ultima = citas[^1];
        var sinLasPrimeras = CitaConEspacio().Replace(t[..ultima.Index], string.Empty);
        return (sinLasPrimeras + t[ultima.Index..]).Trim();
    }

    /// <summary>
    /// El texto sin citas ni negritas, con los espacios colapsados y sin el punto final: lo que lee
    /// una persona (en la ronda 4, «… 6 de 20 encuestas [1]» sin el punto contaba como redactada).
    /// </summary>
    public static string Sin(string texto) =>
        Espacios().Replace(CitaConEspacio().Replace(texto, string.Empty).Replace("**", string.Empty, StringComparison.Ordinal), " ").Trim().TrimEnd('.').TrimEnd();

    /// <summary>Si la redactada sólo le suma citas (o espacios) a la plantilla.</summary>
    public static bool SoloSumaCitas(string redactada, string plantilla) =>
        string.Equals(Sin(redactada), Sin(plantilla), StringComparison.Ordinal);

    [GeneratedRegex(@"\s*\[(\d+)\]")]
    private static partial Regex EspacioAntes();

    [GeneratedRegex(@"\s*\[\d+\]")]
    private static partial Regex CitaConEspacio();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
