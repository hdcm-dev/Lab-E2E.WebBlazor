using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebBlazor.E2E.Base.Login.Servicios;

namespace WebBlazor.E2E.Base.Login.Endpoints;

/// <summary>
/// Puntos de acción de la identidad. Son endpoints y no manejadores de componente
/// porque la credencial de sesión se emite en el ciclo de request: con el circuito
/// ya establecido, la respuesta HTTP ya se envió y no hay dónde escribir la cabecera
/// que crea la cookie.
/// </summary>
public static class IdentidadEndpoints
{
    /// <summary>Ruta de la superficie de acceso.</summary>
    public const string SuperficieDeAcceso = "/login";

    /// <summary>Publica el ingreso y la salida.</summary>
    public static void MapearIdentidad(this IEndpointRouteBuilder app)
    {
        app.MapPost("/identidad/ingreso", async (
            HttpContext contexto,
            [FromForm] string? identificador,
            [FromForm] string? secreto,
            [FromForm] string? returnurl,
            IServicioDeIdentidad servicio) =>
        {
            var resultado = servicio.Autenticar(identificador, secreto);

            if (!resultado.Aceptado || resultado.Principal is null)
            {
                // Tercera capa del guard: la de la acción. Resuelve devolviendo a la
                // superficie correcta con un código del catálogo, sin decir qué falló.
                return Results.Redirect($"{SuperficieDeAcceso}?estado={resultado.Codigo}");
            }

            // La cookie se emite acá, en el ciclo de request, fuera de todo circuito.
            await contexto.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, resultado.Principal);

            return Results.Redirect(DestinoSeguro(returnurl));
        });

        app.MapPost("/identidad/salida", async (HttpContext contexto) =>
        {
            await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect($"{SuperficieDeAcceso}?estado={CatalogoDeResultados.SesionCerrada}");
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
