using Microsoft.EntityFrameworkCore;
using MovilidadUrbana.Web.Aplicacion.Abstracciones;
using MovilidadUrbana.Web.Dominio.Entidades;

namespace MovilidadUrbana.Web.Infraestructura.Persistencia;

/// <summary>
/// Deja la sesión con su juego de datos inicial la primera vez que se la toca. La marca en la
/// tabla `Sesiones` evita volver a sembrar cuando la persona borró todas las localidades a mano.
/// </summary>
public sealed class SembradorDeSesion(IDbContextFactory<ContextoDeDatos> fabrica, IContextoDeSesion sesion)
{
    private static readonly (string Nombre, string Provincia, string CodigoPostal, int Habitantes)[] LocalidadesIniciales =
    [
        ("Corrientes", "Corrientes", "3400", 346334),
        ("Resistencia", "Chaco", "3500", 291720)
    ];

    private bool _yaVerificada;

    public async Task AsegurarAsync(CancellationToken cancelacion = default)
    {
        if (_yaVerificada) return;

        await using var contexto = await fabrica.CreateDbContextAsync(cancelacion);

        if (!await contexto.Sesiones.AnyAsync(s => s.Id == sesion.Id, cancelacion))
        {
            contexto.Sesiones.Add(new Sesion { Id = sesion.Id, CreadaEn = DateTimeOffset.UtcNow });
            contexto.Localidades.AddRange(LocalidadesIniciales.Select(inicial => new Localidad
            {
                SesionId = sesion.Id,
                Nombre = inicial.Nombre,
                Provincia = inicial.Provincia,
                CodigoPostal = inicial.CodigoPostal,
                Habitantes = inicial.Habitantes
            }));

            try
            {
                await contexto.SaveChangesAsync(cancelacion);
            }
            catch (DbUpdateException)
            {
                // Otra petición de la misma sesión ganó la carrera insertando la marca: los datos ya están.
            }
        }

        _yaVerificada = true;
    }
}
