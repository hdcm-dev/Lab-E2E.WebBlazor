using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.MAUI.Aplicacion;
using MovilidadUrbana.MAUI.Aplicacion.Encuestas;
using MovilidadUrbana.MAUI.Aplicacion.Localidades;
using MovilidadUrbana.MAUI.Infraestructura;
using MovilidadUrbana.MAUI.Infraestructura.Persistencia;
using MovilidadUrbana.MAUI.Presentacion.Abstracciones;
using MovilidadUrbana.MAUI.Presentacion.Localidades;

namespace MovilidadUrbana.MAUI.Tests;

/// <summary>
/// Compone las capas reales como lo hace la aplicación —infraestructura, aplicación y una sesión— sobre
/// un archivo SQLite propio del caso. Cada instancia es un dispositivo nuevo.
/// </summary>
public sealed class Entorno : IDisposable
{
    private readonly string _archivo = Path.Combine(Path.GetTempPath(), $"movilidad-maui-{Guid.NewGuid():n}.db");
    private readonly ServiceProvider _raiz;
    private readonly IServiceScope _alcance;

    public NavegadorFalso Navegador { get; } = new();
    public AvisosFalsos Avisos { get; } = new();

    public Entorno()
    {
        // La aplicación fija es-AR al arrancar; los formatos del resumen dependen de eso.
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("es-AR");

        var servicios = new ServiceCollection();
        servicios.AgregarInfraestructura($"Data Source={_archivo}");
        servicios.AgregarAplicacion();
        _raiz = servicios.BuildServiceProvider();
        PreparadorDeBaseDeDatos.Preparar(_raiz);
        _alcance = _raiz.CreateScope();
    }

    public ServicioDeLocalidades Localidades => _alcance.ServiceProvider.GetRequiredService<ServicioDeLocalidades>();
    public ServicioDeEncuestas Encuestas => _alcance.ServiceProvider.GetRequiredService<ServicioDeEncuestas>();

    public LocalidadesViewModel Lista() => new(Localidades, Navegador);
    public LocalidadEditorViewModel Editor() => new(Localidades, Navegador, Avisos);
    public Presentacion.Encuestas.EncuestaViewModel Encuesta() => new(Encuestas, Localidades);

    public void Dispose()
    {
        _alcance.Dispose();
        _raiz.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _archivo, _archivo + "-wal", _archivo + "-shm" }) File.Delete(f);
    }
}

public sealed class NavegadorFalso : INavegador
{
    public List<LocalidadItem?> EditoresAbiertos { get; } = [];
    public int Vueltas { get; private set; }

    public Task IrAlEditorDeLocalidadAsync(LocalidadItem? localidad) { EditoresAbiertos.Add(localidad); return Task.CompletedTask; }
    public Task VolverAsync() { Vueltas++; return Task.CompletedTask; }
}

public sealed class AvisosFalsos : IAvisos
{
    public List<string> Mostrados { get; } = [];
    public bool RespuestaAConfirmar { get; set; } = true;

    public Task MostrarAsync(string mensaje) { Mostrados.Add(mensaje); return Task.CompletedTask; }
    public Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar, string cancelar) => Task.FromResult(RespuestaAConfirmar);
}
