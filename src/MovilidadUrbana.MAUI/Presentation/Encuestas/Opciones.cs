using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MovilidadUrbana.MAUI.Presentation.Encuestas;

/// <summary>Una opción de una elección única: su clave persistida y su etiqueta.</summary>
public sealed record OptionItem(string Clave, string Etiqueta);

/// <summary>Una opción de una elección múltiple, que se marca y se desmarca.</summary>
public sealed partial class SelectableOption(string clave, string etiqueta) : ObservableObject
{
    public string Clave => clave;

    public string Etiqueta => etiqueta;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Toda la fila alterna la opción, no solo la casilla: el área táctil es la fila entera.</summary>
    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}
