using MovilidadUrbana.MAUI.Presentacion.Localidades;
using MovilidadUrbana.MAUI.Servicios;

namespace MovilidadUrbana.MAUI.Paginas;

public partial class LocalidadEditorPage : ContentPage, IQueryAttributable
{
    private readonly LocalidadEditorViewModel _vm;

    public LocalidadEditorPage(LocalidadEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        TecladoEnPantalla.MantenerVisible(this, BarraInferior);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        query.TryGetValue(NavegadorDeShell.ParametroDeLocalidad, out var valor);
        _vm.Preparar(valor as LocalidadItem);
    }

    /// <summary>En un alta el cursor arranca en el primer campo: nada que tocar antes de escribir.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_vm.EsEdicion) Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => CampoNombre.Focus());
    }
}
