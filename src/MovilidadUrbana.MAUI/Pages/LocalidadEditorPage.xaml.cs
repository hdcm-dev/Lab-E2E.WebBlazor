using MovilidadUrbana.MAUI.Presentation.Localidades;
using MovilidadUrbana.MAUI.Services;

namespace MovilidadUrbana.MAUI.Pages;

public partial class LocalidadEditorPage : ContentPage, IQueryAttributable
{
    private readonly LocalidadEditorViewModel _vm;

    public LocalidadEditorPage(LocalidadEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        OnScreenKeyboard.KeepAboveKeyboard(this, BottomBar);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        query.TryGetValue(ShellNavigationService.LocalidadParameter, out var valor);
        _vm.Initialize(valor as LocalidadItem);
    }

    /// <summary>En un alta el cursor arranca en el primer campo: nada que tocar antes de escribir.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_vm.IsEdit) Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => NombreEntry.Focus());
    }
}
