using MovilidadUrbana.MAUI.Presentation.Localidades;

namespace MovilidadUrbana.MAUI.Pages;

public partial class LocalidadesPage : ContentPage
{
    private readonly LocalidadesViewModel _vm;

    public LocalidadesPage(LocalidadesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    /// <summary>Se recarga al volver del editor: la lista refleja el alta, la edición o la baja.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}
