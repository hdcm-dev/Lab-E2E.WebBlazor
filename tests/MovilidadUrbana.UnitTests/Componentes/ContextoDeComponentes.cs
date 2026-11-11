using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Application.Resultados;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.UnitTests.Componentes;

/// <summary>
/// Contexto de bUnit con los servicios de la aplicación tomados de un <see cref="Arnes" />: la
/// misma composición de producción, una sesión fijada y el render como en el circuito.
/// </summary>
public abstract class ContextoDeComponentes : BunitContext
{
    protected const string Sesion = "sesion-bunit";

    protected Arnes Arnes { get; private set; } = default!;

    private IServiceScope _alcance = default!;

    protected void Preparar(Arnes arnes)
    {
        Arnes = arnes;
        _alcance = arnes.Sesion(Sesion);
        var sp = _alcance.ServiceProvider;
        Services.AddSingleton(sp.GetRequiredService<LocalidadService>());
        Services.AddSingleton(sp.GetRequiredService<ResultadosDeEncuestaService>());
        Services.AddSingleton(sp.GetRequiredService<AsistenteService>());
        Services.AddSingleton(sp.GetRequiredService<IEstadoDelAsistente>());
        Services.AddSingleton(sp.GetRequiredService<ILectorDeVotos>());
        Services.AddSingleton<IDialogService, DialogService>();
        Services.AddSingleton<EstadoDelPanel>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    protected void Ir(string ruta) => Services.GetRequiredService<NavigationManager>().NavigateTo(ruta);

    protected string Ruta => Services.GetRequiredService<NavigationManager>().Uri;

    [TearDown]
    public async Task LiberarAsync()
    {
        await DisposeAsync();
        _alcance?.Dispose();
        Arnes?.Dispose();
    }
}
