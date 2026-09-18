using MovilidadUrbana.MAUI.Pages;
using MovilidadUrbana.MAUI.Services;

namespace MovilidadUrbana.MAUI;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        Localidades.ContentTemplate = new DataTemplate(() => services.GetRequiredService<LocalidadesPage>());
        Encuesta.ContentTemplate = new DataTemplate(() => services.GetRequiredService<EncuestaPage>());
        Routing.RegisterRoute(ShellNavigationService.EditorRoute, typeof(LocalidadEditorPage));
    }
}
