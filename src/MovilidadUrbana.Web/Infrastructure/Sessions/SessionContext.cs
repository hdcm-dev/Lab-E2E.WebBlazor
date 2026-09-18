using MovilidadUrbana.Web.Application.Abstractions;

namespace MovilidadUrbana.Web.Infrastructure.Sessions;

/// <summary>
/// Implementación con alcance de ámbito: una petición HTTP, o un circuito de Blazor.
///
/// El valor lo establece <see cref="MiddlewareDeSesion"/> durante la petición y, ya en el
/// circuito interactivo, el componente raíz `Routes`, que lo recibe como parámetro desde
/// `App.razor`. El identificador provisorio del constructor evita que un ámbito sin cookie
/// termine leyendo o escribiendo en un espacio de datos compartido.
/// </summary>
public sealed class SessionContext : ISessionContext
{
    public const string CookieName = "sesion-movilidad";

    public const int MaxLength = 64;

    public string Id { get; private set; } = Guid.NewGuid().ToString("n");

    public void Set(string id)
    {
        if (IsValid(id)) Id = id;
    }

    public static bool IsValid(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= MaxLength;
}
