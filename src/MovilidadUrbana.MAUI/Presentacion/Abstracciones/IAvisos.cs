namespace MovilidadUrbana.MAUI.Presentacion.Abstracciones;

/// <summary>Mensajes breves y confirmaciones del sistema operativo, vistos desde el ViewModel.</summary>
public interface IAvisos
{
    /// <summary>Confirma el resultado de una acción sin bloquear, y desaparece solo.</summary>
    Task MostrarAsync(string mensaje);

    /// <summary>Pide confirmación antes de una acción que no se puede deshacer.</summary>
    Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar, string cancelar);
}
