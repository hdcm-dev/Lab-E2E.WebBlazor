namespace WebBlazor.E2E.Base.Login.E2ETests;

/// <summary>
/// Lo que este proyecto agrega sobre el HolaMundo base es una sola cosa: la
/// superficie está detrás de un acceso. Por eso el caso de prueba es el mismo, y
/// lo único que cambia es cómo se llega al estado conocido del que parte.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HolaMundoE2ETest : PruebaDeSuperficie
{
    // Iniciar en estado conocido: la pantalla abierta y con la sesión establecida.
    [SetUp]
    public async Task Setup()
    {
        await Page.GotoAsync("/login");
        await IngresarAsync();

        await Page.GotoAsync("/HolaMundo");

        // La superficie llega pintada antes de que el circuito abra, y en esa ventana
        // el botón se ve y se puede clickear pero no responde. `Expect` reintenta:
        // la prueba queda detenida hasta que la superficie declara que ya es interactiva.
        await EsperarCircuitoAbiertoAsync();
    }

    [Test]
    [Description("Mostrar Mensaje de texto")]
    public async Task MostrarMensaje()
    {
        string frase = "Hola mundo! - que tal?";

        await Page.GetByTestId("campo-frase").FillAsync(frase);
        await Page.GetByTestId("boton-mostrar-frase").ClickAsync();
        await Expect(Page.GetByTestId("campo-mensaje")).ToHaveTextAsync(frase);
    }
}
