using Bunit;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Components.Pages;

namespace MovilidadUrbana.UnitTests.Componentes;

/// <summary>La pantalla /resultados (E1): la vista por consulta, el estado vacío y la advertencia de n chico.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ResultadosPaginaTests : ContextoDeComponentes
{
    private async Task ConEncuestasAsync(int n)
    {
        var arnes = new Arnes(habilitada: false);
        if (n > 0) await arnes.SembrarEncuestasAsync(Sesion, FixtureDeEncuestas.Veinte().Take(n));
        Preparar(arnes);
    }

    [Test]
    public async Task VistaMedios_MuestraLasBarras()
    {
        await ConEncuestasAsync(20);
        Ir("resultados?vista=medios");
        var pagina = Render<Web.Components.Pages.Resultados>();
        pagina.WaitForAssertion(() => Assert.That(pagina.FindAll("[data-testid=seccion-medios]"), Has.Count.EqualTo(1)));
        Assert.Multiple(() =>
        {
            Assert.That(pagina.Find("[data-testid=grafico-medios]").GetAttribute("data-forma"), Is.EqualTo("barras"));
            Assert.That(pagina.FindAll("[data-testid=barra]"), Has.Count.EqualTo(6));
            Assert.That(pagina.FindAll("[data-testid=seccion-motivos]"), Is.Empty);
            Assert.That(pagina.Find("[data-testid=total-encuestas]").TextContent, Is.EqualTo("20"));
        });
    }

    [Test(Description = "E-2: una vista desconocida se ignora y la página carga")]
    public async Task VistaDesconocida_SeIgnora()
    {
        await ConEncuestasAsync(20);
        Ir("resultados?vista=xyz");
        var pagina = Render<Web.Components.Pages.Resultados>();
        pagina.WaitForAssertion(() => Assert.That(pagina.FindAll("[data-testid=seccion-motivos]"), Has.Count.EqualTo(1)));
        Assert.That(pagina.Find("[data-testid=grafico-motivos]").GetAttribute("data-forma"), Is.EqualTo("torta"));
        Assert.That(pagina.FindAll("[data-testid=grafico-motivos] [data-testid=porcion]").Sum(p => int.Parse(p.GetAttribute("data-cantidad")!)), Is.EqualTo(20));
    }

    [Test]
    public async Task SinEncuestas_EstadoVacioConChip()
    {
        await ConEncuestasAsync(0);
        Ir("resultados");
        var pagina = Render<Web.Components.Pages.Resultados>();
        pagina.WaitForAssertion(() => Assert.That(pagina.FindAll("[data-testid=sin-encuestas]"), Has.Count.EqualTo(1)));
        var enlace = pagina.Find("[data-testid=sin-encuestas] a");
        Assert.Multiple(() =>
        {
            Assert.That(enlace.TextContent, Is.EqualTo("Empezar una encuesta"));
            Assert.That(enlace.GetAttribute("href"), Is.EqualTo("encuesta/1"));
        });
    }

    [Test]
    public async Task NChico_Advertencia()
    {
        await ConEncuestasAsync(3);
        Ir("resultados");
        var pagina = Render<Web.Components.Pages.Resultados>();
        pagina.WaitForAssertion(() => Assert.That(pagina.FindAll("[data-testid=advertencia-n-chico]"), Has.Count.EqualTo(1)));
    }
}
