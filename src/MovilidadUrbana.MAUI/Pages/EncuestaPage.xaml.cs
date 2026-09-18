using System.ComponentModel;
using MovilidadUrbana.MAUI.Presentation.Encuestas;

namespace MovilidadUrbana.MAUI.Pages;

public partial class EncuestaPage : ContentPage
{
    private readonly EncuestaViewModel _vm;

    public EncuestaPage(EncuestaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.PropertyChanged += OnChange;
    }

    /// <summary>Las localidades pueden haber cambiado en la otra pestaña.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    /// <summary>Al cambiar de paso, o al aparecer un aviso, se vuelve arriba: lo nuevo queda a la vista.</summary>
    private async void OnChange(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EncuestaViewModel.Paso) or nameof(EncuestaViewModel.Completada) or nameof(EncuestaViewModel.Notice))
            await Desplazamiento.ScrollToAsync(0, 0, animated: true);
    }
}
