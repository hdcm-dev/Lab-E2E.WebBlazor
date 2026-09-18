using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilidadUrbana.MAUI.Application.Localidades;
using MovilidadUrbana.MAUI.Domain;
using MovilidadUrbana.MAUI.Presentation.Abstractions;

namespace MovilidadUrbana.MAUI.Presentation.Localidades;

/// <summary>
/// Alta y modificación de una localidad. La validación la decide el servicio de aplicación, el mismo
/// que usan la web y la API: acá solo se muestran sus errores junto a cada campo.
/// </summary>
public sealed partial class LocalidadEditorViewModel(
    LocalidadService service,
    INavigationService navegador,
    IAlertService avisos) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEdit), nameof(Title))]
    private int? _id;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _provincia;
    [ObservableProperty] private string _codigoPostal = string.Empty;
    [ObservableProperty] private string _habitantes = string.Empty;

    [ObservableProperty] private string? _nombreError;
    [ObservableProperty] private string? _provinciaError;
    [ObservableProperty] private string? _codigoPostalError;
    [ObservableProperty] private string? _habitantesError;
    [ObservableProperty] private string? _notice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(DeleteCommand))]
    private bool _busy;

    public IReadOnlyList<string> Provincias => Catalogos.Provincias;

    public bool IsEdit => Id is not null;

    public string Title => IsEdit ? "Editar localidad" : "Nueva localidad";

    public string RequisitoDelNombre => LocalidadPolicy.RequisitoDelNombre;
    public string RequisitoDeLaProvincia => LocalidadPolicy.RequisitoDeLaProvincia;
    public string RequisitoDelCodigoPostal => LocalidadPolicy.RequisitoDelCodigoPostal;
    public string RequisitoDeLosHabitantes => LocalidadPolicy.RequisitoDeLosHabitantes;

    /// <summary>Deja el editor listo: vacío para un alta, o con los datos de la localidad a modificar.</summary>
    public void Initialize(LocalidadItem? localidad)
    {
        Id = localidad?.Id;
        Nombre = localidad?.Nombre ?? string.Empty;
        Provincia = localidad?.Provincia;
        CodigoPostal = localidad?.CodigoPostal ?? string.Empty;
        Habitantes = localidad?.Habitantes.ToString() ?? string.Empty;
        ApplyErrors(new Dictionary<string, string>());
        Notice = null;
    }

    // Corregir un campo borra su error: el mensaje ya no describe lo que hay escrito.
    partial void OnNombreChanged(string value) => NombreError = null;
    partial void OnProvinciaChanged(string? value) => ProvinciaError = null;
    partial void OnCodigoPostalChanged(string value) => CodigoPostalError = null;
    partial void OnHabitantesChanged(string value) => HabitantesError = null;

    private bool PuedeOperar() => !Busy;

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private async Task SaveAsync()
    {
        Busy = true;
        try
        {
            var result = await service.SaveAsync(new LocalidadModel
            {
                Id = Id,
                Nombre = Nombre,
                Provincia = Provincia ?? string.Empty,
                CodigoPostal = CodigoPostal,
                Habitantes = ParseInt(Habitantes)
            });

            ApplyErrors(result.Errors);
            if (!result.IsSuccess)
            {
                Notice = result.Message;
                return;
            }

            await avisos.ShowAsync(result.Message);
            await navegador.GoBackAsync();
        }
        finally
        {
            Busy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private async Task DeleteAsync()
    {
        if (Id is not int id) return;

        var confirmada = await avisos.ConfirmAsync(
            "Eliminar localidad",
            $"¿Eliminar {Nombre}? Esta acción no se puede deshacer.",
            "Eliminar",
            "Cancelar");
        if (!confirmada) return;

        Busy = true;
        try
        {
            var result = await service.DeleteAsync(id);
            if (!result.IsSuccess)
            {
                Notice = result.Message;
                return;
            }

            await avisos.ShowAsync(result.Message);
            await navegador.GoBackAsync();
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>El teclado numérico deja tipear separadores de miles: se ignoran.</summary>
    private static int? ParseInt(string texto)
    {
        var limpio = new string([.. texto.Where(char.IsDigit)]);
        return limpio.Length > 0 && int.TryParse(limpio, out var valor) ? valor : null;
    }

    private void ApplyErrors(IReadOnlyDictionary<string, string> errors)
    {
        NombreError = errors.GetValueOrDefault("nombre");
        ProvinciaError = errors.GetValueOrDefault("provincia");
        CodigoPostalError = errors.GetValueOrDefault("codigoPostal");
        HabitantesError = errors.GetValueOrDefault("habitantes");
    }
}
