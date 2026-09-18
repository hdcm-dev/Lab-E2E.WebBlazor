using MovilidadUrbana.MAUI.Application.Abstractions;
using MovilidadUrbana.MAUI.Domain.Entities;
using MovilidadUrbana.MAUI.Domain.Rules;

namespace MovilidadUrbana.MAUI.Application.Localidades;

/// <summary>Casos de uso del ABM: listar, dar de alta, modificar y dar de baja localidades.</summary>
public sealed class LocalidadService(ILocalidadRepository repository)
{
    public Task<IReadOnlyList<Localidad>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public async Task<Result> SaveAsync(LocalidadModel model, CancellationToken cancellationToken = default)
    {
        var errors = Validate(model);
        if (errors.Count > 0) return Result.Invalid(errors);

        var existentes = await repository.GetAllAsync(cancellationToken);
        var duplicada = existentes.Any(item =>
            item.Id != model.Id &&
            LocalidadRules.MismaLocalidad(item.Nombre, item.Provincia, model.Nombre, model.Provincia));

        if (duplicada)
        {
            return Result.Invalid("nombre", "Ya existe una localidad con ese nombre en la provincia.");
        }

        var nombre = model.Nombre.Trim();

        if (model.Id is int id)
        {
            var localidad = await repository.GetByIdAsync(id, cancellationToken);
            if (localidad is null)
            {
                return Result.Invalid("nombre", "La localidad ya no existe.");
            }

            localidad.Nombre = nombre;
            localidad.Provincia = model.Provincia;
            localidad.CodigoPostal = model.CodigoPostal.Trim();
            localidad.Habitantes = model.Habitantes!.Value;
            await repository.UpdateAsync(localidad, cancellationToken);

            return Result.Success($"Se actualizó la localidad {nombre}.");
        }

        await repository.AddAsync(
            new Localidad
            {
                Nombre = nombre,
                Provincia = model.Provincia,
                CodigoPostal = model.CodigoPostal.Trim(),
                Habitantes = model.Habitantes!.Value
            },
            cancellationToken);

        return Result.Success($"Se agregó la localidad {nombre}.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var localidad = await repository.GetByIdAsync(id, cancellationToken);
        if (localidad is null)
        {
            return Result.Invalid("nombre", "La localidad ya no existe.");
        }

        await repository.DeleteAsync(id, cancellationToken);
        return Result.Success($"Se eliminó la localidad {localidad.Nombre}.");
    }

    private static Dictionary<string, string> Validate(LocalidadModel model)
    {
        var errors = new Dictionary<string, string>();

        if (!LocalidadRules.NombreValido(model.Nombre))
        {
            errors["nombre"] = $"El nombre debe tener al menos {LocalidadRules.LargoMinimoDelNombre} caracteres.";
        }
        if (!LocalidadRules.ProvinciaValida(model.Provincia))
        {
            errors["provincia"] = "Seleccione una provincia.";
        }
        if (!LocalidadRules.CodigoPostalValido(model.CodigoPostal))
        {
            errors["codigoPostal"] =
                $"El código postal debe tener {LocalidadRules.DigitosDelCodigoPostal} dígitos.";
        }
        if (!LocalidadRules.HabitantesValidos(model.Habitantes))
        {
            errors["habitantes"] = "Ingrese una cantidad de habitantes mayor a cero.";
        }

        return errors;
    }
}
