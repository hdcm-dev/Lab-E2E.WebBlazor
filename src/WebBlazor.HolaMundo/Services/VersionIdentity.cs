using System.Reflection;

namespace WebBlazor.HolaMundo.Services;

/// <summary>
/// Resuelve la identidad de versión del ensamblado de entrada. Se registra como
/// <c>Singleton</c> en <c>Program.cs</c>: el valor no cambia mientras el proceso vive.
/// </summary>
public sealed class VersionIdentity : IVersionIdentity
{
    private const string SinDeterminar = "0.0.0";

    /// <summary>Lee la versión informacional del ensamblado y la descompone.</summary>
    public VersionIdentity()
    {
        var informacional = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        UnknownSource = string.IsNullOrWhiteSpace(informacional);

        var crudo = informacional ?? SinDeterminar;
        var separador = crudo.IndexOf('+');

        DisplayVersion = separador >= 0 ? crudo[..separador] : crudo;

        // El identificador de construcción es el metadato que el proceso de compilación
        // agrega; sin él, el artefacto no se puede cruzar con su corrida.
        BuildId = separador >= 0 ? crudo[(separador + 1)..] : string.Empty;

        if (BuildId.Length == 0)
        {
            UnknownSource = true;
        }

        // Una versión con etiqueta de precalificación (`-alpha`, `-rc.1`) es preliminar.
        IsPrerelease = DisplayVersion.Contains('-');
    }

    /// <inheritdoc />
    public string DisplayVersion { get; }

    /// <inheritdoc />
    public string BuildId { get; }

    /// <inheritdoc />
    public bool IsPrerelease { get; }

    /// <inheritdoc />
    public bool UnknownSource { get; }
}
