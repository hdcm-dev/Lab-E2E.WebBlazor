using MovilidadUrbana.Infraestructura.Sesiones;

namespace MovilidadUrbana.ApiWeb.Sesiones;

/// <summary>
/// Identifica el espacio de datos del cliente por el encabezado <c>X-Sesion-Id</c>, que es la forma
/// idiomática en una API —la web lo hace por cookie—. Si el cliente no lo manda, la petición recibe
/// un identificador nuevo y lo devuelve en el mismo encabezado, para que lo repita en las siguientes.
/// </summary>
public sealed class MiddlewareDeSesionPorEncabezado(RequestDelegate siguiente)
{
    public const string Encabezado = "X-Sesion-Id";

    public async Task InvokeAsync(HttpContext contexto, ContextoDeSesion sesion)
    {
        var id = contexto.Request.Headers[Encabezado].ToString();
        if (ContextoDeSesion.EsValido(id)) sesion.Establecer(id);

        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[Encabezado] = sesion.Id;
            return Task.CompletedTask;
        });

        await siguiente(contexto);
    }
}
