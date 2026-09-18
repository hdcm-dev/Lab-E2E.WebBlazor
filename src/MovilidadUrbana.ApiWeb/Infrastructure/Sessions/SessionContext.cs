using MovilidadUrbana.ApiWeb.Application.Abstractions;

namespace MovilidadUrbana.ApiWeb.Infrastructure.Sessions;

/// <summary>
/// Implementación con alcance de ámbito: una petición HTTP.
///
/// El valor lo establece el middleware de sesión por encabezado (`MiddlewareDeSesionPorEncabezado`) en cada petición. El identificador provisorio del constructor evita que un ámbito sin
/// sesión termine leyendo o escribiendo en un espacio de datos compartido.
/// </summary>
public sealed class SessionContext : ISessionContext
{
    public const int MaxLength = 64;

    public string Id { get; private set; } = Guid.NewGuid().ToString("n");

    public void Set(string id)
    {
        if (IsValid(id)) Id = id;
    }

    public static bool IsValid(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= MaxLength;
}
