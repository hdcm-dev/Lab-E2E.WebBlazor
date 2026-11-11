using Microsoft.AspNetCore.Components;
using MovilidadUrbana.Web.Application;
using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Components.Shared;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.Web.Components.Pages;

/// <summary>
/// ABM de localidades. La validación que decide vive en el servicio de aplicación; acá se
/// presentan sus errores por campo y el requisito de cada uno antes del intento.
/// </summary>
public partial class Localidades : ComponentBase
{
    private static readonly IReadOnlyDictionary<string, string> NoErrors = new Dictionary<string, string>();

    private IReadOnlyList<Localidad> _all = [];
    private IReadOnlyList<Localidad> _visible = [];
    private LocalidadModel _model = new();
    private IReadOnlyDictionary<string, string> _errors = NoErrors;
    private (string Message, BandTone Tone)? _notice;
    private SurfaceState _state = SurfaceState.Cargando;
    private ElementReference _campoDelNombre;
    private string _texto = string.Empty;
    private string _provincia = string.Empty;
    private string _announcement = string.Empty;
    private bool _busy;

    [Inject] private LocalidadService Servicio { get; set; } = default!;

    [Inject] private IDialogService Dialogs { get; set; } = default!;

    [Inject] private ILogger<Localidades> Registro { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // --- Deeplinks del catálogo de destinos ----------------------------------------------------
    // La página lee de la consulta sólo lo que el catálogo declara para sus destinos: `alta` y su
    // prellenado, `baja` y el filtro `provincia`. Cada acción se abre una sola vez: después se
    // limpia la dirección, así que recargar no la vuelve a abrir.

    [SupplyParameterFromQuery(Name = "alta")] public string? Alta { get; set; }

    [SupplyParameterFromQuery(Name = "baja")] public int? Baja { get; set; }

    [SupplyParameterFromQuery(Name = "nombre")] public string? Nombre { get; set; }

    [SupplyParameterFromQuery(Name = "provincia")] public string? Provincia { get; set; }

    [SupplyParameterFromQuery(Name = "cp")] public string? Cp { get; set; }

    [SupplyParameterFromQuery(Name = "habitantes")] public int? Habitantes { get; set; }

    [SupplyParameterFromQuery(Name = "propuesta")] public string? Propuesta { get; set; }

    private bool _propuestaDeIa;

    private bool HasFilter => _texto.Length > 0 || _provincia.Length > 0;

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override async Task OnParametersSetAsync()
    {
        // En el prerenderizado no se actúa: la acción se abre una vez, ya en el circuito.
        if (!RendererInfo.IsInteractive) return;

        if (Alta == "1")
        {
            _model = new LocalidadModel
            {
                Nombre = Nombre ?? string.Empty,
                Provincia = Provincia is not null && Domain.Rules.LocalidadRules.ProvinciaValida(Provincia) ? Provincia : string.Empty,
                CodigoPostal = Cp ?? string.Empty,
                Habitantes = Habitantes
            };
            _propuestaDeIa = Propuesta == "ia";
            var prellenado = Nombre is not null || Provincia is not null || Cp is not null || Habitantes is not null;
            _errors = prellenado ? Servicio.ValidarSinGuardar(_model) : NoErrors;
            _notice = null;
            LimpiarLaDireccion();
            await InvokeAsync(StateHasChanged);
            try { await FocusNameAsync(); } catch (InvalidOperationException) { }
            return;
        }

        if (Baja is int id)
        {
            LimpiarLaDireccion();
            if (_state is SurfaceState.Cargando) await LoadAsync();
            var localidad = _all.FirstOrDefault(l => l.Id == id);
            if (localidad is null || !Permisos.PuedeEliminar(_all.Select(l => l.Id), id))
            {
                // Inquilino por código (REQ-IA-18): una localidad ajena es indistinguible de una inexistente.
                _notice = ("No se encontró esa localidad en tu sesión.", BandTone.Error);
                return;
            }

            _ = InvokeAsync(() => ConfirmDeleteAsync(localidad));
            return;
        }

        if (Provincia is not null && Domain.Rules.LocalidadRules.ProvinciaValida(Provincia) && _provincia != Provincia)
        {
            _provincia = Provincia;
            ApplyFilter();
        }
    }

    private void LimpiarLaDireccion() => Navigation.NavigateTo("localidades", forceLoad: false, replace: true);

    /// <summary>
    /// Trae la colección. La carga es idempotente y sin efectos, que es la condición para que el
    /// prerenderizado la corra dos veces sin consecuencias.
    /// </summary>
    private async Task LoadAsync()
    {
        _state = SurfaceState.Cargando;
        _announcement = "Cargando las localidades.";

        try
        {
            _all = await Servicio.GetAllAsync();
            ApplyFilter();
        }
        catch (Exception excepcion)
        {
            // El detalle va al diagnóstico; a la pantalla va qué pasó y qué se puede hacer.
            Registro.LogError(excepcion, "No se pudieron traer las localidades.");
            _state = SurfaceState.Indisponible;
            _announcement = "No pudimos traer las localidades.";
        }
    }

    /// <summary>
    /// Vacío de colección y vacío de filtrado son estados distintos, con acciones distintas:
    /// confundirlos le ofrece a la persona la acción equivocada.
    /// </summary>
    private void ApplyFilter()
    {
        _visible = _all
            .Where(localidad => _provincia.Length == 0 || localidad.Provincia == _provincia)
            .Where(localidad => _texto.Length == 0
                                || localidad.Nombre.Contains(_texto, StringComparison.OrdinalIgnoreCase)
                                || localidad.CodigoPostal.Contains(_texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _state = _all.Count == 0 ? SurfaceState.Vacio
            : _visible.Count == 0 ? SurfaceState.FiltradoSinResultados
            : SurfaceState.ConDatos;

        _announcement = _state switch
        {
            SurfaceState.Vacio => "Todavía no hay localidades.",
            SurfaceState.FiltradoSinResultados => "Ninguna localidad coincide con el filtro.",
            _ => $"Se muestran {_visible.Count} localidades de {_all.Count}."
        };
    }

    private void OnSearch(ChangeEventArgs argumentos)
    {
        _texto = argumentos.Value?.ToString() ?? string.Empty;
        ApplyFilter();
    }

    private void ClearFilter()
    {
        _texto = string.Empty;
        _provincia = string.Empty;
        ApplyFilter();
    }

    private string? Error(string campo) => _errors.TryGetValue(campo, out var mensaje) ? mensaje : null;

    private static string Initials(string nombre)
    {
        var palabras = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return palabras.Length switch
        {
            0 => "—",
            1 => palabras[0][..1].ToUpperInvariant(),
            _ => (palabras[0][..1] + palabras[1][..1]).ToUpperInvariant()
        };
    }

    private async Task SaveAsync()
    {
        // La bandera se setea antes del `await`: la ventana entre el click y la ida al servidor
        // alcanza para un segundo envío.
        if (_busy) return;
        _busy = true;

        try
        {
            var result = await Servicio.SaveAsync(_model);
            _errors = result.Errors;

            if (!result.IsSuccess)
            {
                _notice = (result.Message, BandTone.Error);
                _announcement = result.Message;
                return;
            }

            _notice = (result.Message, BandTone.Exito);
            _model = new LocalidadModel();
            _propuestaDeIa = false;
            await LoadAsync();
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudo guardar la localidad.");
            _notice = ("No pudimos guardar la localidad. Volvé a intentar en unos segundos.", BandTone.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Edit(Localidad localidad)
    {
        _model = new LocalidadModel
        {
            Id = localidad.Id,
            Nombre = localidad.Nombre,
            Provincia = localidad.Provincia,
            CodigoPostal = localidad.CodigoPostal,
            Habitantes = localidad.Habitantes
        };
        _errors = NoErrors;
        _notice = null;
    }

    private void Cancel()
    {
        _propuestaDeIa = false;
        _model = new LocalidadModel();
        _errors = NoErrors;
        _notice = null;
    }

    /// <summary>
    /// Primer grado de confirmación: la baja es acotada —no arrastra dependientes, porque las
    /// respuestas de encuesta guardan el nombre de la localidad y no su clave—, así que se
    /// confirma sin escritura.
    /// </summary>
    private async Task ConfirmDeleteAsync(Localidad localidad)
    {
        var confirmada = await Dialogs.ConfirmAsync(new ConfirmationRequest(
            Title: $"Dar de baja la localidad {localidad.Nombre}",
            Aviso: "La operación no se deshace. Las encuestas ya registradas conservan el nombre de la localidad.",
            RotuloDeAccion: "Dar de baja"));

        if (!confirmada) return;

        await DeleteAsync(localidad.Id);
    }

    private async Task DeleteAsync(int id)
    {
        if (_busy) return;
        _busy = true;

        try
        {
            // Si se estaba editando justo la localidad dada de baja, el formulario vuelve a modo alta.
            if (_model.Id == id)
            {
                _model = new LocalidadModel();
                _errors = NoErrors;
            }

            var result = await Servicio.DeleteAsync(id);
            _notice = (result.Message, result.IsSuccess ? BandTone.Exito : BandTone.Error);
            await LoadAsync();
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudo dar de baja la localidad.");
            _notice = ("No pudimos dar de baja la localidad. Volvé a intentar en unos segundos.", BandTone.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task FocusNameAsync() => await _campoDelNombre.FocusAsync();
}
