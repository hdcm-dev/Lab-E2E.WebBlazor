using MovilidadUrbana.MAUI.Paginas;
using MovilidadUrbana.MAUI.Servicios;

namespace MovilidadUrbana.MAUI;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider servicios)
    {
        InitializeComponent();
        Localidades.ContentTemplate = new DataTemplate(() => servicios.GetRequiredService<LocalidadesPage>());
        Encuesta.ContentTemplate = new DataTemplate(() => servicios.GetRequiredService<EncuestaPage>());
        Routing.RegisterRoute(NavegadorDeShell.RutaDelEditor, typeof(LocalidadEditorPage));
    }
}
