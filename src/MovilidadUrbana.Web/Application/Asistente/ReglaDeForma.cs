namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Cómo se muestra un dato (K5 CD-7). La decide el código, nunca el modelo.</summary>
public enum FormaDeGrafico { Ninguno, Numero, Barras, TortaConTabla, Tabla }

/// <summary>Tipo de dato que llega a la regla.</summary>
public enum TipoDeDato { Numero, Distribucion, Comparacion }

/// <summary>
/// Regla de forma (REQ-IA-40), función pura: un número va en una frase; una distribución de
/// respuesta única con 2 a 6 categorías, en torta con su tabla; una multirrespuesta o más de 6
/// categorías, en barras ordenadas; una comparación con varias columnas, en tabla. Los medios
/// son multirrespuesta: nunca van en torta.
/// </summary>
public static class ReglaDeForma
{
    public const int MaximoDePorciones = 6;

    public static FormaDeGrafico Decidir(TipoDeDato tipo, int categorias = 0, bool sumable = false) => tipo switch
    {
        TipoDeDato.Numero => FormaDeGrafico.Numero,
        TipoDeDato.Comparacion => FormaDeGrafico.Tabla,
        TipoDeDato.Distribucion when sumable && categorias is >= 2 and <= MaximoDePorciones => FormaDeGrafico.TortaConTabla,
        TipoDeDato.Distribucion => FormaDeGrafico.Barras,
        _ => FormaDeGrafico.Ninguno
    };

    /// <summary>
    /// Porciones de la torta en grados. Se reparten con el método del resto mayor para que la
    /// suma de los porcentajes enteros sea exactamente 100 y la de las cantidades, el total.
    /// </summary>
    public static IReadOnlyList<int> PorcentajesEnteros(IReadOnlyList<int> cantidades)
    {
        var total = cantidades.Sum();
        if (total == 0) return cantidades.Select(_ => 0).ToList();
        var exactos = cantidades.Select(c => c * 100.0 / total).ToArray();
        var enteros = exactos.Select(e => (int)Math.Floor(e)).ToArray();
        var faltan = 100 - enteros.Sum();
        foreach (var i in exactos.Select((e, i) => (resto: e - Math.Floor(e), i)).OrderByDescending(p => p.resto).ThenBy(p => p.i).Take(faltan).Select(p => p.i))
        {
            enteros[i]++;
        }
        return enteros;
    }
}
