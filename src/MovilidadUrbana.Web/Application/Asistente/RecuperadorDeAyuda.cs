using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Un fragmento recuperado con su puntaje y la cobertura de la pregunta.</summary>
public sealed record Recuperado(FragmentoDeAyuda Fragmento, double Puntaje, double Cobertura);

/// <summary>
/// Recuperación léxica con compuerta (ADR 0006): raíz de seis letras, sinónimos aplicados antes,
/// pesos 4/3/2/1 (preguntas de la ficha, título de sección, título de página, cuerpo) y un umbral
/// de cobertura: si la ayuda no cubre lo bastante las palabras de la pregunta, no hay fragmento
/// y la respuesta es «No está en la ayuda» (F5), sin llamada al modelo.
/// </summary>
public sealed class RecuperadorDeAyuda(CorpusDeAyuda corpus)
{
    public const int TopK = 3;

    /// <summary>Fracción mínima de raíces de la pregunta que tienen que aparecer en el fragmento.</summary>
    public const double UmbralDeCobertura = 0.6;

    private static readonly HashSet<string> Vacias =
    [
        "a", "al", "algo", "como", "con", "cual", "cuales", "cuando", "cuanto", "cuanta", "cuantos", "cuantas", "de", "del", "donde",
        "el", "en", "es", "esta", "este", "esto", "hay", "la", "las", "le", "lo", "los", "me", "mi", "mis", "no", "o", "para",
        "por", "puedo", "que", "se", "si", "sin", "su", "sus", "te", "tengo", "tiene", "un", "una", "uno", "unos", "y", "ya", "yo",
        "hago", "hace", "hacer", "pongo", "poner", "acepta", "deja", "quiero", "necesito", "doy", "dar", "tener", "tienen", "ser",
        "seguir", "lleva", "va", "van", "son", "sea", "ahi", "alli", "favor", "decime", "explicame", "sabes"
    ];

    public static IReadOnlyList<string> Raices(string textoNormalizado) =>
        Texto.Palabras(textoNormalizado).Where(p => !Vacias.Contains(p)).Select(Texto.Raiz).Distinct().ToList();

    public IReadOnlyList<Recuperado> Buscar(string preguntaNormalizada, Func<FragmentoDeAyuda, bool>? filtro = null)
    {
        var raices = Raices(preguntaNormalizada);
        if (raices.Count == 0) return [];

        var candidatos = corpus.Vigentes.Where(f => filtro is null || filtro(f));
        var puntuados = new List<Recuperado>();
        foreach (var f in candidatos)
        {
            var preguntas = RaicesDe(string.Join(' ', f.Preguntas));
            var seccion = RaicesDe(f.Seccion);
            var pagina = RaicesDe(f.TituloDePagina);
            var cuerpo = RaicesDe(f.Texto);

            double puntaje = 0;
            var cubiertas = 0;
            foreach (var r in raices)
            {
                var p = (preguntas.Contains(r) ? 4 : 0) + (seccion.Contains(r) ? 3 : 0) + (pagina.Contains(r) ? 2 : 0) + (cuerpo.Contains(r) ? 1 : 0);
                if (p > 0) cubiertas++;
                puntaje += p;
            }

            var cobertura = (double)cubiertas / raices.Count;
            if (cobertura >= UmbralDeCobertura) puntuados.Add(new Recuperado(f, puntaje, cobertura));
        }

        return puntuados
            .OrderByDescending(r => r.Cobertura).ThenByDescending(r => r.Puntaje).ThenBy(r => r.Fragmento.Id, StringComparer.Ordinal)
            .Take(TopK)
            .ToList();
    }

    private static HashSet<string> RaicesDe(string texto) =>
        Raices(Sinonimos.Aplicar(texto).Texto).ToHashSet(StringComparer.Ordinal);
}
