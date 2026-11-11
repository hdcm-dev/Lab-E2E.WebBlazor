using System.Globalization;
using System.Text;

namespace MovilidadUrbana.Web.Domain.Asistente;

/// <summary>
/// Normalización de texto que comparten los sinónimos, el glosario, el enrutador y la búsqueda
/// en la ayuda: minúsculas, sin tildes, sin signos y con los espacios colapsados. Es una función
/// pura: la misma pregunta da siempre la misma forma normalizada.
/// </summary>
public static class Texto
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
            if (categoria == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(caracter) || caracter == '/' || caracter == '%') resultado.Append(char.ToLowerInvariant(caracter));
            else if (caracter is '.' or ',' && resultado.Length > 0 && char.IsDigit(resultado[^1])) resultado.Append(caracter);
            else resultado.Append(' ');
        }

        return string.Join(' ', resultado.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(palabra => palabra.Trim('.', ',')));
    }

    /// <summary>Palabras de la forma normalizada.</summary>
    public static IReadOnlyList<string> Palabras(string? texto) =>
        Normalizar(texto).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Raíz de seis letras: «localidades» y «localidad» comparten «locali».</summary>
    public static string Raiz(string palabra) => palabra.Length <= 6 ? palabra : palabra[..6];
}
