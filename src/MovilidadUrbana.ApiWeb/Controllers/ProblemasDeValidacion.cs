using Microsoft.AspNetCore.Mvc;

namespace MovilidadUrbana.ApiWeb.Controllers;

/// <summary>
/// Traduce los errores por campo de la capa de aplicación al <see cref="ValidationProblemDetails"/>
/// estándar (RFC 9457), sin que la aplicación sepa que existe HTTP.
/// </summary>
internal static class ProblemasDeValidacion
{
    public static ValidationProblemDetails De(IReadOnlyDictionary<string, string> errores, string? titulo = null)
    {
        var detalles = new ValidationProblemDetails(errores.ToDictionary(e => e.Key, e => new[] { e.Value }))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = titulo ?? "La solicitud tiene datos inválidos."
        };
        return detalles;
    }
}
