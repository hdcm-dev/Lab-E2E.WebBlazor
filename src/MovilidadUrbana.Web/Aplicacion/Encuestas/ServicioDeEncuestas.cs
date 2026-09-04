using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Dominio.Entidades;
using MovilidadUrbana.Web.Dominio.Reglas;

namespace MovilidadUrbana.Web.Aplicacion.Encuestas;

/// <summary>
/// Casos de uso de la encuesta. La validación es por paso: el asistente no deja avanzar
/// mientras el paso actual tenga errores.
/// </summary>
public sealed class ServicioDeEncuestas(IRepositorioDeEncuestas repositorio)
{
    public Task<int> ContarAsync(CancellationToken cancelacion = default) =>
        repositorio.ContarAsync(cancelacion);

    public IReadOnlyDictionary<string, string> ValidarPaso(int paso, ModeloDeEncuesta modelo)
    {
        var errores = new Dictionary<string, string>();

        switch (paso)
        {
            case 1:
                if (!ReglasDeEncuesta.NombreValido(modelo.Nombre))
                {
                    errores["nombre"] =
                        $"Ingrese nombre y apellido (mínimo {ReglasDeEncuesta.LargoMinimoDelNombre} caracteres).";
                }
                if (!ReglasDeEncuesta.EdadValida(modelo.Edad))
                {
                    errores["edad"] = $"La edad debe estar entre {ReglasDeEncuesta.EdadMinima} y {ReglasDeEncuesta.EdadMaxima} años.";
                }
                if (string.IsNullOrWhiteSpace(modelo.Localidad))
                {
                    errores["localidad"] = "Seleccione una localidad.";
                }
                break;

            case 2:
                if (modelo.Medios.Count == 0)
                {
                    errores["medios"] = "Seleccione al menos un medio de transporte.";
                }
                if (string.IsNullOrWhiteSpace(modelo.Frecuencia))
                {
                    errores["frecuencia"] = "Seleccione la frecuencia de uso.";
                }
                break;

            case 3:
                if (!ReglasDeEncuesta.DistanciaValida(modelo.Distancia))
                {
                    errores["distancia"] = $"Ingrese una distancia entre {ReglasDeEncuesta.DistanciaMinima:0} y {ReglasDeEncuesta.DistanciaMaxima:0} km.";
                }
                if (!ReglasDeEncuesta.MinutosValidos(modelo.Minutos))
                {
                    errores["minutos"] = $"Ingrese un tiempo entre {ReglasDeEncuesta.MinutosMinimos} y {ReglasDeEncuesta.MinutosMaximos} minutos.";
                }
                if (string.IsNullOrWhiteSpace(modelo.Motivo))
                {
                    errores["motivo"] = "Seleccione el motivo principal del viaje.";
                }
                break;
        }

        return errores;
    }

    /// <summary>Registra la encuesta y devuelve la respuesta ya persistida.</summary>
    public async Task<RespuestaDeEncuesta> RegistrarAsync(ModeloDeEncuesta modelo, CancellationToken cancelacion = default)
    {
        var respuesta = new RespuestaDeEncuesta
        {
            Nombre = modelo.Nombre.Trim(),
            Edad = modelo.Edad!.Value,
            Localidad = modelo.Localidad,
            // Se guarda en el orden del catálogo y no en el de tipeo, para que el resumen sea estable.
            Medios = [.. Dominio.Catalogos.Medios.Where(m => modelo.Medios.Contains(m.Clave)).Select(m => m.Clave)],
            Frecuencia = modelo.Frecuencia,
            Distancia = modelo.Distancia!.Value,
            Minutos = modelo.Minutos!.Value,
            Motivo = modelo.Motivo,
            RegistradaEn = DateTimeOffset.UtcNow
        };

        await repositorio.AgregarAsync(respuesta, cancelacion);
        return respuesta;
    }
}
