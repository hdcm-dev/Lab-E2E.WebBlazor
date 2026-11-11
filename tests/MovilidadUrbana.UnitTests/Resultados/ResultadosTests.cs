using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.UnitTests.Arquitectura;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Application.Resultados;
using MovilidadUrbana.Web.Infrastructure.Persistence;

namespace MovilidadUrbana.UnitTests.Resultados;

/// <summary>Cifras por código (E1): regla de forma, porciones exactas, sin datos personales, contra SQL.</summary>
public class ResultadosTests
{
    private const string Sesion = "sesion-c";

    [TestCase(TipoDeDato.Numero, 0, false, FormaDeGrafico.Numero)]
    [TestCase(TipoDeDato.Distribucion, 4, true, FormaDeGrafico.TortaConTabla)]   // motivos
    [TestCase(TipoDeDato.Distribucion, 3, true, FormaDeGrafico.TortaConTabla)]   // frecuencias
    [TestCase(TipoDeDato.Distribucion, 6, false, FormaDeGrafico.Barras)]         // medios
    [TestCase(TipoDeDato.Distribucion, 7, true, FormaDeGrafico.Barras)]          // más de 6
    [TestCase(TipoDeDato.Distribucion, 1, true, FormaDeGrafico.Barras)]          // una sola categoría no es torta
    [TestCase(TipoDeDato.Comparacion, 5, false, FormaDeGrafico.Tabla)]           // promedios
    public void ReglaDeForma_Tabla(TipoDeDato tipo, int categorias, bool sumable, FormaDeGrafico esperada) =>
        Assert.That(ReglaDeForma.Decidir(tipo, categorias, sumable), Is.EqualTo(esperada));

    [Test]
    public async Task Medios_NuncaTorta()
    {
        using var arnes = new Arnes(habilitada: false);
        await arnes.SembrarEncuestasAsync(Sesion, FixtureDeEncuestas.Veinte());
        using var alcance = arnes.Sesion(Sesion);
        var medios = await alcance.ServiceProvider.GetRequiredService<ResultadosDeEncuestaService>().MediosAsync();
        Assert.Multiple(() =>
        {
            Assert.That(medios.Sumable, Is.False);
            Assert.That(ReglaDeForma.Decidir(TipoDeDato.Distribucion, medios.Filas.Count, medios.Sumable), Is.EqualTo(FormaDeGrafico.Barras));
            Assert.That(medios.Filas.Select(f => f.Cantidad), Is.Ordered.Descending);
        });
    }

    [Test(Description = "REQ-IA-41: propiedad con 1.000 listas aleatorias")]
    public void Porciones_SumanElTotal()
    {
        var azar = new Random(20261111);
        for (var i = 0; i < 1000; i++)
        {
            var cantidades = Enumerable.Range(0, azar.Next(2, 7)).Select(_ => azar.Next(0, 50)).ToList();
            var porcentajes = ReglaDeForma.PorcentajesEnteros(cantidades);
            var esperado = cantidades.Sum() == 0 ? 0 : 100;
            Assert.That(porcentajes.Sum(), Is.EqualTo(esperado), string.Join(",", cantidades));
            var d = new Distribucion("x", cantidades.Sum(), cantidades.Select((c, k) => new FilaDeDistribucion($"k{k}", $"K{k}", c)).ToList(), true, "n");
            Assert.That(d.TotalDeMarcas, Is.EqualTo(cantidades.Sum()));
        }
    }

    [Test(Description = "REQ-IA-10: ningún tipo alcanzable desde el servicio tiene Nombre ni Edad")]
    public void Resultados_SinDatosPersonales()
    {
        var visitados = new HashSet<Type>();
        var pendientes = new Queue<Type>(typeof(ResultadosDeEncuestaService).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(m => m.ReturnType));
        while (pendientes.TryDequeue(out var t))
        {
            if (!visitados.Add(t)) continue;
            foreach (var a in t.GetGenericArguments()) pendientes.Enqueue(a);
            if (t.Namespace?.StartsWith("MovilidadUrbana", StringComparison.Ordinal) != true) continue;
            foreach (var p in t.GetProperties())
            {
                Assert.That(p.Name, Is.Not.EqualTo("Nombre").And.Not.EqualTo("Edad"), $"{t.Name}.{p.Name}");
                pendientes.Enqueue(p.PropertyType);
            }
        }
        Assert.That(visitados.Any(t => t == typeof(Distribucion)), Is.True);
    }

    private static IEnumerable<TestCaseData> Fixtures()
    {
        yield return new TestCaseData("veinte", 31).SetName("Cifras_CoincidenConSql(veinte)");
        yield return new TestCaseData("empates", 33).SetName("Cifras_CoincidenConSql(empates en la mitad)");
    }

    [TestCaseSource(nameof(Fixtures))]
    [Description("Las cifras coinciden con la consulta SQL independiente (cifras.sql), sin tolerancia, también con promedios en la mitad")]
    public async Task Cifras_CoincidenConSql(string fixture, int marcas)
    {
        using var arnes = new Arnes(habilitada: false);
        await arnes.SembrarEncuestasAsync(Sesion, fixture == "empates" ? FixtureDeEncuestas.EmpatesCercanos() : FixtureDeEncuestas.Veinte());
        using var alcance = arnes.Sesion(Sesion);
        var servicio = alcance.ServiceProvider.GetRequiredService<ResultadosDeEncuestaService>();
        var factory = alcance.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        var conexion = (SqliteConnection)context.Database.GetDbConnection();
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = await File.ReadAllTextAsync(Path.Combine(Repositorio.Raiz, "tools", "asistente", "cifras.sql"));
        comando.Parameters.AddWithValue("@sesion", Sesion);
        var sql = new Dictionary<(string, string), double>();
        await using (var lector = await comando.ExecuteReaderAsync())
        {
            while (await lector.ReadAsync()) if (!lector.IsDBNull(2)) sql[(lector.GetString(0), lector.GetString(1))] = lector.GetDouble(2);
        }

        var medios = await servicio.MediosAsync();
        var motivos = await servicio.MotivosAsync();
        var frecuencias = await servicio.FrecuenciasAsync();
        var porMedio = (await servicio.PromediosAsync(AgrupacionDePromedios.Medio)).Filas;
        var todas = (await servicio.PromediosAsync(AgrupacionDePromedios.Ninguno)).Filas.Single();
        Assert.Multiple(() =>
        {
            Assert.That(sql[("encuestas", "total")], Is.EqualTo(20));
            foreach (var f in medios.Filas) Assert.That(f.Cantidad, Is.EqualTo(sql[("medios", f.Clave)]), f.Clave);
            foreach (var f in motivos.Filas) Assert.That(f.Cantidad, Is.EqualTo(sql.GetValueOrDefault(("motivos", f.Clave))), f.Clave);
            foreach (var f in frecuencias.Filas) Assert.That(f.Cantidad, Is.EqualTo(sql.GetValueOrDefault(("frecuencias", f.Clave))), f.Clave);
            // Sin tolerancia: el redondeo de la aplicación tiene que ser el de ROUND de SQLite.
            Assert.That(todas.MinutosPromedio, Is.EqualTo(sql[("minutos_promedio", "todas")]), "minutos, todas");
            Assert.That(todas.DistanciaPromedio, Is.EqualTo(sql[("distancia_promedio", "todas")]), "km, todas");
            foreach (var f in porMedio)
            {
                Assert.That(f.MinutosPromedio, Is.EqualTo(sql[("minutos_promedio", f.Grupo)]), $"minutos, {f.Grupo}");
                Assert.That(f.DistanciaPromedio, Is.EqualTo(sql[("distancia_promedio", f.Grupo)]), $"km, {f.Grupo}");
            }
            Assert.That(medios.Filas.Sum(f => f.Cantidad), Is.EqualTo(marcas));
        });
    }

    [Test(Description = "Un promedio en la mitad se redondea hacia arriba, como ROUND de SQLite: 41,25 → 41,3 y 15,25 → 15,3")]
    public async Task PromedioEnLaMitad_SeRedondeaComoSql()
    {
        using var arnes = new Arnes(habilitada: false);
        await arnes.SembrarEncuestasAsync(Sesion, FixtureDeEncuestas.EmpatesCercanos());
        using var alcance = arnes.Sesion(Sesion);
        var todas = (await alcance.ServiceProvider.GetRequiredService<ResultadosDeEncuestaService>().PromediosAsync(AgrupacionDePromedios.Ninguno)).Filas.Single();
        Assert.Multiple(() =>
        {
            Assert.That(todas.MinutosPromedio, Is.EqualTo(41.3));
            Assert.That(todas.DistanciaPromedio, Is.EqualTo(15.3));
        });
    }

    [TestCase(new double[] { 1.5, 3 }, 2.3)]
    [TestCase(new double[] { 1, 2, 3 }, 2)]
    [TestCase(new double[] { 1, 2, 3, 4 }, 2.5)]
    [TestCase(new double[] { }, 0)]
    public void Mediana(double[] valores, double esperada) =>
        Assert.That(ResultadosDeEncuestaService.Mediana(valores), Is.EqualTo(esperada));
}
