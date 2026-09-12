using System.Net;
using System.Net.Http.Json;
using MovilidadUrbana.ApiWeb.Contratos;
using MovilidadUrbana.ApiWeb.Sesiones;

namespace MovilidadUrbana.ApiWeb.Tests;

[TestFixture]
public class LocalidadesTests
{
    private FabricaDeApi _fabrica = default!;

    [OneTimeSetUp] public void Levantar() => _fabrica = new FabricaDeApi();
    [OneTimeTearDown] public void Bajar() => _fabrica.Dispose();

    /// <summary>Cada caso estrena su sesión, igual que las E2E de la web estrenan su cookie.</summary>
    private HttpClient Cliente()
    {
        var cliente = _fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(MiddlewareDeSesionPorEncabezado.Encabezado, Guid.NewGuid().ToString("n"));
        return cliente;
    }

    [Test]
    [Description("Una sesión nueva arranca con las localidades sembradas")]
    public async Task UnaSesionNuevaArrancaConLasSembradas()
    {
        var lista = await Cliente().GetFromJsonAsync<List<LocalidadDto>>("/api/v1/localidades");

        Assert.That(lista!.Select(l => l.Nombre), Is.EquivalentTo(new[] { "Corrientes", "Resistencia" }));
    }

    [Test]
    [Description("Sin encabezado de sesión, la respuesta devuelve uno para que el cliente lo repita")]
    public async Task SinEncabezadoLaRespuestaDevuelveUno()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/api/v1/localidades");

        Assert.That(respuesta.Headers.TryGetValues(MiddlewareDeSesionPorEncabezado.Encabezado, out var valores), Is.True);
        Assert.That(valores!.Single(), Is.Not.Empty);
    }

    [Test]
    [Description("Da de alta una localidad: 201, Location y la misma sesión la lista")]
    public async Task DaDeAltaUnaLocalidad()
    {
        var cliente = Cliente();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/localidades",
            new SolicitudDeLocalidad("Goya", "Corrientes", "3450", 90000));
        var creada = await respuesta.Content.ReadFromJsonAsync<LocalidadDto>();

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(respuesta.Headers.Location!.ToString(), Does.EndWith($"/api/v1/localidades/{creada!.Id}"));
        var lista = await cliente.GetFromJsonAsync<List<LocalidadDto>>("/api/v1/localidades");
        Assert.That(lista!.Select(l => l.Nombre), Does.Contain("Goya"));
    }

    [Test]
    [Description("Con campos inválidos responde 400 con un error por campo, en el formato estándar")]
    public async Task RechazaElAltaConCamposInvalidos()
    {
        var respuesta = await Cliente().PostAsJsonAsync("/api/v1/localidades",
            new SolicitudDeLocalidad("Go", "", "12", 0));
        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDeValidacion>();

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problema!.Errors.Keys, Is.EquivalentTo(new[] { "nombre", "provincia", "codigoPostal", "habitantes" }));
    }

    [Test]
    [Description("No permite duplicar nombre dentro de la misma provincia")]
    public async Task NoPermiteDuplicar()
    {
        var respuesta = await Cliente().PostAsJsonAsync("/api/v1/localidades",
            new SolicitudDeLocalidad("Corrientes", "Corrientes", "3400", 1));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    [Description("Modifica y después da de baja: 200, luego 204 y 404")]
    public async Task ModificaYDaDeBaja()
    {
        var cliente = Cliente();
        var id = (await cliente.GetFromJsonAsync<List<LocalidadDto>>("/api/v1/localidades"))!.First().Id;

        var modificada = await cliente.PutAsJsonAsync($"/api/v1/localidades/{id}",
            new SolicitudDeLocalidad("Corrientes Capital", "Corrientes", "3400", 400000));
        Assert.That(modificada.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await modificada.Content.ReadFromJsonAsync<LocalidadDto>())!.Nombre, Is.EqualTo("Corrientes Capital"));

        Assert.That((await cliente.DeleteAsync($"/api/v1/localidades/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await cliente.GetAsync($"/api/v1/localidades/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    [Description("Cada sesión trabaja sobre su propio conjunto de datos")]
    public async Task CadaSesionTieneSusDatos()
    {
        var una = Cliente(); var otra = Cliente();
        await una.PostAsJsonAsync("/api/v1/localidades", new SolicitudDeLocalidad("Bella Vista", "Corrientes", "3432", 30000));

        var deLaOtra = await otra.GetFromJsonAsync<List<LocalidadDto>>("/api/v1/localidades");

        Assert.That(deLaOtra!.Select(l => l.Nombre), Does.Not.Contain("Bella Vista"));
    }

    /// <summary>Lo que se lee de un ValidationProblemDetails; alcanza con los errores.</summary>
    private sealed record ProblemaDeValidacion(string? Title, int? Status, Dictionary<string, string[]> Errors);
}
