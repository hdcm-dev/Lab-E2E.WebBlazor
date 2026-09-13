namespace MovilidadUrbana.MAUI.Controles;

/// <summary>
/// Campo para números con decimales. En Android el teclado numérico filtra los caracteres según el
/// idioma del sistema y puede descartar la coma: «12,5» llegaba como «125» y se registraba así, sin
/// error. Este campo acepta coma y punto por igual, y el ViewModel interpreta los dos.
/// </summary>
public sealed class EntradaDecimal : Entry
{
    public EntradaDecimal() => Keyboard = Keyboard.Numeric;
}
