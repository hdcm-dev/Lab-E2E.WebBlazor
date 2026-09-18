using Microsoft.AspNetCore.Mvc;
using MovilidadUrbana.ApiWeb.Application.Abstractions;
using MovilidadUrbana.ApiWeb.Application.Localidades;
using MovilidadUrbana.ApiWeb.Contracts;
using MovilidadUrbana.ApiWeb.Domain.Entities;

namespace MovilidadUrbana.ApiWeb.Controllers;

/// <summary>ABM de localidades. Cada sesión —encabezado <c>X-Sesion-Id</c>— ve solo las suyas.</summary>
[ApiController]
[Route("api/v1/localidades")]
[Produces("application/json")]
public sealed class LocalidadesController(
    LocalidadService service,
    ILocalidadRepository repository) : ControllerBase
{
    /// <summary>Lista las localidades de la sesión.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LocalidadDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocalidadDto>>> GetAll(CancellationToken cancellationToken)
    {
        var localidades = await service.GetAllAsync(cancellationToken);
        return Ok(localidades.Select(ToDto).ToList());
    }

    /// <summary>Devuelve una localidad por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<LocalidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocalidadDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var localidad = await repository.GetByIdAsync(id, cancellationToken);
        return localidad is null ? NotFound() : Ok(ToDto(localidad));
    }

    /// <summary>Da de alta una localidad.</summary>
    [HttpPost]
    [ProducesResponseType<LocalidadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocalidadDto>> Create(LocalidadRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveAsync(ToModel(request), cancellationToken);
        if (!result.IsSuccess) return BadRequest(ValidationProblems.De(result.Errors));

        // El servicio no devuelve la entidad creada: se la ubica por nombre y provincia, que el
        // propio servicio garantiza únicos dentro de la sesión.
        var creada = (await repository.GetAllAsync(cancellationToken))
            .Single(l => l.Nombre == request.Nombre!.Trim() && l.Provincia == request.Provincia);

        return CreatedAtAction(nameof(GetById), new { id = creada.Id }, ToDto(creada));
    }

    /// <summary>Modifica una localidad existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<LocalidadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocalidadDto>> Update(int id, LocalidadRequest request, CancellationToken cancellationToken)
    {
        if (await repository.GetByIdAsync(id, cancellationToken) is null) return NotFound();

        var model = ToModel(request);
        model.Id = id;
        var result = await service.SaveAsync(model, cancellationToken);
        if (!result.IsSuccess) return BadRequest(ValidationProblems.De(result.Errors));

        return Ok(ToDto((await repository.GetByIdAsync(id, cancellationToken))!));
    }

    /// <summary>Da de baja una localidad.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    private static LocalidadDto ToDto(Localidad l) => new(l.Id, l.Nombre, l.Provincia, l.CodigoPostal, l.Habitantes);

    private static LocalidadModel ToModel(LocalidadRequest s) => new()
    {
        Nombre = s.Nombre ?? string.Empty,
        Provincia = s.Provincia ?? string.Empty,
        CodigoPostal = s.CodigoPostal ?? string.Empty,
        Habitantes = s.Habitantes
    };
}
