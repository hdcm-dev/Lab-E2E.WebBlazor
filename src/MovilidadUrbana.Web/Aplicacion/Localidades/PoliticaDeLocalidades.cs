using MovilidadUrbana.Web.Dominio.Reglas;

namespace MovilidadUrbana.Web.Aplicacion.Localidades;

/// <summary>
/// Requisitos del ABM enunciados para la persona, derivados de las reglas del dominio. Existe
/// para que la pantalla no transcriba la política: si un límite cambia en
/// <see cref="ReglasDeLocalidad" />, el requisito que se muestra cambia con él.
///
/// Se muestran <b>antes</b> del intento; el mensaje de error que aparece al fallar lo decide el
/// servicio de aplicación.
/// </summary>
public static class PoliticaDeLocalidades
{
    public static string RequisitoDelNombre =>
        $"Entre {ReglasDeLocalidad.LargoMinimoDelNombre} y {ReglasDeLocalidad.LargoMaximoDelNombre} caracteres, único por provincia.";

    public static string RequisitoDeLaProvincia => "Una de las provincias del catálogo.";

    public static string RequisitoDelCodigoPostal =>
        $"{ReglasDeLocalidad.DigitosDelCodigoPostal} dígitos.";

    public static string RequisitoDeLosHabitantes =>
        $"Un número entero de {ReglasDeLocalidad.HabitantesMinimos} en adelante.";
}
