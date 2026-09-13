using Microsoft.AspNetCore.Components;
using MovilidadUrbana.Web.Aplicacion.Encuestas;
using MovilidadUrbana.Web.Aplicacion.Localidades;
using MovilidadUrbana.Web.Components.Componentes;
using MovilidadUrbana.Web.Dominio.Entidades;
using MovilidadUrbana.Web.Dominio.Reglas;
using MovilidadUrbana.Web.Servicios;

namespace MovilidadUrbana.Web.Components.Pages;

/// <summary>
/// Encuesta de transporte, resuelta como asistente. La validación por paso la decide el servicio
/// de aplicación: hacia adelante se valida el paso vigente, y hacia atrás nunca.
/// </summary>
public partial class Encuesta : ComponentBase
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores = new Dictionary<string, string>();

    /// <summary>Un rótulo por cada paso de <see cref="ReglasDeEncuesta.TotalDePasos" />.</summary>
    private static readonly string[] RotulosDePaso =
    [
        "Datos de la persona",
        "Medios que utiliza para viajar",
        "Distancia recorrida"
    ];

    private IReadOnlyList<Localidad> _localidades = [];
    private ModeloDeEncuesta _modelo = new();
    private IReadOnlyDictionary<string, string> _errores = SinErrores;
    private RespuestaDeEncuesta? _respuesta;
    private string? _aviso;
    private string _anuncio = string.Empty;
    private int _paso = 1;
    private int _pasoMaximoAlcanzado = 1;
    private int _registradas;
    private bool _procesando;

    /// <summary>Paso pedido por la dirección. El paso es direccionable para poder verificarse.</summary>
    [Parameter] public int? Paso { get; set; }

    [Inject] private ServicioDeEncuestas ServicioDeEncuestas { get; set; } = default!;

    [Inject] private ServicioDeLocalidades ServicioDeLocalidades { get; set; } = default!;

    [Inject] private NavigationManager Navegacion { get; set; } = default!;

    [Inject] private IServicioDeFoco Foco { get; set; } = default!;

    [Inject] private ILogger<Encuesta> Registro { get; set; } = default!;

    private IReadOnlyList<string> Rotulos => RotulosDePaso;

    /// <summary>
    /// El error de los medios es del conjunto, así que lo cita el <c>fieldset</c> y no una casilla.
    /// </summary>
    private string DescriptoresDeLosMedios =>
        Error("medios") is null ? "medios-requisito" : "medios-requisito medios-error";

    protected override async Task OnInitializedAsync()
    {
        // El desplegable se alimenta del ABM: las dos pantallas comparten el mismo almacén.
        _localidades = await ServicioDeLocalidades.ListarAsync();
        _registradas = await ServicioDeEncuestas.ContarAsync();
    }

    /// <summary>
    /// Un paso pedido por dirección no puede saltear los anteriores: eso es lo que la barra de
    /// validación de la maqueta permitía y el producto no, porque el paso siguiente depende de que
    /// el anterior esté válido.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (_respuesta is not null) return;

        var pedido = Math.Clamp(Paso ?? 1, 1, ReglasDeEncuesta.TotalDePasos);
        _paso = Math.Min(pedido, _pasoMaximoAlcanzado);
    }

    private string? Error(string campo) => _errores.TryGetValue(campo, out var mensaje) ? mensaje : null;

    /// <summary>Envío del formulario: equivale a pedir el paso siguiente.</summary>
    private Task AvanzarAsync() => _paso == ReglasDeEncuesta.TotalDePasos
        ? FinalizarAsync()
        : IrAlPasoAsync(_paso + 1);

    private async Task IrAlPasoAsync(int destino)
    {
        if (destino < 1 || destino > ReglasDeEncuesta.TotalDePasos) return;

        // Solo se avanza con el paso actual válido. Hacia atrás nunca se valida.
        if (destino > _paso && !ValidarPasoActual()) return;

        _aviso = null;
        _errores = SinErrores;
        _paso = destino;
        _pasoMaximoAlcanzado = Math.Max(_pasoMaximoAlcanzado, _paso);
        _anuncio = $"Paso {_paso} de {ReglasDeEncuesta.TotalDePasos}: {RotulosDePaso[_paso - 1]}";

        // La dirección refleja el paso y reemplaza la entrada del historial: el botón de
        // retroceso no tiene que devolver a un limbo.
        Navegacion.NavigateTo(RutaDelPaso(_paso), replace: true);
        await Foco.AlContenidoPrincipalAsync();
    }

    private async Task FinalizarAsync()
    {
        if (_procesando) return;
        if (!ValidarPasoActual()) return;

        _procesando = true;

        try
        {
            _respuesta = await ServicioDeEncuestas.RegistrarAsync(_modelo);
            _registradas = await ServicioDeEncuestas.ContarAsync();
            _anuncio = "Encuesta registrada.";
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudo registrar la encuesta.");
            _aviso = "No pudimos registrar la encuesta. Volvé a intentar en unos segundos.";
        }
        finally
        {
            _procesando = false;
        }
    }

    private bool ValidarPasoActual()
    {
        _aviso = null;
        _errores = ServicioDeEncuestas.ValidarPaso(_paso, _modelo);

        if (_errores.Count == 0) return true;

        _aviso = "Complete los datos del paso antes de continuar.";
        _anuncio = _aviso;
        return false;
    }

    private async Task ReiniciarAsync()
    {
        _modelo = new ModeloDeEncuesta();
        _errores = SinErrores;
        _respuesta = null;
        _aviso = null;
        _paso = 1;
        _pasoMaximoAlcanzado = 1;
        _anuncio = $"Paso 1 de {ReglasDeEncuesta.TotalDePasos}: {RotulosDePaso[0]}";

        Navegacion.NavigateTo(RutaDelPaso(1), replace: true);
        await Foco.AlContenidoPrincipalAsync();
    }

    private static string RutaDelPaso(int paso) => paso == 1 ? "/encuesta" : $"/encuesta/{paso}";
}
