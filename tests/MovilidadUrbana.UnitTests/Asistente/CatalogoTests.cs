using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Components;

namespace MovilidadUrbana.UnitTests.Asistente;

/// <summary>Conformidad catálogo ↔ <c>@page</c> ↔ <c>[SupplyParameterFromQuery]</c> (K6 §14.3).</summary>
public class CatalogoTests
{
    private static IEnumerable<string> Inconformidades(CatalogoDeDestinos catalogo)
    {
        var rutas = RutasDeLaAplicacion.Plantillas().ToList();
        foreach (var d in catalogo.Todos)
        {
            if (!rutas.Any(r => string.Equals(r, d.Plantilla, StringComparison.OrdinalIgnoreCase) || (r.Contains('{') && d.Plantilla.StartsWith(r.Split('/')[0], StringComparison.Ordinal))))
                yield return $"{d.Id}: ruta {d.Plantilla}";
            var leidos = RutasDeLaAplicacion.ParametrosDeConsulta(d.Plantilla);
            foreach (var clave in d.Consulta.Keys.Concat(d.Parametros).Where(c => !leidos.Contains(c)))
                yield return $"{d.Id}: la página no lee «{clave}»";
        }
    }

    [Test]
    public void Catalogo_ConformeConLasPaginas() => Assert.That(Inconformidades(new CatalogoDeDestinos()), Is.Empty);

    [Test(Description = "Par rojo: un parámetro que la página no lee se detecta")]
    public void ParametroQueLaPaginaNoLee_SeDetecta()
    {
        var malo = new CatalogoDeDestinos([.. CatalogoDeDestinos.Definiciones.Where(d => d.Id != Destinos.ResultadosResumen),
            new DefinicionDeDestino(Destinos.ResultadosResumen, "Ver resultados", "resultados", new Dictionary<string, string>(), ["vista", "orden"], [])]);
        Assert.That(Inconformidades(malo), Has.Some.Contains("«orden»"));
    }

    [Test(Description = "Un catálogo con una ruta inexistente no arranca")]
    public void RutaInexistente_CortaElArranque()
    {
        var malo = new CatalogoDeDestinos([new DefinicionDeDestino("x", "X", "admin", new Dictionary<string, string>(), [], [])]);
        Assert.Throws<InvalidOperationException>(() => malo.ValidarContra(RutasDeLaAplicacion.Plantillas()));
        Assert.DoesNotThrow(() => new CatalogoDeDestinos().ValidarContra(RutasDeLaAplicacion.Plantillas()));
    }

    [TestCase("http://x")]
    [TestCase("/admin")]
    [TestCase("a//b")]
    public void PlantillaNoRelativa_Rechazada(string plantilla) =>
        Assert.Throws<InvalidOperationException>(() => _ = new CatalogoDeDestinos([new DefinicionDeDestino("x", "X", plantilla, new Dictionary<string, string>(), [], [])]));

    [Test]
    public void IdDuplicado_Rechazado() =>
        Assert.Throws<InvalidOperationException>(() => _ = new CatalogoDeDestinos([.. CatalogoDeDestinos.Definiciones, CatalogoDeDestinos.Definiciones[0]]));

    [Test]
    public void ParametroNoDeclarado_SeDescarta()
    {
        var chip = new CatalogoDeDestinos().Resolver(Destinos.LocalidadesAlta, new Dictionary<string, string?> { ["nombre"] = "Goya", ["admin"] = "1" });
        Assert.That(chip.Href, Is.EqualTo("localidades?alta=1&nombre=Goya"));
    }

    [Test(Description = "Cada destino declarado por el corpus vigente existe en el catálogo")]
    public void DestinosDelCorpus_ExistenEnElCatalogo()
    {
        var catalogo = new CatalogoDeDestinos();
        Assert.That(new CorpusDeAyuda().Vigentes.SelectMany(f => f.Destinos).Distinct().Where(d => !catalogo.Existe(d)), Is.Empty);
    }
}
