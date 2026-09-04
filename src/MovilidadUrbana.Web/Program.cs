using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Aplicacion.Encuestas;
using MovilidadUrbana.Web.Aplicacion.Localidades;
using MovilidadUrbana.Web.Components;
using MovilidadUrbana.Web.Infraestructura.Persistencia;
using MovilidadUrbana.Web.Infraestructura.Sesiones;
using MovilidadUrbana.Web.Servicios;

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

// --- Presentación ----------------------------------------------------------------------------
// La identidad de versión se resuelve una sola vez, acá: la cadena que ve la persona en el sello
// es la misma que queda registrada en el diagnóstico.
builder.Services.AddSingleton<IIdentidadDeVersion>(
    IdentidadDeVersion.DelEnsamblado(System.Reflection.Assembly.GetEntryAssembly()));

// Estado de interfaz del circuito: ni un almacenamiento de navegador improvisado.
builder.Services.AddScoped<IServicioDeDialogos, ServicioDeDialogos>();
builder.Services.AddScoped<IServicioDeFoco, ServicioDeFoco>();

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
