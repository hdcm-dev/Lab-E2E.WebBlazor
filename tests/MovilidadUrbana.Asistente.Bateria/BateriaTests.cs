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
/// Una corrida de la batería (bateria.v3 por omisión; K3 §2, K7 §9), con la composición de
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

    private sealed record ParDeControl(string Id, string Intencion, string Esperado, string? Defecto, string Pregunta, Dictionary<string, double> Datos, string Fija, string Redactada);

    private sealed record ArchivoDePares(string Version, ParDeControl[] Pares);

    /// <summary>Lo que se cuenta por forma redactada (intención): intentos del redactor, rechazos y pares juzgados.</summary>
    private sealed class PorForma
    {
        public int Intentos { get; set; }
        public int Rechazos { get; set; }
        public int SoloCitas { get; set; }
        public int Redactadas { get; set; }
        public int Identicas { get; set; }
        public int Juzgadas { get; set; }
        public int Peores { get; set; }
        public int ControlJuzgados { get; set; }
        public int ControlAciertos { get; set; }
    }

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
        // ADR 0008: la redacción está apagada por omisión; la batería la prende con Asistente__Redaccion__Habilitada=true.
        var redaccion = camino == "modelo" && habilitacion.RedaccionHabilitada;

        using var conModelo = new Composicion(entorno);
        using var sinModelo = new Composicion(new ConfigurationBuilder().AddInMemoryCollection(
            entorno.AsEnumerable().Where(p => p.Key != Habilitacion.ClaveModelo)).Build());

        var lineas = new List<string>();
        var latencias = new List<double>();
        var aciertos = 0;
        var inventados = 0;
        var porResultado = new Dictionary<string, int>();
        // Juez distinto del redactor (K7 R9.8): otro modelo del servidor (BATERIA_JUEZ_MODELO) o, si no
        // hay, las reglas deterministas declaradas en JuezPorReglas. Las reglas corren siempre.
        var reglas = new JuezPorReglas();
        var modeloJuez = Environment.GetEnvironmentVariable("BATERIA_JUEZ_MODELO");
        var endpoint = entorno[Habilitacion.ClaveEndpoint];
        using var juezModelo = !string.IsNullOrWhiteSpace(modeloJuez) && !string.IsNullOrWhiteSpace(endpoint) && modeloJuez != entorno[Habilitacion.ClaveModelo]
            ? new JuezPorModelo(endpoint!, modeloJuez!) : null;
        IJuez juez = juezModelo is null ? reglas : juezModelo;
        var porForma = Intenciones.ConRedaccion.ToDictionary(i => i, _ => new PorForma(), StringComparer.Ordinal);
        int comparadas = 0, juzgadas = 0, identicas = 0, peores = 0, mejores = 0, iguales = 0;
        var motivosDeRechazo = new Dictionary<string, int>(StringComparer.Ordinal);
        var distintasDeLaFija = new List<string>();
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
            porForma.TryGetValue(r.Intencion, out var forma);
            var intento = redaccion && forma is not null && r.Resultado != ResultadoDelTurno.Abstencion;
            if (intento)
            {
                // Tasa de rechazo del validador (K6 §12.10): cada vez que el redactor se llamó.
                forma!.Intentos++;
                if (r.Resultado == ResultadoDelTurno.Rechazado)
                {
                    forma.Rechazos++;
                    var clave = (r.MotivoDelResultado ?? "sin motivo").Split(':')[0];
                    motivosDeRechazo[clave] = motivosDeRechazo.GetValueOrDefault(clave) + 1;
                }
                if (r.MotivoDelResultado == AsistenteService.MotivoSoloCitas) forma.SoloCitas++;
                if (r.RedactadaConIa) forma.Redactadas++;
            }

            var necesitaFija = r.RedactadaConIa || (camino == "modelo" && !redaccion && r.Intencion != Intenciones.AltaDesdeFrase);
            if (necesitaFija)
            {
                var sesionFija = sesion + "-fija";
                if (c.Sesion != "sinEncuestas") await sinModelo.SembrarAsync(sesionFija, c.Sesion == "empates" ? FixtureDeEncuestas.EmpatesCercanos() : FixtureDeEncuestas.Veinte());
                if (c.Sesion == "sinLocalidades") await sinModelo.VaciarLocalidadesAsync(sesionFija);
                var fija = await sinModelo.PreguntarAsync(sesionFija, c.Pregunta);
                if (!r.RedactadaConIa)
                {
                    // Bandera apagada: la respuesta tiene que ser la plantilla, palabra por palabra.
                    if (!string.Equals(r.Texto, fija.Texto, StringComparison.Ordinal)) { distintasDeLaFija.Add(c.Id); fallas.Add("con la redacción apagada, distinta de la plantilla"); }
                }
                else
                {
                    comparadas++;
                    if (Citas.SoloSumaCitas(r.Texto, fija.Texto))
                    {
                        // No debería pasar: el servicio ya no las cuenta como redactadas (K5 CD-8).
                        identicas++; forma!.Identicas++;
                        fallas.Add("redactada que sólo suma citas");
                        comparacion = new { juez = "ninguno", ganadora = "idéntica", fija = fija.Texto };
                    }
                    else
                    {
                        var datos = r.Cifras.GroupBy(x => x.Etiqueta).ToDictionary(g => g.Key, g => g.First().Valor);
                        var v = await juez.CompararAsync(c.Pregunta, datos, r.Texto, fija.Texto, $"{c.Id}-{corrida}");
                        var vr = juez == reglas ? v : await reglas.CompararAsync(c.Pregunta, datos, r.Texto, fija.Texto, $"{c.Id}-{corrida}");
                        juzgadas++; forma!.Juzgadas++;
                        if (v.Ganadora == "fija") { peores++; forma.Peores++; fallas.Add("a ciegas, la redactada es peor que la fija"); }
                        else if (v.Ganadora == "redactada") mejores++;
                        else iguales++;
                        comparacion = new { juez = juez.Nombre, x = v.X, veredicto = v.Crudo, ganadora = v.Ganadora, reglas = vr.Ganadora, fija = fija.Texto };
                    }
                }
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

        // Pares fijos de control: la compuerta se ejerce en cada corrida aunque ninguna redactada
        // difiera de la fija (K7 R9.8). El juez principal decide; las reglas se registran al lado.
        var pares = JsonSerializer.Deserialize<ArchivoDePares>(await File.ReadAllBytesAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "pares-a-ciegas.v1.json")), Json)!;
        var lineasDeControl = new List<string>();
        int controlJuzgados = 0, controlAciertos = 0, controlAciertosReglas = 0, controlPeores = 0, controlPeoresDetectadosReglas = 0;
        foreach (var par in pares.Pares)
        {
            var v = await juez.CompararAsync(par.Pregunta, par.Datos, par.Redactada, par.Fija, $"{par.Id}-{corrida}");
            var vr = juez == reglas ? v : await reglas.CompararAsync(par.Pregunta, par.Datos, par.Redactada, par.Fija, $"{par.Id}-{corrida}");
            bool Acierta(string g) => par.Esperado == "peor" ? g == "fija" : g != "fija";
            controlJuzgados++;
            if (Acierta(v.Ganadora)) controlAciertos++;
            if (Acierta(vr.Ganadora)) controlAciertosReglas++;
            if (par.Esperado == "peor") { controlPeores++; if (vr.Ganadora == "fija") controlPeoresDetectadosReglas++; }
            if (porForma.TryGetValue(par.Intencion, out var f)) { f.ControlJuzgados++; if (Acierta(v.Ganadora)) f.ControlAciertos++; }
            lineasDeControl.Add(JsonSerializer.Serialize(new { id = par.Id, intencion = par.Intencion, esperado = par.Esperado, defecto = par.Defecto, juez = juez.Nombre, x = v.X, veredicto = v.Crudo, ganadora = v.Ganadora, acierta = Acierta(v.Ganadora), reglas = vr.Ganadora, motivoReglas = vr.Crudo }, Json));
        }
        var paresJuzgados = controlJuzgados + juzgadas;
        var compuerta = paresJuzgados == 0 ? "NO EJECUTADO" : "EJERCIDA";
        var intentos = porForma.Values.Sum(f => f.Intentos);
        var rechazos = porForma.Values.Sum(f => f.Rechazos);

        var minimo = bateria.Minimo ?? 34;
        var ordenadas = latencias.Order().ToArray();
        double Percentil(double p) => ordenadas[Math.Min(ordenadas.Length - 1, (int)Math.Ceiling(p * ordenadas.Length) - 1)];
        var resumen = new
        {
            bateria = bateria.Version, hash, corrida, camino, fecha = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            modelo = camino == "modelo" ? "por variable OLLAMA_MODEL (fuera del repositorio)" : "ninguno",
            semilla = entorno["Asistente:Semilla"] ?? "7", temperatura = entorno["Asistente:Temperatura"] ?? "0",
            redaccion = redaccion ? "habilitada" : "apagada",
            comparacionACiegas = new
            {
                compuerta, juez = juez.Nombre, paresJuzgados,
                control = new { pares = pares.Version, juzgados = controlJuzgados, aciertos = controlAciertos, aciertosReglas = controlAciertosReglas, peores = controlPeores, peoresDetectadosPorReglas = controlPeoresDetectadosReglas },
                redactadas = new { comparadas, juzgadas, identicas, peores, mejores, iguales }
            },
            validador = new { intentos, rechazos, tasaDeRechazo = intentos == 0 ? (double?)null : Math.Round((double)rechazos / intentos, 3), soloCitas = porForma.Values.Sum(f => f.SoloCitas), porMotivo = motivosDeRechazo },
            porForma = porForma.ToDictionary(kv => kv.Key, kv => new { intentos = kv.Value.Intentos, rechazos = kv.Value.Rechazos, soloCitas = kv.Value.SoloCitas, redactadas = kv.Value.Redactadas, juzgadas = kv.Value.Juzgadas, peores = kv.Value.Peores, controlJuzgados = kv.Value.ControlJuzgados, controlAciertos = kv.Value.ControlAciertos }),
            igualesALaPlantilla = camino == "modelo" && !redaccion ? new { distintas = distintasDeLaFija } : null,
            k = aciertos, m = bateria.Casos.Length, minimo, inventados, redactadas = lineas.Count(l => l.Contains("\"redactadaConIa\":true", StringComparison.Ordinal)), porResultado,
            p50ms = Math.Round(Percentil(0.5)), p95ms = Math.Round(Percentil(0.95)), maxms = Math.Round(ordenadas[^1])
        };

        await File.WriteAllLinesAsync(Path.Combine(salida, $"corrida-{corrida}.jsonl"), lineas, new UTF8Encoding(false));
        await File.WriteAllLinesAsync(Path.Combine(salida, $"control-{corrida}.jsonl"), lineasDeControl, new UTF8Encoding(false));
        await File.WriteAllTextAsync(Path.Combine(salida, $"resumen-{corrida}.json"), JsonSerializer.Serialize(resumen, new JsonSerializerOptions(Json) { WriteIndented = true }), new UTF8Encoding(false));
        TestContext.Progress.WriteLine($"BATERIA corrida {corrida} ({camino}, redacción {resumen.redaccion}): {aciertos}/{bateria.Casos.Length}, inventados {inventados}, p50 {resumen.p50ms} ms, p95 {resumen.p95ms} ms");
        TestContext.Progress.WriteLine($"BATERIA compuerta {compuerta}: juez {juez.Nombre}, pares juzgados {paresJuzgados} (control {controlJuzgados}, aciertos {controlAciertos}; redactadas {juzgadas}), peores {peores}; validador {rechazos}/{intentos} rechazos");
        foreach (var l in lineas.Where(l => l.Contains("\"ok\":false", StringComparison.Ordinal))) TestContext.Progress.WriteLine("FALLA " + l);

        // El umbral no se baja (K3 §11): el mínimo de la versión (34 de 37, 40 de 43) y cero cifras
        // inventadas ni atribuidas a otra categoría.
        Assert.Multiple(() =>
        {
            Assert.That(inventados, Is.Zero);
            Assert.That(peores, Is.Zero, "a ciegas, ninguna redactada peor que la fija");
            Assert.That(paresJuzgados, Is.GreaterThan(0), "compuerta NO EJECUTADO: 0 pares juzgados");
            Assert.That(controlPeoresDetectadosReglas, Is.EqualTo(controlPeores), "las reglas declaradas detectan todos los pares peores de control");
            Assert.That(distintasDeLaFija, Is.Empty, "con la redacción apagada, cada respuesta es la plantilla");
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

    /// <summary>Veredicto a ciegas: qué texto fue X, lo que respondió el juez y quién ganó (redactada, fija o igual).</summary>
    private sealed record Veredicto(string X, string Crudo, string Ganadora);

    /// <summary>
    /// Juez a ciegas: ve la pregunta, los datos y dos respuestas, X e Y, sin saber cuál es la del
    /// modelo. El orden sale del hash del caso y la corrida; citas y negritas se quitan de las dos
    /// porque la plantilla cita una sola vez y delataría el origen.
    /// </summary>
    private interface IJuez
    {
        string Nombre { get; }

        Task<Veredicto> CompararAsync(string pregunta, IReadOnlyDictionary<string, double> datos, string redactada, string fija, string clave);
    }

    private static string Ciega(string texto) => Regex.Replace(Regex.Replace(Regex.Replace(texto, @"\s*\[\d+\]", string.Empty), @"\*\*", string.Empty), @"\s+", " ").Trim();

    private static bool RedactadaEsX(string clave) => SHA256.HashData(Encoding.UTF8.GetBytes(clave))[0] % 2 == 0;

    /// <summary>
    /// Juez por modelo, distinto del que redacta (BATERIA_JUEZ_MODELO). Un veredicto ilegible cuenta como peor.
    /// </summary>
    private sealed class JuezPorModelo(string endpoint, string modelo) : IJuez, IDisposable
    {
        private const string Prompt =
            "Sos un evaluador a ciegas de respuestas de ayuda de una aplicación web, en español rioplatense. " +
            "Vas a ver una pregunta, los datos que calculó la aplicación y dos respuestas, X e Y, armadas con esos mismos datos. Decidí cuál es mejor para la persona que preguntó: " +
            "primero que sea exacta (que no diga nada falso respecto de los datos, no cambie el sentido de una nota ni agregue datos), después que sea clara y breve, " +
            "y por último que use voseo y suene natural. Si son igual de buenas, respondé IGUAL. Respondé sólo una palabra: X, Y o IGUAL.";

        private readonly OllamaSharp.OllamaApiClient _cliente = new(new Uri(endpoint), modelo);

        public string Nombre => "modelo distinto del redactor (BATERIA_JUEZ_MODELO)";

        public async Task<Veredicto> CompararAsync(string pregunta, IReadOnlyDictionary<string, double> datos, string redactada, string fija, string clave)
        {
            var redactadaEsX = RedactadaEsX(clave);
            var (x, y) = redactadaEsX ? (redactada, fija) : (fija, redactada);
            var opciones = new Microsoft.Extensions.AI.ChatOptions { Temperature = 0, Seed = 7, Tools = [] };
            opciones.RawRepresentationFactory = _ => new OllamaSharp.Models.Chat.ChatRequest { Think = false };
            var listaDeDatos = string.Join("; ", datos.Select(d => $"{d.Key} = {d.Value.ToString(CultureInfo.InvariantCulture)}"));
            var r = await ((Microsoft.Extensions.AI.IChatClient)_cliente).GetResponseAsync(
                [new(Microsoft.Extensions.AI.ChatRole.System, Prompt),
                 new(Microsoft.Extensions.AI.ChatRole.User, $"PREGUNTA: {pregunta}\n\nDATOS: {listaDeDatos}\n\nX: {Ciega(x)}\n\nY: {Ciega(y)}")], opciones);
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

        public void Dispose() => _cliente.Dispose();
    }

    /// <summary>
    /// Juez por reglas deterministas, declaradas y simétricas: cada texto suma un defecto por regla
    /// que rompe y gana el que tiene menos (empate = igual). No sabe cuál es la redactada.
    /// R1 registro ajeno (tuteo, vosotros) · R2 habla de porcentajes y el otro no · R3 nota de
    /// multirrespuesta invertida («no llega», «menos de cien») · R4 un número que no está en los
    /// datos ni en la pregunta · R5 una categoría con una cifra distinta de la de los datos · R6 más
    /// larga que 1,5 × la otra + 40 · R7 un trigrama repetido · R8 eco de instrucción.
    /// </summary>
    private sealed class JuezPorReglas : IJuez
    {
        public string Nombre => "reglas deterministas declaradas (R1–R8)";

        public Task<Veredicto> CompararAsync(string pregunta, IReadOnlyDictionary<string, double> datos, string redactada, string fija, string clave)
        {
            var redactadaEsX = RedactadaEsX(clave);
            var (x, y) = redactadaEsX ? (Ciega(redactada), Ciega(fija)) : (Ciega(fija), Ciega(redactada));
            var dx = Defectos(x, y, pregunta, datos);
            var dy = Defectos(y, x, pregunta, datos);
            var ganaX = dx.Count < dy.Count;
            var ganadora = dx.Count == dy.Count ? "igual" : (ganaX == redactadaEsX ? "redactada" : "fija");
            var crudo = $"X[{string.Join(",", dx)}] Y[{string.Join(",", dy)}]";
            return Task.FromResult(new Veredicto(redactadaEsX ? "redactada" : "fija", crudo, ganadora));
        }

        public static List<string> Defectos(string a, string b, string pregunta, IReadOnlyDictionary<string, double> datos)
        {
            var d = new List<string>();
            var ta = VinculoDeCifras.SinTildes(a).ToLowerInvariant();
            var tb = VinculoDeCifras.SinTildes(b).ToLowerInvariant();
            if (Regex.IsMatch(a, @"(?i)(?<!\p{L})(vosotr[oa]s|vuestr[oa]s?|tú|contigo|tienes|puedes|debes|quieres|sabes|eres)(?!\p{L})")) d.Add("R1");
            bool Porcentual(string t) => t.Contains('%') || t.Contains("porcentaje", StringComparison.Ordinal);
            if (Porcentual(ta) && !Porcentual(tb)) d.Add("R2");
            if (Regex.IsMatch(ta, @"no llega\w*|menos del? (100|cien)|no alcanza\w*")) d.Add("R3");
            var permitidos = datos.Values.Concat(ValidadorDeCifras.Numeros(pregunta)).ToList();
            if (ValidadorDeCifras.Numeros(a).Any(n => !permitidos.Any(p => Math.Abs(p - n) < 0.05))) d.Add("R4");
            foreach (var (etiqueta, valor) in datos)
            {
                foreach (Match m in Regex.Matches(a, $@"(?i)\b{Regex.Escape(etiqueta)}\b\s*(?::|,)?\s*(?:con)?\s*(\d+)\b"))
                {
                    if (Math.Abs(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) - valor) > 0.05) { d.Add("R5"); break; }
                }
            }
            if (a.Length > b.Length * 1.5 + 40) d.Add("R6");
            var palabras = Regex.Matches(ta, @"[\p{L}\d]+").Select(m => m.Value).ToList();
            var vistos = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i + 2 < palabras.Count; i++) if (!vistos.Add($"{palabras[i]} {palabras[i + 1]} {palabras[i + 2]}")) { d.Add("R7"); break; }
            if (ValidadorDeCifras.Eco(a, b) is not null) d.Add("R8");
            return d.Distinct().ToList();
        }
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
