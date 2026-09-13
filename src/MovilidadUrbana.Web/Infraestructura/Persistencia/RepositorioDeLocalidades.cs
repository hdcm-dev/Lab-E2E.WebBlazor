using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Dominio.Entidades;

namespace MovilidadUrbana.Web.Infraestructura.Persistencia;

/// <summary>
/// Acceso a localidades acotado a la sesión actual: ninguna consulta sale del espacio de datos
/// del visitante, ni siquiera si le llega el identificador de otra sesión.
///
/// Usa <see cref="IDbContextFactory{TContext}"/> y abre un contexto por operación, que es lo
/// recomendado en Blazor Server: un `DbContext` con alcance de ámbito viviría lo que dura el
/// circuito —minutos u horas— y no está pensado para eso.
/// </summary>
public sealed class RepositorioDeLocalidades(
    IDbContextFactory<ContextoDeDatos> fabrica,
    IContextoDeSesion sesion,
    SembradorDeSesion sembrador) : IRepositorioDeLocalidades
{
    public async Task<IReadOnlyList<Localidad>> ListarAsync(CancellationToken cancelacion = default)
    {
        await sembrador.AsegurarAsync(cancelacion);
        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        return await contexto.Localidades
            .AsNoTracking()
            .Where(l => l.SesionId == sesion.Id)
            .OrderBy(l => l.Id)
            .ToListAsync(cancelacion);
    }

    public async Task<Localidad?> ObtenerAsync(int id, CancellationToken cancelacion = default)
    {
        await sembrador.AsegurarAsync(cancelacion);
        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        return await contexto.Localidades
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.SesionId == sesion.Id, cancelacion);
    }

    public async Task<int> AgregarAsync(Localidad localidad, CancellationToken cancelacion = default)
    {
        await sembrador.AsegurarAsync(cancelacion);
        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        localidad.SesionId = sesion.Id;
        contexto.Localidades.Add(localidad);
        await contexto.SaveChangesAsync(cancelacion);
        return localidad.Id;
    }

    public async Task ActualizarAsync(Localidad localidad, CancellationToken cancelacion = default)
    {
        // La entidad llegó desde `ObtenerAsync`, que ya filtró por sesión; la comprobación deja
        // igual la garantía escrita en el código y no en la memoria de quien lo lea.
        if (localidad.SesionId != sesion.Id) return;

        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        contexto.Localidades.Update(localidad);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarAsync(int id, CancellationToken cancelacion = default)
    {
        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);
        await contexto.Localidades
            .Where(l => l.Id == id && l.SesionId == sesion.Id)
            .ExecuteDeleteAsync(cancelacion);
    }
}
