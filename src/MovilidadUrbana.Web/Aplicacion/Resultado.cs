namespace MovilidadUrbana.Web.Aplicacion;

/// <summary>
/// Resultado de un caso de uso: si salió bien, el aviso a mostrar y los errores por campo.
/// Las claves de <see cref="Errores"/> son los nombres de campo que la pantalla conoce
/// (`nombre`, `provincia`, …), para que la vista solo tenga que ubicarlos.
/// </summary>
public sealed record Resultado(bool EsCorrecto, string Mensaje, IReadOnlyDictionary<string, string> Errores)
{
    private static readonly IReadOnlyDictionary<string, string> SinErrores =
        new Dictionary<string, string>();

    public static Resultado Correcto(string mensaje) => new(true, mensaje, SinErrores);

    public static Resultado Invalido(IReadOnlyDictionary<string, string> errores) =>
        new(false, "Revise los campos marcados en rojo.", errores);

    public static Resultado Invalido(string campo, string mensaje) =>
        Invalido(new Dictionary<string, string> { [campo] = mensaje });
}
