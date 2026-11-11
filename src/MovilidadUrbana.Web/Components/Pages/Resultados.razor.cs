using Microsoft.AspNetCore.Components;
using MovilidadUrbana.Web.Application.Resultados;
using MovilidadUrbana.Web.Components.Shared;

namespace MovilidadUrbana.Web.Components.Pages;

/// <summary>
/// Pantalla de resultados. La vista se puede pedir por la consulta (<c>?vista=medios</c>) para
/// que el chip del asistente abra justo lo que nombra; una vista desconocida se ignora (E-2).
/// </summary>
public partial class Resultados : ComponentBase
{
    public static readonly IReadOnlyList<(string Clave, string Rotulo)> Vistas =
    [
        ("resumen", "Resumen"), ("medios", "Medios"), ("motivos", "Motivos"), ("frecuencias", "Frecuencias"), ("distancias", "Distancias y tiempos")
    ];

    private SurfaceState _estado = SurfaceState.Cargando;
    private int _total;
    private Distribucion? _medios;
    private Distribucion? _motivos;
    private Distribucion? _frecuencias;
    private Promedios? _promedios;
    private string _anuncio = string.Empty;

    [Inject] private ResultadosDeEncuestaService Servicio { get; set; } = default!;

    [Inject] private ILogger<Resultados> Registro { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "vista")] public string? Vista { get; set; }

    private string VistaActual => Vistas.Any(v => v.Clave == Vista) ? Vista! : "resumen";

    private bool Mostrar(string clave) => VistaActual == "resumen" || VistaActual == clave;

    protected override Task OnInitializedAsync() => CargarAsync();

    private async Task CargarAsync()
    {
        _estado = SurfaceState.Cargando;
        try
        {
            _total = await Servicio.ContarAsync();
            if (_total == 0)
            {
                _estado = SurfaceState.Vacio;
                _anuncio = "Todavía no hay encuestas en tu sesión.";
                return;
            }

            _medios = await Servicio.MediosAsync();
            _motivos = await Servicio.MotivosAsync();
            _frecuencias = await Servicio.FrecuenciasAsync();
            _promedios = await Servicio.PromediosAsync(AgrupacionDePromedios.Medio);
            _estado = SurfaceState.ConDatos;
            _anuncio = $"Resultados de {_total} encuestas.";
        }
        catch (Exception excepcion)
        {
            Registro.LogError(excepcion, "No se pudieron traer los resultados.");
            _estado = SurfaceState.Indisponible;
            _anuncio = "No pudimos traer los resultados.";
        }
    }
}
