using MovilidadUrbana.ApiWeb.Dominio.Entidades;

namespace MovilidadUrbana.ApiWeb.Aplicacion.Abstracciones;

public interface IRepositorioDeEncuestas
{
    Task<int> AgregarAsync(RespuestaDeEncuesta respuesta, CancellationToken cancelacion = default);

    Task<int> ContarAsync(CancellationToken cancelacion = default);
}
