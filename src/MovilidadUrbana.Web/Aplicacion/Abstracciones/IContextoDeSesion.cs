namespace MovilidadUrbana.Web.Aplicacion.Abstracciones;

/// <summary>
/// Identifica el espacio de datos del visitante actual. La implementación vive en
/// Infraestructura porque depende de cookies y de HTTP; la aplicación solo necesita el valor.
/// </summary>
public interface IContextoDeSesion
{
    string Id { get; }
}
