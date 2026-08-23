using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Dominio.Entidades;

namespace MovilidadUrbana.Web.Infraestructura.Persistencia;

public sealed class RepositorioDeEncuestas(
    IDbContextFactory<ContextoDeDatos> fabrica,
    IContextoDeSesion sesion) : IRepositorioDeEncuestas
{
    public async Task<int> AgregarAsync(RespuestaDeEncuesta respuesta, CancellationToken cancelacion = default)
    {
        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        respuesta.SesionId = sesion.Id;
        contexto.Encuestas.Add(respuesta);
        await contexto.SaveChangesAsync(cancelacion);
        return respuesta.Id;
    }

    public async Task<int> ContarAsync(CancellationToken cancelacion = default)
    {
        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        return await contexto.Encuestas.CountAsync(e => e.SesionId == sesion.Id, cancelacion);
    }
}
