using MovilidadUrbana.MAUI.Presentacion.Abstracciones;

namespace MovilidadUrbana.MAUI.Servicios;

/// <summary>
/// El aviso breve es el Toast de Android: confirma sin pedir un toque y sin tapar la pantalla a la que
/// se vuelve. La confirmación es el diálogo modal del sistema.
/// </summary>
public sealed class AvisosDelSistema : IAvisos
{
    public Task MostrarAsync(string mensaje)
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
            Android.Widget.Toast.MakeText(Platform.AppContext, mensaje, Android.Widget.ToastLength.Short)?.Show());
#endif
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar, string cancelar) =>
        Shell.Current.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar);
}
