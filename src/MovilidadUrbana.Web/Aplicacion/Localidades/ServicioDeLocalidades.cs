using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Dominio.Entidades;
using MovilidadUrbana.Web.Dominio.Reglas;

namespace MovilidadUrbana.Web.Aplicacion.Localidades;

/// <summary>Casos de uso del ABM: listar, dar de alta, modificar y dar de baja localidades.</summary>
public sealed class ServicioDeLocalidades(IRepositorioDeLocalidades repositorio)
{
    public Task<IReadOnlyList<Localidad>> ListarAsync(CancellationToken cancelacion = default) =>
        repositorio.ListarAsync(cancelacion);

    public async Task<Resultado> GuardarAsync(ModeloDeLocalidad modelo, CancellationToken cancelacion = default)
    {
        var errores = Validar(modelo);
        if (errores.Count > 0) return Resultado.Invalido(errores);

        var existentes = await repositorio.ListarAsync(cancelacion);
        var duplicada = existentes.Any(item =>
            item.Id != modelo.Id &&
            ReglasDeLocalidad.MismaLocalidad(item.Nombre, item.Provincia, modelo.Nombre, modelo.Provincia));

        if (duplicada)
        {
            return Resultado.Invalido("nombre", "Ya existe una localidad con ese nombre en la provincia.");
        }

        var nombre = modelo.Nombre.Trim();

        if (modelo.Id is int id)
        {
            var localidad = await repositorio.ObtenerAsync(id, cancelacion);
            if (localidad is null)
            {
                return Resultado.Invalido("nombre", "La localidad ya no existe.");
            }

            localidad.Nombre = nombre;
            localidad.Provincia = modelo.Provincia;
            localidad.CodigoPostal = modelo.CodigoPostal.Trim();
            localidad.Habitantes = modelo.Habitantes!.Value;
            await repositorio.ActualizarAsync(localidad, cancelacion);

            return Resultado.Correcto($"Se actualizó la localidad {nombre}.");
        }

        await repositorio.AgregarAsync(
            new Localidad
            {
                Nombre = nombre,
                Provincia = modelo.Provincia,
                CodigoPostal = modelo.CodigoPostal.Trim(),
                Habitantes = modelo.Habitantes!.Value
            },
            cancelacion);

        return Resultado.Correcto($"Se agregó la localidad {nombre}.");
    }

    public async Task<Resultado> EliminarAsync(int id, CancellationToken cancelacion = default)
    {
        var localidad = await repositorio.ObtenerAsync(id, cancelacion);
        if (localidad is null)
        {
            return Resultado.Invalido("nombre", "La localidad ya no existe.");
        }

        await repositorio.EliminarAsync(id, cancelacion);
        return Resultado.Correcto($"Se eliminó la localidad {localidad.Nombre}.");
    }

    private static Dictionary<string, string> Validar(ModeloDeLocalidad modelo)
    {
        var errores = new Dictionary<string, string>();

        if (!ReglasDeLocalidad.NombreValido(modelo.Nombre))
        {
            errores["nombre"] = $"El nombre debe tener al menos {ReglasDeLocalidad.LargoMinimoDelNombre} caracteres.";
        }
        if (!ReglasDeLocalidad.ProvinciaValida(modelo.Provincia))
        {
            errores["provincia"] = "Seleccione una provincia.";
        }
        if (!ReglasDeLocalidad.CodigoPostalValido(modelo.CodigoPostal))
        {
            errores["codigoPostal"] =
                $"El código postal debe tener {ReglasDeLocalidad.DigitosDelCodigoPostal} dígitos.";
        }
        if (!ReglasDeLocalidad.HabitantesValidos(modelo.Habitantes))
        {
            errores["habitantes"] = "Ingrese una cantidad de habitantes mayor a cero.";
        }

        return errores;
    }
}
