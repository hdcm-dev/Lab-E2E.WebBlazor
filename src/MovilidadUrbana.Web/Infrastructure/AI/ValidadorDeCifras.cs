using System.Globalization;
using System.Text.RegularExpressions;
using MovilidadUrbana.Web.Application.Asistente;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Controles sobre el texto del modelo (paso 7): cada número afirmado tiene que estar anclado en
/// lo que se le entregó (cifras, fragmentos, pregunta, plantilla), cada oración cita una fuente
/// entregada y ninguna dice que el asistente hizo algo. Si un control falla, el texto se rechaza
/// y la respuesta es la plantilla con las cifras del código (REQ-IA-14, REQ-IA-22).
/// </summary>
public static partial class ValidadorDeCifras
{
    public const int LargoMaximo = 700;

    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>Números de un texto en es-AR: «346.334» es un entero; «12,5» y «12.5» son decimales.</summary>
    public static IReadOnlyList<double> Numeros(string texto)
    {
        var lista = new List<double>();
        foreach (Match m in Numero().Matches(CitaRx().Replace(texto, " ")))
        {
            var t = m.Value;
            double v;
            if (MilesRx().IsMatch(t)) v = double.Parse(t.Replace(".", string.Empty).Replace(',', '.'), CultureInfo.InvariantCulture);
            else v = double.Parse(t.Replace(',', '.'), CultureInfo.InvariantCulture);
            lista.Add(v);
        }
        return lista;
    }

    public static string? Validar(string texto, PedidoDeRedaccion pedido)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "texto vacío";
        if (texto.Length > LargoMaximo) return "texto demasiado largo";

        var permitidos = new List<double>();
        permitidos.AddRange(pedido.Cifras.Select(c => c.Valor));
        permitidos.AddRange(Numeros(pedido.Pregunta));
        permitidos.AddRange(Numeros(pedido.Plantilla));
        foreach (var f in pedido.Fragmentos) permitidos.AddRange(Numeros(f.Texto));

        foreach (var n in Numeros(texto))
        {
            if (!permitidos.Any(p => Math.Abs(p - n) < 0.05)) return $"cifra sin origen: {n.ToString(Cultura)}";
        }

        // Un número escrito en palabras escapa al control de cifras: sólo vale si ya está en las fuentes.
        var fuentesEnTexto = Texto(pedido);
        foreach (Match m in NumeroEnPalabras().Matches(texto))
        {
            if (!fuentesEnTexto.Contains(m.Value.ToLowerInvariant(), StringComparison.Ordinal)) return $"cifra en palabras sin origen: {m.Value}";
        }

        if (PrimeraPersonaDeAccion().IsMatch(texto)) return "primera persona de acción";

        var fuentes = Math.Max(pedido.Fragmentos.Count, pedido.Cifras.Count > 0 ? 1 : 0);
        foreach (var oracion in Oraciones(texto))
        {
            var citas = CitaRx().Matches(oracion).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
            if (citas.Count == 0) return "oración sin cita";
            if (citas.Any(c => c < 1 || c > fuentes)) return "cita a una fuente no entregada";
        }

        return null;
    }

    private static string Texto(PedidoDeRedaccion pedido) =>
        string.Join(' ', new[] { pedido.Pregunta, pedido.Plantilla }
            .Concat(pedido.Fragmentos.Select(f => f.Texto))
            .Concat(pedido.Cifras.Select(c => c.Etiqueta + " " + c.Nota))).ToLowerInvariant();

    private static IEnumerable<string> Oraciones(string texto) =>
        OracionRx().Split(texto.Trim()).Select(o => o.Trim()).Where(o => o.Length > 0 && o.Any(char.IsLetter));

    [GeneratedRegex(@"\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d+(?:[.,]\d+)?")]
    private static partial Regex Numero();

    [GeneratedRegex(@"^\d{1,3}(?:\.\d{3})+(?:,\d+)?$")]
    private static partial Regex MilesRx();

    [GeneratedRegex(@"(?i)\b(dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez|once|doce|trece|catorce|quince|diecis[eé]is|diecisiete|dieciocho|diecinueve|veinte|veinti\w+|treinta|cuarenta|cincuenta|sesenta|setenta|ochenta|noventa|cien|ciento|mil|millones)\b")]
    private static partial Regex NumeroEnPalabras();

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex CitaRx();

    // Una oración termina en punto, signo de cierre o salto de línea, después de su cita.
    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-ZÁÉÍÓÚÑ¿¡«*])|\n+")]
    private static partial Regex OracionRx();

    // Pretérito en primera persona («agregué», «borré»), con borde de palabra al final: «cargues» o
    // «guardes» son de la persona y no cuentan.
    [GeneratedRegex(@"(?i)\b(yo\s|(agregu|borr|elimin|cargu|guard|registr|actualic)[eé]\b|cre[eé]\s+la\b|lo hice|la hice)")]
    private static partial Regex PrimeraPersonaDeAccion();
}
