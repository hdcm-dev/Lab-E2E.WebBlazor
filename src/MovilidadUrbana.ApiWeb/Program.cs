using System.Globalization;
using MovilidadUrbana.ApiWeb.Application;
using MovilidadUrbana.ApiWeb.Sessions;
using MovilidadUrbana.ApiWeb.Infrastructure;
using MovilidadUrbana.ApiWeb.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Los textos de validación y los formatos del resumen son los mismos que en la web.
var cultura = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

// --- Infraestructura y aplicación: las mismas capas que la web ---------------------------------
var connectionString = builder.Configuration.GetConnectionString("BaseDeDatos")
    ?? MovilidadUrbana.ApiWeb.Infrastructure.DependencyInjection.DefaultConnectionString;
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

// --- Presentación REST ------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // El contrato en /openapi/v1.json y su documentación navegable en /scalar/v1, donde cada
    // ruta se puede probar desde el navegador. El encabezado de sesión se declara ahí mismo para
    // que quien lo repita en cada pedido vea sus propios datos.
    app.MapOpenApi();
    app.MapScalarApiReference(opciones => opciones
        .WithTitle("Movilidad Urbana — API")
        .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl));
}

app.UseMiddleware<SessionHeaderMiddleware>();
app.MapControllers();

app.Run();

/// <summary>Punto de entrada visible para las pruebas con <c>WebApplicationFactory</c>.</summary>
public partial class Program;
