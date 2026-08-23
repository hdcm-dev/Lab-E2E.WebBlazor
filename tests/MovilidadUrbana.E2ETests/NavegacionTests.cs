using Microsoft.Playwright;

namespace MovilidadUrbana.E2ETests;

[TestFixture]
public class NavegacionTests : PruebaE2E
{
    [Test]
    [Description("La portada ofrece acceso a las dos pantallas")]
    public async Task LaPortadaOfreceAccesoALasDosPantallas()
    {
        await IrAAsync("/");

        await Expect(Page).ToHaveTitleAsync(new Regex("Inicio"));
        await Expect(Page.GetByTestId("titulo")).ToHaveTextAsync("Demostración de pruebas E2E");
        await Expect(Page.GetByTestId("ir-localidades")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("ir-encuesta")).ToBeVisibleAsync();
    }

    [Test]
    [Description("El menú marca la página activa")]
    public async Task ElMenuMarcaLaPaginaActiva()
    {
        await IrAAsync("/localidades");

        await Expect(Page.GetByTestId("nav-localidades")).ToHaveAttributeAsync("aria-current", "page");
        await Expect(Page.GetByTestId("nav-encuesta")).Not.ToHaveAttributeAsync("aria-current", "page");
    }

    [Test]
    [Description("Desde la portada se llega al ABM y a la encuesta")]
    public async Task DesdeLaPortadaSeLlegaAlAbmYALaEncuesta()
    {
        await IrAAsync("/");

        await Page.GetByTestId("ir-localidades").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/localidades$"));
        await Expect(Page.GetByTestId("titulo")).ToHaveTextAsync("Localidades");

        // La navegación siguiente ya ocurre dentro del mismo circuito, sin recargar la página.
        await IrPorMenuAsync("nav-encuesta");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/encuesta$"));
        await Expect(Page.GetByTestId("etiqueta-paso")).ToContainTextAsync("Paso 1 de 3");
    }

    [Test]
    [Description("Una dirección inexistente muestra la pantalla de no encontrado")]
    public async Task UnaDireccionInexistenteMuestraLaPantallaDeNoEncontrado()
    {
        await IrAAsync("/ruta-que-no-existe");

        await Expect(Page.GetByTestId("titulo")).ToHaveTextAsync("No encontrado");
    }
}
