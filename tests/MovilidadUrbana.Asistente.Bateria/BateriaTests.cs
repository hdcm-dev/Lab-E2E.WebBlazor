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

    private sealed record Archivo(string Version, string Congelada, Caso[] Casos, int? Minimo);

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    [Test]
    public async Task Corrida()
    {
        var ruta = Path.Combine(TestContext.CurrentContext.TestDirectory, Environment.GetEnvironmentVariable("BATERIA_ARCHIVO") ?? "bateria.v3.json");
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
        using var juez = camino == "modelo" ? new JuezACiegas(entorno[Habilitacion.ClaveEndpoint]!, entorno[Habilitacion.ClaveModelo]!) : null;
        int comparadas = 0, peores = 0, mejores = 0, iguales = 0;
        foreach (var c in bateria.Casos)
        {
            var composicion = c.SinModelo ? sinModelo : conModelo;
            var sesion = $"{c.Id}-{corrida}-{Guid.NewGuid():n}";
            if (c.Sesion != "sinEncuestas") await composicion.SembrarAsync(sesion, c.Sesion == "empates" ? FixtureDeEncuestas.EmpatesCercanos() : FixtureDeEncuestas.Veinte());
            if (c.Sesion == "sinLocalidades") await composicion.VaciarLocalidadesAsync(sesion);

            composicion.UltimoCrudo = null;
            var reloj = Stopwatch.StartNew();
            var r = await composicion.PreguntarAsync(sesion, c.Pregunta);
            reloj.Stop();
            latencias.Add(reloj.Elapsed.TotalMilliseconds);

            var fallas = Evaluar(c, r);
            var cruzadas = CifrasCruzadas(r);
            if (cruzadas.Count > 0) { inventados++; fallas.AddRange(cruzadas); }
            var inventado = NumerosInventados(c, r);
            if (inventado.Count > 0) { inventados++; fallas.Add($"cifra inventada: {string.Join(", ", inventado)}"); }

            // Compuerta «la redactada no puede ser peor que la fija»: la misma pregunta por el camino
            // de la plantilla, sobre los mismos datos, y una comparación a ciegas (orden sorteado por
            // caso y corrida, sin citas ni marcas que delaten el origen). Se registra en la línea.
            object? comparacion = null;
            if (r.RedactadaConIa && juez is not null)
            {
                var sesionFija = sesion + "-fija";
                if (c.Sesion != "sinEncuestas") await sinModelo.SembrarAsync(sesionFija, c.Sesion == "empates" ? FixtureDeEncuestas.EmpatesCercanos() : FixtureDeEncuestas.Veinte());
                if (c.Sesion == "sinLocalidades") await sinModelo.VaciarLocalidadesAsync(sesionFija);
                var fija = await sinModelo.PreguntarAsync(sesionFija, c.Pregunta);
                var v = await juez.CompararAsync(c.Pregunta, r.Texto, fija.Texto, $"{c.Id}-{corrida}");
                comparadas++;
                if (v.Ganadora == "fija") { peores++; fallas.Add("a ciegas, la redactada es peor que la fija"); }
                else if (v.Ganadora == "redactada") mejores++;
                else iguales++;
                comparacion = new { x = v.X, veredicto = v.Crudo, ganadora = v.Ganadora, fija = fija.Texto };
            }
            if (fallas.Count == 0) aciertos++;
            porResultado[r.Resultado.ToString()] = porResultado.GetValueOrDefault(r.Resultado.ToString()) + 1;

            lineas.Add(JsonSerializer.Serialize(new
            {
                id = c.Id, persona = c.Persona, tipo = c.Tipo, ok = fallas.Count == 0, fallas,
                intencion = r.Intencion, anillo = r.Anillo.ToString(), forma = r.Forma.ToString(), resultado = r.Resultado.ToString(),
                redactadaConIa = r.RedactadaConIa, destino = r.Destinos.FirstOrDefault()?.DestinoId, href = r.Destinos.FirstOrDefault()?.Href,
                grafico = r.Grafico.ToString(), motivo = r.MotivoDelResultado, texto = r.Texto, comparacion, crudo = r.Resultado == ResultadoDelTurno.Rechazado ? composicion.UltimoCrudo : null, aviso = r.Aviso, ms = Math.Round(reloj.Elapsed.TotalMilliseconds)
            }, Json));
        }

        var minimo = bateria.Minimo ?? 34;
        var ordenadas = latencias.Order().ToArray();
        double Percentil(double p) => ordenadas[Math.Min(ordenadas.Length - 1, (int)Math.Ceiling(p * ordenadas.Length) - 1)];
        var resumen = new
        {
            bateria = bateria.Version, hash, corrida, camino, fecha = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            modelo = camino == "modelo" ? "por variable OLLAMA_MODEL (fuera del repositorio)" : "ninguno",
            semilla = entorno["Asistente:Semilla"] ?? "7", temperatura = entorno["Asistente:Temperatura"] ?? "0",
            comparacionACiegas = new { comparadas, peores, mejores, iguales },
            k = aciertos, m = bateria.Casos.Length, minimo, inventados, redactadas = lineas.Count(l => l.Contains("\"redactadaConIa\":true", StringComparison.Ordinal)), porResultado,
            p50ms = Math.Round(Percentil(0.5)), p95ms = Math.Round(Percentil(0.95)), maxms = Math.Round(ordenadas[^1])
        };

        await File.WriteAllLinesAsync(Path.Combine(salida, $"corrida-{corrida}.jsonl"), lineas, new UTF8Encoding(false));
        await File.WriteAllTextAsync(Path.Combine(salida, $"resumen-{corrida}.json"), JsonSerializer.Serialize(resumen, new JsonSerializerOptions(Json) { WriteIndented = true }), new UTF8Encoding(false));
        TestContext.Progress.WriteLine($"BATERIA corrida {corrida} ({camino}): {aciertos}/{bateria.Casos.Length}, inventados {inventados}, p50 {resumen.p50ms} ms, p95 {resumen.p95ms} ms");
        foreach (var l in lineas.Where(l => l.Contains("\"ok\":false", StringComparison.Ordinal))) TestContext.Progress.WriteLine("FALLA " + l);

        // El umbral no se baja (K3 §11): el mínimo de la versión (34 de 37, 40 de 43) y cero cifras
        // inventadas ni atribuidas a otra categoría.
        Assert.Multiple(() =>
        {
            Assert.That(inventados, Is.Zero);
            Assert.That(peores, Is.Zero, "a ciegas, ninguna redactada peor que la fija");
            Assert.That(aciertos, Is.GreaterThanOrEqualTo(minimo));
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
        // Una cifra que la ayuda escribe en palabras («el CPA de ocho caracteres») también tiene origen.
        foreach (var f in Corpus.Fragmentos)
            foreach (Match m in Regex.Matches(VinculoDeCifras.SinTildes(f.Texto).ToLowerInvariant(), @"\p{L}+"))
                if (ValidadorDeCifras.ValorEnPalabras(m.Value) is double v) permitidos.Add(v);
        foreach (var n in new double[] { 1, 2, 3, 5, 6, 100, 346334, 291720, 3400, 3500 }) permitidos.Add(n);
        if (r.Distribucion is { } d) { permitidos.Add(d.Encuestas); foreach (var x in d.Filas) permitidos.Add(x.Cantidad); }
        return ValidadorDeCifras.Numeros(r.Texto).Select(n => Math.Round(n, 1)).Where(n => !permitidos.Contains(n)).ToList();
    }

    /// <summary>
    /// Oráculo de atribución, independiente del validador: la cifra que sigue a una categoría
    /// («Trabajo: 6», «Trabajo con 6») y la de «A y B, cada una con N» tienen que ser las de la
    /// distribución del código.
    /// </summary>
    private static List<string> CifrasCruzadas(RespuestaDelAsistente r)
    {
        var f = new List<string>();
        if (r.Distribucion is not { } d) return f;
        var texto = Regex.Replace(r.Texto, @"\[\d+\]|\*\*", string.Empty);
        foreach (var fila in d.Filas)
        {
            foreach (Match m in Regex.Matches(texto, $@"(?i)\b{Regex.Escape(fila.Etiqueta)}\b\s*(?::|=|,)?\s*(?:con|tiene|suma|es de)?\s*(\d+)\b"))
            {
                if (int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) != fila.Cantidad) f.Add($"cifra cruzada: «{m.Value}» con {fila.Etiqueta} = {fila.Cantidad}");
            }
        }
        foreach (Match m in Regex.Matches(texto, @"(?i)\b([\p{L}]+(?: [\p{L}]+)?) y ([\p{L}]+(?: [\p{L}]+)?),? cada (?:una|uno) con (\d+)"))
        {
            var n = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            foreach (var nombre in new[] { m.Groups[1].Value, m.Groups[2].Value })
            {
                var fila = d.Filas.FirstOrDefault(x => string.Equals(x.Etiqueta, nombre, StringComparison.OrdinalIgnoreCase));
                if (fila is not null && fila.Cantidad != n) f.Add($"cifra cruzada: «{m.Value}» con {fila.Etiqueta} = {fila.Cantidad}");
            }
        }
        return f;
    }

    private static readonly CorpusDeAyuda Corpus = new();

    /// <summary>
    /// Juez a ciegas: ve la pregunta y dos respuestas, X e Y, sin saber cuál es la del modelo. El
    /// orden sale del hash del caso y la corrida; citas y negritas se quitan de las dos porque la
    /// plantilla cita una sola vez y delataría el origen. Un veredicto ilegible cuenta como peor; dos textos idénticos sin citas cuentan como iguales sin preguntar.
    /// </summary>
    private sealed class JuezACiegas(string endpoint, string modelo) : IDisposable
    {
        public sealed record Veredicto(string X, string Crudo, string Ganadora);

        private const string Prompt =
            "Sos un evaluador a ciegas de respuestas de ayuda de una aplicación web, en español rioplatense. " +
            "Vas a ver una pregunta y dos respuestas, X e Y, armadas con los mismos datos. Decidí cuál es mejor para la persona que preguntó: " +
            "primero que sea exacta (que no diga nada falso, no cambie el sentido de una nota ni agregue datos), después que sea clara y breve, " +
            "y por último que use voseo y suene natural. Si son igual de buenas, respondé IGUAL. Respondé sólo una palabra: X, Y o IGUAL.";

        private readonly OllamaSharp.OllamaApiClient _cliente = new(new Uri(endpoint), modelo);

        public async Task<Veredicto> CompararAsync(string pregunta, string redactada, string fija, string clave)
        {
            // Dos textos iguales sin citas ni marcas no pueden ser uno peor que otro: no se le pregunta al juez.
            if (string.Equals(Ciega(redactada), Ciega(fija), StringComparison.Ordinal)) return new Veredicto("-", "idénticas sin citas", "igual");
            var redactadaEsX = SHA256.HashData(Encoding.UTF8.GetBytes(clave))[0] % 2 == 0;
            var (x, y) = redactadaEsX ? (redactada, fija) : (fija, redactada);
            var opciones = new Microsoft.Extensions.AI.ChatOptions { Temperature = 0, Seed = 7, Tools = [] };
            opciones.RawRepresentationFactory = _ => new OllamaSharp.Models.Chat.ChatRequest { Think = false };
            var r = await ((Microsoft.Extensions.AI.IChatClient)_cliente).GetResponseAsync(
                [new(Microsoft.Extensions.AI.ChatRole.System, Prompt),
                 new(Microsoft.Extensions.AI.ChatRole.User, $"PREGUNTA: {pregunta}\n\nX: {Ciega(x)}\n\nY: {Ciega(y)}")], opciones);
            var crudo = (r.Text ?? string.Empty).Trim();
            var palabra = Regex.Match(crudo.ToUpperInvariant(), @"\b(IGUAL|X|Y)\b").Value;
            var ganadora = palabra switch
            {
                "IGUAL" => "igual",
                "X" => redactadaEsX ? "redactada" : "fija",
                "Y" => redactadaEsX ? "fija" : "redactada",
                _ => "fija"
            };
            return new Veredicto(redactadaEsX ? "redactada" : "fija", crudo.Length > 40 ? crudo[..40] : crudo, ganadora);
        }

        private static string Ciega(string texto) => Regex.Replace(Regex.Replace(Regex.Replace(texto, @"\s*\[\d+\]", string.Empty), @"\*\*", string.Empty), @"\s+", " ").Trim();

        public void Dispose() => _cliente.Dispose();
    }

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
            // Para clasificar los rechazos (legítimos o evitables) la corrida guarda lo que el modelo
            // escribió antes del validador; la aplicación no lo guarda en ningún lado.
            var original = s.LastOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.AI.IChatClient) && d.ImplementationFactory is not null);
            if (original is not null) ((IServiceCollection)s).Add(ServiceDescriptor.Singleton<Microsoft.Extensions.AI.IChatClient>(sp =>
                new Grabador((Microsoft.Extensions.AI.IChatClient)original.ImplementationFactory!(sp), this)));
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

        public string? UltimoCrudo { get; set; }

        private sealed class Grabador(Microsoft.Extensions.AI.IChatClient interno, Composicion duena) : Microsoft.Extensions.AI.DelegatingChatClient(interno)
        {
            public override async Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                var r = await base.GetResponseAsync(messages, options, cancellationToken);
                duena.UltimoCrudo = r.Text;
                return r;
            }
        }

        public void Dispose()
        {
            _servicios.Dispose();
            _conexion.Dispose();
        }
    }
}
