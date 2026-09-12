namespace WebBlazor.E2E.Base.Login.E2ETests;

/// <summary>
/// Lo que toda prueba de este proyecto necesita antes de mirar una superficie: cómo
/// se abre el navegador y cómo se llega a un estado de sesión conocido.
/// </summary>
/// <remarks>
/// Acá va solo lo que es común a todos los casos y no dice nada sobre ninguno. Lo
/// que un caso concreto necesita —qué credencial usa, a qué superficie entra— lo
/// declara ese caso, porque es parte de lo que la prueba está afirmando.
/// </remarks>
public abstract class PruebaDeSuperficie : PageTest
{
    /// <summary>Dónde escucha la aplicación bajo prueba.</summary>
    protected const string UrlBase = "https://localhost:7212";

    /// <summary>La credencial de laboratorio que el servicio de identidad admite.</summary>
    protected const string Identificador = "admin";

    /// <summary>El secreto de esa credencial.</summary>
    protected const string Secreto = "admin";

    // El certificado de desarrollo no lo valida el navegador de Playwright. La URL
    // base se declara una sola vez y las llamadas quedan relativas.
    public override BrowserNewContextOptions ContextOptions() =>
        new() { IgnoreHTTPSErrors = true, BaseURL = UrlBase };

    /// <summary>
    /// Ingresa por la superficie de acceso, que es el circuito que la aplicación
    /// ofrece. La cookie de sesión la emite el servidor al aceptar el POST y el
    /// contexto del navegador la conserva: no se fabrica acá, porque una cookie
    /// inventada no está firmada y el guard la rechaza.
    /// </summary>
    protected async Task IngresarAsync(string? identificador = null, string? secreto = null)
    {
        await Page.GetByTestId("campo-usuario").FillAsync(identificador ?? Identificador);
        await Page.GetByTestId("campo-clave").FillAsync(secreto ?? Secreto);
        await Page.GetByTestId("boton-ingresar").ClickAsync();
    }

    /// <summary>
    /// Espera a que la superficie declare que su circuito ya abrió. Antes de eso el
    /// marcado está pintado pero los manejadores no están conectados, y un clic no
    /// lo escucha nadie.
    /// </summary>
    protected Task EsperarCircuitoAbiertoAsync() =>
        Expect(Page.GetByTestId("estado-app")).ToHaveAttributeAsync("data-interactivo", "true");
}
