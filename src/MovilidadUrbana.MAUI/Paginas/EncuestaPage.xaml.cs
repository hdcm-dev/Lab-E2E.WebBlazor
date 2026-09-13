using System.ComponentModel;
using MovilidadUrbana.MAUI.Presentacion.Encuestas;

namespace MovilidadUrbana.MAUI.Paginas;

public partial class EncuestaPage : ContentPage
{
    private readonly EncuestaViewModel _vm;

    public EncuestaPage(EncuestaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += AlCambiar;
    }

    /// <summary>Las localidades pueden haber cambiado en la otra pestaña.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarCommand.ExecuteAsync(null);
    }

    /// <summary>Al cambiar de paso, o al aparecer un aviso, se vuelve arriba: lo nuevo queda a la vista.</summary>
    private async void AlCambiar(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EncuestaViewModel.Paso) or nameof(EncuestaViewModel.Completada) or nameof(EncuestaViewModel.Aviso))
            await Desplazamiento.ScrollToAsync(0, 0, animated: true);
    }
}
