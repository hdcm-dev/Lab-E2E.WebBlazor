namespace MovilidadUrbana.Web.Infraestructura.Sesiones;

/// <summary>
/// Asigna a cada visitante un identificador de sesión en una cookie y lo publica en el
/// <see cref="ContextoDeSesion"/> de la petición.
///
/// Es lo que permite que las pruebas E2E se aíslen entre sí: la prueba escribe la cookie con un
/// valor propio antes de navegar y así trabaja sobre su propio conjunto de datos, aunque el
/// servidor y el archivo SQLite sean compartidos.
/// </summary>
public sealed class MiddlewareDeSesion(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto, ContextoDeSesion sesion)
    {
        var id = contexto.Request.Cookies[ContextoDeSesion.NombreDeCookie];

        // La cookie se emite únicamente al pedir un documento. Si se emitiera también en las
        // peticiones de css y js —que el navegador lanza en paralelo— la primera visita generaría
        // varios identificadores a la vez y se quedaría con el último en llegar.
        if (!ContextoDeSesion.EsValido(id) && EsUnDocumento(contexto.Request))
        {
            id = sesion.Id;
            contexto.Response.Cookies.Append(
                ContextoDeSesion.NombreDeCookie,
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

        sesion.Establecer(id ?? string.Empty);

        await siguiente(contexto);
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
