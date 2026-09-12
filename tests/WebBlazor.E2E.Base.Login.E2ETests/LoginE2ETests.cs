namespace WebBlazor.E2E.Base.Login.E2ETests;

/// <summary>
/// La superficie de acceso y el guard que la rodea.
/// </summary>
/// <remarks>
/// A diferencia de la superficie Hola Mundo, esta no abre circuito: es SSR estático
/// y su formulario viaja por POST. No hay ventana de hidratación que esperar, porque
/// el navegador procesa el envío como lo que es —una navegación—, y las esperas de
/// Playwright ya cubren las navegaciones.
///
/// Lo que se afirma es lo que la persona ve: la superficie en la que queda y el
/// texto que le dicen. Nunca la cookie, que es el mecanismo con el que la aplicación
/// lo consigue y podría cambiar sin que cambie la promesa.
/// </remarks>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class LoginE2ETests : PruebaDeSuperficie
{
    // El estado conocido del que parten estos casos es *sin sesión*, que es con lo
    // que arranca todo contexto nuevo de Playwright. El [SetUp] solo abre la pantalla.
    [SetUp]
    public Task Setup() => Page.GotoAsync("/login");

    [Test]
    [Description("Una credencial admitida abre la superficie de trabajo")]
    public async Task IngresoAceptadoAbreLaSuperficieDeTrabajo()
    {
        await IngresarAsync();

        // Que la sesión exista se comprueba por lo que la persona puede ver, no
        // leyendo la cookie: se comprueba el efecto, no la implementación.
        await Expect(Page).ToHaveURLAsync($"{UrlBase}/");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inicio" })).ToBeVisibleAsync();
    }

    [Test]
    [Description("Aceptado el ingreso, se vuelve al destino que se había pedido")]
    public async Task IngresoAceptadoVuelveAlDestinoPedido()
    {
        await Page.GotoAsync("/login?returnurl=/HolaMundo");
        await IngresarAsync();

        await Expect(Page).ToHaveURLAsync($"{UrlBase}/HolaMundo");
    }

    [Test]
    [Description("Un destino externo no se honra: el ingreso no es una redirección abierta")]
    public async Task UnDestinoExternoNoSeHonra()
    {
        await Page.GotoAsync("/login?returnurl=https://ejemplo.invalido/");
        await IngresarAsync();

        // El caso se escribe desde la promesa —«de acá no se sale»— y no desde el
        // detalle de a qué ruta local se cae, que es una decisión reversible.
        await Expect(Page).ToHaveURLAsync(new Regex($"^{Regex.Escape(UrlBase)}/"));
    }

    [Test]
    [Description("Una credencial rechazada devuelve al acceso con el mensaje del catálogo")]
    public async Task IngresoRechazadoVuelveAlAccesoConElMensajeDelCatalogo()
    {
        await IngresarAsync(secreto: "lo-que-no-es");

        await Expect(Page.GetByTestId("mensaje-resultado"))
            .ToContainTextAsync("No pudimos validar el ingreso.");
    }

    [Test]
    [Description("El rechazo no dice cuál de los dos campos falló")]
    public async Task ElRechazoNoDistingueQueCampoFallo()
    {
        // La superficie promete un rechazo indiferenciado: distinguir «no existe» de
        // «el secreto no es» le confirma la existencia de la identidad a quien no
        // debería saberlo. Que la promesa se cumpla se ve comparando los dos
        // desenlaces entre sí, no mirando uno solo.
        await IngresarAsync(identificador: "nadie");
        var conIdentificadorInexistente = await Page.GetByTestId("mensaje-resultado").TextContentAsync();

        await Page.GotoAsync("/login");
        await IngresarAsync(secreto: "lo-que-no-es");
        var conSecretoIncorrecto = await Page.GetByTestId("mensaje-resultado").TextContentAsync();

        Assert.That(conSecretoIncorrecto, Is.EqualTo(conIdentificadorInexistente));
    }

    [Test]
    [Description("Lo que falta no se intenta: el navegador retiene el envío incompleto")]
    public async Task UnEnvioIncompletoNoSale()
    {
        await Page.GetByTestId("campo-usuario").FillAsync(Identificador);
        await Page.GetByTestId("boton-ingresar").ClickAsync();

        // Los campos son nativos y llevan `required`: el requisito se enuncia antes
        // del intento y el viaje inútil no ocurre. La superficie no cambió.
        await Expect(Page).ToHaveURLAsync($"{UrlBase}/login");
        await Expect(Page.GetByTestId("mensaje-resultado")).Not.ToBeVisibleAsync();
    }

    [Test]
    [Description("Sin sesión, la superficie protegida devuelve al acceso conservando el destino")]
    public async Task LaSuperficieProtegidaExigeSesion()
    {
        await Page.GotoAsync("/HolaMundo");

        // El guard resuelve llevando a donde corresponde, no devolviendo un error, y
        // se lleva anotado el destino para poder devolver a la persona ahí.
        await Expect(Page).ToHaveURLAsync(new Regex(@"/login\?returnurl=%2FHolaMundo$"));
        await Expect(Page.GetByTestId("boton-ingresar")).ToBeVisibleAsync();
    }

    [Test]
    [Description("El destino sobrevive al rebote: se entra donde se quería entrar")]
    public async Task ElDestinoSobreviveAlRebote()
    {
        // El caso anterior comprueba el rebote; este comprueba que el rebote sirvió
        // para algo. Van separados porque fallan por motivos distintos.
        await Page.GotoAsync("/HolaMundo");
        await IngresarAsync();

        await Expect(Page).ToHaveURLAsync($"{UrlBase}/HolaMundo");
    }

    [Test]
    [Description("Cerrar la sesión devuelve al acceso y revoca el paso")]
    public async Task CerrarLaSesionRevocaElPaso()
    {
        await IngresarAsync();
        await Page.GetByTestId("boton-cerrar-sesion").ClickAsync();

        await Expect(Page.GetByTestId("mensaje-resultado")).ToContainTextAsync("Cerraste la sesión.");

        // Que la salida haya surtido efecto se comprueba volviendo a pedir lo
        // protegido: el desenlace observable, no el estado interno.
        await Page.GotoAsync("/HolaMundo");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/login\?returnurl=%2FHolaMundo$"));
    }
}
