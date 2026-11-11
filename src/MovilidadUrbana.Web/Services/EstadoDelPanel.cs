namespace MovilidadUrbana.Web.Services;

/// <summary>
/// Estado de interfaz del panel del asistente, por circuito: si está abierto. La barra lateral lo
/// abre y el panel, montado una sola vez en el layout, lo escucha.
/// </summary>
public sealed class EstadoDelPanel
{
    public bool Abierto { get; private set; }

    public event Action? OnChange;

    public void Alternar() => Fijar(!Abierto);

    public void Fijar(bool abierto)
    {
        if (Abierto == abierto) return;
        Abierto = abierto;
        OnChange?.Invoke();
    }
}
