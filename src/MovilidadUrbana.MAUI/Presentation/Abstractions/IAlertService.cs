namespace MovilidadUrbana.MAUI.Presentation.Abstractions;

/// <summary>Mensajes breves y confirmaciones del sistema operativo, vistos desde el ViewModel.</summary>
public interface IAlertService
{
    /// <summary>Confirma el resultado de una acción sin bloquear, y desaparece solo.</summary>
    Task ShowAsync(string mensaje);

    /// <summary>Pide confirmación antes de una acción que no se puede deshacer.</summary>
    Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar, string cancelar);
}
