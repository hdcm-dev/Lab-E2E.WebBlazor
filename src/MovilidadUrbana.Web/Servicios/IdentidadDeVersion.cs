using System.Reflection;

namespace MovilidadUrbana.Web.Servicios;

/// <summary>
/// Resuelve la identidad de versión desde los atributos del ensamblado. Se construye una sola vez,
/// en el punto de composición, y se registra como servicio de instancia única.
/// </summary>
public sealed class IdentidadDeVersion : IIdentidadDeVersion
{
    private IdentidadDeVersion(string versionLegible, bool esPreliminar, bool origenIndeterminado)
    {
        VersionLegible = versionLegible;
        EsPreliminar = esPreliminar;
        OrigenIndeterminado = origenIndeterminado;
    }

    public string VersionLegible { get; }

    public bool EsPreliminar { get; }

    public bool OrigenIndeterminado { get; }

    /// <summary>
    /// Deriva la identidad de la versión informativa del ensamblado. Sin metadatos de
    /// construcción —el <c>+&lt;identificador&gt;</c> que agrega la cadena de compilación— el
    /// binario no se puede atar a una construcción concreta, y eso se declara en lugar de
    /// disimularse.
    /// </summary>
    public static IdentidadDeVersion DelEnsamblado(Assembly? ensamblado)
    {
        var informativa = ensamblado?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informativa))
        {
            return new IdentidadDeVersion("versión no declarada", esPreliminar: false, origenIndeterminado: true);
        }

        var separador = informativa.IndexOf('+');
        var legible = separador < 0 ? informativa : informativa[..separador];

        return new IdentidadDeVersion(
            versionLegible: legible,
            esPreliminar: legible.Contains('-'),
            origenIndeterminado: separador < 0);
    }
}
