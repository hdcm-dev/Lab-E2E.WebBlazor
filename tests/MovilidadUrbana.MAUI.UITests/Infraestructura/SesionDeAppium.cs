using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;

// En el namespace raíz: un [SetUpFixture] alcanza solo a su namespace y a los de abajo.
namespace MovilidadUrbana.MAUI.UITests;

/// <summary>
/// Abre una sola sesión de Appium para toda la corrida. El servidor tiene que estar escuchando
/// (por defecto en http://127.0.0.1:4723, se cambia con APPIUM_URL) y la app instalada en el
/// dispositivo. NoReset=true conserva los datos entre pruebas; cada prueba deja lo que encontró.
/// </summary>
[SetUpFixture]
public class SesionDeAppium
{
    public const string Paquete = "ar.lab.movilidadurbana";

    private static AndroidDriver? _driver;

    public static AndroidDriver App => _driver ?? throw new InvalidOperationException("La sesión de Appium no está abierta.");

    [OneTimeSetUp]
    public void Abrir()
    {
        var url = Environment.GetEnvironmentVariable("APPIUM_URL") ?? "http://127.0.0.1:4723";
        var opciones = new AppiumOptions { AutomationName = "UIAutomator2", PlatformName = "Android" };
        opciones.AddAdditionalAppiumOption(MobileCapabilityType.NoReset, true);
        opciones.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppPackage, Paquete);
        opciones.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppActivity, $"{Paquete}.MainActivity");
        // La primera pantalla tarda hasta 25 s en frío en el teléfono de prueba.
        opciones.AddAdditionalAppiumOption("appWaitActivity", "*");
        opciones.AddAdditionalAppiumOption("appWaitDuration", 60000);
        opciones.AddAdditionalAppiumOption("newCommandTimeout", 300);

        _driver = new AndroidDriver(new Uri(url), opciones, TimeSpan.FromSeconds(120));

        // Con NoReset, si el proceso quedó vivo en segundo plano el driver no lo trae al frente
        // («already running and noReset is enabled»): se activa a mano y se espera la primera pantalla.
        _driver.ActivateApp(Paquete);
        var limite = DateTime.UtcNow + TimeSpan.FromSeconds(90);
        while (_driver.FindElements(OpenQA.Selenium.Appium.MobileBy.Id("lista-localidades")).Count == 0
               && _driver.FindElements(OpenQA.Selenium.Appium.MobileBy.Id("etiqueta-paso")).Count == 0)
        {
            if (DateTime.UtcNow > limite) throw new InvalidOperationException("La aplicación no mostró su primera pantalla en 90 s.");
            Thread.Sleep(1000);
        }
    }

    [OneTimeTearDown]
    public void Cerrar()
    {
        _driver?.Quit();
        _driver?.Dispose();
        _driver = null;
    }
}
