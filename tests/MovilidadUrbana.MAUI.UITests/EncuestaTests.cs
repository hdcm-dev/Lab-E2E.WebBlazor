using MovilidadUrbana.MAUI.UITests.Infraestructura;

namespace MovilidadUrbana.MAUI.UITests;

/// <summary>La pestaña Encuesta: el asistente de tres pasos, su validación, el registro y el resumen.</summary>
[TestFixture]
public class EncuestaTests : PruebaDePantalla
{
    [SetUp]
    public void IrALaEncuesta()
    {
        IrAPestaña("Encuesta");
        Elemento("etiqueta-paso");
        if (Existe("nueva-encuesta")) Tocar("nueva-encuesta");
    }

    [Test]
    [Description("«Siguiente» con el paso 1 vacío no avanza y muestra el aviso")]
    public void NoAvanzaConElPaso1Vacio()
    {
        Assume.That(Texto("etiqueta-paso"), Is.EqualTo("Paso 1 de 3"));

        Tocar("siguiente");

        Elemento("aviso");
        Assert.That(Texto("etiqueta-paso"), Is.EqualTo("Paso 1 de 3"));
        Capturar(nameof(NoAvanzaConElPaso1Vacio));
    }

    [Test]
    [Description("Recorre los tres pasos con una distancia con coma, registra y muestra el resumen con «12,5 km»")]
    public void RecorreRegistraYMuestraElResumen()
    {
        Escribir("nombre", "Ana Perez");
        Escribir("edad", "34");
        Ver("localidad");
        Elegir("localidad", "Corrientes");
        Tocar("siguiente");

        Elemento("colectivo").Click();
        VerTexto("Todos los días").Click();
        Tocar("siguiente");

        Elemento("distancia");
        Escribir("distancia", "12,5");
        Escribir("minutos", "45");
        VerTexto("Trabajo").Click();
        Capturar(nameof(RecorreRegistraYMuestraElResumen) + "-paso3");
        Tocar("registrar");

        Elemento("encuesta-completada");
        Assert.That(Texto("etiqueta-paso"), Is.EqualTo("Encuesta completada"));
        VerTexto("12,5 km");
        Capturar(nameof(RecorreRegistraYMuestraElResumen) + "-resumen");

        Ver("nueva-encuesta").Click();
        Assert.That(Texto("etiqueta-paso"), Is.EqualTo("Paso 1 de 3"));
    }
}
