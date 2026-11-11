using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Application.Resultados;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Una localidad vista por el asistente: sin la sesión.</summary>
public sealed record LocalidadVista(int Id, string Nombre, string Provincia, string CodigoPostal, int Habitantes);

/// <summary>
/// Funciones de lectura del anillo 1. Cada una envuelve un caso de uso (REQ-IA-04): ninguna
/// toca un repositorio ni el contexto de datos. Se crean por circuito, cerradas sobre la sesión
/// que ya puso el código: ninguna firma recibe la sesión (REQ-IA-06).
/// </summary>
public sealed class FuncionesDeLectura(LocalidadService localidades, ResultadosDeEncuestaService resultados)
{
    [FuncionDeSoloLectura("listar_localidades")]
    public async Task<IReadOnlyList<LocalidadVista>> ListarLocalidadesAsync(CancellationToken cancellationToken = default) =>
        (await localidades.GetAllAsync(cancellationToken)).Select(Ver).ToList();

    [FuncionDeSoloLectura("contar_localidades")]
    public async Task<(int Total, IReadOnlyList<(string Provincia, int Cantidad)> PorProvincia)> ContarLocalidadesAsync(
        string? provincia = null, CancellationToken cancellationToken = default)
    {
        var todas = await localidades.GetAllAsync(cancellationToken);
        var filtradas = todas.Where(l => provincia is null || l.Provincia == provincia).ToList();
        var porProvincia = filtradas.GroupBy(l => l.Provincia).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Count())).ToList();
        return (filtradas.Count, porProvincia);
    }

    [FuncionDeSoloLectura("contar_encuestas")]
    public Task<int> ContarEncuestasAsync(CancellationToken cancellationToken = default) =>
        resultados.ContarAsync(cancellationToken);

    [FuncionDeSoloLectura("distribucion_medios")]
    public Task<Distribucion> DistribucionMediosAsync(CancellationToken cancellationToken = default) =>
        resultados.MediosAsync(cancellationToken);

    [FuncionDeSoloLectura("distribucion_motivos")]
    public Task<Distribucion> DistribucionMotivosAsync(CancellationToken cancellationToken = default) =>
        resultados.MotivosAsync(cancellationToken);

    [FuncionDeSoloLectura("distribucion_frecuencias")]
    public Task<Distribucion> DistribucionFrecuenciasAsync(CancellationToken cancellationToken = default) =>
        resultados.FrecuenciasAsync(cancellationToken);

    [FuncionDeSoloLectura("promedios_viaje")]
    public Task<Promedios> PromediosViajeAsync(AgrupacionDePromedios por, CancellationToken cancellationToken = default) =>
        resultados.PromediosAsync(por, cancellationToken);

    [FuncionDeSoloLectura("encuestas_por_localidad")]
    public Task<Promedios> EncuestasPorLocalidadAsync(CancellationToken cancellationToken = default) =>
        resultados.PromediosAsync(AgrupacionDePromedios.Localidad, cancellationToken);

    private static LocalidadVista Ver(Localidad l) => new(l.Id, l.Nombre, l.Provincia, l.CodigoPostal, l.Habitantes);
}
