using MovilidadUrbana.Web.Dominio.Entidades;

namespace MovilidadUrbana.Web.Aplicacion.Abstracciones;

public interface IRepositorioDeEncuestas
{
    Task<int> AgregarAsync(RespuestaDeEncuesta respuesta, CancellationToken cancelacion = default);

    Task<int> ContarAsync(CancellationToken cancelacion = default);
}
