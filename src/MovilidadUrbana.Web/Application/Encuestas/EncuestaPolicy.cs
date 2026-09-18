using MovilidadUrbana.Web.Domain.Rules;

namespace MovilidadUrbana.Web.Application.Encuestas;

/// <summary>
/// Requisitos de la encuesta enunciados para la persona, derivados de
/// <see cref="ReglasDeEncuesta" />. Ninguno se escribe a mano en la vista: los rangos son los
/// mismos que decide la validación.
/// </summary>
public static class EncuestaPolicy
{
    public static string RequisitoDelNombre =>
        $"Al menos {EncuestaRules.LargoMinimoDelNombre} caracteres.";

    public static string RequisitoDeLaEdad =>
        $"Entre {EncuestaRules.EdadMinima} y {EncuestaRules.EdadMaxima} años.";

    public static string RequisitoDeLaLocalidad => "Una de las localidades cargadas en el ABM.";

    public static string RequisitoDeLosMedios => "Al menos uno.";

    public static string RequisitoDeLaFrecuencia => "Con qué asiduidad usa esos medios.";

    public static string RequisitoDeLaDistancia =>
        $"Entre {EncuestaRules.DistanciaMinima:0} y {EncuestaRules.DistanciaMaxima:0} km por día.";

    public static string RequisitoDeLosMinutos =>
        $"Entre {EncuestaRules.MinutosMinimos} y {EncuestaRules.MinutosMaximos} minutos.";

    public static string RequisitoDelMotivo => "El motivo principal del viaje.";
}
