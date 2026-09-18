using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.MAUI.Application;
using MovilidadUrbana.MAUI.Application.Encuestas;
using MovilidadUrbana.MAUI.Application.Localidades;
using MovilidadUrbana.MAUI.Infrastructure;
using MovilidadUrbana.MAUI.Infrastructure.Persistence;
using MovilidadUrbana.MAUI.Presentation.Abstractions;
using MovilidadUrbana.MAUI.Presentation.Localidades;

namespace MovilidadUrbana.MAUI.Tests;

/// <summary>
/// Compone las capas reales como lo hace la aplicación —infraestructura, aplicación y una sesión— sobre
/// un archivo SQLite propio del caso. Cada instancia es un dispositivo nuevo.
/// </summary>
public sealed class TestEnvironment : IDisposable
{
    private readonly string _archivo = Path.Combine(Path.GetTempPath(), $"movilidad-maui-{Guid.NewGuid():n}.db");
    private readonly ServiceProvider _raiz;
    private readonly IServiceScope _alcance;

    public FakeNavigationService Navegador { get; } = new();
    public FakeAlertService Avisos { get; } = new();

    public TestEnvironment()
    {
        // La aplicación fija es-AR al arrancar; los formatos del resumen dependen de eso.
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("es-AR");

        var services = new ServiceCollection();
        services.AddInfrastructure($"Data Source={_archivo}");
        services.AddApplication();
        _raiz = services.BuildServiceProvider();
        DatabaseInitializer.Initialize(_raiz);
        _alcance = _raiz.CreateScope();
    }

    public LocalidadService Localidades => _alcance.ServiceProvider.GetRequiredService<LocalidadService>();
    public EncuestaService Encuestas => _alcance.ServiceProvider.GetRequiredService<EncuestaService>();

    public LocalidadesViewModel Lista() => new(Localidades, Navegador);
    public LocalidadEditorViewModel Editor() => new(Localidades, Navegador, Avisos);
    public Presentation.Encuestas.EncuestaViewModel Encuesta() => new(Encuestas, Localidades);

    public void Dispose()
    {
        _alcance.Dispose();
        _raiz.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _archivo, _archivo + "-wal", _archivo + "-shm" }) File.Delete(f);
    }
}

public sealed class FakeNavigationService : INavigationService
{
    public List<LocalidadItem?> OpenedEditors { get; } = [];
    public int GoBackCount { get; private set; }

    public Task NavigateToLocalidadEditorAsync(LocalidadItem? localidad) { OpenedEditors.Add(localidad); return Task.CompletedTask; }
    public Task GoBackAsync() { GoBackCount++; return Task.CompletedTask; }
}

public sealed class FakeAlertService : IAlertService
{
    public List<string> Shown { get; } = [];
    public bool ConfirmResult { get; set; } = true;

    public Task ShowAsync(string mensaje) { Shown.Add(mensaje); return Task.CompletedTask; }
    public Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar, string cancelar) => Task.FromResult(ConfirmResult);
}
