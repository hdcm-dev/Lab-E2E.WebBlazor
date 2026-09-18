using System.Net;

namespace MovilidadUrbana.ApiWeb.Tests;

[TestFixture]
public class DocumentationTests
{
    private ApiWebApplicationFactory _factory = default!;

    [OneTimeSetUp] public void SetUpFactory() => _factory = new ApiWebApplicationFactory();
    [OneTimeTearDown] public void TearDownFactory() => _factory.Dispose();

    [Test]
    [Description("El contrato OpenAPI se sirve y declara las ocho rutas")]
    public async Task ElContratoOpenApiDeclaraLasRutas()
    {
        var contrato = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");

        Assert.That(contrato, Does.Contain("\"/api/v1/localidades\"").And.Contain("\"/api/v1/encuestas\"")
            .And.Contain("\"/api/v1/encuestas/pasos/{paso}/validacion\"").And.Contain("\"/api/v1/encuestas/contador\""));
    }

    [Test]
    [Description("La documentación navegable de Scalar se sirve en /scalar/v1 y apunta al contrato")]
    public async Task ScalarSeSirveYApuntaAlContrato()
    {
        var respuesta = await _factory.CreateClient().GetAsync("/scalar/v1");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(respuesta.Content.Headers.ContentType!.MediaType, Is.EqualTo("text/html"));
        Assert.That(html, Does.Contain("Movilidad Urbana — API").And.Contain("openapi/v1.json"));
    }
}
