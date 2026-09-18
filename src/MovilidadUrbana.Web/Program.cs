using System.Globalization;
using MovilidadUrbana.Web.Application;
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

// --- Presentación ----------------------------------------------------------------------------
// La identidad de versión se resuelve una sola vez, acá: la cadena que ve la persona en el sello
// es la misma que queda registrada en el diagnóstico.
builder.Services.AddSingleton<IVersionIdentity>(
    VersionIdentity.FromAssembly(System.Reflection.Assembly.GetEntryAssembly()));

// Estado de interfaz del circuito: ni un almacenamiento de navegador improvisado.
builder.Services.AddScoped<IDialogService, DialogService>();
builder.Services.AddScoped<IFocusService, FocusService>();

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/no-encontrado", createScopeForStatusCodePages: true);

app.UseMiddleware<SessionMiddleware>();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
