using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilidadUrbana.MAUI.Aplicacion.Localidades;
using MovilidadUrbana.MAUI.Dominio;
using MovilidadUrbana.MAUI.Presentacion.Abstracciones;

namespace MovilidadUrbana.MAUI.Presentacion.Localidades;

/// <summary>
/// La lista del ABM: busca y filtra sobre lo ya traído, y resuelve sus cuatro estados. Dar de alta y
/// modificar se delegan al editor.
/// </summary>
public sealed partial class LocalidadesViewModel(ServicioDeLocalidades servicio, INavegador navegador) : ObservableObject
{
    public const string TodasLasProvincias = "Todas las provincias";

    private IReadOnlyList<LocalidadItem> _todas = [];

    public ObservableCollection<LocalidadItem> Visibles { get; } = [];

    public IReadOnlyList<string> Provincias { get; } = [TodasLasProvincias, .. Catalogos.Provincias];

    [ObservableProperty]
    private string _texto = string.Empty;

    [ObservableProperty]
    private string _provincia = TodasLasProvincias;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsCargando), nameof(EsVacio), nameof(EsFiltradoSinResultados), nameof(Resumen))]
    private EstadoDeLista _estado = EstadoDeLista.Cargando;

    [ObservableProperty]
    private bool _refrescando;

    public bool EsCargando => Estado == EstadoDeLista.Cargando;

    public bool EsVacio => Estado == EstadoDeLista.Vacio;

    public bool EsFiltradoSinResultados => Estado == EstadoDeLista.FiltradoSinResultados;

    public string Resumen => _todas.Count == Visibles.Count
        ? $"{Visibles.Count} localidades"
        : $"{Visibles.Count} de {_todas.Count} localidades";

    partial void OnTextoChanged(string value) => Filtrar();

    partial void OnProvinciaChanged(string value) => Filtrar();

    [RelayCommand]
    private async Task CargarAsync()
    {
        try
        {
            var localidades = await servicio.ListarAsync();
            _todas = [.. localidades
                .OrderBy(l => l.Nombre, StringComparer.CurrentCulture)
                .Select(l => new LocalidadItem(l.Id, l.Nombre, l.Provincia, l.CodigoPostal, l.Habitantes))];
            Filtrar();
        }
        finally
        {
            Refrescando = false;
        }
    }

    [RelayCommand]
    private Task AgregarAsync() => navegador.IrAlEditorDeLocalidadAsync(null);

    [RelayCommand]
    private Task EditarAsync(LocalidadItem localidad) => navegador.IrAlEditorDeLocalidadAsync(localidad);

    [RelayCommand]
    private void LimpiarFiltro()
    {
        Texto = string.Empty;
        Provincia = TodasLasProvincias;
    }

    private void Filtrar()
    {
        var texto = Texto.Trim();
        var coincidentes = _todas.Where(l =>
            (Provincia == TodasLasProvincias || l.Provincia == Provincia) &&
            (texto.Length == 0 ||
             l.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ||
             l.CodigoPostal.Contains(texto, StringComparison.Ordinal)));

        Visibles.Clear();
        foreach (var localidad in coincidentes) Visibles.Add(localidad);

        Estado = _todas.Count == 0 ? EstadoDeLista.Vacio
            : Visibles.Count == 0 ? EstadoDeLista.FiltradoSinResultados
            : EstadoDeLista.ConDatos;
        OnPropertyChanged(nameof(Resumen));
    }
}
