using MovilidadUrbana.Web.Aplicacion.Abstracciones;

namespace MovilidadUrbana.Web.Infraestructura.Sesiones;

/// <summary>
/// Implementación con alcance de ámbito: una petición HTTP, o un circuito de Blazor.
///
/// El valor lo establece <see cref="MiddlewareDeSesion"/> durante la petición y, ya en el
/// circuito interactivo, el componente raíz `Routes`, que lo recibe como parámetro desde
/// `App.razor`. El identificador provisorio del constructor evita que un ámbito sin cookie
/// termine leyendo o escribiendo en un espacio de datos compartido.
/// </summary>
public sealed class ContextoDeSesion : IContextoDeSesion
{
    public const string NombreDeCookie = "sesion-movilidad";

    public const int LargoMaximo = 64;

    public string Id { get; private set; } = Guid.NewGuid().ToString("n");

    public void Establecer(string id)
    {
        if (EsValido(id)) Id = id;
    }

    public static bool EsValido(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= LargoMaximo;
}
