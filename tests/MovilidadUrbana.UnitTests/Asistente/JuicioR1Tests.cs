using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Asistente;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.UnitTests.Asistente;

/// <summary>
/// Juicio con el marco (R1, R3, R4, R9 · plan P1 y P2 de Movilidad): la localidad nombrada en una
/// distribución o un promedio recorta las cifras (antes se descartaba en silencio: Q6, Q21); se
/// aclara sólo si el dato cambia la respuesta (marco §3); y una pregunta compuesta se responde
/// entera (marco §4: Q18, Q19, Q20). Fixture «Veinte»: Corrientes 12 encuestas, Resistencia 8.
/// </summary>
public class JuicioR1Tests
{
    private const string Sesion = "sesion-juicio-r1";

    private static async Task<RespuestaDelAsistente> PreguntarAsync(string pregunta, Func<Arnes, Task>? preparar = null)
    {
        using var arnes = new Arnes(habilitada: false);
        await arnes.SembrarEncuestasAsync(Sesion, FixtureDeEncuestas.Veinte());
        if (preparar is not null) await preparar(arnes);
        return await arnes.PreguntarAsync(Sesion, pregunta);
    }

    // ---- 1. La localidad nombrada recorta la distribución -----------------------------------

    [Test(Description = "Q6: «colectivo en Corrientes» cuenta sólo las encuestas de Corrientes y dice el alcance")]
    public async Task Distribucion_LocalidadNombrada_Recorta()
    {
        var r = await PreguntarAsync("¿Cuántos viajan en colectivo en Resistencia?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Intencion, Is.EqualTo(Intenciones.Medios));
            Assert.That(r.Forma, Is.EqualTo(Forma.F1));
            Assert.That(r.Texto, Does.Contain("Colectivo: 0 de 8 encuestas de tu sesión en la localidad Resistencia"));
            Assert.That(r.Texto, Does.Not.Contain("de 20"));
            Assert.That(r.Distribucion!.Encuestas, Is.EqualTo(8));
        });
    }

    [Test(Description = "Q6 con homónima: si localidad y provincia dan lo mismo, no se aclara; se responde y se dice el supuesto")]
    public async Task Distribucion_Homonima_MismaRespuesta_SinAclaracion()
    {
        var r = await PreguntarAsync("¿Cuántos viajan en colectivo en Corrientes?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Forma, Is.Not.EqualTo(Forma.F3), "la provincia Corrientes sólo tiene la localidad Corrientes: aclarar no cambia nada (T-2)");
            Assert.That(r.Opciones, Is.Empty);
            Assert.That(r.Texto, Does.Contain("Colectivo: 12 de 12 encuestas"));
            Assert.That(r.Texto, Does.Contain("da lo mismo"), "dice el supuesto (T-7, R9)");
        });
    }

    [Test(Description = "Q6 con homónima que sí cambia la respuesta: F3 con candidatas legibles y la cifra de cada una")]
    public async Task Distribucion_Homonima_CambiaLaRespuesta_Aclara()
    {
        var r = await PreguntarAsync("¿Cuántos viajan en colectivo en Corrientes?", async a =>
        {
            await a.AgregarLocalidadAsync(Sesion, "Goya", "Corrientes", "3450", 90000);
            var goya = FixtureDeEncuestas.Persona(1);
            goya.Localidad = "Goya";
            await a.SembrarEncuestasAsync(Sesion, [goya]);
        });
        Assert.Multiple(() =>
        {
            Assert.That(r.Forma, Is.EqualTo(Forma.F3));
            Assert.That(r.Opciones.Select(o => o.Rotulo), Has.Some.Contain("La localidad Corrientes").And.Some.Contain("La provincia Corrientes"));
            Assert.That(r.Opciones.Select(o => o.Pregunta), Has.Some.Contain("la localidad Corrientes").And.Some.Contain("la provincia Corrientes"));
        });
    }

    [Test(Description = "La opción «la provincia Corrientes» suma las localidades de esa provincia")]
    public async Task Distribucion_ProvinciaForzada_SumaSusLocalidades()
    {
        var r = await PreguntarAsync("¿Cuántos viajan en colectivo en la provincia Corrientes?", async a =>
        {
            await a.AgregarLocalidadAsync(Sesion, "Goya", "Corrientes", "3450", 90000);
            var goya = FixtureDeEncuestas.Persona(1);
            goya.Localidad = "Goya";
            await a.SembrarEncuestasAsync(Sesion, [goya]);
        });
        Assert.That(r.Texto, Does.Contain("Colectivo: 13 de 13 encuestas de tu sesión en la provincia Corrientes"));
    }

    [Test(Description = "Q21: dos localidades nombradas → desglose por localidad, nunca una suma silenciosa (A6)")]
    public async Task Distribucion_DosLocalidades_Desglose()
    {
        var r = await PreguntarAsync("¿Cuántos viajan en bici en Corrientes y en Resistencia?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Forma, Is.Not.EqualTo(Forma.F3), "Resistencia es localidad: Corrientes se toma como localidad y se dice");
            Assert.That(r.Texto, Does.Contain("la localidad Corrientes").And.Contain("3 de 12"));
            Assert.That(r.Texto, Does.Contain("la localidad Resistencia").And.Contain("2 de 8"));
            Assert.That(r.Texto, Does.Not.Contain("de 20"));
        });
    }

    [Test(Description = "Sin localidad nombrada la respuesta no cambia")]
    public async Task Distribucion_SinLocalidad_IgualQueAntes()
    {
        var r = await PreguntarAsync("¿Cuántos viajan en colectivo?");
        Assert.That(r.Texto, Does.StartWith("Colectivo: 12 de 20 encuestas de tu sesión."));
    }

    [Test(Description = "Promedios: la localidad nombrada recorta y se dice")]
    public async Task Promedios_LocalidadNombrada_Recorta()
    {
        var r = await PreguntarAsync("¿Cuánto tardan en promedio en Resistencia?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Texto, Does.Contain("localidad Resistencia").And.Contain("sobre 8 respuestas"));
            Assert.That(r.Promedios!.Filas.Single().Encuestas, Is.EqualTo(8));
        });
    }

    // ---- 2. Preguntas compuestas (marco §4) --------------------------------------------------

    [Test(Description = "Q18: «qué es una localidad y dónde la cargo» responde las dos partes, concepto primero")]
    public async Task Compuesta_ConceptoYAccion()
    {
        var r = await PreguntarAsync("¿Qué es una localidad y dónde la cargo?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Partes, Has.Count.EqualTo(2));
            Assert.That(r.Partes[0], Is.EqualTo(Intenciones.Glosario));
            Assert.That(r.Texto, Does.StartWith("Localidad:"));
            Assert.That(r.Destinos.Select(d => d.DestinoId), Does.Contain(Destinos.LocalidadesAlta), "lleva a la acción (T-5)");
            Assert.That(r.Citas.Select(c => c.Numero), Is.Unique);
        });
    }

    [Test(Description = "Q19: «cuántos usan moto y cuántos el tren» da las dos cifras")]
    public async Task Compuesta_DosCategorias()
    {
        var r = await PreguntarAsync("¿Cuántos usan moto y cuántos el tren?");
        Assert.That(r.Texto, Does.Contain("Moto: 1").And.Contain("Tren: 2"));
    }

    [Test(Description = "Q20: glosario y cifra en el mismo turno")]
    public async Task Compuesta_GlosarioYCifra()
    {
        var r = await PreguntarAsync("¿Qué es la frecuencia diaria y cuántos viajan todos los días?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Partes, Is.EqualTo(new[] { Intenciones.Glosario, Intenciones.Frecuencias }));
            Assert.That(r.Texto, Does.Contain("Frecuencia:").And.Contain("11 de 20"));
            Assert.That(r.Distribucion, Is.Not.Null);
        });
    }

    [Test(Description = "§4.5: una parte de afuera no tumba la otra: se responde la permitida y la otra va con F7")]
    public async Task Compuesta_UnaParteAfuera()
    {
        var r = await PreguntarAsync("¿Cuántos usan bici y qué clima hace mañana?");
        Assert.Multiple(() =>
        {
            Assert.That(r.Texto, Does.Contain("Bicicleta: 5 de 20"));
            Assert.That(r.Partes, Does.Contain(Intenciones.Afuera));
            Assert.That(r.Resultado, Is.Not.EqualTo(ResultadoDelTurno.Afuera), "el turno no es un rechazo: una parte se respondió");
        });
    }

    [TestCase("Agregá Goya, Corrientes, CP 3450, 90000 habitantes")]
    [TestCase("¿Cuántos viajan en bici en Corrientes y en Resistencia?")]
    [TestCase("¿Cómo doy de alta una localidad?")]
    public void Separador_NoParteLoSimple(string pregunta) =>
        Assert.That(SeparadorDePreguntas.Partir(pregunta), Has.Count.EqualTo(1));

    [TestCase("¿Qué es una localidad y dónde la cargo?", 2)]
    [TestCase("¿Cuántos usan moto y cuántos el tren?", 2)]
    [TestCase("¿Qué es la frecuencia diaria? ¿Cuántos viajan todos los días?", 2)]
    public void Separador_ParteLasCompuestas(string pregunta, int partes) =>
        Assert.That(SeparadorDePreguntas.Partir(pregunta), Has.Count.EqualTo(partes));
}
