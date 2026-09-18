using MovilidadUrbana.Web.Infrastructure.Sessions;
namespace MovilidadUrbana.Web.Sessions;

/// <summary>
/// Vive en la web y no en Infraestructura: es un detalle de HTTP, y dejarlo allá obligaba a esa capa a
/// depender de ASP.NET Core, lo que impedía usarla desde la aplicación Android.
///
/// Asigna a cada visitante un identificador de sesión en una cookie y lo publica en el
/// <see cref="ContextoDeSesion"/> de la petición.
///
/// Es lo que permite que las pruebas E2E se aíslen entre sí: la prueba escribe la cookie con un
/// valor propio antes de navegar y así trabaja sobre su propio conjunto de datos, aunque el
/// servidor y el archivo SQLite sean compartidos.
/// </summary>
public sealed class SessionMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext context, SessionContext session)
    {
        var id = context.Request.Cookies[SessionContext.CookieName];

        // La cookie se emite únicamente al pedir un documento. Si se emitiera también en las
        // peticiones de css y js —que el navegador lanza en paralelo— la primera visita generaría
        // varios identificadores a la vez y se quedaría con el último en llegar.
        if (!SessionContext.IsValid(id) && EsUnDocumento(context.Request))
        {
            id = session.Id;
            context.Response.Cookies.Append(
                SessionContext.CookieName,
                id,
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromDays(1)
                });
        }

        session.Set(id ?? string.Empty);

        await siguiente(context);
    }

    /// <summary>
    /// Documento = navegación a una pantalla. Se descartan los recursos estáticos —que se
    /// reconocen por la extensión— y los puntos de entrada del propio Blazor.
    /// </summary>
    private static bool EsUnDocumento(HttpRequest peticion) =>
        HttpMethods.IsGet(peticion.Method) &&
        !peticion.Path.StartsWithSegments("/_framework") &&
        !peticion.Path.StartsWithSegments("/_blazor") &&
        !Path.HasExtension(peticion.Path.Value);
}
