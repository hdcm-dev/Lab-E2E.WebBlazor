using System.Globalization;
using Microsoft.Extensions.Logging;
using MovilidadUrbana.MAUI.Aplicacion;
using MovilidadUrbana.MAUI.Infraestructura;
using MovilidadUrbana.MAUI.Infraestructura.Persistencia;
using MovilidadUrbana.MAUI.Paginas;
using MovilidadUrbana.MAUI.Presentacion.Abstracciones;
using MovilidadUrbana.MAUI.Presentacion.Encuestas;
using MovilidadUrbana.MAUI.Presentacion.Localidades;
using MovilidadUrbana.MAUI.Servicios;

namespace MovilidadUrbana.MAUI;

/// <summary>
/// Raíz de composición. Registra las mismas capas que la web y la API —Aplicacion e Infraestructura—
/// con una base SQLite dentro del almacenamiento privado de la aplicación: no hay servidor.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Los formatos de números del resumen y la lista son los de Argentina, igual que en la web.
        // Default* alcanza a los hilos nuevos; el hilo principal ya existe y se fija aparte.
        var cultura = new CultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = cultura;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = cultura;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        QuitarSubrayadoNativo();
        AceptarComaDecimal();

        var archivo = Path.Combine(FileSystem.AppDataDirectory, "movilidad.db");
        builder.Services.AgregarInfraestructura($"Data Source={archivo};Default Timeout=30");
        builder.Services.AgregarAplicacion();

        builder.Services.AddSingleton<SesionDelDispositivo>();
        builder.Services.AddSingleton<INavegador, NavegadorDeShell>();
        builder.Services.AddSingleton<IAvisos, AvisosDelSistema>();

        // Los ViewModels toman los servicios del ámbito del dispositivo, que ya tiene la sesión puesta.
        builder.Services.AddTransient(sp => sp.GetRequiredService<SesionDelDispositivo>().Crear<LocalidadesViewModel>());
        builder.Services.AddTransient(sp => sp.GetRequiredService<SesionDelDispositivo>().Crear<LocalidadEditorViewModel>());
        builder.Services.AddSingleton(sp => sp.GetRequiredService<SesionDelDispositivo>().Crear<EncuestaViewModel>());

        builder.Services.AddTransient<LocalidadesPage>();
        builder.Services.AddTransient<LocalidadEditorPage>();
        builder.Services.AddTransient<EncuestaPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        PreparadorDeBaseDeDatos.Preparar(app.Services);
        return app;
    }

    /// <summary>Ver <see cref="Controles.EntradaDecimal"/>. Se engancha a la clave Keyboard para correr después de ella.</summary>
    private static void AceptarComaDecimal()
    {
#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(Entry.Keyboard), (h, v) =>
        {
            if (v is not Controles.EntradaDecimal) return;
            h.PlatformView.InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal;
            h.PlatformView.KeyListener = Android.Text.Method.DigitsKeyListener.GetInstance("0123456789,.");
        });
#endif
    }

    /// <summary>
    /// Los campos van dentro de una caja con borde (estilo «Campo»). El subrayado que Android dibuja por
    /// su cuenta quedaría como una segunda línea adentro de la caja.
    /// </summary>
    private static void QuitarSubrayadoNativo()
    {
#if ANDROID
        var transparente = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinSubrayado", (h, _) => h.PlatformView.BackgroundTintList = transparente);
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("SinSubrayado", (h, _) => h.PlatformView.BackgroundTintList = transparente);
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("SinSubrayado", (h, _) =>
        {
            // El id de la placa del SearchView no está expuesto en el binding de .NET: se resuelve por nombre.
            var id = h.PlatformView.Resources?.GetIdentifier("search_plate", "id", "android") ?? 0;
            var placa = id == 0 ? null : h.PlatformView.FindViewById(id);
            placa?.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });
#endif
    }
}
