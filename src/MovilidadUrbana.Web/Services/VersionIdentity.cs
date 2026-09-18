using System.Reflection;

namespace MovilidadUrbana.Web.Services;

/// <summary>
/// Resuelve la identidad de versión desde los atributos del ensamblado. Se construye una sola vez,
/// en el punto de composición, y se registra como servicio de instancia única.
/// </summary>
public sealed class VersionIdentity : IVersionIdentity
{
    private VersionIdentity(string versionLegible, bool esPreliminar, bool origenIndeterminado)
    {
        DisplayVersion = versionLegible;
        IsPrerelease = esPreliminar;
        UnknownSource = origenIndeterminado;
    }

    public string DisplayVersion { get; }

    public bool IsPrerelease { get; }

    public bool UnknownSource { get; }

    /// <summary>
    /// Deriva la identidad de la versión informativa del ensamblado. Sin metadatos de
    /// construcción —el <c>+&lt;identificador&gt;</c> que agrega la cadena de compilación— el
    /// binario no se puede atar a una construcción concreta, y eso se declara en lugar de
    /// disimularse.
    /// </summary>
    public static VersionIdentity FromAssembly(Assembly? ensamblado)
    {
        var informativa = ensamblado?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informativa))
        {
            return new VersionIdentity("versión no declarada", esPreliminar: false, origenIndeterminado: true);
        }

        var separador = informativa.IndexOf('+');
        var legible = separador < 0 ? informativa : informativa[..separador];

        return new VersionIdentity(
            versionLegible: legible,
            esPreliminar: legible.Contains('-'),
            origenIndeterminado: separador < 0);
    }
}
