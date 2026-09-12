namespace WebBlazor.E2E.Base.HolaMundo.E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HolaMundoE2ETests : PageTest
{
    [SetUp]
    public async Task Setup()
    {
        // La aplicación tiene que estar escuchando acá: la prueba no la levanta. Lo hacen
        // scripts/pruebas.sh y el workflow e2e-holamundo.yml, con esta misma URL.
        await Page.GotoAsync("http://localhost:5027/HolaMundo");

        // La superficie llega pintada antes de que el circuito abra, y en esa ventana
        // el botón se ve y se puede clickear pero no responde. `Expect` reintenta:
        // la prueba queda detenida hasta que la superficie declara que ya es interactiva.
        await Expect(Page.GetByTestId("estado-app"))
            .ToHaveAttributeAsync("data-interactivo", "true");
    }

    [Test]
    [Description("Mostrar Mensaje de texto")]
    public async Task MostrarMensaje()
    {
        string frase = "Hola mundo! - que tal?";

        await Page.GetByTestId("campo-frase").FillAsync(frase);
        await Page.GetByTestId("boton-mostrar-frase").ClickAsync();
        //await Expect(Page.GetByTestId("campo-mensaje")).ToHaveValueAsync(frase);
        await Expect(Page.GetByTestId("campo-mensaje")).ToHaveTextAsync(frase);
    }
}
