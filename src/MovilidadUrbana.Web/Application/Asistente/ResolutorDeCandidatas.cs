using MovilidadUrbana.Web.Domain;
using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Lo que la pregunta nombra, resuelto contra la sesión y el catálogo.</summary>
public sealed record Mencion(IReadOnlyList<LocalidadVista> Localidades, string? Provincia, bool Ambigua, string? Termino)
{
    public bool Vacia => Localidades.Count == 0 && Provincia is null;
}

/// <summary>
/// Resuelve qué nombra la pregunta (paso 2 del pipeline): localidades de la sesión y provincias
/// del catálogo. Si un nombre es a la vez una localidad cargada y una provincia, y la pregunta no
/// dice cuál («la localidad…», «la provincia…»), la mención es ambigua y la respuesta es una
/// aclaración (F3). Función pura sobre la lista de la sesión.
/// </summary>
public static class ResolutorDeCandidatas
{
    public static Mencion Resolver(PreguntaNormalizada p, IReadOnlyList<LocalidadVista> sesion)
    {
        var texto = " " + p.Texto + " ";
        var provincias = Catalogos.Provincias.Where(prov => texto.Contains(" " + Texto.Normalizar(prov) + " ", StringComparison.Ordinal)).ToList();
        var localidades = sesion.Where(l => texto.Contains(" " + Texto.Normalizar(l.Nombre) + " ", StringComparison.Ordinal)).ToList();

        string? provincia = provincias.FirstOrDefault();
        var forzadaProvincia = provincias.FirstOrDefault(prov =>
            texto.Contains(" provincia " + Texto.Normalizar(prov) + " ", StringComparison.Ordinal) ||
            texto.Contains(" provincia de " + Texto.Normalizar(prov) + " ", StringComparison.Ordinal));
        var forzadaLocalidad = localidades.FirstOrDefault(l =>
            texto.Contains(" localidad " + Texto.Normalizar(l.Nombre) + " ", StringComparison.Ordinal) ||
            texto.Contains(" localidad de " + Texto.Normalizar(l.Nombre) + " ", StringComparison.Ordinal));

        if (forzadaProvincia is not null && forzadaLocalidad is null)
        {
            // «la provincia Corrientes»: la localidad homónima no cuenta como mención.
            var nombre = Texto.Normalizar(forzadaProvincia);
            return new Mencion(localidades.Where(l => Texto.Normalizar(l.Nombre) != nombre).ToList(), forzadaProvincia, false, forzadaProvincia);
        }

        if (forzadaLocalidad is not null)
        {
            var nombre = Texto.Normalizar(forzadaLocalidad.Nombre);
            var otraProvincia = provincias.FirstOrDefault(prov => Texto.Normalizar(prov) != nombre);
            var elegidas = localidades.Where(l => Texto.Normalizar(l.Nombre) == nombre &&
                                                  (otraProvincia is null || l.Provincia == otraProvincia)).ToList();
            return new Mencion(elegidas, null, false, forzadaLocalidad.Nombre);
        }

        // Homónimas con provincia nombrada («San Martín, Mendoza»): la provincia filtra.
        var distintas = provincias.Where(prov => localidades.All(l => Texto.Normalizar(l.Nombre) != Texto.Normalizar(prov))).ToList();
        if (localidades.Count > 1 && distintas.Count == 1)
        {
            localidades = localidades.Where(l => l.Provincia == distintas[0]).ToList();
            return new Mencion(localidades, null, false, localidades.FirstOrDefault()?.Nombre);
        }

        var ambigua = provincia is not null && localidades.Any(l => Texto.Normalizar(l.Nombre) == Texto.Normalizar(provincia));
        return new Mencion(localidades, provincia, ambigua, provincia ?? localidades.FirstOrDefault()?.Nombre);
    }
}
