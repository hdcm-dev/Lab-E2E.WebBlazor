using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilidadUrbana.MAUI.Application.Encuestas;
using MovilidadUrbana.MAUI.Application.Localidades;
using MovilidadUrbana.MAUI.Domain;
using MovilidadUrbana.MAUI.Domain.Rules;

namespace MovilidadUrbana.MAUI.Presentation.Encuestas;

/// <summary>
/// La encuesta en tres pasos. Hacia adelante se valida el paso vigente con el servicio de aplicación,
/// hacia atrás nunca, y lo cargado se conserva. Registrada, la pantalla pasa a su estado de éxito con
/// el resumen.
/// </summary>
public sealed partial class EncuestaViewModel(EncuestaService service, LocalidadService localidades) : ObservableObject
{
    public const string IncompleteStepNotice = "Complete los datos del paso antes de continuar.";

    public int TotalDePasos => EncuestaRules.TotalDePasos;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepLabel), nameof(StepTitle), nameof(Progress),
        nameof(IsFirstStep), nameof(IsLastStep), nameof(ShowStep1), nameof(ShowStep2),
        nameof(ShowStep3), nameof(ShowNext), nameof(ShowRegistrar))]
    private int _paso = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepLabel), nameof(StepTitle), nameof(Progress),
        nameof(ShowStep1), nameof(ShowStep2), nameof(ShowStep3), nameof(ShowNext),
        nameof(ShowRegistrar), nameof(ShowNavigation))]
    private bool _completada;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _edad = string.Empty;
    [ObservableProperty] private string? _localidad;
    [ObservableProperty] private string? _frecuencia;
    [ObservableProperty] private string _distancia = string.Empty;
    [ObservableProperty] private string _minutos = string.Empty;
    [ObservableProperty] private string? _motivo;

    [ObservableProperty] private string? _nombreError;
    [ObservableProperty] private string? _edadError;
    [ObservableProperty] private string? _localidadError;
    [ObservableProperty] private string? _mediosError;
    [ObservableProperty] private string? _frecuenciaError;
    [ObservableProperty] private string? _distanciaError;
    [ObservableProperty] private string? _minutosError;
    [ObservableProperty] private string? _motivoError;
    [ObservableProperty] private string? _notice;

    [ObservableProperty] private int _registradas;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
    private bool _busy;

    public ObservableCollection<string> Localidades { get; } = [];

    public IReadOnlyList<SelectableOption> Medios => field ??= CreateMedios();

    /// <summary>Marcar o desmarcar un medio borra el error de «al menos uno».</summary>
    private List<SelectableOption> CreateMedios()
    {
        var medios = Catalogos.Medios.Select(m => new SelectableOption(m.Clave, m.Etiqueta)).ToList();
        foreach (var medio in medios) medio.PropertyChanged += (_, _) => MediosError = null;
        return medios;
    }

    public IReadOnlyList<OptionItem> Frecuencias { get; } =
        [.. Catalogos.Frecuencias.Select(f => new OptionItem(f.Clave, f.Etiqueta))];

    public IReadOnlyList<OptionItem> Motivos { get; } =
        [.. Catalogos.Motivos.Select(m => new OptionItem(m.Clave, m.Etiqueta))];

    public ObservableCollection<CampoDeResumen> Resumen { get; } = [];

    public string StepLabel => Completada ? "Encuesta completada" : $"Paso {Paso} de {TotalDePasos}";

    public string StepTitle => Completada ? "¡Gracias por participar!" : Paso switch
    {
        1 => "Datos de la persona",
        2 => "Medios que utiliza para viajar",
        _ => "Distancia recorrida"
    };

    public double Progress => Completada ? 1 : (double)Paso / TotalDePasos;

    public bool IsFirstStep => Paso == 1;
    public bool IsLastStep => Paso == TotalDePasos;
    public bool ShowStep1 => !Completada && Paso == 1;
    public bool ShowStep2 => !Completada && Paso == 2;
    public bool ShowStep3 => !Completada && Paso == 3;
    public bool ShowNavigation => !Completada;
    public bool ShowNext => !Completada && !IsLastStep;
    public bool ShowRegistrar => !Completada && IsLastStep;

    public string RequisitoDelNombre => EncuestaPolicy.RequisitoDelNombre;
    public string RequisitoDeLaEdad => EncuestaPolicy.RequisitoDeLaEdad;
    public string RequisitoDeLaLocalidad => EncuestaPolicy.RequisitoDeLaLocalidad;
    public string RequisitoDeLosMedios => EncuestaPolicy.RequisitoDeLosMedios;
    public string RequisitoDeLaFrecuencia => EncuestaPolicy.RequisitoDeLaFrecuencia;
    public string RequisitoDeLaDistancia => EncuestaPolicy.RequisitoDeLaDistancia;
    public string RequisitoDeLosMinutos => EncuestaPolicy.RequisitoDeLosMinutos;
    public string RequisitoDelMotivo => EncuestaPolicy.RequisitoDelMotivo;

    // Corregir un campo borra su error: el mensaje ya no describe lo que hay cargado.
    partial void OnNombreChanged(string value) => NombreError = null;
    partial void OnEdadChanged(string value) => EdadError = null;
    partial void OnLocalidadChanged(string? value) => LocalidadError = null;
    partial void OnFrecuenciaChanged(string? value) => FrecuenciaError = null;
    partial void OnDistanciaChanged(string value) => DistanciaError = null;
    partial void OnMinutosChanged(string value) => MinutosError = null;
    partial void OnMotivoChanged(string? value) => MotivoError = null;

    /// <summary>Trae las localidades del ABM —las dos pantallas comparten la base— y el contador.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        var nombres = (await localidades.GetAllAsync())
            .Select(l => l.Nombre)
            .OrderBy(n => n, StringComparer.CurrentCulture)
            .ToList();

        Localidades.Clear();
        foreach (var nombre in nombres) Localidades.Add(nombre);
        if (Localidad is not null && !nombres.Contains(Localidad)) Localidad = null;

        Registradas = await service.CountAsync();
    }

    [RelayCommand]
    private void Next()
    {
        if (ValidarPasoVigente()) Paso++;
    }

    [RelayCommand]
    private void Previous()
    {
        if (Paso == 1) return;
        ClearErrors();
        Paso--;
    }

    private bool PuedeRegistrar() => !Busy;

    [RelayCommand(CanExecute = nameof(PuedeRegistrar))]
    private async Task RegistrarAsync()
    {
        if (!ValidarPasoVigente()) return;

        Busy = true;
        try
        {
            var respuesta = await service.RegistrarAsync(ToModel());
            Resumen.Clear();
            foreach (var campo in ResumenDeEncuesta.De(respuesta)) Resumen.Add(campo);
            Registradas = await service.CountAsync();
            Completada = true;
        }
        catch (Exception)
        {
            Notice = "No pudimos registrar la encuesta. Volvé a intentar en unos segundos.";
        }
        finally
        {
            Busy = false;
        }
    }

    [RelayCommand]
    private void NuevaEncuesta()
    {
        Nombre = Edad = Distancia = Minutos = string.Empty;
        Localidad = Frecuencia = Motivo = null;
        foreach (var medio in Medios) medio.IsSelected = false;
        Resumen.Clear();
        ClearErrors();
        Completada = false;
        Paso = 1;
    }

    private bool ValidarPasoVigente()
    {
        var errors = service.ValidarPaso(Paso, ToModel());

        NombreError = errors.GetValueOrDefault("nombre");
        EdadError = errors.GetValueOrDefault("edad");
        LocalidadError = errors.GetValueOrDefault("localidad");
        MediosError = errors.GetValueOrDefault("medios");
        FrecuenciaError = errors.GetValueOrDefault("frecuencia");
        DistanciaError = errors.GetValueOrDefault("distancia");
        MinutosError = errors.GetValueOrDefault("minutos");
        MotivoError = errors.GetValueOrDefault("motivo");
        Notice = errors.Count > 0 ? IncompleteStepNotice : null;

        return errors.Count == 0;
    }

    private void ClearErrors()
    {
        NombreError = EdadError = LocalidadError = MediosError = null;
        FrecuenciaError = DistanciaError = MinutosError = MotivoError = null;
        Notice = null;
    }

    private EncuestaModel ToModel()
    {
        var model = new EncuestaModel
        {
            Nombre = Nombre,
            Edad = int.TryParse(Edad, NumberStyles.Integer, CultureInfo.InvariantCulture, out var edad) ? edad : null,
            Localidad = Localidad ?? string.Empty,
            Frecuencia = Frecuencia ?? string.Empty,
            Distancia = ParseDecimal(Distancia),
            Minutos = int.TryParse(Minutos, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutos) ? minutos : null,
            Motivo = Motivo ?? string.Empty
        };
        foreach (var medio in Medios.Where(m => m.IsSelected)) model.AlternarMedio(medio.Clave, elegido: true);
        return model;
    }

    /// <summary>Acepta coma o punto decimal: el teclado numérico del teléfono ofrece uno u otro.</summary>
    private static double? ParseDecimal(string texto) =>
        double.TryParse(texto.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : null;
}
