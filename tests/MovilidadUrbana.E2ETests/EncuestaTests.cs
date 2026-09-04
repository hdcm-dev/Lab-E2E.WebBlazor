using Microsoft.Playwright;

namespace MovilidadUrbana.E2ETests;

[TestFixture]
public class EncuestaTests : PruebaE2E
{
    [SetUp]
    public async Task AbrirLaEncuestaAsync() => await IrAAsync("/encuesta");

    /// <summary>Completa el paso indicado con datos válidos, sin avanzar.</summary>
    private async Task CompletarPaso1Async()
    {
        await Page.GetByTestId("campo-nombre").FillAsync("Ana Pérez");
        await Page.GetByTestId("campo-edad").FillAsync("34");
        await Page.GetByTestId("campo-localidad").SelectOptionAsync("Corrientes");
    }

    private async Task CompletarPaso2Async()
    {
        await Page.GetByTestId("medio-colectivo").CheckAsync();
        await Page.GetByTestId("medio-bicicleta").CheckAsync();
        await Page.GetByTestId("campo-frecuencia").SelectOptionAsync("diaria");
    }

    private async Task CompletarPaso3Async()
    {
        await Page.GetByTestId("campo-distancia").FillAsync("12.5");
        await Page.GetByTestId("campo-minutos").FillAsync("45");
        await Page.GetByTestId("campo-motivo").SelectOptionAsync("trabajo");
    }

    private Task SiguienteAsync() => Page.GetByTestId("boton-siguiente").ClickAsync();

    [Test]
    [Description("Arranca en el paso 1 con el anterior deshabilitado")]
    public async Task ArrancaEnElPaso1ConElAnteriorDeshabilitado()
    {
        await Expect(Page.GetByTestId("paso-1")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("paso-2")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("paso-3")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("etiqueta-paso")).ToHaveTextAsync("Paso 1 de 3");
        await Expect(Page.GetByTestId("boton-anterior")).ToBeDisabledAsync();
        await Expect(Page.GetByTestId("boton-finalizar")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("contador-encuestas")).ToHaveTextAsync("Registradas: 0");
    }

    [Test]
    [Description("El desplegable de localidades se alimenta del ABM")]
    public async Task ElDesplegableDeLocalidadesSeAlimentaDelAbm()
    {
        var opciones = Page.GetByTestId("campo-localidad").Locator("option");
        await Expect(opciones).ToHaveCountAsync(3); // marcador de posición + 2 localidades sembradas
        await Expect(opciones.Nth(1)).ToHaveTextAsync("Corrientes (Corrientes)");
    }

    [Test]
    [Description("No avanza del paso 1 con datos inválidos")]
    public async Task NoAvanzaDelPaso1ConDatosInvalidos()
    {
        await Page.GetByTestId("campo-edad").FillAsync("12");
        await SiguienteAsync();

        await Expect(Page.GetByTestId("aviso")).ToHaveTextAsync("Complete los datos del paso antes de continuar.");
        await Expect(Page.GetByTestId("error-nombre")).ToHaveTextAsync(new Regex("mínimo 3 caracteres"));
        await Expect(Page.GetByTestId("error-edad")).ToHaveTextAsync(new Regex("entre 16 y 110"));
        await Expect(Page.GetByTestId("error-localidad")).ToHaveTextAsync(new Regex("Seleccione una localidad"));
        await Expect(Page.GetByTestId("paso-1")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("etiqueta-paso")).ToHaveTextAsync("Paso 1 de 3");
    }

    [Test]
    [Description("No avanza del paso 2 sin medios ni frecuencia")]
    public async Task NoAvanzaDelPaso2SinMediosNiFrecuencia()
    {
        await CompletarPaso1Async();
        await SiguienteAsync();
        await Expect(Page.GetByTestId("paso-2")).ToBeVisibleAsync();

        await SiguienteAsync();
        await Expect(Page.GetByTestId("error-medios")).ToHaveTextAsync("Seleccione al menos un medio de transporte.");
        await Expect(Page.GetByTestId("error-frecuencia")).ToHaveTextAsync(new Regex("Seleccione la frecuencia"));
        await Expect(Page.GetByTestId("etiqueta-paso")).ToHaveTextAsync("Paso 2 de 3");
    }

    [Test]
    [Description("Permite volver atrás conservando lo cargado")]
    public async Task PermiteVolverAtrasConservandoLoCargado()
    {
        await CompletarPaso1Async();
        await SiguienteAsync();
        await CompletarPaso2Async();
        await SiguienteAsync();

        await Expect(Page.GetByTestId("paso-3")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("boton-siguiente")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("boton-finalizar")).ToBeVisibleAsync();

        await Page.GetByTestId("boton-anterior").ClickAsync();
        await Expect(Page.GetByTestId("paso-2")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("medio-colectivo")).ToBeCheckedAsync();
        await Expect(Page.GetByTestId("campo-frecuencia")).ToHaveValueAsync("diaria");

        await Page.GetByTestId("boton-anterior").ClickAsync();
        await Expect(Page.GetByTestId("campo-nombre")).ToHaveValueAsync("Ana Pérez");
        await Expect(Page.GetByTestId("boton-anterior")).ToBeDisabledAsync();
    }

    [Test]
    [Description("El indicador de pasos acompaña el avance")]
    public async Task ElIndicadorDePasosAcompanaElAvance()
    {
        // Los tres estados de paso del catálogo: pendiente, actual y completado.
        var pasos = Page.Locator("[data-paso]");
        await Expect(pasos).ToHaveCountAsync(3);
        await Expect(pasos.Nth(0)).ToHaveAttributeAsync("aria-current", "step");
        await Expect(pasos.Nth(1)).ToHaveClassAsync(new Regex("mq-paso--pendiente"));

        await CompletarPaso1Async();
        await SiguienteAsync();
        await Expect(pasos.Nth(1)).ToHaveAttributeAsync("aria-current", "step");
        await Expect(pasos.Nth(0)).ToHaveClassAsync(new Regex("mq-paso--completado"));

        await CompletarPaso2Async();
        await SiguienteAsync();
        await Expect(pasos.Nth(2)).ToHaveAttributeAsync("aria-current", "step");
        await Expect(pasos.Nth(1)).ToHaveClassAsync(new Regex("mq-paso--completado"));
    }

    [Test]
    [Description("No finaliza con el paso 3 incompleto")]
    public async Task NoFinalizaConElPaso3Incompleto()
    {
        await CompletarPaso1Async();
        await SiguienteAsync();
        await CompletarPaso2Async();
        await SiguienteAsync();

        await Page.GetByTestId("campo-distancia").FillAsync("900");
        await Page.GetByTestId("boton-finalizar").ClickAsync();

        await Expect(Page.GetByTestId("error-distancia")).ToHaveTextAsync(new Regex("entre 0 y 500 km"));
        await Expect(Page.GetByTestId("error-minutos")).ToHaveTextAsync(new Regex("entre 1 y 600 minutos"));
        await Expect(Page.GetByTestId("resumen")).ToBeHiddenAsync();
    }

    [Test]
    [Description("Recorre los tres pasos, muestra el resumen y registra la respuesta")]
    public async Task RecorreLosTresPasosMuestraElResumenYRegistraLaRespuesta()
    {
        await CompletarPaso1Async();
        await SiguienteAsync();
        await CompletarPaso2Async();
        await SiguienteAsync();
        await CompletarPaso3Async();
        await Page.GetByTestId("boton-finalizar").ClickAsync();

        await Expect(Page.GetByTestId("mensaje-envio")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("resumen-persona")).ToHaveTextAsync("Ana Pérez (34 años)");
        await Expect(Page.GetByTestId("resumen-localidad")).ToHaveTextAsync("Corrientes");
        await Expect(Page.GetByTestId("resumen-medios")).ToHaveTextAsync("Colectivo, Bicicleta");
        await Expect(Page.GetByTestId("resumen-frecuencia")).ToHaveTextAsync("Todos los días");
        await Expect(Page.GetByTestId("resumen-distancia")).ToHaveTextAsync("12,5 km");
        await Expect(Page.GetByTestId("resumen-minutos")).ToHaveTextAsync("45 min");
        await Expect(Page.GetByTestId("resumen-motivo")).ToHaveTextAsync("Trabajo");

        await Expect(Page.GetByTestId("formulario")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("boton-reiniciar")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("etiqueta-paso")).ToHaveTextAsync("Encuesta completada");

        // La respuesta quedó guardada en el servidor: el contador lo confirma tras recargar.
        await Expect(Page.GetByTestId("contador-encuestas")).ToHaveTextAsync("Registradas: 1");
        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("contador-encuestas")).ToHaveTextAsync("Registradas: 1");
    }

    [Test]
    [Description("«Nueva encuesta» devuelve el asistente al paso 1")]
    public async Task NuevaEncuestaDevuelveElAsistenteAlPaso1()
    {
        await CompletarPaso1Async();
        await SiguienteAsync();
        await CompletarPaso2Async();
        await SiguienteAsync();
        await CompletarPaso3Async();
        await Page.GetByTestId("boton-finalizar").ClickAsync();

        await Page.GetByTestId("boton-reiniciar").ClickAsync();

        await Expect(Page.GetByTestId("paso-1")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("etiqueta-paso")).ToHaveTextAsync("Paso 1 de 3");
        await Expect(Page.GetByTestId("campo-nombre")).ToHaveValueAsync("");
        await Expect(Page.GetByTestId("contador-encuestas")).ToHaveTextAsync("Registradas: 1");
    }
}
