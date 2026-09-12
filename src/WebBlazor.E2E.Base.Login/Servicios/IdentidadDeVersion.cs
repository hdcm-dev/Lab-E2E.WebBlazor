using System.Reflection;

namespace WebBlazor.E2E.Base.Login.Servicios;

/// <summary>
/// Resuelve la identidad de versión del ensamblado de entrada. Se registra como
/// <c>Singleton</c> en <c>Program.cs</c>: el valor no cambia mientras el proceso vive.
/// </summary>
public sealed class IdentidadDeVersion : IIdentidadDeVersion
{
    private const string SinDeterminar = "0.0.0";

    /// <summary>Lee la versión informacional del ensamblado y la descompone.</summary>
    public IdentidadDeVersion()
    {
        var informacional = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        OrigenIndeterminado = string.IsNullOrWhiteSpace(informacional);

        var crudo = informacional ?? SinDeterminar;
        var separador = crudo.IndexOf('+');

        VersionLegible = separador >= 0 ? crudo[..separador] : crudo;

        // El identificador de construcción es el metadato que el proceso de compilación
        // agrega; sin él, el artefacto no se puede cruzar con su corrida.
        IdentificadorDeConstruccion = separador >= 0 ? crudo[(separador + 1)..] : string.Empty;

        if (IdentificadorDeConstruccion.Length == 0)
        {
            OrigenIndeterminado = true;
        }

        // Una versión con etiqueta de precalificación (`-alpha`, `-rc.1`) es preliminar.
        EsPreliminar = VersionLegible.Contains('-');
    }

    /// <inheritdoc />
    public string VersionLegible { get; }

    /// <inheritdoc />
    public string IdentificadorDeConstruccion { get; }

    /// <inheritdoc />
    public bool EsPreliminar { get; }

    /// <inheritdoc />
    public bool OrigenIndeterminado { get; }
}
