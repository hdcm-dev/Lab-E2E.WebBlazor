using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilidadUrbana.MAUI.Aplicacion.Localidades;
using MovilidadUrbana.MAUI.Dominio;
using MovilidadUrbana.MAUI.Presentacion.Abstracciones;

namespace MovilidadUrbana.MAUI.Presentacion.Localidades;

/// <summary>
/// Alta y modificación de una localidad. La validación la decide el servicio de aplicación, el mismo
/// que usan la web y la API: acá solo se muestran sus errores junto a cada campo.
/// </summary>
public sealed partial class LocalidadEditorViewModel(
    ServicioDeLocalidades servicio,
    INavegador navegador,
    IAvisos avisos) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsEdicion), nameof(Titulo))]
    private int? _id;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _provincia;
    [ObservableProperty] private string _codigoPostal = string.Empty;
    [ObservableProperty] private string _habitantes = string.Empty;

    [ObservableProperty] private string? _errorNombre;
    [ObservableProperty] private string? _errorProvincia;
    [ObservableProperty] private string? _errorCodigoPostal;
    [ObservableProperty] private string? _errorHabitantes;
    [ObservableProperty] private string? _aviso;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand), nameof(EliminarCommand))]
    private bool _procesando;

    public IReadOnlyList<string> Provincias => Catalogos.Provincias;

    public bool EsEdicion => Id is not null;

    public string Titulo => EsEdicion ? "Editar localidad" : "Nueva localidad";

    public string RequisitoDelNombre => PoliticaDeLocalidades.RequisitoDelNombre;
    public string RequisitoDeLaProvincia => PoliticaDeLocalidades.RequisitoDeLaProvincia;
    public string RequisitoDelCodigoPostal => PoliticaDeLocalidades.RequisitoDelCodigoPostal;
    public string RequisitoDeLosHabitantes => PoliticaDeLocalidades.RequisitoDeLosHabitantes;

    /// <summary>Deja el editor listo: vacío para un alta, o con los datos de la localidad a modificar.</summary>
    public void Preparar(LocalidadItem? localidad)
    {
        Id = localidad?.Id;
        Nombre = localidad?.Nombre ?? string.Empty;
        Provincia = localidad?.Provincia;
        CodigoPostal = localidad?.CodigoPostal ?? string.Empty;
        Habitantes = localidad?.Habitantes.ToString() ?? string.Empty;
        AplicarErrores(new Dictionary<string, string>());
        Aviso = null;
    }

    // Corregir un campo borra su error: el mensaje ya no describe lo que hay escrito.
    partial void OnNombreChanged(string value) => ErrorNombre = null;
    partial void OnProvinciaChanged(string? value) => ErrorProvincia = null;
    partial void OnCodigoPostalChanged(string value) => ErrorCodigoPostal = null;
    partial void OnHabitantesChanged(string value) => ErrorHabitantes = null;

    private bool PuedeOperar() => !Procesando;

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private async Task GuardarAsync()
    {
        Procesando = true;
        try
        {
            var resultado = await servicio.GuardarAsync(new ModeloDeLocalidad
            {
                Id = Id,
                Nombre = Nombre,
                Provincia = Provincia ?? string.Empty,
                CodigoPostal = CodigoPostal,
                Habitantes = LeerEntero(Habitantes)
            });

            AplicarErrores(resultado.Errores);
            if (!resultado.EsCorrecto)
            {
                Aviso = resultado.Mensaje;
                return;
            }

            await avisos.MostrarAsync(resultado.Mensaje);
            await navegador.VolverAsync();
        }
        finally
        {
            Procesando = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private async Task EliminarAsync()
    {
        if (Id is not int id) return;

        var confirmada = await avisos.ConfirmarAsync(
            "Eliminar localidad",
            $"¿Eliminar {Nombre}? Esta acción no se puede deshacer.",
            "Eliminar",
            "Cancelar");
        if (!confirmada) return;

        Procesando = true;
        try
        {
            var resultado = await servicio.EliminarAsync(id);
            if (!resultado.EsCorrecto)
            {
                Aviso = resultado.Mensaje;
                return;
            }

            await avisos.MostrarAsync(resultado.Mensaje);
            await navegador.VolverAsync();
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>El teclado numérico deja tipear separadores de miles: se ignoran.</summary>
    private static int? LeerEntero(string texto)
    {
        var limpio = new string([.. texto.Where(char.IsDigit)]);
        return limpio.Length > 0 && int.TryParse(limpio, out var valor) ? valor : null;
    }

    private void AplicarErrores(IReadOnlyDictionary<string, string> errores)
    {
        ErrorNombre = errores.GetValueOrDefault("nombre");
        ErrorProvincia = errores.GetValueOrDefault("provincia");
        ErrorCodigoPostal = errores.GetValueOrDefault("codigoPostal");
        ErrorHabitantes = errores.GetValueOrDefault("habitantes");
    }
}
