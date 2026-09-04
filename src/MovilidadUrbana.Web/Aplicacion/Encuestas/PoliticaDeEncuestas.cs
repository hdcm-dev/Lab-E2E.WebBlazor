using MovilidadUrbana.Web.Dominio.Reglas;

namespace MovilidadUrbana.Web.Aplicacion.Encuestas;

/// <summary>
/// Requisitos de la encuesta enunciados para la persona, derivados de
/// <see cref="ReglasDeEncuesta" />. Ninguno se escribe a mano en la vista: los rangos son los
/// mismos que decide la validación.
/// </summary>
public static class PoliticaDeEncuestas
{
    public static string RequisitoDelNombre =>
        $"Al menos {ReglasDeEncuesta.LargoMinimoDelNombre} caracteres.";

    public static string RequisitoDeLaEdad =>
        $"Entre {ReglasDeEncuesta.EdadMinima} y {ReglasDeEncuesta.EdadMaxima} años.";

    public static string RequisitoDeLaLocalidad => "Una de las localidades cargadas en el ABM.";

    public static string RequisitoDeLosMedios => "Al menos uno.";

    public static string RequisitoDeLaFrecuencia => "Con qué asiduidad usa esos medios.";

    public static string RequisitoDeLaDistancia =>
        $"Entre {ReglasDeEncuesta.DistanciaMinima:0} y {ReglasDeEncuesta.DistanciaMaxima:0} km por día.";

    public static string RequisitoDeLosMinutos =>
        $"Entre {ReglasDeEncuesta.MinutosMinimos} y {ReglasDeEncuesta.MinutosMaximos} minutos.";

    public static string RequisitoDelMotivo => "El motivo principal del viaje.";
}
