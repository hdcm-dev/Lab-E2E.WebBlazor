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
        // «ocho caracteres» en la ayuda ancla un «8» del texto: es la misma cifra, en cifras.
        foreach (var f in pedido.Fragmentos.Select(x => x.Texto).Append(pedido.Plantilla))
        {
            foreach (Match m in NumeroEnPalabras().Matches(f))
            {
                if (ValorEnPalabras(VinculoDeCifras.SinTildes(m.Value.ToLowerInvariant())) is double v) permitidos.Add(v);
            }
        }

        foreach (var n in Numeros(texto))
        {
            if (!permitidos.Any(p => Math.Abs(p - n) < 0.05)) return $"cifra sin origen: {n.ToString(Cultura)}";
        }

        // Un número escrito en palabras vale lo mismo que en cifras: se convierte y pasa por el mismo
        // control de origen; también vale si está tal cual en las fuentes.
        var fuentesEnTexto = Texto(pedido);
        foreach (Match m in NumeroEnPalabras().Matches(texto))
        {
            var palabra = m.Value.ToLowerInvariant();
            var valor = ValorEnPalabras(VinculoDeCifras.SinTildes(palabra));
            var anclado = fuentesEnTexto.Contains(palabra, StringComparison.Ordinal) || (valor is double v && permitidos.Any(p => Math.Abs(p - v) < 0.05));
            if (!anclado) return $"cifra en palabras sin origen: {m.Value}";
        }

        if (PrimeraPersonaDeAccion().IsMatch(texto)) return "primera persona de acción";

        // Lo que pide la pregunta («ignorá los datos») no se repite en la respuesta (B17).
        var deLaAyuda = VinculoDeCifras.SinTildes(string.Join(' ', pedido.Fragmentos.Select(f => f.Texto))).ToLowerInvariant();
        foreach (Match m in EcoDeInstruccion().Matches(VinculoDeCifras.SinTildes(texto).ToLowerInvariant()))
        {
            if (!deLaAyuda.Contains(m.Value, StringComparison.Ordinal)) return $"eco de una instrucción: {m.Value}";
        }

        // Un período o una cantidad de veces que no está en las fuentes es una paráfrasis que cambia
        // el sentido (B19: «veces por semana» por «algunos días por semana»).
        var fuentesSinTildes = VinculoDeCifras.SinTildes(fuentesEnTexto);
        foreach (Match m in PeriodoRx().Matches(VinculoDeCifras.SinTildes(texto).ToLowerInvariant()))
        {
            if (!fuentesSinTildes.Contains(m.Value, StringComparison.Ordinal)) return $"período sin origen: {m.Value}";
        }

        var fuentes = Math.Max(pedido.Fragmentos.Count, pedido.Cifras.Count > 0 ? 1 : 0);
        foreach (var oracion in Oraciones(texto))
        {
            var citas = CitaRx().Matches(oracion).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
            if (citas.Count == 0) return "oración sin cita";
            if (citas.Any(c => c < 1 || c > fuentes)) return "cita a una fuente no entregada";
            if (CitaColgante().IsMatch(oracion)) return "cita colgante";
        }

        // Una cifra de la sesión sin «sesión» pierde su nota de métrica (K5, B14).
        if (pedido.Cifras.Count > 0 && pedido.Plantilla.Contains("sesión", StringComparison.OrdinalIgnoreCase)
            && !texto.Contains("sesión", StringComparison.OrdinalIgnoreCase) && !texto.Contains("sesion", StringComparison.OrdinalIgnoreCase))
        {
            return "cifra sin la nota de la sesión";
        }

        // Cada cifra atada a su categoría, y los ordinales al orden de las cifras.
        var vinculo = VinculoDeCifras.Revisar(texto, pedido.Cifras);
        if (vinculo is not null) return vinculo;

        return null;
    }

    private static readonly Dictionary<string, int> Palabras = new(StringComparer.Ordinal)
    {
        ["dos"] = 2, ["tres"] = 3, ["cuatro"] = 4, ["cinco"] = 5, ["seis"] = 6, ["siete"] = 7, ["ocho"] = 8, ["nueve"] = 9, ["diez"] = 10,
        ["once"] = 11, ["doce"] = 12, ["trece"] = 13, ["catorce"] = 14, ["quince"] = 15, ["dieciseis"] = 16, ["diecisiete"] = 17,
        ["dieciocho"] = 18, ["diecinueve"] = 19, ["veinte"] = 20, ["veintiuno"] = 21, ["veintiuna"] = 21, ["veintidos"] = 22,
        ["veintitres"] = 23, ["veinticuatro"] = 24, ["veinticinco"] = 25, ["veintiseis"] = 26, ["veintisiete"] = 27,
        ["veintiocho"] = 28, ["veintinueve"] = 29, ["treinta"] = 30, ["cuarenta"] = 40, ["cincuenta"] = 50, ["sesenta"] = 60,
        ["setenta"] = 70, ["ochenta"] = 80, ["noventa"] = 90, ["cien"] = 100
    };

    /// <summary>Valor de un número escrito en palabras («nueve», «veinte», «cien»), sin tildes; <c>null</c> si no es uno simple.</summary>
    public static double? ValorEnPalabras(string palabra) => Palabras.TryGetValue(palabra, out var v) ? v : null;

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

    [GeneratedRegex(@"(?i)\b(dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez|once|doce|trece|catorce|quince|diecis[eé]is|diecisiete|dieciocho|diecinueve|veinte|veinti\w+|treinta|cuarenta|cincuenta|sesenta|setenta|ochenta|noventa|cien|(?<!por )ciento|mil|millones)\b")]
    private static partial Regex NumeroEnPalabras();

    [GeneratedRegex(@"\b(ignor\w*|olvid\w*|instruccion\w*)")]
    private static partial Regex EcoDeInstruccion();

    [GeneratedRegex(@"\bveces\b|\ben una semana\b|\ba la semana\b|\b(por|cada|al|una vez por) (semana|dia|mes|ano)\b")]
    private static partial Regex PeriodoRx();

    [GeneratedRegex(@"[:;,]\s*\[\d+\]")]
    private static partial Regex CitaColgante();

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
