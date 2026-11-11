using System.Text.RegularExpressions;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Quita del texto del modelo todo enlace y toda ruta (REQ-IA-27): un enlace Markdown queda como
/// su texto, una URL o una ruta absoluta desaparecen. Los destinos salen del catálogo, nunca del texto.
/// </summary>
public static partial class LimpiadorDeUrls
{
    public static string Limpiar(string texto)
    {
        var sinEnlaces = EnlaceMarkdown().Replace(texto, m => m.Groups[1].Value);
        var sinUrls = Url().Replace(sinEnlaces, string.Empty);
        var sinRutas = Ruta().Replace(sinUrls, "$1");
        var sinHtml = Html().Replace(sinRutas, string.Empty);
        return Espacios().Replace(sinHtml, " ").Trim();
    }

    [GeneratedRegex(@"\[([^\]]*)\]\(([^)]*)\)")]
    private static partial Regex EnlaceMarkdown();

    [GeneratedRegex(@"(?i)\b(?:https?|ftp|file|javascript|data):[^\s)]*|\bwww\.[^\s)]+")]
    private static partial Regex Url();

    [GeneratedRegex(@"(^|\s)/[A-Za-z0-9_\-/?=&.]+")]
    private static partial Regex Ruta();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Html();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex Espacios();
}
