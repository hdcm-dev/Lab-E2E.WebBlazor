using MovilidadUrbana.Web.Application.Abstractions;
using MovilidadUrbana.Web.Domain;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.Web.Application.Resultados;

/// <summary>
/// Cifras de la encuesta, calculadas por código y sin IA: es el escalón −1 del analista. La
/// pantalla <c>/resultados</c> y el asistente leen las mismas cifras de acá; ninguna sale de un
/// modelo. Los tipos de retorno son agregados: los datos personales no salen de este servicio.
/// </summary>
public sealed class ResultadosDeEncuestaService(IEncuestaRepository repository)
{
    public const string NotaDeSesion = "de tu sesión";

    public async Task<int> ContarAsync(CancellationToken cancellationToken = default) =>
        await repository.CountAsync(cancellationToken);

    public async Task<Distribucion> MediosAsync(CancellationToken cancellationToken = default)
    {
        var respuestas = await repository.GetAllAsync(cancellationToken);
        var filas = Catalogos.Medios
            .Select(m => new FilaDeDistribucion(m.Clave, m.Etiqueta, respuestas.Count(r => r.Medios.Contains(m.Clave))))
            .OrderByDescending(f => f.Cantidad).ThenBy(f => f.Etiqueta, StringComparer.Ordinal)
            .ToList();
        return new Distribucion("medios", respuestas.Count, filas, Sumable: false,
            $"{NotaDeSesion}, n = {respuestas.Count} respuestas; una persona puede marcar varios medios, así que los porcentajes no suman 100 %.");
    }

    public async Task<Distribucion> MotivosAsync(CancellationToken cancellationToken = default)
    {
        var respuestas = await repository.GetAllAsync(cancellationToken);
        return Unica("motivos", respuestas, Catalogos.Motivos, r => r.Motivo);
    }

    public async Task<Distribucion> FrecuenciasAsync(CancellationToken cancellationToken = default)
    {
        var respuestas = await repository.GetAllAsync(cancellationToken);
        return Unica("frecuencias", respuestas, Catalogos.Frecuencias, r => r.Frecuencia);
    }

    public async Task<Promedios> PromediosAsync(AgrupacionDePromedios por, CancellationToken cancellationToken = default)
    {
        var respuestas = await repository.GetAllAsync(cancellationToken);
        IEnumerable<(string Grupo, string Etiqueta, IReadOnlyList<RespuestaDeEncuesta> Items)> grupos = por switch
        {
            AgrupacionDePromedios.Medio => Catalogos.Medios
                .Select(m => (m.Clave, m.Etiqueta, (IReadOnlyList<RespuestaDeEncuesta>)respuestas.Where(r => r.Medios.Contains(m.Clave)).ToList())),
            AgrupacionDePromedios.Localidad => respuestas
                .GroupBy(r => r.Localidad, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (g.Key, g.Key, (IReadOnlyList<RespuestaDeEncuesta>)g.ToList())),
            _ => [("todas", "Todas las respuestas", respuestas)]
        };

        var filas = grupos
            .Where(g => g.Items.Count > 0)
            .Select(g => new FilaDePromedio(g.Grupo, g.Etiqueta, g.Items.Count,
                Math.Round(g.Items.Average(r => r.Distancia), 1),
                Mediana(g.Items.Select(r => r.Distancia)),
                Math.Round(g.Items.Average(r => (double)r.Minutos), 1),
                Mediana(g.Items.Select(r => (double)r.Minutos))))
            .ToList();

        return new Promedios(por.ToString().ToLowerInvariant(), filas,
            $"{NotaDeSesion}: promedio y mediana sobre las respuestas de cada fila; con menos de 5 son sólo descriptivos.");
    }

    public static double Mediana(IEnumerable<double> valores)
    {
        var ordenados = valores.Order().ToArray();
        if (ordenados.Length == 0) return 0;
        var medio = ordenados.Length / 2;
        var mediana = ordenados.Length % 2 == 1 ? ordenados[medio] : (ordenados[medio - 1] + ordenados[medio]) / 2;
        return Math.Round(mediana, 1);
    }

    private static Distribucion Unica(
        string variable,
        IReadOnlyList<RespuestaDeEncuesta> respuestas,
        IReadOnlyList<(string Clave, string Etiqueta)> catalogo,
        Func<RespuestaDeEncuesta, string> selector)
    {
        var filas = catalogo
            .Select(c => new FilaDeDistribucion(c.Clave, c.Etiqueta, respuestas.Count(r => selector(r) == c.Clave)))
            .ToList();
        return new Distribucion(variable, respuestas.Count, filas, Sumable: true,
            $"{NotaDeSesion}, n = {respuestas.Count} respuestas; cada persona elige una sola opción.");
    }
}
