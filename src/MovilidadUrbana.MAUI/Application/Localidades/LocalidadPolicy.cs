using MovilidadUrbana.MAUI.Domain.Rules;

namespace MovilidadUrbana.MAUI.Application.Localidades;

/// <summary>
/// Requisitos del ABM enunciados para la persona, derivados de las reglas del dominio. Existe
/// para que la pantalla no transcriba la política: si un límite cambia en
/// <see cref="ReglasDeLocalidad" />, el requisito que se muestra cambia con él.
///
/// Se muestran <b>antes</b> del intento; el mensaje de error que aparece al fallar lo decide el
/// servicio de aplicación.
/// </summary>
public static class LocalidadPolicy
{
    public static string RequisitoDelNombre =>
        $"Entre {LocalidadRules.LargoMinimoDelNombre} y {LocalidadRules.LargoMaximoDelNombre} caracteres, único por provincia.";

    public static string RequisitoDeLaProvincia => "Una de las provincias del catálogo.";

    public static string RequisitoDelCodigoPostal =>
        $"{LocalidadRules.DigitosDelCodigoPostal} dígitos.";

    public static string RequisitoDeLosHabitantes =>
        $"Un número entero de {LocalidadRules.HabitantesMinimos} en adelante.";
}
