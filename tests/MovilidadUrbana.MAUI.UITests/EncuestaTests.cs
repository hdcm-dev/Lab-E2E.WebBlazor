using MovilidadUrbana.MAUI.UITests.Infrastructure;

namespace MovilidadUrbana.MAUI.UITests;

/// <summary>La pestaña Encuesta: el asistente de tres pasos, su validación, el registro y el resumen.</summary>
[TestFixture]
public class EncuestaTests : ScreenTestBase
{
    [SetUp]
    public void IrALaEncuesta()
    {
        GoToTab("Encuesta");
        FindById("etiqueta-paso");
        if (Exists("nueva-encuesta")) Tap("nueva-encuesta");
    }

    [Test]
    [Description("«Siguiente» con el paso 1 vacío no avanza y muestra el aviso")]
    public void NoAvanzaConElPaso1Vacio()
    {
        Assume.That(GetText("etiqueta-paso"), Is.EqualTo("Paso 1 de 3"));

        Tap("siguiente");

        FindById("aviso");
        Assert.That(GetText("etiqueta-paso"), Is.EqualTo("Paso 1 de 3"));
        Capture(nameof(NoAvanzaConElPaso1Vacio));
    }

    [Test]
    [Description("Recorre los tres pasos con una distancia con coma, registra y muestra el resumen con «12,5 km»")]
    public void RecorreRegistraYMuestraElResumen()
    {
        TypeText("nombre", "Ana Perez");
        TypeText("edad", "34");
        ScrollIntoView("localidad");
        Pick("localidad", "Corrientes");
        Tap("siguiente");

        FindById("colectivo").Click();
        ScrollToText("Todos los días").Click();
        Tap("siguiente");

        FindById("distancia");
        TypeText("distancia", "12,5");
        TypeText("minutos", "45");
        ScrollToText("Trabajo").Click();
        Capture(nameof(RecorreRegistraYMuestraElResumen) + "-paso3");
        Tap("registrar");

        FindById("encuesta-completada");
        Assert.That(GetText("etiqueta-paso"), Is.EqualTo("Encuesta completada"));
        ScrollToText("12,5 km");
        Capture(nameof(RecorreRegistraYMuestraElResumen) + "-resumen");

        ScrollIntoView("nueva-encuesta").Click();
        Assert.That(GetText("etiqueta-paso"), Is.EqualTo("Paso 1 de 3"));
    }
}
