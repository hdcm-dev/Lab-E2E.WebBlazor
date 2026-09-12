namespace WebBlazor.E2E.Base.HolaMundo.Servicios;

/// <summary>
/// Identidad de la construcción que está corriendo. Se resuelve una sola vez, en
/// el punto de composición del host, y es la misma cadena que ve la persona y que
/// se registra en el diagnóstico.
/// </summary>
public interface IIdentidadDeVersion
{
    /// <summary>Versión legible, la que se exhibe en el sello.</summary>
    string VersionLegible { get; }

    /// <summary>Identificador de la construcción, para cruzar un reporte con su artefacto.</summary>
    string IdentificadorDeConstruccion { get; }

    /// <summary>La construcción es preliminar y no debería tomarse por definitiva.</summary>
    bool EsPreliminar { get; }

    /// <summary>No se pudo determinar de qué construcción viene el artefacto.</summary>
    bool OrigenIndeterminado { get; }
}
