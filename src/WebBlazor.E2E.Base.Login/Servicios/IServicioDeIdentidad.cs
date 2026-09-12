using System.Security.Claims;

namespace WebBlazor.E2E.Base.Login.Servicios;

/// <summary>
/// Resuelve si un intento de ingreso se acepta. La validación que decide vive acá,
/// no en la superficie: la de la superficie es de conveniencia.
/// </summary>
public interface IServicioDeIdentidad
{
    /// <summary>Evalúa las credenciales y devuelve el resultado con su código.</summary>
    ResultadoDeAutenticacion Autenticar(string? identificador, string? secreto);
}

/// <summary>
/// Resultado de un intento de ingreso.
/// </summary>
/// <param name="Aceptado">El intento fue aceptado.</param>
/// <param name="Codigo">Código del catálogo con el que se le informa el desenlace a la persona.</param>
/// <param name="Principal">Identidad emitida cuando el intento se acepta.</param>
public sealed record ResultadoDeAutenticacion(bool Aceptado, string Codigo, ClaimsPrincipal? Principal = null);
