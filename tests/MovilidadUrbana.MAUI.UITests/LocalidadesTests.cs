using MovilidadUrbana.MAUI.UITests.Infrastructure;

namespace MovilidadUrbana.MAUI.UITests;

/// <summary>
/// La pestaña Localidades: lista, filtro, alta, edición y baja. Cada prueba deja la lista como la
/// encontró, así el orden no importa y el dispositivo no necesita limpiarse entre corridas.
/// </summary>
[TestFixture]
public class LocalidadesTests : ScreenTestBase
{
    [SetUp]
    public void IrALaLista()
    {
        GoToTab("Localidades");
        FindById("lista-localidades");
    }

    [Test]
    [Description("Un filtro sin coincidencias muestra el estado «sin resultados» y «Limpiar filtro» lo deshace")]
    public void FiltrarSinCoincidenciasYLimpiar()
    {
        TypeText("buscar", "zzz");

        FindById("estado-sin-resultados");
        Capture(nameof(FiltrarSinCoincidenciasYLimpiar));
        FindByText("Limpiar filtro").Click();

        WaitUntilGone("estado-sin-resultados");
        Assert.That(GetText("buscar"), Is.EqualTo("Nombre o código postal").Or.Empty);
    }

    [Test]
    [Description("Guardar con los campos vacíos muestra un error por campo y no vuelve a la lista")]
    public void AltaInvalidaMuestraErrores()
    {
        Tap("agregar");
        FindById("guardar");
        HideKeyboard();

        Tap("guardar");

        FindById("aviso");
        Assert.That(Exists("guardar"), Is.True, "Tendría que seguir en el editor");
        Capture(nameof(AltaInvalidaMuestraErrores));
        App.Navigate().Back();
        FindById("lista-localidades");
    }

    [Test]
    [Description("Alta válida, edición y baja confirmada: la lista vuelve a quedar como estaba")]
    public void AltaEdicionYBaja()
    {
        // Nombre único por corrida: si una corrida anterior quedó a medias, no choca con su resto.
        var nombre = $"Prueba {DateTime.Now:HHmmss}";
        var renombrada = nombre + " B";

        Tap("agregar");
        FindById("guardar");
        HideKeyboard();
        TypeText("nombre", nombre);
        Pick("provincia", "Corrientes");
        ScrollIntoView("codigo-postal");
        TypeText("codigo-postal", "3450");
        ScrollIntoView("habitantes");
        TypeText("habitantes", "90000");
        Tap("guardar");

        FindById("lista-localidades");
        var fila = FindByText(nombre);
        Capture(nameof(AltaEdicionYBaja) + "-alta");

        fila.Click();
        FindById("guardar");
        Assert.That(GetText("nombre"), Is.EqualTo(nombre));
        TypeText("nombre", renombrada);
        Tap("guardar");
        FindById("lista-localidades");
        FindByText(renombrada).Click();

        ScrollIntoView("eliminar").Click();
        FindByText("Eliminar").Click();

        FindById("lista-localidades");
        Assert.That(App.FindElements(OpenQA.Selenium.Appium.MobileBy.AndroidUIAutomator("new UiSelector().text(\"" + renombrada + "\")")), Is.Empty);
        Capture(nameof(AltaEdicionYBaja) + "-baja");
    }
}
