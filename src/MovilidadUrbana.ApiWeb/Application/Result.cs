namespace MovilidadUrbana.ApiWeb.Application;

/// <summary>
/// Resultado de un caso de uso: si salió bien, el aviso a mostrar y los errores por campo.
/// Las claves de <see cref="Errores"/> son los nombres de campo que la pantalla conoce
/// (`nombre`, `provincia`, …), para que la vista solo tenga que ubicarlos.
/// </summary>
public sealed record Result(bool IsSuccess, string Message, IReadOnlyDictionary<string, string> Errors)
{
    private static readonly IReadOnlyDictionary<string, string> NoErrors =
        new Dictionary<string, string>();

    public static Result Success(string mensaje) => new(true, mensaje, NoErrors);

    public static Result Invalid(IReadOnlyDictionary<string, string> errors) =>
        new(false, "Revise los campos marcados en rojo.", errors);

    public static Result Invalid(string campo, string mensaje) =>
        Invalid(new Dictionary<string, string> { [campo] = mensaje });
}
