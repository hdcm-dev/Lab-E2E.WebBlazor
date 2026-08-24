using System.Diagnostics;
using System.Net.Sockets;

namespace MovilidadUrbana.E2ETests;

/// <summary>
/// Levanta la aplicación bajo prueba antes de la primera prueba y la baja al terminar.
///
/// Es el reemplazo de la sección `webServer` que ofrece el runner de JavaScript: el binding de
/// .NET no tiene equivalente, así que el ciclo de vida del servidor lo maneja un
/// <see cref="SetUpFixtureAttribute"/> a nivel de ensamblado.
///
/// Si está definida la variable `URL_BASE` no se levanta nada y se prueba contra ese entorno.
///
/// Vive en el namespace de las pruebas y no en uno anidado: un `SetUpFixture` cubre su propio
/// namespace y los que cuelgan de él, nunca el de arriba.
/// </summary>
[SetUpFixture]
public class ServidorDeLaAplicacion
{
    private const string NombreDelEnsamblado = "MovilidadUrbana.Web";

    /// <summary>
    /// El apphost que genera `dotnet publish` lleva `.exe` en Windows y no lleva extensión en
    /// Linux y macOS. Con el nombre fijo de Linux, la publicación existe pero el fixture no la
    /// encuentra y el error apunta al lugar equivocado.
    /// </summary>
    private static string NombreDelApphost =>
        OperatingSystem.IsWindows() ? NombreDelEnsamblado + ".exe" : NombreDelEnsamblado;

    private static Process? _proceso;

    /// <summary>URL contra la que corren todas las pruebas.</summary>
    public static string UrlBase { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task LevantarAsync()
    {
        var urlExterna = Environment.GetEnvironmentVariable("URL_BASE");
        if (!string.IsNullOrWhiteSpace(urlExterna))
        {
            UrlBase = urlExterna.TrimEnd('/');
            TestContext.Progress.WriteLine($"Probando contra un entorno ya desplegado: {UrlBase}");
            return;
        }

        var puerto = int.TryParse(Environment.GetEnvironmentVariable("PUERTO"), out var valor) ? valor : 4173;
        UrlBase = $"http://127.0.0.1:{puerto}";

        var carpeta = UbicarLaPublicacion();
        var (ejecutable, argumentos) = ResolverElArranque(carpeta);

        var arranque = new ProcessStartInfo(ejecutable)
        {
            // ASP.NET Core toma el directorio actual como raíz de contenido: si se lo lanza desde
            // otro lado no encuentra `wwwroot` y los recursos estáticos salen vacíos.
            WorkingDirectory = carpeta,
            UseShellExecute = false
        };
        foreach (var argumento in argumentos) arranque.ArgumentList.Add(argumento);
        arranque.Environment["ASPNETCORE_URLS"] = UrlBase;
        arranque.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        arranque.Environment["ConnectionStrings__BaseDeDatos"] =
            $"Data Source={RutaDeLaBaseDeDatos()};Default Timeout=30";
        arranque.Environment["Logging__LogLevel__Default"] = "Warning";

        _proceso = Process.Start(arranque)
            ?? throw new InvalidOperationException($"No se pudo iniciar {ejecutable}.");

        await EsperarAQueEscucheAsync(puerto);
    }

    [OneTimeTearDown]
    public void Bajar()
    {
        if (_proceso is null || _proceso.HasExited) return;
        _proceso.Kill(entireProcessTree: true);
        _proceso.WaitForExit(10_000);
        _proceso.Dispose();
    }

    private static string RutaDeLaBaseDeDatos() =>
        Environment.GetEnvironmentVariable("BASE_DE_DATOS")
        ?? Path.Combine(UbicarLaRaizDelRepositorio(), "datos-e2e", "movilidad.db");

    /// <summary>
    /// Las pruebas ejercitan el binario publicado, no `dotnet run`: es el mismo artefacto que
    /// se despliega. `scripts/publicar.sh` lo deja en `publicacion/` en la raíz del repositorio.
    /// </summary>
    private static string UbicarLaPublicacion()
    {
        var indicada = Environment.GetEnvironmentVariable("CARPETA_APLICACION");
        return string.IsNullOrWhiteSpace(indicada)
            ? Path.Combine(UbicarLaRaizDelRepositorio(), "publicacion")
            : Path.GetFullPath(indicada);
    }

    /// <summary>
    /// Decide con qué se lanza la aplicación publicada.
    ///
    /// Una publicación autocontenida —la que usa CI— trae el apphost nativo y se ejecuta directo.
    /// Una publicación dependiente del framework —la más cómoda en la máquina de quien desarrolla,
    /// porque no hay que elegir un identificador de plataforma— puede no traerlo, y entonces se
    /// arranca con `dotnet <ensamblado>.dll`. Se admiten las dos para que la misma prueba corra en
    /// Windows con Visual Studio y en Linux dentro de un contenedor.
    /// </summary>
    private static (string Ejecutable, string[] Argumentos) ResolverElArranque(string carpeta)
    {
        var apphost = Path.Combine(carpeta, NombreDelApphost);
        if (File.Exists(apphost)) return (apphost, []);

        var ensamblado = Path.Combine(carpeta, NombreDelEnsamblado + ".dll");
        if (File.Exists(ensamblado)) return ("dotnet", [ensamblado]);

        throw new FileNotFoundException(
            $"No se encontró la aplicación publicada en «{carpeta}»: falta {NombreDelApphost} " +
            $"y también {NombreDelEnsamblado}.dll.{Environment.NewLine}" +
            $"Publicala antes de correr las pruebas:{Environment.NewLine}" +
            $"    {ComandoDePublicacionSugerido()}");
    }

    private static string ComandoDePublicacionSugerido() => OperatingSystem.IsWindows()
        ? @"dotnet publish src\MovilidadUrbana.Web -c Release -o publicacion"
        : "scripts/publicar.sh   (o: dotnet publish src/MovilidadUrbana.Web -c Release -o publicacion)";

    private static string UbicarLaRaizDelRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            if (directorio.GetFiles("*.sln").Length > 0) return directorio.FullName;
            directorio = directorio.Parent;
        }

        throw new DirectoryNotFoundException(
            $"No se encontró la raíz del repositorio desde «{AppContext.BaseDirectory}».");
    }

    /// <summary>Espera a que Kestrel acepte conexiones. Crear el archivo SQLite lleva unos segundos.</summary>
    private static async Task EsperarAQueEscucheAsync(int puerto)
    {
        var limite = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < limite)
        {
            if (_proceso?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"La aplicación terminó sola con código {_proceso.ExitCode} antes de escuchar.");
            }

            try
            {
                using var cliente = new TcpClient();
                await cliente.ConnectAsync("127.0.0.1", puerto);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(500);
            }
        }

        throw new TimeoutException($"La aplicación no respondió en 127.0.0.1:{puerto} en 90 segundos.");
    }
}
