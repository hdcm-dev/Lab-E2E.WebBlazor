using Bunit;
using MovilidadUrbana.Web.Components.Shared.Asistente;

namespace MovilidadUrbana.UnitTests.Componentes;

/// <summary>Defecto 3 de la verificación: el panel mostraba «**Guardar**» crudo.</summary>
public class TextoConMarcasTests
{
    [Test]
    public void ParCerrado_EsNegrita()
    {
        var s = TextoConMarcas.Segmentar("Tocá **Guardar** y después **Siguiente**.");
        Assert.Multiple(() =>
        {
            Assert.That(s.Select(x => x.Texto), Is.EqualTo(new[] { "Tocá ", "Guardar", " y después ", "Siguiente", "." }));
            Assert.That(s.Select(x => x.Negrita), Is.EqualTo(new[] { false, true, false, true, false }));
        });
    }

    [TestCase("Tocá **Guardar [1].", "Tocá Guardar [1].")]
    [TestCase("Sin marcas.", "Sin marcas.")]
    [TestCase("<b>x</b> **y**", "<b>x</b> y")]
    public void SinAsteriscosDobles(string texto, string plano) =>
        Assert.That(TextoConMarcas.Plano(texto), Is.EqualTo(plano));
}

/// <summary>Seguridad: lo que va entre **…** se pinta como texto dentro de &lt;strong&gt;, nunca como marcado.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class TextoMarcadoTests : BunitContext
{
    [TestCase("Tocá **<script>alert(1)</script>** ahora.", "<script>alert(1)</script>")]
    [TestCase("Tocá **<b>Guardar</b>** [1].", "<b>Guardar</b>")]
    [TestCase("**<img src=x onerror=alert(1)>**", "<img src=x onerror=alert(1)>")]
    public void DentroDeLaNegrita_SeEscapa(string texto, string crudo)
    {
        var c = Render<TextoMarcado>(p => p.Add(x => x.Texto, texto));
        var strong = c.Find("strong");
        Assert.Multiple(() =>
        {
            Assert.That(strong.TextContent, Is.EqualTo(crudo), "el contenido llega como texto");
            Assert.That(strong.Children, Is.Empty, "ningún elemento dentro de la negrita");
            Assert.That(c.FindAll("script, b, img"), Is.Empty);
            Assert.That(c.Markup, Does.Contain("&lt;"));
        });
    }
}
