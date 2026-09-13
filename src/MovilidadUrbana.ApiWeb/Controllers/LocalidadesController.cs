using Microsoft.AspNetCore.Mvc;
using MovilidadUrbana.ApiWeb.Aplicacion.Abstracciones;
using MovilidadUrbana.ApiWeb.Aplicacion.Localidades;
using MovilidadUrbana.ApiWeb.Contratos;
using MovilidadUrbana.ApiWeb.Dominio.Entidades;

namespace MovilidadUrbana.ApiWeb.Controllers;

/// <summary>ABM de localidades. Cada sesión —encabezado <c>X-Sesion-Id</c>— ve solo las suyas.</summary>
[ApiController]
[Route("api/v1/localidades")]
[Produces("application/json")]
public sealed class LocalidadesController(
    ServicioDeLocalidades servicio,
    IRepositorioDeLocalidades repositorio) : ControllerBase
{
    /// <summary>Lista las localidades de la sesión.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LocalidadDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocalidadDto>>> Listar(CancellationToken cancelacion)
    {
        var localidades = await servicio.ListarAsync(cancelacion);
        return Ok(localidades.Select(ADto).ToList());
    }

    /// <summary>Devuelve una localidad por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<LocalidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocalidadDto>> Obtener(int id, CancellationToken cancelacion)
    {
        var localidad = await repositorio.ObtenerAsync(id, cancelacion);
        return localidad is null ? NotFound() : Ok(ADto(localidad));
    }

    /// <summary>Da de alta una localidad.</summary>
    [HttpPost]
    [ProducesResponseType<LocalidadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocalidadDto>> Crear(SolicitudDeLocalidad solicitud, CancellationToken cancelacion)
    {
        var resultado = await servicio.GuardarAsync(AModelo(solicitud), cancelacion);
        if (!resultado.EsCorrecto) return BadRequest(ProblemasDeValidacion.De(resultado.Errores));

        // El servicio no devuelve la entidad creada: se la ubica por nombre y provincia, que el
        // propio servicio garantiza únicos dentro de la sesión.
        var creada = (await repositorio.ListarAsync(cancelacion))
            .Single(l => l.Nombre == solicitud.Nombre!.Trim() && l.Provincia == solicitud.Provincia);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, ADto(creada));
    }

    /// <summary>Modifica una localidad existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<LocalidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocalidadDto>> Modificar(int id, SolicitudDeLocalidad solicitud, CancellationToken cancelacion)
    {
        if (await repositorio.ObtenerAsync(id, cancelacion) is null) return NotFound();

        var modelo = AModelo(solicitud);
        modelo.Id = id;
        var resultado = await servicio.GuardarAsync(modelo, cancelacion);
        if (!resultado.EsCorrecto) return BadRequest(ProblemasDeValidacion.De(resultado.Errores));

        return Ok(ADto((await repositorio.ObtenerAsync(id, cancelacion))!));
    }

    /// <summary>Da de baja una localidad.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancelacion)
    {
        var resultado = await servicio.EliminarAsync(id, cancelacion);
        return resultado.EsCorrecto ? NoContent() : NotFound();
    }

    private static LocalidadDto ADto(Localidad l) => new(l.Id, l.Nombre, l.Provincia, l.CodigoPostal, l.Habitantes);

    private static ModeloDeLocalidad AModelo(SolicitudDeLocalidad s) => new()
    {
        Nombre = s.Nombre ?? string.Empty,
        Provincia = s.Provincia ?? string.Empty,
        CodigoPostal = s.CodigoPostal ?? string.Empty,
        Habitantes = s.Habitantes
    };
}
