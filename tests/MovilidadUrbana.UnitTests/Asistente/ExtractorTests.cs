using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Asistente;
using MovilidadUrbana.Web.Infrastructure.AI;

namespace MovilidadUrbana.UnitTests.Asistente;

/// <summary>Alta desde una frase (E6): A0 con persona que firma; la propuesta sólo prellena.</summary>
public class ExtractorTests
{
    private const string Sesion = "sesion-e";

    [Test(Description = "REQ-IA-03: una salida que no es JSON no rompe: no hay propuesta")]
    public void Extractor_TextoNoJson_SinPropuesta()
    {
        var r = ExtractorDeLocalidad.Interpretar("Claro, la localidad es Goya.", "Agregá Goya");
        Assert.That(r.Tipo, Is.EqualTo(TipoDeResultado.Rechazado));
    }

    [Test(Description = "B04: un número que no está en la frase se descarta")]
    public void NumeroInventado_SeDescarta()
    {
        var r = ExtractorDeLocalidad.Interpretar("""{"nombre":"Bella Vista","provincia":"corrientes","codigoPostal":"3432","habitantes":35000}""",
            "Agregá Bella Vista en Corrientes, CP 3432, no sé cuántos viven");
        Assert.Multiple(() =>
        {
            Assert.That(r.Tipo, Is.EqualTo(TipoDeResultado.Ok));
            Assert.That(r.Valor!.Habitantes, Is.Null);
            Assert.That(r.Valor.Provincia, Is.EqualTo("Corrientes"), "normalizada al catálogo");
            Assert.That(r.Valor.CodigoPostal, Is.EqualTo("3432"));
        });
    }

    [Test]
    public void MilesConPunto_SonElMismoNumero()
    {
        var r = ExtractorDeLocalidad.Interpretar("""{"nombre":"Goya","provincia":"Corrientes","codigoPostal":"3450","habitantes":90000}""",
            "Agregá Goya, Corrientes, código postal 3450, 90.000 habitantes");
        Assert.That(r.Valor!.Habitantes, Is.EqualTo(90000));
    }

    [Test(Description = "B03: el chip abre el alta prellenada; el texto no dice «la agregué»")]
    public async Task AltaDesdeFrase_PrellenaYNoEscribe()
    {
        var modelo = new ClienteDeChatGuionado("""{"nombre":"Goya","provincia":"Corrientes","codigoPostal":"3450","habitantes":90000}""");
        using var arnes = new Arnes(modelo);
        var r = await arnes.PreguntarAsync(Sesion, "Agregá Goya, Corrientes, código postal 3450, 90000 habitantes");
        using var alcance = arnes.Sesion(Sesion);
        var servicio = (MovilidadUrbana.Web.Application.Localidades.LocalidadService)alcance.ServiceProvider.GetService(typeof(MovilidadUrbana.Web.Application.Localidades.LocalidadService))!;
        var localidades = await servicio.GetAllAsync();
        Assert.Multiple(() =>
        {
            Assert.That(r.Forma, Is.EqualTo(Forma.F1));
            Assert.That(r.Destinos.Single().Href, Is.EqualTo("localidades?alta=1&nombre=Goya&provincia=Corrientes&cp=3450&habitantes=90000&propuesta=ia"));
            Assert.That(r.Texto, Does.Not.Contain("agregué").And.Not.Contain("la agregué"));
            Assert.That(localidades.Select(l => l.Nombre), Has.No.Member("Goya"), "el asistente no escribe");
            Assert.That(modelo.Pedidos.Single().Mensajes.Last().Text, Is.EqualTo("Agregá Goya, Corrientes, código postal 3450, 90000 habitantes"), "al extractor viaja sólo la frase, tal cual");
        });
    }

    [Test(Description = "E6.5: sin modelo, el chip abre el alta vacía")]
    public async Task SinModelo_AltaVacia()
    {
        using var arnes = new Arnes(habilitada: false);
        var r = await arnes.PreguntarAsync(Sesion, "Agregá Goya, Corrientes, código postal 3450, 90000 habitantes");
        Assert.That(r.Destinos.Single().Href, Is.EqualTo("localidades?alta=1"));
    }
}
