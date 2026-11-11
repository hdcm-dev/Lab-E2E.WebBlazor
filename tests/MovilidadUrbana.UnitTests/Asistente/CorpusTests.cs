using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.UnitTests.Asistente;

/// <summary>Corpus y recuperación (K6 Parte A): cobertura, recall@3 y abstención.</summary>
public class CorpusTests
{
    private static readonly CorpusDeAyuda Corpus = new();
    private static readonly RecuperadorDeAyuda Recuperador = new(Corpus);

    [Test(Description = "REQ-IA-25: las 4 pantallas del anillo 1 tienen ficha vigente (umbral 100 %)")]
    public void CoberturaDelCorpus_SobreUmbral()
    {
        var contextos = new[] { "", "localidades", "encuesta", "resultados" };
        var cubiertos = contextos.Count(c => Corpus.Vigentes.Any(f => f.Contexto == c && f.Anillo == Anillo.Operar));
        Assert.That(cubiertos, Is.EqualTo(4));
    }

    [Test(Description = "Los requisitos se generan desde *Policy: si cambia la regla, cambia la ayuda")]
    public void Requisitos_GeneradosDesdeLaPolitica()
    {
        var cp = Corpus.Vigentes.Single(f => f.Id == "requisitos-de-la-localidad#codigo-postal");
        Assert.That(cp.Texto, Does.Contain(LocalidadPolicy.RequisitoDelCodigoPostal));
        Assert.That(Corpus.Vigentes.Count(f => f.Pagina.StartsWith("requisitos-", StringComparison.Ordinal)), Is.EqualTo(12));
    }

    [Test(Description = "REQ-IA-26 por estado: un borrador nunca es candidato")]
    public void Borrador_NoEsCandidato()
    {
        Assert.That(Corpus.Fragmentos.Any(f => f.Estado == "borrador"), Is.True);
        Assert.That(Recuperador.Buscar(Sinonimos.Aplicar("¿Cómo exporto las localidades a Excel?").Texto).Select(r => r.Fragmento.Estado), Has.None.EqualTo("borrador"));
    }

    private static readonly (string Pregunta, string? Esperado)[] Conjunto =
    [
        ("¿Cómo doy de alta una localidad?", "alta-de-una-localidad#dar-de-alta"),
        ("¿Dónde agrego una ciudad?", "alta-de-una-localidad#dar-de-alta"),
        ("¿Cómo borro una localidad?", "editar-o-eliminar-una-localidad#eliminar"),
        ("¿Puedo cambiar los habitantes?", "editar-o-eliminar-una-localidad#editar"),
        ("¿Cuántos dígitos lleva el código postal?", "requisitos-de-la-localidad#codigo-postal"),
        ("¿Puedo repetir el nombre?", "requisitos-de-la-localidad#nombre"),
        ("¿Dónde cargo una encuesta?", "cargar-una-encuesta#los-tres-pasos"),
        ("¿Cuántos pasos tiene la encuesta?", "cargar-una-encuesta#los-tres-pasos"),
        ("¿Qué edad se acepta?", "requisitos-de-la-encuesta#edad"),
        ("¿Cuántos minutos puedo poner?", "requisitos-de-la-encuesta#minutos"),
        ("¿Por qué los medios no suman 100 %?", "leer-los-resultados#medios"),
        ("¿Qué es la mediana?", "glosario#mediana"),
        ("¿Qué puedo hacer acá?", "inicio#pantallas"),
        ("¿Por qué no veo lo de mi compañero?", "por-que-no-veo-lo-de-otro#cada-sesion-ve-solo-sus-datos"),
        ("¿Cómo cargo una localidad por la API?", "derivacion-api#otra-aplicacion"),
        ("¿Qué prueba cubre el ABM?", "derivacion-pruebas#pruebas"),
        ("¿Qué es la frecuencia?", "glosario#frecuencia"),
        ("¿Qué significa multirrespuesta?", "glosario#multirrespuesta"),
        ("¿Cómo exporto a Excel?", null),
        ("¿Qué es un titular?", null),
        ("¿Cuál es el horario del 110?", null)
    ];

    [Test(Description = "recall@3 léxico ≥ 0,9 y 100 % de «ninguna» en las no respondibles")]
    public void RecallLexico_SobreUmbral()
    {
        var respondibles = Conjunto.Where(c => c.Esperado is not null).ToList();
        var aciertos = respondibles.Count(c => Recuperador.Buscar(Sinonimos.Aplicar(c.Pregunta).Texto).Any(r => r.Fragmento.Id == c.Esperado));
        var ningunas = Conjunto.Where(c => c.Esperado is null).All(c => Recuperador.Buscar(Sinonimos.Aplicar(c.Pregunta).Texto).Count == 0);
        TestContext.Out.WriteLine($"recall@3 = {aciertos}/{respondibles.Count}");
        foreach (var c in Conjunto)
        {
            var top = Recuperador.Buscar(Sinonimos.Aplicar(c.Pregunta).Texto).Select(r => r.Fragmento.Id).ToList();
            if (c.Esperado is null ? top.Count > 0 : !top.Contains(c.Esperado)) TestContext.Out.WriteLine($"FALLA «{c.Pregunta}» → [{string.Join(", ", top)}]");
        }
        Assert.Multiple(() =>
        {
            Assert.That((double)aciertos / respondibles.Count, Is.GreaterThanOrEqualTo(0.9));
            Assert.That(ningunas, Is.True);
        });
    }
}
