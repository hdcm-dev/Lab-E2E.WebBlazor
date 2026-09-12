using MovilidadUrbana.Dominio.Entidades;

namespace MovilidadUrbana.Aplicacion.Abstracciones;

public interface IRepositorioDeEncuestas
{
    Task<int> AgregarAsync(RespuestaDeEncuesta respuesta, CancellationToken cancelacion = default);

    Task<int> ContarAsync(CancellationToken cancelacion = default);
}
