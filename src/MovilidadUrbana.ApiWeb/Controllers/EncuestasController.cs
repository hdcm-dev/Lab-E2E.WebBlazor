using Microsoft.AspNetCore.Mvc;
using MovilidadUrbana.ApiWeb.Aplicacion.Encuestas;
using MovilidadUrbana.ApiWeb.Contratos;
using MovilidadUrbana.ApiWeb.Dominio.Reglas;

namespace MovilidadUrbana.ApiWeb.Controllers;

/// <summary>
/// La encuesta de transporte. La web la carga en tres pasos; la API la recibe entera, pero deja
/// validar un paso por separado para que un cliente pueda reproducir el asistente.
/// </summary>
[ApiController]
[Route("api/v1/encuestas")]
[Produces("application/json")]
public sealed class EncuestasController(ServicioDeEncuestas servicio) : ControllerBase
{
    /// <summary>Cuántas encuestas registró esta sesión.</summary>
    [HttpGet("contador")]
    [ProducesResponseType<ContadorDeEncuestasDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContadorDeEncuestasDto>> Contar(CancellationToken cancelacion) =>
        Ok(new ContadorDeEncuestasDto(await servicio.ContarAsync(cancelacion)));

    /// <summary>Valida un paso —1, 2 o 3— sin registrar nada.</summary>
    [HttpPost("pasos/{paso:int}/validacion")]
    [ProducesResponseType<ValidacionDePasoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ValidacionDePasoDto> ValidarPaso(int paso, SolicitudDeEncuesta solicitud)
    {
        if (paso < 1 || paso > ReglasDeEncuesta.TotalDePasos) return NotFound();

        var errores = servicio.ValidarPaso(paso, AModelo(solicitud));
        return Ok(new ValidacionDePasoDto(paso, errores.Count == 0, errores));
    }

    /// <summary>Registra una encuesta completa. Valida los tres pasos antes de guardar.</summary>
    [HttpPost]
    [ProducesResponseType<EncuestaRegistradaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncuestaRegistradaDto>> Registrar(SolicitudDeEncuesta solicitud, CancellationToken cancelacion)
    {
        var modelo = AModelo(solicitud);

        var errores = Enumerable.Range(1, ReglasDeEncuesta.TotalDePasos)
            .SelectMany(paso => servicio.ValidarPaso(paso, modelo))
            .ToDictionary(e => e.Key, e => e.Value);
        if (errores.Count > 0) return BadRequest(ProblemasDeValidacion.De(errores));

        var respuesta = await servicio.RegistrarAsync(modelo, cancelacion);
        var dto = new EncuestaRegistradaDto(
            respuesta.Id,
            respuesta.RegistradaEn,
            ResumenDeEncuesta.De(respuesta).Select(c => new CampoDeResumenDto(c.Clave, c.Etiqueta, c.Valor)).ToList());

        return Created($"/api/v1/encuestas/{respuesta.Id}", dto);
    }

    private static ModeloDeEncuesta AModelo(SolicitudDeEncuesta s)
    {
        var modelo = new ModeloDeEncuesta
        {
            Nombre = s.Nombre ?? string.Empty,
            Edad = s.Edad,
            Localidad = s.Localidad ?? string.Empty,
            Frecuencia = s.Frecuencia ?? string.Empty,
            Distancia = s.Distancia,
            Minutos = s.Minutos,
            Motivo = s.Motivo ?? string.Empty
        };
        foreach (var medio in s.Medios ?? []) modelo.AlternarMedio(medio, elegido: true);
        return modelo;
    }
}
