using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Infrastructure;
using MovilidadUrbana.Web.Infrastructure.AI;
using MovilidadUrbana.Web.Infrastructure.Persistence;
using MovilidadUrbana.Web.Infrastructure.Sessions;

namespace MovilidadUrbana.Asistente.Bateria;

/// <summary>
/// Una corrida de la batería bateria.v1 (K3 §2, K7 §9): las 37 preguntas, con la composición de
/// producción y el modelo que diga el entorno (OLLAMA_*). Cada caso se evalúa con oráculos
/// cerrados (forma, destino, textos, cifras no inventadas) y la corrida deja un registro
/// recalculable: una línea JSON por caso y un resumen con k/M, latencias y el hash de la batería.
/// Sin OLLAMA_* corre por el camino de la plantilla: es la línea base.
/// </summary>
[Category("LLM")]
public class BateriaTests
{
    private sealed record Caso(
        string Id, string Persona, string Tipo, string Pregunta, string Forma,
        string? Destino, string[]? Contiene, string[]? NoContiene, string[]? HrefContiene, string[]? HrefNoContiene,
        string? Grafico, bool SinDestino, string? Sesion, bool SinModelo, string? Aviso);

    private sealed record Archivo(string Version, string Congelada, Caso[] Casos);

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    [Test]
    public async Task Corrida()
    {
        var ruta = Path.Combine(TestContext.CurrentContext.TestDirectory, "bateria.v1.json");
        var crudo = await File.ReadAllBytesAsync(ruta);
        var hash = Convert.ToHexString(SHA256.HashData(crudo))[..16].ToLowerInvariant();
        var bateria = JsonSerializer.Deserialize<Archivo>(crudo, Json)!;
        var corrida = Environment.GetEnvironmentVariable("BATERIA_CORRIDA") ?? "1";
        var salida = Environment.GetEnvironmentVariable("BATERIA_SALIDA") ?? Path.Combine(TestContext.CurrentContext.WorkDirectory, "bateria");
        Directory.CreateDirectory(salida);

        var entorno = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var habilitacion = Habilitacion.Evaluar(entorno);
        var camino = habilitacion.Motivo == MotivoDeHabilitacion.Habilitada ? "modelo" : "plantilla";

        using var conModelo = new Composicion(entorno);
        using var sinModelo = new Composicion(new ConfigurationBuilder().AddInMemoryCollection(
            entorno.AsEnumerable().Where(p => p.Key != Habilitacion.ClaveModelo)).Build());

        var lineas = new List<string>();
        var latencias = new List<double>();
        var aciertos = 0;
        var inventados = 0;
        var porResultado = new Dictionary<string, int>();
        foreach (var c in bateria.Casos)
        {
            var composicion = c.SinModelo ? sinModelo : conModelo;
            var sesion = $"{c.Id}-{corrida}-{Guid.NewGuid():n}";
            if (c.Sesion != "sinEncuestas") await composicion.SembrarAsync(sesion, FixtureDeEncuestas.Veinte());
            if (c.Sesion == "sinLocalidades") await composicion.VaciarLocalidadesAsync(sesion);

            var reloj = Stopwatch.StartNew();
            var r = await composicion.PreguntarAsync(sesion, c.Pregunta);
            reloj.Stop();
            latencias.Add(reloj.Elapsed.TotalMilliseconds);

            var fallas = Evaluar(c, r);
            var inventado = NumerosInventados(c, r);
            if (inventado.Count > 0) { inventados++; fallas.Add($"cifra inventada: {string.Join(", ", inventado)}"); }
            if (fallas.Count == 0) aciertos++;
            porResultado[r.Resultado.ToString()] = porResultado.GetValueOrDefault(r.Resultado.ToString()) + 1;

            lineas.Add(JsonSerializer.Serialize(new
            {
                id = c.Id, persona = c.Persona, tipo = c.Tipo, ok = fallas.Count == 0, fallas,
                intencion = r.Intencion, anillo = r.Anillo.ToString(), forma = r.Forma.ToString(), resultado = r.Resultado.ToString(),
                redactadaConIa = r.RedactadaConIa, destino = r.Destinos.FirstOrDefault()?.DestinoId, href = r.Destinos.FirstOrDefault()?.Href,
                grafico = r.Grafico.ToString(), motivo = r.MotivoDelResultado, texto = r.Texto, aviso = r.Aviso, ms = Math.Round(reloj.Elapsed.TotalMilliseconds)
            }, Json));
        }

        var ordenadas = latencias.Order().ToArray();
        double Percentil(double p) => ordenadas[Math.Min(ordenadas.Length - 1, (int)Math.Ceiling(p * ordenadas.Length) - 1)];
        var resumen = new
        {
            bateria = bateria.Version, hash, corrida, camino, fecha = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            modelo = camino == "modelo" ? "por variable OLLAMA_MODEL (fuera del repositorio)" : "ninguno",
            k = aciertos, m = bateria.Casos.Length, inventados, porResultado,
            p50ms = Math.Round(Percentil(0.5)), p95ms = Math.Round(Percentil(0.95)), maxms = Math.Round(ordenadas[^1])
        };

        await File.WriteAllLinesAsync(Path.Combine(salida, $"corrida-{corrida}.jsonl"), lineas, new UTF8Encoding(false));
        await File.WriteAllTextAsync(Path.Combine(salida, $"resumen-{corrida}.json"), JsonSerializer.Serialize(resumen, new JsonSerializerOptions(Json) { WriteIndented = true }), new UTF8Encoding(false));
        TestContext.Progress.WriteLine($"BATERIA corrida {corrida} ({camino}): {aciertos}/{bateria.Casos.Length}, inventados {inventados}, p50 {resumen.p50ms} ms, p95 {resumen.p95ms} ms");
        foreach (var l in lineas.Where(l => l.Contains("\"ok\":false", StringComparison.Ordinal))) TestContext.Progress.WriteLine("FALLA " + l);

        // El umbral no se baja (K3 §11): mínimo 34 de 37 y cero cifras inventadas.
        Assert.Multiple(() =>
        {
            Assert.That(inventados, Is.Zero);
            Assert.That(aciertos, Is.GreaterThanOrEqualTo(34));
        });
    }

    private static List<string> Evaluar(Caso c, RespuestaDelAsistente r)
    {
        var f = new List<string>();
        if (r.Forma.ToString() != c.Forma) f.Add($"forma {r.Forma} ≠ {c.Forma}");
        var chip = r.Destinos.FirstOrDefault();
        if (c.Destino is not null && chip?.DestinoId != c.Destino) f.Add($"destino {chip?.DestinoId ?? "ninguno"} ≠ {c.Destino}");
        if (c.SinDestino && r.Destinos.Count > 0) f.Add("ofreció un destino");
        foreach (var t in c.Contiene ?? []) if (!r.Texto.Contains(t, StringComparison.OrdinalIgnoreCase)) f.Add($"no contiene «{t}»");
        foreach (var t in c.NoContiene ?? []) if (r.Texto.Contains(t, StringComparison.OrdinalIgnoreCase)) f.Add($"contiene «{t}»");
        foreach (var t in c.HrefContiene ?? []) if (chip is null || !chip.Href.Contains(t, StringComparison.Ordinal)) f.Add($"href sin «{t}»");
        foreach (var t in c.HrefNoContiene ?? []) if (chip is not null && chip.Href.Contains(t, StringComparison.Ordinal)) f.Add($"href con «{t}»");
        if (c.Grafico is not null && r.Grafico.ToString() != c.Grafico) f.Add($"gráfico {r.Grafico} ≠ {c.Grafico}");
        if (c.Aviso is not null && (r.Aviso is null || !r.Aviso.Contains(c.Aviso, StringComparison.Ordinal))) f.Add("sin el aviso F8");
        if (Regex.IsMatch(r.Texto, @"https?:|<[a-z]")) f.Add("texto con URL o HTML");
        return f;
    }

    /// <summary>Oráculo de cifras: todo número del texto tiene que tener origen en el código o en la ayuda.</summary>
    private static List<double> NumerosInventados(Caso c, RespuestaDelAsistente r)
    {
        var permitidos = new HashSet<double>(r.Cifras.Select(x => Math.Round(x.Valor, 1)));
        foreach (var n in ValidadorDeCifras.Numeros(c.Pregunta)) permitidos.Add(Math.Round(n, 1));
        foreach (var f in Corpus.Fragmentos) foreach (var n in ValidadorDeCifras.Numeros(f.Texto)) permitidos.Add(Math.Round(n, 1));
        foreach (var n in new double[] { 1, 2, 3, 5, 6, 100, 346334, 291720, 3400, 3500 }) permitidos.Add(n);
        if (r.Distribucion is { } d) { permitidos.Add(d.Encuestas); foreach (var x in d.Filas) permitidos.Add(x.Cantidad); }
        return ValidadorDeCifras.Numeros(r.Texto).Select(n => Math.Round(n, 1)).Where(n => !permitidos.Contains(n)).ToList();
    }

    private static readonly CorpusDeAyuda Corpus = new();

    /// <summary>La composición de producción sobre una base en memoria propia de la corrida.</summary>
    private sealed class Composicion : IDisposable
    {
        private readonly SqliteConnection _conexion;
        private readonly ServiceProvider _servicios;

        public Composicion(IConfiguration configuracion)
        {
            var cadena = $"Data Source=file:bateria-{Guid.NewGuid():n}?mode=memory&cache=shared";
            _conexion = new SqliteConnection(cadena);
            _conexion.Open();
            var s = new ServiceCollection();
            s.AddLogging(l => l.SetMinimumLevel(LogLevel.Warning));
            s.AddInfrastructure(cadena);
            s.AddApplication();
            s.AddAI(configuracion, contenidoEnTrazas: false);
            _servicios = s.BuildServiceProvider();
            DatabaseInitializer.Initialize(_servicios);
        }

        private IServiceScope Sesion(string id)
        {
            var a = _servicios.CreateScope();
            a.ServiceProvider.GetRequiredService<SessionContext>().Set(id);
            return a;
        }

        public async Task SembrarAsync(string sesion, IEnumerable<RespuestaDeEncuesta> respuestas)
        {
            using var a = Sesion(sesion);
            await a.ServiceProvider.GetRequiredService<SessionSeeder>().EnsureSeededAsync();
            await using var ctx = await a.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
            foreach (var r in respuestas) { r.Id = 0; r.SessionId = sesion; ctx.Encuestas.Add(r); }
            await ctx.SaveChangesAsync();
        }

        public async Task VaciarLocalidadesAsync(string sesion)
        {
            using var a = Sesion(sesion);
            await a.ServiceProvider.GetRequiredService<SessionSeeder>().EnsureSeededAsync();
            await using var ctx = await a.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
            await ctx.Localidades.Where(l => l.SessionId == sesion).ExecuteDeleteAsync();
        }

        public async Task<RespuestaDelAsistente> PreguntarAsync(string sesion, string pregunta)
        {
            using var a = Sesion(sesion);
            return await a.ServiceProvider.GetRequiredService<AsistenteService>().ResponderAsync(pregunta);
        }

        public void Dispose()
        {
            _servicios.Dispose();
            _conexion.Dispose();
        }
    }
}
