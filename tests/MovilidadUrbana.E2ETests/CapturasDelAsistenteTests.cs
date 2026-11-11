using Microsoft.Playwright;

namespace MovilidadUrbana.E2ETests;

/// <summary>
/// Evidencia visual (K3 §6): las pantallas tocadas a 1280 y a 390 de ancho, con el desborde
/// horizontal medido (0), el CLS medido (0) y el estilo computado del panel verificado. Las
/// capturas quedan en <c>resultados/capturas</c>; se corren con <c>--filter Category=Capturas</c>.
/// </summary>
[TestFixture]
[Category("Capturas")]
public class CapturasDelAsistenteTests : E2ETestBase
{
    private static readonly int[] Anchos = [1280, 390];

    private static string Carpeta()
    {
        var carpeta = Path.Combine(TestContext.CurrentContext.WorkDirectory, "resultados", "capturas");
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    private async Task MedirAsync(string nombre, int ancho)
    {
        await Page.WaitForTimeoutAsync(400);
        var desborde = await Page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
        var cls = await Page.EvaluateAsync<double>("""
            () => new Promise(r => {
                let total = 0;
                new PerformanceObserver(l => { for (const e of l.getEntries()) if (!e.hadRecentInput) total += e.value; })
                    .observe({ type: 'layout-shift', buffered: true });
                setTimeout(() => r(total), 300);
            })
            """);
        await Page.ScreenshotAsync(new() { Path = Path.Combine(Carpeta(), $"{nombre}-{ancho}.png"), FullPage = true });
        TestContext.Progress.WriteLine($"CAPTURA {nombre}-{ancho}: desborde={desborde} cls={cls:0.000}");
        Assert.Multiple(() =>
        {
            Assert.That(desborde, Is.LessThanOrEqualTo(0), $"{nombre} a {ancho}: desborde horizontal");
            Assert.That(cls, Is.LessThan(0.01), $"{nombre} a {ancho}: CLS");
        });
    }

    [TestCaseSource(nameof(Anchos))]
    public async Task Panel(int ancho)
    {
        await Page.SetViewportSizeAsync(ancho, 900);
        await IrAAsync("/localidades");
        await Page.GetByTestId("nav-asistente").ClickAsync();
        await Page.GetByTestId("asistente-pregunta").FillAsync("¿Cuántas localidades tengo?");
        await Page.GetByTestId("asistente-enviar").ClickAsync();
        await Expect(Page.GetByTestId("burbuja-asistente")).ToHaveCountAsync(1);

        // El estilo computado prueba que la hoja se aplicó (K2-AP-1): fijo a la derecha, con su ancho.
        var estilo = await Page.GetByTestId("panel-asistente").EvaluateAsync<string[]>("e => { const s = getComputedStyle(e); return [s.position, s.width]; }");
        Assert.That(estilo[0], Is.EqualTo("fixed"));
        Assert.That(estilo[1], Is.EqualTo(ancho <= 768 ? $"{ancho}px" : "400px"));
        await MedirAsync("panel", ancho);
    }

    [TestCaseSource(nameof(Anchos))]
    public async Task Resultados(int ancho)
    {
        await Page.SetViewportSizeAsync(ancho, 900);
        await IrAAsync("/resultados");
        await Expect(Page.GetByTestId("sin-encuestas")).ToBeVisibleAsync();
        await MedirAsync("resultados-vacio", ancho);
    }

    [TestCaseSource(nameof(Anchos))]
    public async Task ResultadosConDatos(int ancho)
    {
        await Page.SetViewportSizeAsync(ancho, 900);
        await IrAAsync("/encuesta");
        await Page.GetByTestId("campo-nombre").FillAsync("Ana Pérez");
        await Page.GetByTestId("campo-edad").FillAsync("34");
        await Page.GetByTestId("campo-localidad").SelectOptionAsync("Corrientes");
        await Page.GetByTestId("boton-siguiente").ClickAsync();
        await Page.GetByTestId("medio-colectivo").CheckAsync();
        await Page.GetByTestId("medio-bicicleta").CheckAsync();
        await Page.GetByTestId("campo-frecuencia").SelectOptionAsync("diaria");
        await Page.GetByTestId("boton-siguiente").ClickAsync();
        await Page.GetByTestId("campo-distancia").FillAsync("12.5");
        await Page.GetByTestId("campo-minutos").FillAsync("45");
        await Page.GetByTestId("campo-motivo").SelectOptionAsync("trabajo");
        await Page.GetByTestId("boton-finalizar").ClickAsync();
        await Expect(Page.GetByTestId("mensaje-envio")).ToBeVisibleAsync();

        await IrAAsync("/resultados");
        await Expect(Page.GetByTestId("grafico-medios")).ToHaveAttributeAsync("data-forma", "barras");
        await Expect(Page.GetByTestId("grafico-motivos")).ToHaveAttributeAsync("data-forma", "torta");
        await Expect(Page.GetByTestId("advertencia-n-chico")).ToBeVisibleAsync();
        await MedirAsync("resultados-con-datos", ancho);
    }

    [TestCaseSource(nameof(Anchos))]
    public async Task AltaPrellenada(int ancho)
    {
        await Page.SetViewportSizeAsync(ancho, 900);
        await IrAAsync("/localidades?alta=1&nombre=Goya&provincia=Corrientes&cp=3450&propuesta=ia");
        await Expect(Page.GetByTestId("propuesta-ia")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("error-habitantes")).ToBeVisibleAsync();
        await MedirAsync("alta-prellenada", ancho);
    }

    [TestCaseSource(nameof(Anchos))]
    public async Task Medicion(int ancho)
    {
        await Page.SetViewportSizeAsync(ancho, 900);
        await IrAAsync("/asistente/medicion");
        await Expect(Page.GetByTestId("titulo")).ToHaveTextAsync("Medición del asistente");
        await MedirAsync("medicion", ancho);
    }
}
