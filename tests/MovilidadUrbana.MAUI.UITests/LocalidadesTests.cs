using MovilidadUrbana.MAUI.UITests.Infraestructura;

namespace MovilidadUrbana.MAUI.UITests;

/// <summary>
/// La pestaña Localidades: lista, filtro, alta, edición y baja. Cada prueba deja la lista como la
/// encontró, así el orden no importa y el dispositivo no necesita limpiarse entre corridas.
/// </summary>
[TestFixture]
public class LocalidadesTests : PruebaDePantalla
{
    [SetUp]
    public void IrALaLista()
    {
        IrAPestaña("Localidades");
        Elemento("lista-localidades");
    }

    [Test]
    [Description("Un filtro sin coincidencias muestra el estado «sin resultados» y «Limpiar filtro» lo deshace")]
    public void FiltrarSinCoincidenciasYLimpiar()
    {
        Escribir("buscar", "zzz");

        Elemento("estado-sin-resultados");
        Capturar(nameof(FiltrarSinCoincidenciasYLimpiar));
        PorTexto("Limpiar filtro").Click();

        EsperarQueDesaparezca("estado-sin-resultados");
        Assert.That(Texto("buscar"), Is.EqualTo("Nombre o código postal").Or.Empty);
    }

    [Test]
    [Description("Guardar con los campos vacíos muestra un error por campo y no vuelve a la lista")]
    public void AltaInvalidaMuestraErrores()
    {
        Tocar("agregar");
        Elemento("guardar");
        CerrarTeclado();

        Tocar("guardar");

        Elemento("aviso");
        Assert.That(Existe("guardar"), Is.True, "Tendría que seguir en el editor");
        Capturar(nameof(AltaInvalidaMuestraErrores));
        App.Navigate().Back();
        Elemento("lista-localidades");
    }

    [Test]
    [Description("Alta válida, edición y baja confirmada: la lista vuelve a quedar como estaba")]
    public void AltaEdicionYBaja()
    {
        // Nombre único por corrida: si una corrida anterior quedó a medias, no choca con su resto.
        var nombre = $"Prueba {DateTime.Now:HHmmss}";
        var renombrada = nombre + " B";

        Tocar("agregar");
        Elemento("guardar");
        CerrarTeclado();
        Escribir("nombre", nombre);
        Elegir("provincia", "Corrientes");
        Ver("codigo-postal");
        Escribir("codigo-postal", "3450");
        Ver("habitantes");
        Escribir("habitantes", "90000");
        Tocar("guardar");

        Elemento("lista-localidades");
        var fila = PorTexto(nombre);
        Capturar(nameof(AltaEdicionYBaja) + "-alta");

        fila.Click();
        Elemento("guardar");
        Assert.That(Texto("nombre"), Is.EqualTo(nombre));
        Escribir("nombre", renombrada);
        Tocar("guardar");
        Elemento("lista-localidades");
        PorTexto(renombrada).Click();

        Ver("eliminar").Click();
        PorTexto("Eliminar").Click();

        Elemento("lista-localidades");
        Assert.That(App.FindElements(OpenQA.Selenium.Appium.MobileBy.AndroidUIAutomator("new UiSelector().text(\"" + renombrada + "\")")), Is.Empty);
        Capturar(nameof(AltaEdicionYBaja) + "-baja");
    }
}
