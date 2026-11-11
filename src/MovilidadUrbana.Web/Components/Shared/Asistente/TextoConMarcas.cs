using System.Text.RegularExpressions;

namespace MovilidadUrbana.Web.Components.Shared.Asistente;

/// <summary>
/// La ayuda y el modelo nombran los botones en negrita con <c>**…**</c>. El panel no muestra esas
/// marcas: un par cerrado se vuelve un segmento en negrita, y un asterisco doble suelto se quita.
/// No hay HTML de por medio: cada segmento se pinta como texto, así que nada del modelo llega
/// como marcado.
/// </summary>
public static partial class TextoConMarcas
{
    public sealed record Segmento(string Texto, bool Negrita);

    public static IReadOnlyList<Segmento> Segmentar(string? texto)
    {
        var lista = new List<Segmento>();
        if (string.IsNullOrEmpty(texto)) return lista;
        var desde = 0;
        foreach (Match m in Par().Matches(texto))
        {
            if (m.Index > desde) lista.Add(new Segmento(Limpiar(texto[desde..m.Index]), false));
            lista.Add(new Segmento(Limpiar(m.Groups[1].Value), true));
            desde = m.Index + m.Length;
        }
        if (desde < texto.Length) lista.Add(new Segmento(Limpiar(texto[desde..]), false));
        return lista.Where(s => s.Texto.Length > 0).ToList();
    }

    /// <summary>El texto sin marcas, para lectores y comparaciones.</summary>
    public static string Plano(string? texto) => string.Concat(Segmentar(texto).Select(s => s.Texto));

    private static string Limpiar(string s) => s.Replace("**", string.Empty, StringComparison.Ordinal).Replace("__", string.Empty, StringComparison.Ordinal);

    [GeneratedRegex(@"\*\*(?!\s)(.+?)(?<!\s)\*\*")]
    private static partial Regex Par();
}
