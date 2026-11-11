using System.Globalization;
using System.Text.RegularExpressions;
using MovilidadUrbana.Web.Infrastructure.AI;

namespace MovilidadUrbana.UnitTests.Asistente;

/// <summary>
/// Reglas deterministas declaradas del juez a ciegas de la batería (K7 R9.8). Son simétricas: no
/// saben cuál texto es la redactada; cada texto suma un defecto por regla que rompe.
/// R1 registro ajeno (tuteo, vosotros) · R2 habla de porcentajes y el otro no · R3 nota de
/// multirrespuesta invertida («no llega», «menos de cien») · R4 un número que no está en los
/// datos ni en la pregunta · R5 una categoría con una cifra distinta de la de los datos · R6 más
/// larga que 1,5 × la otra + 40 · R7 un trigrama repetido · R8 eco de instrucción · R9 pierde una
/// aclaración del otro («“Bondi” se entiende como colectivo», B11) · R10 redundancia: dos palabras
/// distintas de la misma raíz a tres palabras o menos («la frecuencia más frecuente», B40).
/// </summary>
public static partial class ReglasDelJuez
{
    public const string Nombre = "reglas deterministas declaradas (R1–R10)";

    /// <summary>Sin citas ni negritas, que delatarían el origen, y con los espacios normalizados.</summary>
    public static string Ciega(string texto) => Regex.Replace(Regex.Replace(Regex.Replace(texto, @"\s*\[\d+\]", string.Empty), @"\*\*", string.Empty), @"\s+", " ").Trim();

    /// <summary>«redactada», «fija» o «igual»: gana el texto con menos defectos.</summary>
    public static string Ganadora(string pregunta, IReadOnlyDictionary<string, double> datos, string redactada, string fija)
    {
        var (r, f) = (Ciega(redactada), Ciega(fija));
        var dr = Defectos(r, f, pregunta, datos).Count;
        var df = Defectos(f, r, pregunta, datos).Count;
        return dr == df ? "igual" : dr < df ? "redactada" : "fija";
    }

    public static List<string> Defectos(string a, string b, string pregunta, IReadOnlyDictionary<string, double> datos)
    {
        var d = new List<string>();
        var ta = VinculoDeCifras.SinTildes(a).ToLowerInvariant();
        var tb = VinculoDeCifras.SinTildes(b).ToLowerInvariant();
        if (Regex.IsMatch(a, @"(?i)(?<!\p{L})(vosotr[oa]s|vuestr[oa]s?|tú|contigo|tienes|puedes|debes|quieres|sabes|eres)(?!\p{L})")) d.Add("R1");
        static bool Porcentual(string t) => t.Contains('%') || t.Contains("porcentaje", StringComparison.Ordinal);
        if (Porcentual(ta) && !Porcentual(tb)) d.Add("R2");
        if (Regex.IsMatch(ta, @"no llega\w*|menos del? (100|cien)|no alcanza\w*")) d.Add("R3");
        var permitidos = datos.Values.Concat(ValidadorDeCifras.Numeros(pregunta)).ToList();
        if (ValidadorDeCifras.Numeros(a).Any(n => !permitidos.Any(p => Math.Abs(p - n) < 0.05))) d.Add("R4");
        foreach (var (etiqueta, valor) in datos)
        {
            foreach (Match m in Regex.Matches(a, $@"(?i)\b{Regex.Escape(etiqueta)}\b\s*(?::|,)?\s*(?:con)?\s*(\d+)\b"))
            {
                if (Math.Abs(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) - valor) > 0.05) { d.Add("R5"); break; }
            }
        }
        if (a.Length > b.Length * 1.5 + 40) d.Add("R6");
        var palabras = Regex.Matches(ta, @"[\p{L}\d]+").Select(m => m.Value).ToList();
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i + 2 < palabras.Count; i++) if (!vistos.Add($"{palabras[i]} {palabras[i + 1]} {palabras[i + 2]}")) { d.Add("R7"); break; }
        if (ValidadorDeCifras.Eco(a, b) is not null) d.Add("R8");
        // R9: cada «término» que el otro aclara («“Bondi” se entiende como colectivo») tiene que aparecer.
        foreach (Match m in Aclaracion().Matches(tb))
            if (!Regex.IsMatch(ta, $@"(?<!\p{{L}}){Regex.Escape(m.Groups[1].Value)}(?!\p{{L}})")) { d.Add("R9"); break; }
        if (Redundante(ta)) d.Add("R10");
        return d.Distinct().ToList();
    }

    /// <summary>
    /// R10: dos palabras distintas de seis letras o más, con las mismas seis primeras y ninguna prefijo
    /// de la otra (no cuenta «provincia» y «provincias»), a tres palabras o menos. Lo que va entre
    /// comillas angulares es un nombre de la pantalla («guardala vos con “Guardar”») y no cuenta.
    /// </summary>
    public static bool Redundante(string texto)
    {
        var palabras = Regex.Matches(Regex.Replace(VinculoDeCifras.SinTildes(texto).ToLowerInvariant(), "«[^»]*»", " "), @"\p{L}+").Select(m => m.Value).ToList();
        for (var i = 0; i < palabras.Count; i++)
            for (var j = i + 1; j < palabras.Count && j <= i + 3; j++)
            {
                var (x, y) = (palabras[i], palabras[j]);
                if (x.Length >= 6 && y.Length >= 6 && x[..6] == y[..6] && !x.StartsWith(y, StringComparison.Ordinal) && !y.StartsWith(x, StringComparison.Ordinal)) return true;
            }
        return false;
    }

    [GeneratedRegex(@"«([^»]+)» se entiende como")]
    private static partial Regex Aclaracion();
}
