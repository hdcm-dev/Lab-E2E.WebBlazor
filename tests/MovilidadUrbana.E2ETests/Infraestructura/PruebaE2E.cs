using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MovilidadUrbana.E2ETests;

/// <summary>
/// Base de todas las pruebas. Hereda de <see cref="PageTest"/>, que da a cada prueba una página
/// nueva dentro de su propio <c>BrowserContext</c>.
///
/// Aporta las dos cosas que esta aplicación necesita y que no vienen de fábrica: una sesión propia
/// por prueba —el aislamiento de datos, que en el servidor es imprescindible— y la espera a que el
/// circuito de Blazor esté conectado antes de tocar nada.
/// </summary>
public abstract class PruebaE2E : PageTest
{
    private const string CookieDeSesion = "sesion-movilidad";

    private const string DispositivoMovil = "Pixel 7";

    /// <summary>Emulación móvil: es el equivalente al proyecto `mobile-chrome` del runner de JavaScript.</summary>
    private static bool EmularMovil =>
        string.Equals(Environment.GetEnvironmentVariable("EMULAR_MOVIL"), "true", StringComparison.OrdinalIgnoreCase);

    public override BrowserNewContextOptions ContextOptions()
    {
        BrowserNewContextOptions opciones;
        if (EmularMovil)
        {
            // Si el descriptor no estuviera, la emulación caería en silencio a escritorio y las
            // pruebas pasarían igual, ocultando que la configuración móvil no se ejercitó.
            if (!Playwright.Devices.TryGetValue(DispositivoMovil, out var dispositivo))
            {
                throw new InvalidOperationException(
                    $"Se pidió emulación móvil pero Playwright no conoce el dispositivo «{DispositivoMovil}».");
            }

            opciones = dispositivo;
        }
        else
        {
            opciones = new BrowserNewContextOptions();
        }

        opciones.BaseURL = ServidorDeLaAplicacion.UrlBase;
        // Los separadores de miles y decimales forman parte de lo que se verifica.
        opciones.Locale = "es-AR";
        opciones.TimezoneId = "America/Argentina/Buenos_Aires";
        return opciones;
    }

    /// <summary>
    /// Cada prueba estrena su cookie de sesión y, con ella, su propio conjunto de datos en el
    /// servidor: por eso pueden correr en paralelo contra una única instancia y una única base.
    ///
    /// Corre después del `[SetUp]` de <see cref="PageTest"/>, que es el que crea el contexto.
    /// </summary>
    [SetUp]
    public async Task EstrenarSesionAsync()
    {
        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = CookieDeSesion,
                Value = Guid.NewGuid().ToString("n"),
                Url = ServidorDeLaAplicacion.UrlBase
            }
        ]);
    }

    /// <summary>Navega a una ruta y devuelve el control recién cuando la página responde a la interacción.</summary>
    protected async Task IrAAsync(string ruta)
    {
        await Page.GotoAsync(ruta);
        await EsperarInteractivoAsync();
    }

    /// <summary>
    /// Espera a que el circuito de Blazor esté conectado.
    ///
    /// Una aplicación interactive server se entrega primero como HTML prerenderizado: los botones
    /// ya se ven, pero todavía no hay nadie del otro lado que escuche el click. `MainLayout`
    /// publica un testigo que pasa a `true` cuando el circuito quedó establecido; sin esta espera
    /// las pruebas fallan de manera intermitente y solo en las máquinas más lentas.
    /// </summary>
    protected async Task EsperarInteractivoAsync() =>
        await Expect(Page.GetByTestId("estado-app")).ToHaveAttributeAsync("data-interactivo", "true");

    /// <summary>
    /// Navega usando la barra superior. En viewport chico el menú viene colapsado, así que primero
    /// hay que desplegarlo: sin esto la misma prueba pasa en escritorio y falla en móvil.
    /// </summary>
    protected async Task IrPorMenuAsync(string testid)
    {
        var alternador = Page.Locator(".navbar-toggler");
        if (await alternador.IsVisibleAsync())
        {
            await alternador.ClickAsync();
            await Expect(Page.Locator("#menu")).ToHaveClassAsync(new Regex("show"));
        }

        await Page.GetByTestId(testid).ClickAsync();
    }
}
