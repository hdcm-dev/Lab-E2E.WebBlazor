using MovilidadUrbana.ApiWeb.Aplicacion.Abstracciones;

namespace MovilidadUrbana.ApiWeb.Infraestructura.Sesiones;

/// <summary>
/// Implementación con alcance de ámbito: una petición HTTP.
///
/// El valor lo establece el middleware de sesión por encabezado (`MiddlewareDeSesionPorEncabezado`) en cada petición. El identificador provisorio del constructor evita que un ámbito sin
/// sesión termine leyendo o escribiendo en un espacio de datos compartido.
/// </summary>
public sealed class ContextoDeSesion : IContextoDeSesion
{
    public const int LargoMaximo = 64;

    public string Id { get; private set; } = Guid.NewGuid().ToString("n");

    public void Establecer(string id)
    {
        if (EsValido(id)) Id = id;
    }

    public static bool EsValido(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= LargoMaximo;
}
