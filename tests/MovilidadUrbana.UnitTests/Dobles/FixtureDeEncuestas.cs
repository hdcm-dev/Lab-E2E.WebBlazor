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

    /// <summary>
    /// Fixture de empates cercanos (bateria.v2): 20 encuestas con cifras vecinas, para que una
    /// cifra atribuida a la categoría de al lado no pase. Motivo: trabajo 6, estudio 5, salud 5,
    /// otros 4. Frecuencia: diaria 8, semanal 7, ocasional 5. Medios (33 marcas): colectivo 8,
    /// auto 7, bicicleta 7, caminata 6, tren 3, moto 2. Los promedios de todas las respuestas caen
    /// en la mitad: 41,25 minutos y 15,25 km, que se redondean a 41,3 y 15,3 (como ROUND de SQLite).
    /// </summary>
    public static IReadOnlyList<RespuestaDeEncuesta> EmpatesCercanos() => Enumerable.Range(1, 20).Select(Empate).ToList();

    public static RespuestaDeEncuesta Empate(int i)
    {
        var medios = new List<string>();
        if (i <= 8) medios.Add("colectivo");
        if (i is >= 9 and <= 15) medios.Add("auto");
        if (i is 3 or 6 or 10 or 13 or 16 or 18 or 20) medios.Add("bicicleta");
        if (i is 1 or 4 or 9 or 14 or 17 or 19) medios.Add("caminata");
        if (i is 2 or 11 or 20) medios.Add("tren");
        if (i is 5 or 12) medios.Add("moto");

        return new RespuestaDeEncuesta
        {
            Nombre = $"Persona {i:00}",
            Edad = 20 + i,
            Localidad = i <= 10 ? "Corrientes" : "Resistencia",
            Medios = medios,
            Frecuencia = i <= 8 ? "diaria" : i <= 15 ? "semanal" : "ocasional",
            Distancia = i == 20 ? 20 : 1.5 * i,
            Minutos = i == 20 ? 65 : 10 + 3 * i,
            Motivo = i <= 6 ? "trabajo" : i <= 11 ? "estudio" : i <= 16 ? "salud" : "otros",
            RegistradaEn = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.FromHours(-3))
        };
    }
}
