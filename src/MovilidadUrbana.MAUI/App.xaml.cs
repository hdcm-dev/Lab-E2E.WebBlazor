using AndroidSpecific = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace MovilidadUrbana.MAUI;

public partial class App : Application
{
    private readonly IServiceProvider _servicios;

    public App(IServiceProvider servicios)
    {
        _servicios = servicios;
        InitializeComponent();
        // La paleta es clara, como la web: no se deriva un tema oscuro que nadie diseñó.
        UserAppTheme = AppTheme.Light;
        // MAUI aplica su propio modo de teclado a la ventana (Pan por defecto) y pisa el AdjustResize del
        // manifiesto: con Pan, enfocar un campo bajo corre toda la pantalla y corta el encabezado.
        AndroidSpecific.Application.UseWindowSoftInputModeAdjust(
            this.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>(),
            AndroidSpecific.WindowSoftInputModeAdjust.Resize);
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell(_servicios)) { Title = "Movilidad Urbana" };
}
