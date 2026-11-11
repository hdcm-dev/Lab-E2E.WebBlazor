using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Components.Shared;
using MovilidadUrbana.Web.Components.Shared.Asistente;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Infrastructure.AI;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.UnitTests.Componentes;

/// <summary>El panel del asistente (E2/E5): F8 a la vista, chips, aclaración, voto y apagado.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class PanelTests : ContextoDeComponentes
{
    [Test]
    public void SinModelo_AvisoYCifraConChip()
    {
        Preparar(new Arnes(habilitada: false));
        Ir("localidades");
        var panel = Render<PanelDelAsistente>();
        Assert.That(panel.Find("[data-testid=asistente-aviso]").TextContent, Does.Contain("falta configurar OLLAMA_ENDPOINT"));
        Assert.That(panel.Find("[data-testid=chip-contexto]").TextContent, Does.Contain("Sobre: Localidades"));

        panel.Find("[data-testid=asistente-pregunta]").Input("¿Cuántas localidades tengo?");
        panel.Find("[data-testid=asistente-formulario]").Submit();
        panel.WaitForAssertion(() => Assert.That(panel.FindAll("[data-testid=burbuja-asistente]"), Has.Count.EqualTo(1)));
        Assert.Multiple(() =>
        {
            Assert.That(panel.Find("[data-testid=respuesta-texto]").TextContent, Does.Contain("2 localidades"));
            Assert.That(panel.Find("[data-testid=rotulo]").TextContent, Is.EqualTo("Respuesta fija"));
            Assert.That(panel.Find("[data-testid=chip-destino]").GetAttribute("href"), Is.EqualTo("localidades"));
        });
    }

    [Test(Description = "Al elegir una opción de la aclaración, la respuesta sigue sin volver a preguntar")]
    public void Aclaracion_ElegirSigue()
    {
        Preparar(new Arnes(habilitada: false));
        var panel = Render<PanelDelAsistente>();
        panel.Find("[data-testid=asistente-pregunta]").Input("Borrá Corrientes");
        panel.Find("[data-testid=asistente-formulario]").Submit();
        panel.WaitForAssertion(() => Assert.That(panel.FindAll("[data-testid=opcion]"), Has.Count.EqualTo(2)));
        panel.FindAll("[data-testid=opcion]")[0].Click();
        panel.WaitForAssertion(() => Assert.That(panel.FindAll("[data-testid=burbuja-asistente]"), Has.Count.EqualTo(2)));
        var ultima = panel.FindAll("[data-testid=burbuja-asistente]")[1];
        Assert.That(ultima.GetAttribute("data-forma"), Is.EqualTo("F1"));
        Assert.That(ultima.QuerySelector("[data-testid=chip-destino]")!.GetAttribute("href"), Does.Match(@"^localidades\?baja=\d+$"));
    }

    [Test(Description = "E5: votar en el panel guarda la huella y se lee de la tabla")]
    public async Task Voto_DesdeElPanel()
    {
        Preparar(new Arnes(habilitada: false));
        var panel = Render<PanelDelAsistente>();
        panel.Find("[data-testid=asistente-pregunta]").Input("¿Cuántas localidades tengo?");
        panel.Find("[data-testid=asistente-formulario]").Submit();
        panel.WaitForAssertion(() => Assert.That(panel.FindAll("[data-testid=voto-no-util]"), Has.Count.EqualTo(1)));
        panel.Find("[data-testid=voto-no-util]").Click();
        panel.FindAll("[data-testid=voto-motivo]")[2].Click();
        panel.WaitForAssertion(() => Assert.That(panel.FindAll("[data-testid=voto-registrado]"), Has.Count.EqualTo(1)));
        var votos = await Services.GetRequiredService<ILectorDeVotos>().GetAllAsync();
        Assert.That(votos.Single(v => v.Origen == OrigenDelVoto.Persona).Motivo, Is.EqualTo(MotivoDelVoto.DatoIncorrecto));
    }

    [Test(Description = "REQ-IA-17: con Asistente__Habilitado=false no hay panel ni entrada en la barra")]
    public void Apagado_ComportamientoPrevio()
    {
        Preparar(new Arnes(configuracion: new Dictionary<string, string?> { ["Asistente:Habilitado"] = "false" }));
        Assert.That(Services.GetRequiredService<IEstadoDelAsistente>().PanelVisible, Is.False);
        var barra = Render<Sidebar>();
        Assert.That(barra.FindAll("[data-testid=nav-asistente]"), Is.Empty);
        Assert.That(barra.FindAll("[data-testid=nav-resultados]"), Has.Count.EqualTo(1));
    }

    [Test]
    public void Encendido_LaBarraAbreElPanel()
    {
        Preparar(new Arnes(habilitada: false));
        var barra = Render<Sidebar>();
        barra.Find("[data-testid=nav-asistente]").Click();
        Assert.That(Services.GetRequiredService<EstadoDelPanel>().Abierto, Is.True);
    }
}
