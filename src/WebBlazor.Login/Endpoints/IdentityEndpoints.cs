using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebBlazor.Login.Services;

namespace WebBlazor.Login.Endpoints;

/// <summary>
/// Puntos de acción de la identidad. Son endpoints y no manejadores de componente
/// porque la credencial de sesión se emite en el ciclo de request: con el circuito
/// ya establecido, la respuesta HTTP ya se envió y no hay dónde escribir la cabecera
/// que crea la cookie.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>Ruta de la superficie de acceso.</summary>
    public const string SuperficieDeAcceso = "/login";

    /// <summary>Publica el ingreso y la salida.</summary>
    public static void MapearIdentidad(this IEndpointRouteBuilder app)
    {
        app.MapPost("/identidad/ingreso", async (
            HttpContext context,
            [FromForm] string? identificador,
            [FromForm] string? secreto,
            [FromForm] string? returnurl,
            IIdentityService service) =>
        {
            var result = service.Authenticate(identificador, secreto);

            if (!result.Aceptado || result.Principal is null)
            {
                // Tercera capa del guard: la de la acción. Resuelve devolviendo a la
                // superficie correcta con un código del catálogo, sin decir qué falló.
                return Results.Redirect($"{SuperficieDeAcceso}?estado={result.Code}");
            }

            // La cookie se emite acá, en el ciclo de request, fuera de todo circuito.
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal);

            return Results.Redirect(DestinoSeguro(returnurl));
        });

        app.MapPost("/identidad/salida", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect($"{SuperficieDeAcceso}?estado={SignInResults.SesionCerrada}");
        });
    }

    /// <summary>
    /// Sólo se admiten rutas locales: un destino externo convertiría el ingreso en
    /// una redirección abierta.
    /// </summary>
    private static string DestinoSeguro(string? returnurl) =>
        !string.IsNullOrEmpty(returnurl)
        && Uri.IsWellFormedUriString(returnurl, UriKind.Relative)
        && returnurl.StartsWith('/')
        && !returnurl.StartsWith("//")
            ? returnurl
            : "/";
}
