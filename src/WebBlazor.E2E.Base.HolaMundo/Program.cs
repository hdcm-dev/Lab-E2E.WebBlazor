using WebBlazor.E2E.Base.HolaMundo.Components;
using WebBlazor.E2E.Base.HolaMundo.Servicios;

var builder = WebApplication.CreateBuilder(args);

#region servicios
// Punto de composición: todo servicio se registra acá y en ningún otro archivo.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// La identidad de versión se resuelve una sola vez, en el host: la superficie no
// la compone ni la lee de una constante de la vista.
builder.Services.AddSingleton<IIdentidadDeVersion, IdentidadDeVersion>();
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
