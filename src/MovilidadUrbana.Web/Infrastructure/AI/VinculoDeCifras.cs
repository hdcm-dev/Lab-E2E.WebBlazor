using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Control de atribución (paso 7): que un número esté entre las cifras entregadas no alcanza;
/// tiene que estar atado a <b>su</b> categoría. «Trabajo y Estudio, cada una con 4» con Trabajo
/// en 5 tiene sólo números válidos, pero le asigna a Trabajo la cifra de Estudio. Este control lee
/// cada oración, ata cada número a la categoría que nombra (la que lo precede, la que lo sigue
/// pegada o la primera de su cláusula), ata los promedios y medianas a su unidad y estadístico, y
/// verifica los ordinales («la segunda», «el más usado») contra el orden de las cifras, con los
/// empates como rango. Un número que no se puede atar a nada sigue con el control de origen.
/// </summary>
public static partial class VinculoDeCifras
{
    private sealed record Etiqueta(Cifra Cifra, string[][] Formas, bool Total);

    private sealed record Token(string Texto, double? Valor);

    private static readonly HashSet<string> Totales = ["encuestas", "respuestas", "localidades", "habitantes declarados"];

    private static readonly Dictionary<string, int> Ordinales = new()
    {
        ["primer"] = 1, ["primero"] = 1, ["primera"] = 1, ["segundo"] = 2, ["segunda"] = 2, ["tercer"] = 3, ["tercero"] = 3, ["tercera"] = 3,
        ["cuarto"] = 4, ["cuarta"] = 4, ["quinto"] = 5, ["quinta"] = 5, ["sexto"] = 6, ["sexta"] = 6
    };

    private static readonly HashSet<string> Adjetivos = ["usado", "usada", "usados", "usadas", "elegido", "elegida", "elegidos", "elegidas", "frecuente", "frecuentes", "comun", "comunes", "marcado", "marcada", "votado", "votada", "popular", "habitual"];

    /// <summary>Motivo del rechazo, o <c>null</c> si cada cifra atada coincide con la de su categoría.</summary>
    public static string? Revisar(string texto, IReadOnlyList<Cifra> cifras)
    {
        if (cifras.Count == 0) return null;
        var etiquetas = cifras.Where(c => !EsEstadistico(c)).Select(Armar).ToList();
        var estadisticos = cifras.Where(EsEstadistico).ToList();

        foreach (var oracion in Regex.Split(CitaRx().Replace(texto, " "), @"(?<=[.!?])\s+|\n+"))
        {
            var t = Tokenizar(oracion);
            var atados = new HashSet<int>();
            var falla = RevisarEstadisticos(t, estadisticos, atados) ?? RevisarCategorias(t, etiquetas, atados) ?? RevisarOrdinales(t, etiquetas);
            if (falla is not null) return falla;
        }
        return null;
    }

    private static string? RevisarEstadisticos(List<Token> t, List<Cifra> estadisticos, HashSet<int> atados)
    {
        if (estadisticos.Count == 0) return null;
        string? unidadAnterior = null;
        var abierto = false;
        string? unidadDelParentesis = null;
        for (var i = 0; i < t.Count; i++)
        {
            if (t[i].Texto == "(") { abierto = true; unidadDelParentesis = unidadAnterior; continue; }
            if (t[i].Texto == ")") { abierto = false; continue; }
            if (t[i].Valor is not double v) continue;

            var unidad = i + 1 < t.Count ? Unidad(t[i + 1].Texto) : null;
            unidad ??= abierto ? unidadDelParentesis : null;
            if (unidad is null) continue;
            unidadAnterior = unidad;

            string? estadistico = null;
            for (var j = i - 1; j >= Math.Max(0, i - 4) && t[j].Valor is null && t[j].Texto != ")"; j--)
            {
                if (t[j].Texto is "promedio" or "mediana") { estadistico = t[j].Texto; break; }
            }
            for (var j = i + 2; estadistico is null && j < Math.Min(t.Count, i + 5) && t[j].Valor is null; j++)
            {
                if (t[j].Texto is "promedio" or "mediana") estadistico = t[j].Texto;
            }

            var candidatas = estadisticos.Where(c => c.Etiqueta.StartsWith(unidad, StringComparison.Ordinal) && (estadistico is null || c.Etiqueta.EndsWith(estadistico, StringComparison.Ordinal))).ToList();
            if (candidatas.Count == 0) continue;
            atados.Add(i);
            if (!candidatas.Any(c => Igual(c.Valor, v)))
            {
                return $"cifra atribuida a otra categoría: {Plantillas.N(v)} como {unidad}{(estadistico is null ? string.Empty : " " + estadistico)}";
            }
        }
        return null;
    }

    private static string? RevisarCategorias(List<Token> t, List<Etiqueta> etiquetas, HashSet<int> atados)
    {
        var menciones = Menciones(t, etiquetas);
        var pendientes = new List<Etiqueta>();
        var consumidas = new HashSet<int>();
        for (var i = 0; i < t.Count; i++)
        {
            if (menciones.TryGetValue(i, out var m))
            {
                if (!consumidas.Contains(i)) pendientes.Add(m.Etiqueta);
                i += m.Largo - 1;
                continue;
            }
            if (t[i].Valor is not double v || atados.Contains(i)) continue;

            List<Etiqueta> atadas;
            if (pendientes.Count > 0)
            {
                atadas = pendientes.Count > 1 && pendientes.Any(p => !p.Total) ? pendientes.Where(p => !p.Total).ToList() : pendientes.ToList();
                pendientes.Clear();
            }
            else
            {
                atadas = [];
                for (var j = i + 1; j < t.Count && t[j].Valor is null && t[j].Texto is not (";" or "(" or ")"); j++)
                {
                    if (menciones.TryGetValue(j, out var siguiente)) { atadas.Add(siguiente.Etiqueta); consumidas.Add(j); break; }
                }
            }

            foreach (var e in atadas)
            {
                if (!Igual(e.Cifra.Valor, v)) return $"cifra atribuida a otra categoría: {Plantillas.N(v)} a {e.Cifra.Etiqueta}";
            }
        }
        return null;
    }

    private static string? RevisarOrdinales(List<Token> t, List<Etiqueta> etiquetas)
    {
        var categorias = etiquetas.Where(e => !e.Total).ToList();
        if (categorias.Count < 2) return null;
        var menciones = Menciones(t, categorias);
        for (var i = 0; i < t.Count; i++)
        {
            int? puesto = null;
            if (Ordinales.TryGetValue(t[i].Texto, out var o)) puesto = o;
            else if (t[i].Texto is "ultimo" or "ultima") puesto = categorias.Count;
            else if (t[i].Texto is "mas" or "menos" && i + 1 < t.Count && Adjetivos.Contains(t[i + 1].Texto)
                     && !Enumerable.Range(Math.Max(0, i - 2), i - Math.Max(0, i - 2)).Any(j => Ordinales.ContainsKey(t[j].Texto)))
            {
                puesto = t[i].Texto == "mas" ? 1 : categorias.Count;
            }
            if (puesto is not int p) continue;

            var cercana = menciones.OrderBy(m => Math.Abs(m.Key - i)).ThenBy(m => m.Key < i ? 1 : 0).Select(m => m.Value.Etiqueta).FirstOrDefault();
            if (cercana is null) continue;
            var desde = 1 + categorias.Count(c => c.Cifra.Valor > cercana.Cifra.Valor + 0.05);
            var hasta = categorias.Count(c => c.Cifra.Valor > cercana.Cifra.Valor - 0.05);
            if (p < desde || p > hasta) return $"orden atribuido a otra categoría: {cercana.Cifra.Etiqueta} en el puesto {p}";
        }
        return null;
    }

    private static Dictionary<int, (Etiqueta Etiqueta, int Largo)> Menciones(List<Token> t, List<Etiqueta> etiquetas)
    {
        var resultado = new Dictionary<int, (Etiqueta, int)>();
        var formas = etiquetas.SelectMany(e => e.Formas.Select(f => (e, f))).OrderByDescending(x => x.f.Length).ToList();
        for (var i = 0; i < t.Count; i++)
        {
            foreach (var (e, f) in formas)
            {
                if (i + f.Length > t.Count) continue;
                if (f.Select((palabra, k) => t[i + k].Texto == palabra).All(x => x))
                {
                    resultado[i] = (e, f.Length);
                    i += f.Length - 1;
                    break;
                }
            }
        }
        return resultado;
    }

    private static Etiqueta Armar(Cifra c)
    {
        var principal = Palabras(c.Etiqueta);
        var formas = new List<string[]> { principal };
        if (c.Etiqueta.StartsWith("habitantes de ", StringComparison.Ordinal)) formas.Add(Palabras(c.Etiqueta["habitantes de ".Length..]));
        foreach (var (clave, etiqueta) in Catalogos.Medios.Concat(Catalogos.Frecuencias).Concat(Catalogos.Motivos))
        {
            if (string.Equals(etiqueta, c.Etiqueta, StringComparison.OrdinalIgnoreCase)) formas.Add([clave]);
        }
        if (c.Etiqueta == "Auto particular") formas.Add(["auto"]);
        var normal = string.Join(' ', principal);
        return new Etiqueta(c, formas.Where(f => f.Length > 0).DistinctBy(f => string.Join(' ', f)).ToArray(), Totales.Contains(normal));
    }

    private static bool EsEstadistico(Cifra c) => EstadisticoRx().IsMatch(c.Etiqueta);

    private static string? Unidad(string palabra) => palabra switch
    {
        "minutos" or "minuto" or "min" => "minutos",
        "km" or "kilometros" or "kilometro" => "km",
        _ => null
    };

    private static bool Igual(double a, double b) => Math.Abs(a - b) < 0.05;

    private static string[] Palabras(string texto) => Tokenizar(texto).Select(x => x.Texto).ToArray();

    private static List<Token> Tokenizar(string texto)
    {
        var lista = new List<Token>();
        foreach (Match m in TokenRx().Matches(SinTildes(texto).ToLowerInvariant()))
        {
            var v = m.Value;
            if (char.IsDigit(v[0])) lista.Add(new Token(v, ValidadorDeCifras.Numeros(v)[0]));
            else lista.Add(new Token(v, ValidadorDeCifras.ValorEnPalabras(v)));
        }
        return lista;
    }

    public static string SinTildes(string texto)
    {
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d+(?:[.,]\d+)?|\p{L}+|[();:%]")]
    private static partial Regex TokenRx();

    [GeneratedRegex(@"^(minutos|km) (promedio|mediana)$")]
    private static partial Regex EstadisticoRx();

    [GeneratedRegex(@"\[\d+\]")]
    private static partial Regex CitaRx();
}
