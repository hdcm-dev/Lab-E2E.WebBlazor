using MovilidadUrbana.Web.Dominio.Entidades;

namespace MovilidadUrbana.Web.Aplicacion.Abstracciones;

public interface IRepositorioDeLocalidades
{
    Task<IReadOnlyList<Localidad>> ListarAsync(CancellationToken cancelacion = default);

    Task<Localidad?> ObtenerAsync(int id, CancellationToken cancelacion = default);

    Task<int> AgregarAsync(Localidad localidad, CancellationToken cancelacion = default);

    Task ActualizarAsync(Localidad localidad, CancellationToken cancelacion = default);

    Task EliminarAsync(int id, CancellationToken cancelacion = default);
}
