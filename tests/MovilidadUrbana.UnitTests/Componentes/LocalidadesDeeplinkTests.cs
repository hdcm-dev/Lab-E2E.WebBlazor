using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Components.Pages;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.UnitTests.Componentes;

/// <summary>REQ-IA-45: el deeplink abre la acción, una sola vez, y sólo sobre datos de la sesión.</summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class LocalidadesDeeplinkTests : ContextoDeComponentes
{
    [SetUp]
    public void Montar() => Preparar(new Arnes(habilitada: false));

    [Test]
    public void AltaPorQuery_PrellenaYMarcaLoQueFalta()
    {
        Ir("localidades?alta=1&nombre=Goya&provincia=Corrientes&cp=3450&propuesta=ia");
        var pagina = Render<Localidades>();
        pagina.WaitForAssertion(() => Assert.That(pagina.Find("[data-testid=campo-nombre]").GetAttribute("value"), Is.EqualTo("Goya")));
        Assert.Multiple(() =>
        {
            Assert.That(pagina.Find("[data-testid=campo-codigo-postal]").GetAttribute("value"), Is.EqualTo("3450"));
            Assert.That(pagina.Find("[data-testid=propuesta-ia]").TextContent, Does.Contain("Propuesto por IA, revisá antes de guardar"));
            Assert.That(pagina.FindAll("[data-testid=error-habitantes]"), Has.Count.EqualTo(1), "la regla marca lo que falta");
            Assert.That(Ruta, Does.EndWith("/localidades"), "la dirección se limpia: recargar no la reabre");
        });
    }

    [Test]
    public void AltaPorQuery_UnaSolaVez()
    {
        Ir("localidades?alta=1&nombre=Goya");
        var pagina = Render<Localidades>();
        pagina.WaitForAssertion(() => Assert.That(Ruta, Does.EndWith("/localidades")));
        pagina.Find("[data-testid=boton-cancelar]").Click();
        // La consulta ya no está: el siguiente render no vuelve a abrir el alta prellenada.
        pagina.Render();
        Assert.That(pagina.Find("[data-testid=campo-nombre]").GetAttribute("value"), Is.Empty);
    }

    [Test]
    public async Task BajaPorQuery_IdPropio_AbreElDialogo()
    {
        var id = await Arnes.AgregarLocalidadAsync(Sesion, "Goya", "Corrientes", "3450", 90000);
        Ir($"localidades?baja={id}");
        Render<Localidades>();
        var dialogos = Services.GetRequiredService<IDialogService>();
        await Task.Delay(100);
        Assert.That(dialogos.Current?.Title, Is.EqualTo("Dar de baja la localidad Goya"));
        dialogos.Resolve(false);
        await Task.Delay(50);
        using var alcance = Arnes.Sesion(Sesion);
        var quedan = await alcance.ServiceProvider.GetRequiredService<Web.Application.Localidades.LocalidadService>().GetAllAsync();
        Assert.That(quedan.Select(l => l.Id), Does.Contain(id), "rechazar la confirmación no borra nada");
    }

    [Test(Description = "REQ-IA-18: el id de otra sesión da «no encontrado» y ningún diálogo")]
    public async Task BajaPorQuery_IdDeOtraSesion_NoEncontrado()
    {
        var ajeno = await Arnes.AgregarLocalidadAsync("otra-sesion", "Goya", "Corrientes", "3450", 90000);
        Ir($"localidades?baja={ajeno}");
        var pagina = Render<Localidades>();
        pagina.WaitForAssertion(() => Assert.That(pagina.Find("[data-testid=aviso]").TextContent, Does.Contain("No se encontró esa localidad en tu sesión")));
        Assert.That(Services.GetRequiredService<IDialogService>().Current, Is.Null);
    }

    [Test]
    public void FiltroPorProvincia_PorQuery()
    {
        Ir("localidades?provincia=Chaco");
        var pagina = Render<Localidades>();
        pagina.WaitForAssertion(() => Assert.That(pagina.FindAll("[data-testid=fila]"), Has.Count.EqualTo(1)));
    }
}
