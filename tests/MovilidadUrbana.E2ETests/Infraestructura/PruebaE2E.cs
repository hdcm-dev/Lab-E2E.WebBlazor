using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace MovilidadUrbana.E2ETests;

/// <summary>
/// Base de todas las pruebas. Hereda de <see cref="PageTest"/>, que da a cada prueba una página
/// nueva dentro de su propio <c>BrowserContext</c>.
///
/// Aporta las tres cosas que esta aplicación necesita y que no vienen de fábrica: una sesión propia
/// por prueba —el aislamiento de datos, que en el servidor es imprescindible—, la espera a que el
/// circuito de Blazor esté conectado antes de tocar nada, y la traza de Playwright de los casos que
/// fallan.
/// </summary>
public abstract class PruebaE2E : PageTest
{
    private const string CookieDeSesion = "sesion-movilidad";

    private const string DispositivoMovil = "Pixel 7";

    private bool _trazando;

    /// <summary>
    /// La traza se graba en todos los casos y se conserva solo en los que fallan. No hay
    /// alternativa: sin reintentos no existe el `on-first-retry` del runner de JavaScript, y una
    /// traza que empieza recién cuando el caso ya falló llega tarde. Se apaga con `TRAZAR=false`.
    /// </summary>
    private static bool TrazaHabilitada =>
        !string.Equals(Environment.GetEnvironmentVariable("TRAZAR"), "false", StringComparison.OrdinalIgnoreCase);

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

        if (TrazaHabilitada)
        {
            await IniciarTrazaAsync();
        }
    }

    /// <summary>
    /// Guarda la traza del caso solo si falló, y la descarta si pasó.
    ///
    /// El binding de .NET no tiene el `trace: 'on-first-retry'` del runner de JavaScript: el
    /// adaptador solo lee `BrowserName`, `ExpectTimeout` y `LaunchOptions` del `.runsettings`, así
    /// que el ciclo de vida de la traza se maneja acá. Un `.zip` por caso fallido se abre con
    /// `playwright show-trace`, y trae el DOM paso a paso, la red y la consola.
    /// </summary>
    [TearDown]
    public async Task GuardarLaTrazaSiFalloAsync()
    {
        if (!_trazando) return;
        _trazando = false;

        var fallo = TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed;
        if (!fallo)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var carpeta = Path.Combine(CarpetaDeResultados(), "trazas");
        Directory.CreateDirectory(carpeta);
        var archivo = Path.Combine(carpeta, $"{NombreDeArchivo(TestContext.CurrentContext.Test.FullName)}.zip");

        await Context.Tracing.StopAsync(new() { Path = archivo });
        TestContext.Progress.WriteLine($"Traza del caso fallido: {archivo}");
    }

    private async Task IniciarTrazaAsync()
    {
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
            Title = TestContext.CurrentContext.Test.Name
        });
        _trazando = true;
    }

    /// <summary>
    /// Misma carpeta que usa el `.runsettings` para los TRX, así CI sube todo con un único paso.
    /// </summary>
    private static string CarpetaDeResultados() =>
        Environment.GetEnvironmentVariable("CARPETA_RESULTADOS")
        ?? Path.Combine(ServidorDeLaAplicacion.RaizDelRepositorio, "resultados");

    private static string NombreDeArchivo(string nombreDelCaso)
    {
        var limpio = string.Join('_', nombreDelCaso.Split(Path.GetInvalidFileNameChars()));
        return limpio.Length <= 120 ? limpio : limpio[^120..];
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
    /// Navega usando la barra lateral del shell. Debajo del punto de quiebre la barra pasa a
    /// navegación superior con los mismos enlaces a la vista: no hay menú que desplegar, así que
    /// la misma llamada sirve en escritorio y en móvil.
    /// </summary>
    protected Task IrPorMenuAsync(string testid) => Page.GetByTestId(testid).ClickAsync();
}
