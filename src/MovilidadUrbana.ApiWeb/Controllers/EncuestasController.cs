using Microsoft.AspNetCore.Mvc;
using MovilidadUrbana.ApiWeb.Application.Encuestas;
using MovilidadUrbana.ApiWeb.Contracts;
using MovilidadUrbana.ApiWeb.Domain.Rules;

namespace MovilidadUrbana.ApiWeb.Controllers;

/// <summary>
/// La encuesta de transporte. La web la carga en tres pasos; la API la recibe entera, pero deja
/// validar un paso por separado para que un cliente pueda reproducir el asistente.
/// </summary>
[ApiController]
[Route("api/v1/encuestas")]
[Produces("application/json")]
public sealed class EncuestasController(EncuestaService service) : ControllerBase
{
    /// <summary>Cuántas encuestas registró esta sesión.</summary>
    [HttpGet("contador")]
    [ProducesResponseType<ContadorDeEncuestasDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContadorDeEncuestasDto>> GetCount(CancellationToken cancellationToken) =>
        Ok(new ContadorDeEncuestasDto(await service.CountAsync(cancellationToken)));

    /// <summary>Valida un paso —1, 2 o 3— sin registrar nada.</summary>
    [HttpPost("pasos/{paso:int}/validacion")]
    [ProducesResponseType<ValidacionDePasoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ValidacionDePasoDto> ValidarPaso(int paso, EncuestaRequest request)
    {
        if (paso < 1 || paso > EncuestaRules.TotalDePasos) return NotFound();

        var errors = service.ValidarPaso(paso, ToModel(request));
        return Ok(new ValidacionDePasoDto(paso, errors.Count == 0, errors));
    }

    /// <summary>Registra una encuesta completa. Valida los tres pasos antes de guardar.</summary>
    [HttpPost]
    [ProducesResponseType<EncuestaRegistradaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncuestaRegistradaDto>> Registrar(EncuestaRequest request, CancellationToken cancellationToken)
    {
        var model = ToModel(request);

        var errors = Enumerable.Range(1, EncuestaRules.TotalDePasos)
            .SelectMany(paso => service.ValidarPaso(paso, model))
            .ToDictionary(e => e.Key, e => e.Value);
        if (errors.Count > 0) return BadRequest(ValidationProblems.De(errors));

        var respuesta = await service.RegistrarAsync(model, cancellationToken);
        var dto = new EncuestaRegistradaDto(
            respuesta.Id,
            respuesta.RegistradaEn,
            ResumenDeEncuesta.De(respuesta).Select(c => new CampoDeResumenDto(c.Clave, c.Etiqueta, c.Valor)).ToList());

        return Created($"/api/v1/encuestas/{respuesta.Id}", dto);
    }

    private static EncuestaModel ToModel(EncuestaRequest s)
    {
        var model = new EncuestaModel
        {
            Nombre = s.Nombre ?? string.Empty,
            Edad = s.Edad,
            Localidad = s.Localidad ?? string.Empty,
            Frecuencia = s.Frecuencia ?? string.Empty,
            Distancia = s.Distancia,
            Minutos = s.Minutos,
            Motivo = s.Motivo ?? string.Empty
        };
        foreach (var medio in s.Medios ?? []) model.AlternarMedio(medio, elegido: true);
        return model;
    }
}
