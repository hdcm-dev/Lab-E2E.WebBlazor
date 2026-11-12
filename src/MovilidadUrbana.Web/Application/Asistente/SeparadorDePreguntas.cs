using System.Text.RegularExpressions;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>
/// Parte un mensaje en las preguntas que trae (marco §4.1), por código y sin modelo: corta en un
/// signo de pregunta seguido de otra, o en «y» / «además» seguidos de un interrogativo («qué»,
/// «cómo», «dónde», «cuántos», «cuál», «por qué», «para qué»). «En Corrientes y en Resistencia»
/// o una lista de datos no se parten: no hay un interrogativo después de la «y».
/// </summary>
public static partial class SeparadorDePreguntas
{
    [GeneratedRegex(@"\?\s*(?=¿?\s*\p{L})|,?\s+(?:y\s+además|y\s+tambi[eé]n|y|además)\s+(?=¿?\s*(?:qu[eé]|c[oó]mo|d[oó]nde|cu[aá]nt[oa]s?|cu[aá]l(?:es)?|por\s+qu[eé]|para\s+qu[eé])\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Corte();

    public static IReadOnlyList<string> Partir(string pregunta)
    {
        if (string.IsNullOrWhiteSpace(pregunta)) return [pregunta];
        try
        {
            var partes = Corte().Split(pregunta)
                .Select(p => p.Trim().TrimStart('¿').Trim().TrimEnd('?').Trim())
                .Where(p => p.Length > 0)
                .Select(p => $"¿{p}?")
                .ToList();
            return partes.Count > 1 ? partes : [pregunta];
        }
        catch (RegexMatchTimeoutException)
        {
            return [pregunta];
        }
    }
}
