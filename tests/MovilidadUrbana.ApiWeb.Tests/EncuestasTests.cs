using System.Net;
using System.Net.Http.Json;
using MovilidadUrbana.ApiWeb.Contratos;
using MovilidadUrbana.ApiWeb.Sesiones;

namespace MovilidadUrbana.ApiWeb.Tests;

[TestFixture]
public class EncuestasTests
{
    private FabricaDeApi _fabrica = default!;

    [OneTimeSetUp] public void Levantar() => _fabrica = new FabricaDeApi();
    [OneTimeTearDown] public void Bajar() => _fabrica.Dispose();

    private HttpClient Cliente()
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(MiddlewareDeSesionPorEncabezado.Encabezado, Guid.NewGuid().ToString("n"));
        return cliente;
    }

    private static SolicitudDeEncuesta Completa() =>
        new("Ana Pérez", 34, "Corrientes", ["colectivo", "bicicleta"], "diaria", 12.5, 45, "trabajo");

    [Test]
    [Description("Registra una encuesta completa: 201 con el resumen, y el contador de la sesión sube")]
    public async Task RegistraUnaEncuestaCompleta()
    {
        var cliente = Cliente();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/encuestas", Completa());
        var registrada = await respuesta.Content.ReadFromJsonAsync<EncuestaRegistradaDto>();

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(registrada!.Resumen.Single(c => c.Clave == "medios").Valor, Is.EqualTo("Colectivo, Bicicleta"));
        Assert.That(registrada.Resumen.Single(c => c.Clave == "distancia").Valor, Is.EqualTo("12,5 km"));
        var contador = await cliente.GetFromJsonAsync<ContadorDeEncuestasDto>("/api/v1/encuestas/contador");
        Assert.That(contador!.Cantidad, Is.EqualTo(1));
    }

    [Test]
    [Description("Con datos incompletos responde 400 con los errores de los tres pasos")]
    public async Task RechazaUnaEncuestaIncompleta()
    {
        var respuesta = await Cliente().PostAsJsonAsync("/api/v1/encuestas",
            new SolicitudDeEncuesta("Al", 12, "", [], "", 900, 0, ""));
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDeValidacion>();

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problema!.Errors.Keys, Is.EquivalentTo(new[] { "nombre", "edad", "localidad", "medios", "frecuencia", "distancia", "minutos", "motivo" }));
    }

    [Test]
    [Description("Valida un paso por separado, sin registrar nada")]
    public async Task ValidaUnPasoPorSeparado()
    {
        var cliente = Cliente();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/encuestas/pasos/2/validacion",
            new SolicitudDeEncuesta(null, null, null, [], "", null, null, null));
        var validacion = await respuesta.Content.ReadFromJsonAsync<ValidacionDePasoDto>();

        Assert.That(validacion!.Valido, Is.False);
        Assert.That(validacion.Errores.Keys, Is.EquivalentTo(new[] { "medios", "frecuencia" }));
        Assert.That((await cliente.GetFromJsonAsync<ContadorDeEncuestasDto>("/api/v1/encuestas/contador"))!.Cantidad, Is.Zero);
    }

    [Test]
    [Description("Un paso que no existe es 404")]
    public async Task UnPasoInexistenteEs404()
    {
        var respuesta = await Cliente().PostAsJsonAsync("/api/v1/encuestas/pasos/4/validacion", Completa());
        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private sealed record ProblemaDeValidacion(string? Title, int? Status, Dictionary<string, string[]> Errors);
}
