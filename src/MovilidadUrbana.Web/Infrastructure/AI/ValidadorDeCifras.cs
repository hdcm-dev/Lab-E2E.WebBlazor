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
            // Como en la referencia (CitarCadaOracion): la cita cierra la oración. Lo que sigue a la
            // última cita es una cláusula sin respaldo (B13: «… [1], mientras que …»).
            if (!CitaAlFinal().IsMatch(oracion)) return "oración sin cita al final";
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

        // La redactada no puede ser peor que la fija (ronda 2): ni otro registro, ni más larga sin
        // motivo, ni repetida, ni con la nota de multirrespuesta cambiada de sentido.
        var forma = ControlarForma(texto, pedido.Plantilla);
        if (forma is not null) return forma;

        // Un lugar, un medio o un motivo que no está en las fuentes es un hecho inventado aunque no
        // tenga números (B24: «una encuesta en Corrientes»). La pregunta no cuenta como fuente.
        var entidad = EntidadSinOrigen(texto, pedido);
        if (entidad is not null) return $"entidad sin origen: {entidad}";

        return null;
    }

    /// <summary>Controles de forma contra la plantilla fija; motivo del rechazo o <c>null</c>.</summary>
    public static string? ControlarForma(string texto, string plantilla)
    {
        if (RegistroAjeno().IsMatch(texto)) return $"registro ajeno: {RegistroAjeno().Match(texto).Value}";

        var largo = SinMarcas(texto).Length;
        var tope = Math.Max(80, (int)(SinMarcas(plantilla).Length * 1.5) + 40);
        if (plantilla.Length > 0 && largo > tope) return $"más larga que la plantilla: {largo} > {tope}";

        var palabras = PalabraRx().Matches(VinculoDeCifras.SinTildes(SinMarcas(texto)).ToLowerInvariant()).Select(m => m.Value).ToList();
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i + 2 < palabras.Count; i++)
        {
            var trigrama = $"{palabras[i]} {palabras[i + 1]} {palabras[i + 2]}";
            if (!vistos.Add(trigrama)) return $"redundancia: «{trigrama}»";
        }

        var t = VinculoDeCifras.SinTildes(texto).ToLowerInvariant();
        var p = VinculoDeCifras.SinTildes(plantilla).ToLowerInvariant();
        // B17, B42: «los porcentajes no suman» cuando la respuesta no muestra ningún porcentaje.
        if (t.Contains("porcentaje", StringComparison.Ordinal) && !p.Contains("porcentaje", StringComparison.Ordinal))
        {
            return "nota de porcentaje sin porcentajes";
        }
        if (p.Contains("no suman 100 %", StringComparison.Ordinal))
        {
            // B11: «no llegan al cien» dice lo contrario: con varias marcas por persona se pasa de cien.
            if (NotaInvertida().IsMatch(t)) return "nota de multirrespuesta con otro sentido";
            // La nota se copia con las palabras de la plantilla: «el cien por ciento» y otras vueltas
            // fueron peores a ciegas (B10, ronda 3).
            if (!NotaMultirrespuesta().IsMatch(t)) return "nota de multirrespuesta alterada";
        }
        return null;
    }

    /// <summary>
    /// Entidades del dominio que el texto puede nombrar sólo si están en las fuentes: provincias,
    /// localidades de la siembra y las categorías de medios, frecuencias y motivos.
    /// </summary>
    public static readonly IReadOnlyList<string> EntidadesDelDominio =
        MovilidadUrbana.Web.Domain.Catalogos.Provincias
            .Concat(["Resistencia"])
            .Concat(MovilidadUrbana.Web.Domain.Catalogos.Medios.SelectMany(m => new[] { m.Clave, m.Etiqueta }))
            .Concat(MovilidadUrbana.Web.Domain.Catalogos.Frecuencias.Select(f => f.Etiqueta))
            .Concat(MovilidadUrbana.Web.Domain.Catalogos.Motivos.Where(m => m.Clave != "otros").Select(m => m.Etiqueta))
            .Select(e => VinculoDeCifras.SinTildes(e).ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static string? EntidadSinOrigen(string texto, PedidoDeRedaccion pedido)
    {
        var t = VinculoDeCifras.SinTildes(texto).ToLowerInvariant();
        var fuentes = VinculoDeCifras.SinTildes(string.Join(' ', new[] { pedido.Plantilla }
            .Concat(pedido.Fragmentos.Select(f => f.Titulo + " " + f.Texto))
            .Concat(pedido.Cifras.Select(c => c.Etiqueta + " " + c.Nota)))).ToLowerInvariant();
        foreach (var e in EntidadesDelDominio)
        {
            if (Regex.IsMatch(t, $@"\b{Regex.Escape(e)}\b") && !Regex.IsMatch(fuentes, $@"\b{Regex.Escape(e)}\b")) return e;
        }
        return null;
    }

    private static string SinMarcas(string s) => CitaRx().Replace(s, string.Empty).Replace("**", string.Empty, StringComparison.Ordinal).Trim();

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

    // Sin tildes: «dejá de lado», «pasá por alto», y en inglés («ignore», «disregard», «instructions»).
    [GeneratedRegex(@"\b(ignor\w*|olvid\w*|instruccion\w*|instruction\w*|disregard\w*|forget\w*|deja\w* de lado|pasa\w* por alto|no tengas en cuenta)")]
    private static partial Regex EcoDeInstruccion();

    [GeneratedRegex(@"\bveces\b|\ben una semana\b|\ba la semana\b|\b(por|cada|al|una vez por) (semana|dia|mes|ano)\b")]
    private static partial Regex PeriodoRx();

    [GeneratedRegex(@"\[\d+\]\**\s*[.!?…]?\**\s*$|[.!?]\s*\[\d+\]\s*$")]
    private static partial Regex CitaAlFinal();

    // Voseo rioplatense: el tuteo y el «vosotros» peninsular son otro registro.
    [GeneratedRegex(@"(?i)(?<!\p{L})(vosotr[oa]s|vuestr[oa]s?|tú|ti mism[oa]|contigo|tienes|puedes|debes|quieres|sabes|eres|haz)(?!\p{L})")]
    private static partial Regex RegistroAjeno();

    [GeneratedRegex(@"[\p{L}\d]+")]
    private static partial Regex PalabraRx();

    [GeneratedRegex(@"no suman 100 %")]
    private static partial Regex NotaMultirrespuesta();

    [GeneratedRegex(@"no llega\w*|menos del? (100|cien)|no alcanza\w*")]
    private static partial Regex NotaInvertida();

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
