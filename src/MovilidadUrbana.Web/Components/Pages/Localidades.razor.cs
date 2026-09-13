using Microsoft.AspNetCore.Components;
using MovilidadUrbana.Web.Aplicacion.Localidades;
using MovilidadUrbana.Web.Components.Componentes;
using MovilidadUrbana.Web.Dominio.Entidades;
using MovilidadUrbana.Web.Servicios;

namespace MovilidadUrbana.Web.Components.Pages;

/// <summary>
/// ABM de localidades. La validación que decide vive en el servicio de aplicación; acá se
/// presentan sus errores por campo y el requisito de cada uno antes del intento.
/// </summary>
public partial class Localidades : ComponentBase
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores = new Dictionary<string, string>();

    private IReadOnlyList<Localidad> _todas = [];
    private IReadOnlyList<Localidad> _visibles = [];
    private ModeloDeLocalidad _modelo = new();
    private IReadOnlyDictionary<string, string> _errores = SinErrores;
    private (string Mensaje, TonoDeBanda Tono)? _aviso;
    private EstadoDeSuperficie _estado = EstadoDeSuperficie.Cargando;
    private ElementReference _campoDelNombre;
    private string _texto = string.Empty;
    private string _provincia = string.Empty;
    private string _anuncio = string.Empty;
    private bool _procesando;

    [Inject] private ServicioDeLocalidades Servicio { get; set; } = default!;

    [Inject] private IServicioDeDialogos Dialogos { get; set; } = default!;

    [Inject] private ILogger<Localidades> Registro { get; set; } = default!;

    private bool HayFiltro => _texto.Length > 0 || _provincia.Length > 0;

    protected override Task OnInitializedAsync() => CargarAsync();

    /// <summary>
    /// Trae la colección. La carga es idempotente y sin efectos, que es la condición para que el
    /// prerenderizado la corra dos veces sin consecuencias.
    /// </summary>
    private async Task CargarAsync()
    {
        _estado = EstadoDeSuperficie.Cargando;
        _anuncio = "Cargando las localidades.";

        try
        {
            _todas = await Servicio.ListarAsync();
            Refiltrar();
        }
        catch (Exception excepcion)
        {
            // El detalle va al diagnóstico; a la pantalla va qué pasó y qué se puede hacer.
            Registro.LogError(excepcion, "No se pudieron traer las localidades.");
            _estado = EstadoDeSuperficie.Indisponible;
            _anuncio = "No pudimos traer las localidades.";
        }
    }

    /// <summary>
    /// Vacío de colección y vacío de filtrado son estados distintos, con acciones distintas:
    /// confundirlos le ofrece a la persona la acción equivocada.
    /// </summary>
    private void Refiltrar()
    {
        _visibles = _todas
            .Where(localidad => _provincia.Length == 0 || localidad.Provincia == _provincia)
            .Where(localidad => _texto.Length == 0
                                || localidad.Nombre.Contains(_texto, StringComparison.OrdinalIgnoreCase)
                                || localidad.CodigoPostal.Contains(_texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _estado = _todas.Count == 0 ? EstadoDeSuperficie.Vacio
            : _visibles.Count == 0 ? EstadoDeSuperficie.FiltradoSinResultados
            : EstadoDeSuperficie.ConDatos;

        _anuncio = _estado switch
        {
            EstadoDeSuperficie.Vacio => "Todavía no hay localidades.",
            EstadoDeSuperficie.FiltradoSinResultados => "Ninguna localidad coincide con el filtro.",
            _ => $"Se muestran {_visibles.Count} localidades de {_todas.Count}."
        };
    }

    private void AlBuscar(ChangeEventArgs argumentos)
    {
        _texto = argumentos.Value?.ToString() ?? string.Empty;
        Refiltrar();
    }

    private void LimpiarElFiltro()
    {
        _texto = string.Empty;
        _provincia = string.Empty;
        Refiltrar();
    }

    private string? Error(string campo) => _errores.TryGetValue(campo, out var mensaje) ? mensaje : null;

    private static string Iniciales(string nombre)
    {
        var palabras = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return palabras.Length switch
        {
            0 => "—",
            1 => palabras[0][..1].ToUpperInvariant(),
            _ => (palabras[0][..1] + palabras[1][..1]).ToUpperInvariant()
        };
    }

    private async Task GuardarAsync()
    {
        // La bandera se setea antes del `await`: la ventana entre el click y la ida al servidor
        // alcanza para un segundo envío.
        if (_procesando) return;
        _procesando = true;

        try
        {
            var resultado = await Servicio.GuardarAsync(_modelo);
            _errores = resultado.Errores;

            if (!resultado.EsCorrecto)
            {
                _aviso = (resultado.Mensaje, TonoDeBanda.Error);
                _anuncio = resultado.Mensaje;
                return;
            }

            _aviso = (resultado.Mensaje, TonoDeBanda.Exito);
            _modelo = new ModeloDeLocalidad();
            await CargarAsync();
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudo guardar la localidad.");
            _aviso = ("No pudimos guardar la localidad. Volvé a intentar en unos segundos.", TonoDeBanda.Error);
        }
        finally
        {
            _procesando = false;
        }
    }

    private void Editar(Localidad localidad)
    {
        _modelo = new ModeloDeLocalidad
        {
            Id = localidad.Id,
            Nombre = localidad.Nombre,
            Provincia = localidad.Provincia,
            CodigoPostal = localidad.CodigoPostal,
            Habitantes = localidad.Habitantes
        };
        _errores = SinErrores;
        _aviso = null;
    }

    private void Cancelar()
    {
        _modelo = new ModeloDeLocalidad();
        _errores = SinErrores;
        _aviso = null;
    }

    /// <summary>
    /// Primer grado de confirmación: la baja es acotada —no arrastra dependientes, porque las
    /// respuestas de encuesta guardan el nombre de la localidad y no su clave—, así que se
    /// confirma sin escritura.
    /// </summary>
    private async Task PedirBajaAsync(Localidad localidad)
    {
        var confirmada = await Dialogos.ConfirmarAsync(new PedidoDeConfirmacion(
            Titulo: $"Dar de baja la localidad {localidad.Nombre}",
            Aviso: "La operación no se deshace. Las encuestas ya registradas conservan el nombre de la localidad.",
            RotuloDeAccion: "Dar de baja"));

        if (!confirmada) return;

        await DarDeBajaAsync(localidad.Id);
    }

    private async Task DarDeBajaAsync(int id)
    {
        if (_procesando) return;
        _procesando = true;

        try
        {
            // Si se estaba editando justo la localidad dada de baja, el formulario vuelve a modo alta.
            if (_modelo.Id == id)
            {
                _modelo = new ModeloDeLocalidad();
                _errores = SinErrores;
            }

            var resultado = await Servicio.EliminarAsync(id);
            _aviso = (resultado.Mensaje, resultado.EsCorrecto ? TonoDeBanda.Exito : TonoDeBanda.Error);
            await CargarAsync();
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudo dar de baja la localidad.");
            _aviso = ("No pudimos dar de baja la localidad. Volvé a intentar en unos segundos.", TonoDeBanda.Error);
        }
        finally
        {
            _procesando = false;
        }
    }

    private async Task EnfocarElNombreAsync() => await _campoDelNombre.FocusAsync();
}
