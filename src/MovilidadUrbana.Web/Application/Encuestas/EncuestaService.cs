using MovilidadUrbana.Web.Domain;
using MovilidadUrbana.Web.Application.Abstractions;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Domain.Rules;

namespace MovilidadUrbana.Web.Application.Encuestas;

/// <summary>
/// Casos de uso de la encuesta. La validación es por paso: el asistente no deja avanzar
/// mientras el paso actual tenga errores.
/// </summary>
public sealed class EncuestaService(IEncuestaRepository repository)
{
    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        repository.CountAsync(cancellationToken);

    public IReadOnlyDictionary<string, string> ValidarPaso(int paso, EncuestaModel model)
    {
        var errors = new Dictionary<string, string>();

        switch (paso)
        {
            case 1:
                if (!EncuestaRules.NombreValido(model.Nombre))
                {
                    errors["nombre"] =
                        $"Ingrese nombre y apellido (mínimo {EncuestaRules.LargoMinimoDelNombre} caracteres).";
                }
                if (!EncuestaRules.EdadValida(model.Edad))
                {
                    errors["edad"] = $"La edad debe estar entre {EncuestaRules.EdadMinima} y {EncuestaRules.EdadMaxima} años.";
                }
                if (string.IsNullOrWhiteSpace(model.Localidad))
                {
                    errors["localidad"] = "Seleccione una localidad.";
                }
                break;

            case 2:
                if (model.Medios.Count == 0)
                {
                    errors["medios"] = "Seleccione al menos un medio de transporte.";
                }
                if (string.IsNullOrWhiteSpace(model.Frecuencia))
                {
                    errors["frecuencia"] = "Seleccione la frecuencia de uso.";
                }
                break;

            case 3:
                if (!EncuestaRules.DistanciaValida(model.Distancia))
                {
                    errors["distancia"] = $"Ingrese una distancia entre {EncuestaRules.DistanciaMinima:0} y {EncuestaRules.DistanciaMaxima:0} km.";
                }
                if (!EncuestaRules.MinutosValidos(model.Minutos))
                {
                    errors["minutos"] = $"Ingrese un tiempo entre {EncuestaRules.MinutosMinimos} y {EncuestaRules.MinutosMaximos} minutos.";
                }
                if (string.IsNullOrWhiteSpace(model.Motivo))
                {
                    errors["motivo"] = "Seleccione el motivo principal del viaje.";
                }
                break;
        }

        return errors;
    }

    /// <summary>Registra la encuesta y devuelve la respuesta ya persistida.</summary>
    public async Task<RespuestaDeEncuesta> RegistrarAsync(EncuestaModel model, CancellationToken cancellationToken = default)
    {
        var respuesta = new RespuestaDeEncuesta
        {
            Nombre = model.Nombre.Trim(),
            Edad = model.Edad!.Value,
            Localidad = model.Localidad,
            // Se guarda en el orden del catálogo y no en el de tipeo, para que el resumen sea estable.
            Medios = [.. Catalogos.Medios.Where(m => model.Medios.Contains(m.Clave)).Select(m => m.Clave)],
            Frecuencia = model.Frecuencia,
            Distancia = model.Distancia!.Value,
            Minutos = model.Minutos!.Value,
            Motivo = model.Motivo,
            RegistradaEn = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(respuesta, cancellationToken);
        return respuesta;
    }
}
