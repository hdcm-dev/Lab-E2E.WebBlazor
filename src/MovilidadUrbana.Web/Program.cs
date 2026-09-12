using System.Globalization;
using MovilidadUrbana.Aplicacion;
using MovilidadUrbana.Infraestructura;
using MovilidadUrbana.Web.Components;
using MovilidadUrbana.Infraestructura.Persistencia;
using MovilidadUrbana.Infraestructura.Sesiones;
using MovilidadUrbana.Web.Servicios;

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
var cadenaDeConexion = builder.Configuration.GetConnectionString("BaseDeDatos")
    ?? ServiciosDeInfraestructura.CadenaDeConexionPorDefecto;
builder.Services.AgregarInfraestructura(cadenaDeConexion);
builder.Services.AgregarAplicacion();

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
