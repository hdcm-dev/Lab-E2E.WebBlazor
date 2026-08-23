using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Aplicacion.Encuestas;
using MovilidadUrbana.Web.Aplicacion.Localidades;
using MovilidadUrbana.Web.Components;
using MovilidadUrbana.Web.Infraestructura.Persistencia;
using MovilidadUrbana.Web.Infraestructura.Sesiones;

var builder = WebApplication.CreateBuilder(args);

// La aplicación se muestra siempre en es-AR: los separadores de miles y decimales forman parte
// de lo que verifican las pruebas E2E, así que no pueden depender de la cultura del servidor.
var cultura = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Infraestructura -------------------------------------------------------------------------
var cadenaDeConexion = builder.Configuration.GetConnectionString("BaseDeDatos")
    ?? "Data Source=datos/movilidad.db;Default Timeout=30";
builder.Services.AddDbContextFactory<ContextoDeDatos>(opciones => opciones.UseSqlite(cadenaDeConexion));

builder.Services.AddScoped<ContextoDeSesion>();
builder.Services.AddScoped<IContextoDeSesion>(sp => sp.GetRequiredService<ContextoDeSesion>());
builder.Services.AddScoped<SembradorDeSesion>();
builder.Services.AddScoped<IRepositorioDeLocalidades, RepositorioDeLocalidades>();
builder.Services.AddScoped<IRepositorioDeEncuestas, RepositorioDeEncuestas>();

// --- Aplicación ------------------------------------------------------------------------------
builder.Services.AddScoped<ServicioDeLocalidades>();
builder.Services.AddScoped<ServicioDeEncuestas>();

var app = builder.Build();

PreparadorDeBaseDeDatos.Preparar(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/no-encontrado", createScopeForStatusCodePages: true);

app.UseMiddleware<MiddlewareDeSesion>();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
