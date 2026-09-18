using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilidadUrbana.MAUI.Application.Localidades;
using MovilidadUrbana.MAUI.Domain;
using MovilidadUrbana.MAUI.Presentation.Abstractions;

namespace MovilidadUrbana.MAUI.Presentation.Localidades;

/// <summary>
/// La lista del ABM: busca y filtra sobre lo ya traído, y resuelve sus cuatro estados. Dar de alta y
/// modificar se delegan al editor.
/// </summary>
public sealed partial class LocalidadesViewModel(LocalidadService service, INavigationService navegador) : ObservableObject
{
    public const string AllProvincias = "Todas las provincias";

    private IReadOnlyList<LocalidadItem> _all = [];

    public ObservableCollection<LocalidadItem> Visible { get; } = [];

    public IReadOnlyList<string> Provincias { get; } = [AllProvincias, .. Catalogos.Provincias];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _provincia = AllProvincias;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading), nameof(IsEmpty), nameof(IsFilteredEmpty), nameof(Resumen))]
    private ListState _state = ListState.Cargando;

    [ObservableProperty]
    private bool _isRefreshing;

    public bool IsLoading => State == ListState.Cargando;

    public bool IsEmpty => State == ListState.Vacio;

    public bool IsFilteredEmpty => State == ListState.FiltradoSinResultados;

    public string Resumen => _all.Count == Visible.Count
        ? $"{Visible.Count} localidades"
        : $"{Visible.Count} de {_all.Count} localidades";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnProvinciaChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var localidades = await service.GetAllAsync();
            _all = [.. localidades
                .OrderBy(l => l.Nombre, StringComparer.CurrentCulture)
                .Select(l => new LocalidadItem(l.Id, l.Nombre, l.Provincia, l.CodigoPostal, l.Habitantes))];
            ApplyFilter();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task AddAsync() => navegador.NavigateToLocalidadEditorAsync(null);

    [RelayCommand]
    private Task EditAsync(LocalidadItem localidad) => navegador.NavigateToLocalidadEditorAsync(localidad);

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        Provincia = AllProvincias;
    }

    private void ApplyFilter()
    {
        var texto = SearchText.Trim();
        var coincidentes = _all.Where(l =>
            (Provincia == AllProvincias || l.Provincia == Provincia) &&
            (texto.Length == 0 ||
             l.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ||
             l.CodigoPostal.Contains(texto, StringComparison.Ordinal)));

        Visible.Clear();
        foreach (var localidad in coincidentes) Visible.Add(localidad);

        State = _all.Count == 0 ? ListState.Vacio
            : Visible.Count == 0 ? ListState.FiltradoSinResultados
            : ListState.ConDatos;
        OnPropertyChanged(nameof(Resumen));
    }
}
