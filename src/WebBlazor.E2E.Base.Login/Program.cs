using Microsoft.AspNetCore.Authentication.Cookies;
using WebBlazor.E2E.Base.Login.Components;
using WebBlazor.E2E.Base.Login.Endpoints;
using WebBlazor.E2E.Base.Login.Servicios;

var builder = WebApplication.CreateBuilder(args);

#region servicios
// Punto de composición: todo servicio se registra acá y en ningún otro archivo.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// La identidad de versión se resuelve una sola vez, en el host.
builder.Services.AddSingleton<IIdentidadDeVersion, IdentidadDeVersion>();

// Quién decide si un ingreso se acepta: la superficie no lo decide.
builder.Services.AddScoped<IServicioDeIdentidad, ServicioDeIdentidad>();
#endregion

#region Autentificación - login - esquema basado en cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "auth_token"; //default Cookie
        options.LoginPath = IdentidadEndpoints.SuperficieDeAcceso;
        options.AccessDeniedPath = "/Error";
        options.ReturnUrlParameter = "returnurl";
        //
        options.Cookie.IsEssential = true;//algunos navegadores bloquean las cookies que no son esenciales
        options.Cookie.MaxAge = null;// TimeSpan.FromMinutes(30);
        //                             //options.IdleTimeout = TimeSpan.FromDays(30); //tiempo de inactividad
        options.Cookie.HttpOnly = true; //evita acceso de javascript
        options.Cookie.SameSite = SameSiteMode.Strict;// Lax para casos como OAuth, OpenID Connect, etc.
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.MapStaticAssets();

#region Autentificación
app.UseAuthentication();
app.UseAuthorization();
#endregion 

app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

#region Identidad
// El ingreso y la salida son acciones del ciclo de request, no del circuito.
app.MapearIdentidad();
#endregion

app.Run();
