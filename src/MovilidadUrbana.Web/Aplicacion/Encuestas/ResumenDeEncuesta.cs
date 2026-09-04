using MovilidadUrbana.Web.Dominio;
using MovilidadUrbana.Web.Dominio.Entidades;

namespace MovilidadUrbana.Web.Aplicacion.Encuestas;

/// <summary>Una fila de la ficha de revisión: su clave, su etiqueta y su valor ya formateado.</summary>
/// <param name="Clave">Nombre del campo, estable, con el que la vista lo identifica.</param>
/// <param name="Etiqueta">Rótulo que se muestra.</param>
/// <param name="Valor">Valor ya formateado. La vista no formatea.</param>
public sealed record CampoDeResumen(string Clave, string Etiqueta, string Valor);

/// <summary>
/// Ficha de revisión de una respuesta registrada. La vista la recorre en lugar de escribir una
/// lista a mano: agregar un campo a la encuesta lo hace aparecer en la ficha sin tocar la
/// superficie.
/// </summary>
public static class ResumenDeEncuesta
{
    public static IReadOnlyList<CampoDeResumen> De(RespuestaDeEncuesta respuesta) =>
    [
        new("persona", "Persona", $"{respuesta.Nombre} ({respuesta.Edad} años)"),
        new("localidad", "Localidad", respuesta.Localidad),
        new("medios", "Medios", string.Join(", ", respuesta.Medios.Select(Catalogos.EtiquetaDeMedio))),
        new("frecuencia", "Frecuencia", Catalogos.EtiquetaDeFrecuencia(respuesta.Frecuencia)),
        new("distancia", "Distancia diaria", $"{respuesta.Distancia:0.###} km"),
        new("minutos", "Tiempo de viaje", $"{respuesta.Minutos} min"),
        new("motivo", "Motivo", Catalogos.EtiquetaDeMotivo(respuesta.Motivo))
    ];
}
