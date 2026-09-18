using Microsoft.AspNetCore.Components;
using MovilidadUrbana.Web.Application.Encuestas;
using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Components.Shared;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Domain.Rules;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.Web.Components.Pages;

/// <summary>
/// Encuesta de transporte, resuelta como asistente. La validación por paso la decide el servicio
/// de aplicación: hacia adelante se valida el paso vigente, y hacia atrás nunca.
/// </summary>
public partial class Encuesta : ComponentBase
{
    private static readonly IReadOnlyDictionary<string, string> NoErrors = new Dictionary<string, string>();

    /// <summary>Un rótulo por cada paso de <see cref="EncuestaRules.TotalDePasos" />.</summary>
    private static readonly string[] StepLabels =
    [
        "Datos de la persona",
        "Medios que utiliza para viajar",
        "Distancia recorrida"
    ];

    private IReadOnlyList<Localidad> _localidades = [];
    private EncuestaModel _model = new();
    private IReadOnlyDictionary<string, string> _errors = NoErrors;
    private RespuestaDeEncuesta? _respuesta;
    private string? _notice;
    private string _announcement = string.Empty;
    private int _paso = 1;
    private int _pasoMaximoAlcanzado = 1;
    private int _registradas;
    private bool _busy;

    /// <summary>Paso pedido por la dirección. El paso es direccionable para poder verificarse.</summary>
    [Parameter] public int? Paso { get; set; }

    [Inject] private EncuestaService EncuestaService { get; set; } = default!;

    [Inject] private LocalidadService LocalidadService { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IFocusService Foco { get; set; } = default!;

    [Inject] private ILogger<Encuesta> Registro { get; set; } = default!;

    private IReadOnlyList<string> Labels => StepLabels;

    /// <summary>
    /// El error de los medios es del conjunto, así que lo cita el <c>fieldset</c> y no una casilla.
    /// </summary>
    private string DescriptoresDeLosMedios =>
        Error("medios") is null ? "medios-requisito" : "medios-requisito medios-error";

    protected override async Task OnInitializedAsync()
    {
        // El desplegable se alimenta del ABM: las dos pantallas comparten el mismo almacén.
        _localidades = await LocalidadService.GetAllAsync();
        _registradas = await EncuestaService.CountAsync();
    }

    /// <summary>
    /// Un paso pedido por dirección no puede saltear los anteriores: eso es lo que la barra de
    /// validación de la maqueta permitía y el producto no, porque el paso siguiente depende de que
    /// el anterior esté válido.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (_respuesta is not null) return;

        var request = Math.Clamp(Paso ?? 1, 1, EncuestaRules.TotalDePasos);
        _paso = Math.Min(request, _pasoMaximoAlcanzado);
    }

    private string? Error(string campo) => _errors.TryGetValue(campo, out var mensaje) ? mensaje : null;

    /// <summary>Envío del formulario: equivale a pedir el paso siguiente.</summary>
    private Task AdvanceAsync() => _paso == EncuestaRules.TotalDePasos
        ? FinishAsync()
        : GoToStepAsync(_paso + 1);

    private async Task GoToStepAsync(int navTarget)
    {
        if (navTarget < 1 || navTarget > EncuestaRules.TotalDePasos) return;

        // Solo se avanza con el paso actual válido. Hacia atrás nunca se valida.
        if (navTarget > _paso && !ValidarPasoActual()) return;

        _notice = null;
        _errors = NoErrors;
        _paso = navTarget;
        _pasoMaximoAlcanzado = Math.Max(_pasoMaximoAlcanzado, _paso);
        _announcement = $"Paso {_paso} de {EncuestaRules.TotalDePasos}: {StepLabels[_paso - 1]}";

        // La dirección refleja el paso y reemplaza la entrada del historial: el botón de
        // retroceso no tiene que devolver a un limbo.
        Navigation.NavigateTo(StepRoute(_paso), replace: true);
        await Foco.FocusMainContentAsync();
    }

    private async Task FinishAsync()
    {
        if (_busy) return;
        if (!ValidarPasoActual()) return;

        _busy = true;

        try
        {
            _respuesta = await EncuestaService.RegistrarAsync(_model);
            _registradas = await EncuestaService.CountAsync();
            _announcement = "Encuesta registrada.";
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudo registrar la encuesta.");
            _notice = "No pudimos registrar la encuesta. Volvé a intentar en unos segundos.";
        }
        finally
        {
            _busy = false;
        }
    }

    private bool ValidarPasoActual()
    {
        _notice = null;
        _errors = EncuestaService.ValidarPaso(_paso, _model);

        if (_errors.Count == 0) return true;

        _notice = "Complete los datos del paso antes de continuar.";
        _announcement = _notice;
        return false;
    }

    private async Task ResetAsync()
    {
        _model = new EncuestaModel();
        _errors = NoErrors;
        _respuesta = null;
        _notice = null;
        _paso = 1;
        _pasoMaximoAlcanzado = 1;
        _announcement = $"Paso 1 de {EncuestaRules.TotalDePasos}: {StepLabels[0]}";

        Navigation.NavigateTo(StepRoute(1), replace: true);
        await Foco.FocusMainContentAsync();
    }

    private static string StepRoute(int paso) => paso == 1 ? "/encuesta" : $"/encuesta/{paso}";
}
