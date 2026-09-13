using MovilidadUrbana.MAUI.Aplicacion.Abstracciones;

namespace MovilidadUrbana.MAUI.Infraestructura.Sesiones;

/// <summary>
/// Implementación con alcance de ámbito.
///
/// El valor lo establece `SesionDelDispositivo` al arrancar: el teléfono entero es una sola sesión, guardada en las preferencias. El identificador provisorio del constructor evita que un ámbito sin
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
