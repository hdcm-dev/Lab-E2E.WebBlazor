using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.MAUI.Infraestructura.Sesiones;

namespace MovilidadUrbana.MAUI.Servicios;

/// <summary>
/// En la web cada visitante es una sesión y en la API cada encabezado `X-Sesion-Id`; en el teléfono,
/// el dispositivo entero es una sola. Su identificador se genera la primera vez y queda guardado en
/// las preferencias de la aplicación, así los datos sobreviven a cerrarla.
///
/// Los servicios de Aplicacion e Infraestructura son de ámbito. Este objeto abre un único ámbito que
/// vive lo que vive la aplicación y fija ahí la sesión.
/// </summary>
public sealed class SesionDelDispositivo : IDisposable
{
    private const string Clave = "sesion-dispositivo";

    private readonly IServiceScope _alcance;

    public SesionDelDispositivo(IServiceProvider servicios)
    {
        var id = Preferences.Default.Get<string?>(Clave, null);
        if (!ContextoDeSesion.EsValido(id))
        {
            id = Guid.NewGuid().ToString("n");
            Preferences.Default.Set(Clave, id);
        }

        _alcance = servicios.CreateScope();
        _alcance.ServiceProvider.GetRequiredService<ContextoDeSesion>().Establecer(id!);
        Id = id!;
    }

    public string Id { get; }

    public T Crear<T>() => ActivatorUtilities.CreateInstance<T>(_alcance.ServiceProvider);

    public void Dispose() => _alcance.Dispose();
}
