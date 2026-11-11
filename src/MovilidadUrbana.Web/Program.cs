using System.Globalization;
using MovilidadUrbana.Web.Application;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Infrastructure.AI;
using MovilidadUrbana.Web.Infrastructure;
using MovilidadUrbana.Web.Components;
using MovilidadUrbana.Web.Infrastructure.Persistence;
using MovilidadUrbana.Web.Sessions;
using MovilidadUrbana.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// La aplicación se muestra siempre en es-AR: los separadores de miles y decimales forman parte
// de lo que verifican las pruebas E2E, así que no pueden depender de la cultura del servidor.
var cultura = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Infraestructura y aplicación ------------------------------------------------------------
// Cada capa registra lo suyo; este archivo solo decide la cadena de conexión y las compone.
var connectionString = builder.Configuration.GetConnectionString("BaseDeDatos")
    ?? MovilidadUrbana.Web.Infrastructure.DependencyInjection.DefaultConnectionString;
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

// --- Capa de IA ------------------------------------------------------------------------------
// Todo desde el entorno (OLLAMA_*, Asistente__Habilitado, Asistente__Redaccion__Habilitada); sin valores por defecto. Sin modelo, el
// asistente sigue con plantillas, cifras y destinos.
builder.Services.AddAI(builder.Configuration, contenidoEnTrazas: builder.Environment.IsDevelopment());

// --- Presentación ----------------------------------------------------------------------------
// La identidad de versión se resuelve una sola vez, acá: la cadena que ve la persona en el sello
// es la misma que queda registrada en el diagnóstico.
builder.Services.AddSingleton<IVersionIdentity>(
    VersionIdentity.FromAssembly(System.Reflection.Assembly.GetEntryAssembly()));

// Estado de interfaz del circuito: ni un almacenamiento de navegador improvisado.
builder.Services.AddScoped<IDialogService, DialogService>();
builder.Services.AddScoped<IFocusService, FocusService>();
builder.Services.AddScoped<EstadoDelPanel>();

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services);

// Un catálogo de destinos que apunta a una ruta inexistente corta el arranque.
app.Services.GetRequiredService<CatalogoDeDestinos>().ValidarContra(RutasDeLaAplicacion.Plantillas());

var habilitacion = app.Services.GetRequiredService<Habilitacion>();
app.Logger.LogInformation("Asistente: {Motivo} {Clave}", habilitacion.Motivo, habilitacion.Clave);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/no-encontrado", createScopeForStatusCodePages: true);

app.UseMiddleware<SessionMiddleware>();
app.UseAntiforgery();

app.MapStaticAssets();

// Motivo exacto de la habilitación, sin el valor de ninguna clave (REQ-IA-29).
app.MapGet("/health/asistente", (IEstadoDelAsistente estado) =>
    Results.Json(new { motivo = estado.Motivo.ToString(), clave = estado.Clave, soloLexico = estado.SoloLexico, redaccion = estado.RedaccionHabilitada }));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>Punto de entrada; parcial para que las pruebas de integración lo alcancen.</summary>
public partial class Program;
