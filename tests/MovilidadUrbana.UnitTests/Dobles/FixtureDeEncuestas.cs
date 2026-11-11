using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.UnitTests.Dobles;

/// <summary>
/// Fixture congelado de la batería (bateria.v1): 20 encuestas sintéticas «Persona 01…20», sólo
/// con valores del catálogo. Medios (31 marcas): colectivo 12, caminata 7, bicicleta 5, auto 4,
/// tren 2, moto 1. Motivo: trabajo 9, estudio 6, salud 3, otros 2. Frecuencia: diaria 11,
/// semanal 6, ocasional 3. Localidad: Corrientes 12, Resistencia 8. Distancia = 1,5 × i km;
/// minutos = 10 + 3 × i. El oráculo independiente es <c>tools/asistente/cifras.sql</c>.
/// </summary>
public static class FixtureDeEncuestas
{
    public static IReadOnlyList<RespuestaDeEncuesta> Veinte() => Enumerable.Range(1, 20).Select(Persona).ToList();

    public static RespuestaDeEncuesta Persona(int i)
    {
        var medios = new List<string>();
        if (i <= 12) medios.Add("colectivo");
        if (i is 4 or 5 or 14 or 20) medios.Add("auto");
        if (i is 1 or 2 or 3 or 13 or 20) medios.Add("bicicleta");
        if (i == 7) medios.Add("moto");
        if (i is >= 13 and <= 19) medios.Add("caminata");
        if (i is 6 or 15) medios.Add("tren");

        return new RespuestaDeEncuesta
        {
            Nombre = $"Persona {i:00}",
            Edad = 20 + i,
            Localidad = i <= 12 ? "Corrientes" : "Resistencia",
            Medios = medios,
            Frecuencia = i <= 11 ? "diaria" : i <= 17 ? "semanal" : "ocasional",
            Distancia = 1.5 * i,
            Minutos = 10 + 3 * i,
            Motivo = i <= 9 ? "trabajo" : i <= 15 ? "estudio" : i <= 18 ? "salud" : "otros",
            RegistradaEn = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(-3))
        };
    }
}
