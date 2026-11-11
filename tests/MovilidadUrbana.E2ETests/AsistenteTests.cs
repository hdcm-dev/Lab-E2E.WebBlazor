using Microsoft.Playwright;

namespace MovilidadUrbana.E2ETests;

/// <summary>
/// El asistente de punta a punta en el navegador, sin modelo (la corrida de CI no tiene
/// OLLAMA_*): el panel responde con plantillas, cifras y destinos, y la IA apagada se ve.
/// Los chips abren la acción en la pantalla, donde la persona la confirma.
/// </summary>
[TestFixture]
public class AsistenteTests : E2ETestBase
{
    private async Task PreguntarAsync(string pregunta)
    {
        if (!await Page.GetByTestId("panel-asistente").IsVisibleAsync())
        {
            await Page.GetByTestId("nav-asistente").ClickAsync();
        }
        var antes = await Page.GetByTestId("burbuja-asistente").CountAsync();
        await Page.GetByTestId("asistente-pregunta").FillAsync(pregunta);
        await Page.GetByTestId("asistente-enviar").ClickAsync();
        await Expect(Page.GetByTestId("burbuja-asistente")).ToHaveCountAsync(antes + 1);
    }

    private ILocator Ultima => Page.GetByTestId("burbuja-asistente").Last;

    [Test]
    [Description("Sin modelo, la cifra y el destino siguen y el aviso está a la vista")]
    public async Task SinModelo_CifraDestinoYAviso()
    {
        await IrAAsync("/");
        await PreguntarAsync("¿Cuántas localidades tengo?");

        await Expect(Page.GetByTestId("asistente-aviso")).ToContainTextAsync("La ayuda con IA está apagada");
        await Expect(Ultima.GetByTestId("respuesta-texto")).ToContainTextAsync("2 localidades");
        await Expect(Ultima.GetByTestId("rotulo")).ToHaveTextAsync("Respuesta fija");
        await Expect(Ultima.GetByTestId("chip-destino")).ToHaveTextAsync("Abrir localidades");
    }

    [Test]
    [Description("El chip «Dar de alta una localidad» abre el formulario de alta")]
    public async Task ChipDeAlta_AbreElFormulario()
    {
        await IrAAsync("/");
        await PreguntarAsync("¿Cómo doy de alta una localidad?");

        await Ultima.GetByTestId("chip-destino").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/localidades$"));
        await Expect(Page.GetByTestId("titulo-formulario")).ToHaveTextAsync("Alta de localidad");
        await Expect(Page.GetByTestId("campo-nombre")).ToBeFocusedAsync();
    }

    [Test]
    [Description("«Borrá Resistencia» abre la confirmación existente, y rechazarla no borra nada")]
    public async Task ChipDeBaja_AbreLaConfirmacion_YRechazarNoBorra()
    {
        await IrAAsync("/localidades");
        await PreguntarAsync("Borrá Resistencia");
        await Expect(Ultima.GetByTestId("chip-destino")).ToHaveTextAsync("Eliminar «Resistencia» (Chaco)");

        await Ultima.GetByTestId("chip-destino").ClickAsync();
        await Expect(Page.GetByTestId("dialogo-titulo")).ToHaveTextAsync("Dar de baja la localidad Resistencia");
        await Page.GetByTestId("boton-cancelar-dialogo").ClickAsync();

        await Expect(Page.GetByTestId("dialogo")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("contador")).ToHaveTextAsync("2");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/localidades$"));
    }

    [Test]
    [Description("Una pregunta de afuera recibe el rechazo amable con sugerencias")]
    public async Task Afuera_RechazoConSugerencias()
    {
        await IrAAsync("/");
        await PreguntarAsync("¿A qué hora pasa el 110 por la plaza?");

        await Expect(Ultima).ToHaveAttributeAsync("data-forma", "F7");
        await Expect(Ultima.GetByTestId("sugerencia")).ToHaveCountAsync(3);
        await Expect(Ultima.GetByTestId("chip-destino")).ToHaveCountAsync(0);
    }

    [Test]
    [Description("La aclaración ofrece opciones y al elegir sigue sin volver a preguntar")]
    public async Task Aclaracion_ElegirSigue()
    {
        await IrAAsync("/");
        await PreguntarAsync("¿Cuántas hay en Corrientes?");
        await Expect(Ultima.GetByTestId("opcion")).ToHaveCountAsync(2);

        await Ultima.GetByTestId("opcion").Last.ClickAsync();
        await Expect(Page.GetByTestId("burbuja-asistente")).ToHaveCountAsync(2);
        await Expect(Ultima).Not.ToHaveAttributeAsync("data-forma", "F3");
    }

    [Test]
    [Description("Resultados sin encuestas: estado vacío con el chip para empezar una")]
    public async Task Resultados_SinEncuestas()
    {
        await IrAAsync("/resultados?vista=xyz");

        await Expect(Page.GetByTestId("titulo")).ToHaveTextAsync("Resultados de la encuesta");
        await Expect(Page.GetByTestId("sin-encuestas")).ToContainTextAsync("Empezar una encuesta");
    }

    [Test]
    [Description("Votar una respuesta deja el voto registrado")]
    public async Task Votar_QuedaRegistrado()
    {
        await IrAAsync("/");
        await PreguntarAsync("¿Qué es una sesión?");
        await Ultima.GetByTestId("voto-util").ClickAsync();
        await Expect(Ultima.GetByTestId("voto-registrado")).ToBeVisibleAsync();
    }
}
