using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilidadUrbana.MAUI.Aplicacion.Encuestas;
using MovilidadUrbana.MAUI.Aplicacion.Localidades;
using MovilidadUrbana.MAUI.Dominio;
using MovilidadUrbana.MAUI.Dominio.Reglas;

namespace MovilidadUrbana.MAUI.Presentacion.Encuestas;

/// <summary>
/// La encuesta en tres pasos. Hacia adelante se valida el paso vigente con el servicio de aplicación,
/// hacia atrás nunca, y lo cargado se conserva. Registrada, la pantalla pasa a su estado de éxito con
/// el resumen.
/// </summary>
public sealed partial class EncuestaViewModel(ServicioDeEncuestas servicio, ServicioDeLocalidades localidades) : ObservableObject
{
    public const string AvisoDePasoIncompleto = "Complete los datos del paso antes de continuar.";

    public int TotalDePasos => ReglasDeEncuesta.TotalDePasos;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EtiquetaDelPaso), nameof(TituloDelPaso), nameof(Progreso),
        nameof(EsPrimerPaso), nameof(EsUltimoPaso), nameof(MostrarPaso1), nameof(MostrarPaso2),
        nameof(MostrarPaso3), nameof(MostrarSiguiente), nameof(MostrarRegistrar))]
    private int _paso = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EtiquetaDelPaso), nameof(TituloDelPaso), nameof(Progreso),
        nameof(MostrarPaso1), nameof(MostrarPaso2), nameof(MostrarPaso3), nameof(MostrarSiguiente),
        nameof(MostrarRegistrar), nameof(MostrarNavegacion))]
    private bool _completada;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _edad = string.Empty;
    [ObservableProperty] private string? _localidad;
    [ObservableProperty] private string? _frecuencia;
    [ObservableProperty] private string _distancia = string.Empty;
    [ObservableProperty] private string _minutos = string.Empty;
    [ObservableProperty] private string? _motivo;

    [ObservableProperty] private string? _errorNombre;
    [ObservableProperty] private string? _errorEdad;
    [ObservableProperty] private string? _errorLocalidad;
    [ObservableProperty] private string? _errorMedios;
    [ObservableProperty] private string? _errorFrecuencia;
    [ObservableProperty] private string? _errorDistancia;
    [ObservableProperty] private string? _errorMinutos;
    [ObservableProperty] private string? _errorMotivo;
    [ObservableProperty] private string? _aviso;

    [ObservableProperty] private int _registradas;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegistrarCommand))]
    private bool _procesando;

    public ObservableCollection<string> Localidades { get; } = [];

    public IReadOnlyList<OpcionElegible> Medios => field ??= CrearMedios();

    /// <summary>Marcar o desmarcar un medio borra el error de «al menos uno».</summary>
    private List<OpcionElegible> CrearMedios()
    {
        var medios = Catalogos.Medios.Select(m => new OpcionElegible(m.Clave, m.Etiqueta)).ToList();
        foreach (var medio in medios) medio.PropertyChanged += (_, _) => ErrorMedios = null;
        return medios;
    }

    public IReadOnlyList<Opcion> Frecuencias { get; } =
        [.. Catalogos.Frecuencias.Select(f => new Opcion(f.Clave, f.Etiqueta))];

    public IReadOnlyList<Opcion> Motivos { get; } =
        [.. Catalogos.Motivos.Select(m => new Opcion(m.Clave, m.Etiqueta))];

    public ObservableCollection<CampoDeResumen> Resumen { get; } = [];

    public string EtiquetaDelPaso => Completada ? "Encuesta completada" : $"Paso {Paso} de {TotalDePasos}";

    public string TituloDelPaso => Completada ? "¡Gracias por participar!" : Paso switch
    {
        1 => "Datos de la persona",
        2 => "Medios que utiliza para viajar",
        _ => "Distancia recorrida"
    };

    public double Progreso => Completada ? 1 : (double)Paso / TotalDePasos;

    public bool EsPrimerPaso => Paso == 1;
    public bool EsUltimoPaso => Paso == TotalDePasos;
    public bool MostrarPaso1 => !Completada && Paso == 1;
    public bool MostrarPaso2 => !Completada && Paso == 2;
    public bool MostrarPaso3 => !Completada && Paso == 3;
    public bool MostrarNavegacion => !Completada;
    public bool MostrarSiguiente => !Completada && !EsUltimoPaso;
    public bool MostrarRegistrar => !Completada && EsUltimoPaso;

    public string RequisitoDelNombre => PoliticaDeEncuestas.RequisitoDelNombre;
    public string RequisitoDeLaEdad => PoliticaDeEncuestas.RequisitoDeLaEdad;
    public string RequisitoDeLaLocalidad => PoliticaDeEncuestas.RequisitoDeLaLocalidad;
    public string RequisitoDeLosMedios => PoliticaDeEncuestas.RequisitoDeLosMedios;
    public string RequisitoDeLaFrecuencia => PoliticaDeEncuestas.RequisitoDeLaFrecuencia;
    public string RequisitoDeLaDistancia => PoliticaDeEncuestas.RequisitoDeLaDistancia;
    public string RequisitoDeLosMinutos => PoliticaDeEncuestas.RequisitoDeLosMinutos;
    public string RequisitoDelMotivo => PoliticaDeEncuestas.RequisitoDelMotivo;

    // Corregir un campo borra su error: el mensaje ya no describe lo que hay cargado.
    partial void OnNombreChanged(string value) => ErrorNombre = null;
    partial void OnEdadChanged(string value) => ErrorEdad = null;
    partial void OnLocalidadChanged(string? value) => ErrorLocalidad = null;
    partial void OnFrecuenciaChanged(string? value) => ErrorFrecuencia = null;
    partial void OnDistanciaChanged(string value) => ErrorDistancia = null;
    partial void OnMinutosChanged(string value) => ErrorMinutos = null;
    partial void OnMotivoChanged(string? value) => ErrorMotivo = null;

    /// <summary>Trae las localidades del ABM —las dos pantallas comparten la base— y el contador.</summary>
    [RelayCommand]
    private async Task CargarAsync()
    {
        var nombres = (await localidades.ListarAsync())
            .Select(l => l.Nombre)
            .OrderBy(n => n, StringComparer.CurrentCulture)
            .ToList();

        Localidades.Clear();
        foreach (var nombre in nombres) Localidades.Add(nombre);
        if (Localidad is not null && !nombres.Contains(Localidad)) Localidad = null;

        Registradas = await servicio.ContarAsync();
    }

    [RelayCommand]
    private void Siguiente()
    {
        if (ValidarPasoVigente()) Paso++;
    }

    [RelayCommand]
    private void Anterior()
    {
        if (Paso == 1) return;
        LimpiarErrores();
        Paso--;
    }

    private bool PuedeRegistrar() => !Procesando;

    [RelayCommand(CanExecute = nameof(PuedeRegistrar))]
    private async Task RegistrarAsync()
    {
        if (!ValidarPasoVigente()) return;

        Procesando = true;
        try
        {
            var respuesta = await servicio.RegistrarAsync(Modelo());
            Resumen.Clear();
            foreach (var campo in ResumenDeEncuesta.De(respuesta)) Resumen.Add(campo);
            Registradas = await servicio.ContarAsync();
            Completada = true;
        }
        catch (Exception)
        {
            Aviso = "No pudimos registrar la encuesta. Volvé a intentar en unos segundos.";
        }
        finally
        {
            Procesando = false;
        }
    }

    [RelayCommand]
    private void NuevaEncuesta()
    {
        Nombre = Edad = Distancia = Minutos = string.Empty;
        Localidad = Frecuencia = Motivo = null;
        foreach (var medio in Medios) medio.Elegida = false;
        Resumen.Clear();
        LimpiarErrores();
        Completada = false;
        Paso = 1;
    }

    private bool ValidarPasoVigente()
    {
        var errores = servicio.ValidarPaso(Paso, Modelo());

        ErrorNombre = errores.GetValueOrDefault("nombre");
        ErrorEdad = errores.GetValueOrDefault("edad");
        ErrorLocalidad = errores.GetValueOrDefault("localidad");
        ErrorMedios = errores.GetValueOrDefault("medios");
        ErrorFrecuencia = errores.GetValueOrDefault("frecuencia");
        ErrorDistancia = errores.GetValueOrDefault("distancia");
        ErrorMinutos = errores.GetValueOrDefault("minutos");
        ErrorMotivo = errores.GetValueOrDefault("motivo");
        Aviso = errores.Count > 0 ? AvisoDePasoIncompleto : null;

        return errores.Count == 0;
    }

    private void LimpiarErrores()
    {
        ErrorNombre = ErrorEdad = ErrorLocalidad = ErrorMedios = null;
        ErrorFrecuencia = ErrorDistancia = ErrorMinutos = ErrorMotivo = null;
        Aviso = null;
    }

    private ModeloDeEncuesta Modelo()
    {
        var modelo = new ModeloDeEncuesta
        {
            Nombre = Nombre,
            Edad = int.TryParse(Edad, NumberStyles.Integer, CultureInfo.InvariantCulture, out var edad) ? edad : null,
            Localidad = Localidad ?? string.Empty,
            Frecuencia = Frecuencia ?? string.Empty,
            Distancia = LeerDecimal(Distancia),
            Minutos = int.TryParse(Minutos, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutos) ? minutos : null,
            Motivo = Motivo ?? string.Empty
        };
        foreach (var medio in Medios.Where(m => m.Elegida)) modelo.AlternarMedio(medio.Clave, elegido: true);
        return modelo;
    }

    /// <summary>Acepta coma o punto decimal: el teclado numérico del teléfono ofrece uno u otro.</summary>
    private static double? LeerDecimal(string texto) =>
        double.TryParse(texto.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : null;
}
