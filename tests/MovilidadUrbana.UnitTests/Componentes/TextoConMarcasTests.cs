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
