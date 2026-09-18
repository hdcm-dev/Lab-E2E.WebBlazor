using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace MovilidadUrbana.MAUI.UITests.Infrastructure;

/// <summary>
/// Lo que toda prueba de pantalla necesita: localizar por AutomationId, esperar por condición —nunca
/// por tiempo fijo—, escribir cerrando el teclado, y desplazar hasta que un elemento quede a la vista.
/// </summary>
public abstract class ScreenTestBase
{
    protected static AndroidDriver App => AppiumSession.App;

    // El teléfono de prueba es lento: guardar y volver a la lista puede pasar los 30 s.
    protected static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    /// <summary>El AutomationId de MAUI llega a Android como resource-id: MobileBy.Id lo encuentra.</summary>
    protected static AppiumElement FindById(string id) => WaitFor(() => App.FindElement(MobileBy.Id(id)), $"No apareció «{id}»");

    protected static AppiumElement FindByText(string texto) =>
        WaitFor(() => App.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{texto}\")")), $"No apareció el texto «{texto}»");

    protected static bool Exists(string id) => App.FindElements(MobileBy.Id(id)).Count > 0;

    protected static void WaitUntilGone(string id) =>
        WaitFor(() => Exists(id) ? throw new NoSuchElementException() : true, $"«{id}» sigue en pantalla");

    protected static void Tap(string id) => FindById(id).Click();

    /// <summary>Escribe y cierra el teclado: con el teclado abierto, lo de abajo queda tapado.</summary>
    protected static void TypeText(string id, string texto)
    {
        var campo = FindById(id);
        campo.Click();
        campo.Clear();
        campo.SendKeys(texto);
        HideKeyboard();
    }

    protected static void HideKeyboard()
    {
        if (!App.IsKeyboardShown()) return;
        App.HideKeyboard();
        // Hasta que el teclado se fue de verdad: tocar mientras se cierra aterriza en otro lugar.
        WaitFor(() => App.IsKeyboardShown() ? throw new WebDriverException("teclado abierto") : true, "El teclado no se cerró");
    }

    /// <summary>Elige una opción de un Picker: abre el diálogo nativo y toca la opción por su texto.</summary>
    protected static void Pick(string idDelSelector, string opcion)
    {
        Tap(idDelSelector);
        FindByText(opcion).Click();
    }

    /// <summary>Desplaza la vista hasta que el elemento quede a la vista, con UiScrollable de UI Automator.</summary>
    protected static AppiumElement ScrollIntoView(string id)
    {
        if (!Exists(id))
        {
            App.FindElement(MobileBy.AndroidUIAutomator(
                $"new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().resourceId(\"{AppiumSession.AppPackage}:id/{id}\"))"));
        }
        return FindById(id);
    }

    protected static AppiumElement ScrollToText(string texto)
    {
        App.FindElement(MobileBy.AndroidUIAutomator(
            $"new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().text(\"{texto}\"))"));
        return FindByText(texto);
    }

    protected static string GetText(string id) => FindById(id).Text;

    /// <summary>
    /// Va a una pestaña. Si una prueba anterior dejó abierta una página apilada —que oculta la barra de
    /// pestañas—, vuelve atrás hasta encontrarla: cada prueba arranca desde un lugar conocido.
    /// </summary>
    protected static void GoToTab(string titulo)
    {
        for (var intento = 0; intento < 4; intento++)
        {
            HideKeyboard();
            var pestañas = App.FindElements(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{titulo}\")"));
            if (pestañas.Count > 0) { pestañas[0].Click(); return; }
            App.Navigate().Back();
            Thread.Sleep(1000);
        }
        FindByText(titulo).Click();
    }

    private static T WaitFor<T>(Func<T> intento, string mensaje)
    {
        var limite = DateTime.UtcNow + Timeout;
        Exception? ultima = null;
        while (DateTime.UtcNow < limite)
        {
            try { return intento(); }
            catch (WebDriverException e) { ultima = e; }
            Thread.Sleep(500);
        }
        throw new AssertionException($"{mensaje} en {Timeout.TotalSeconds:0} s.", ultima);
    }

    /// <summary>Guarda una captura junto a los resultados, con el nombre de la prueba: es la evidencia.</summary>
    protected static void Capture(string nombre)
    {
        var carpeta = Path.Combine(TestContext.CurrentContext.WorkDirectory, "capturas");
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, $"{nombre}.png");
        App.GetScreenshot().SaveAsFile(ruta);
        TestContext.AddTestAttachment(ruta);
    }
}
