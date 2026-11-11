using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.Web.Components.Shared.Asistente;

public partial class PanelDelAsistente : ComponentBase, IDisposable
{
    internal sealed class Turno(string pregunta, RespuestaDelAsistente respuesta)
    {
        public string Pregunta { get; } = pregunta;
        public RespuestaDelAsistente Respuesta { get; } = respuesta;
        public bool PidiendoMotivo { get; set; }
        public bool? Voto { get; set; }
    }

    private static readonly (MotivoDelVoto Motivo, string Rotulo)[] Motivos =
    [
        (MotivoDelVoto.NoEsLoQuePregunte, "No es lo que pregunté"),
        (MotivoDelVoto.FaltaInformacion, "Falta información"),
        (MotivoDelVoto.DatoIncorrecto, "Un dato es incorrecto")
    ];

    private readonly List<Turno> _turnos = [];
    private string _pregunta = string.Empty;
    private bool _ocupado;
    private bool _contextoQuitado;

    [Inject] private AsistenteService Asistente { get; set; } = default!;

    [Inject] private IEstadoDelAsistente Estado { get; set; } = default!;

    [Inject] private EstadoDelPanel Panel { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private ILogger<PanelDelAsistente> Registro { get; set; } = default!;

    private string Ruta => Navigation.ToBaseRelativePath(Navigation.Uri);

    private (string Contexto, string Rotulo)? Contexto => Plantillas.ContextoDe(Ruta);

    protected override void OnInitialized()
    {
        Panel.OnChange += Refrescar;
        Navigation.LocationChanged += AlNavegar;
    }

    private void Refrescar() => InvokeAsync(StateHasChanged);

    private void AlNavegar(object? origen, LocationChangedEventArgs argumentos)
    {
        // La pista de pantalla cambia con la ruta; si la persona la había quitado, vuelve con la pantalla nueva.
        _contextoQuitado = false;
        Refrescar();
    }

    private async Task EnviarAsync(string texto)
    {
        if (_ocupado || string.IsNullOrWhiteSpace(texto)) return;
        _ocupado = true;
        var pregunta = texto.Trim();
        _pregunta = string.Empty;
        try
        {
            var respuesta = await Asistente.ResponderAsync(pregunta, _contextoQuitado ? null : Ruta);
            _turnos.Add(new Turno(pregunta, respuesta));
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "El asistente no pudo responder el turno.");
            _turnos.Add(new Turno(pregunta, new RespuestaDelAsistente
            {
                Intencion = Intenciones.Afuera,
                Anillo = Domain.Asistente.Anillo.Afuera,
                Forma = Domain.Asistente.Forma.F8,
                Resultado = ResultadoDelTurno.Degradado,
                Texto = "No pudimos responder ahora. Volvé a intentar en unos segundos."
            }));
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task VotarAsync(Turno turno, bool util, MotivoDelVoto? motivo)
    {
        try
        {
            await Asistente.VotarAsync(turno.Respuesta, util, motivo);
            turno.Voto = util;
        }
        catch (Exception excepcion)
        {
            Registro.LogWarning(excepcion, "No se pudo registrar el voto.");
        }
    }

    private static string Titulo(string variable) => variable switch
    {
        "medios" => "Medios de transporte",
        "motivos" => "Motivo del viaje",
        "frecuencias" => "Frecuencia de uso",
        _ => variable
    };

    public void Dispose()
    {
        Panel.OnChange -= Refrescar;
        Navigation.LocationChanged -= AlNavegar;
    }
}
