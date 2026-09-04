namespace MovilidadUrbana.Web.Components.Componentes;

/// <summary>
/// Estado vigente de una superficie. Es la realización del conmutador declarativo de la maqueta:
/// donde el marcado estático lleva `data-mq-estado`, el componente lleva esta propiedad y un
/// bloque `@if` por estado.
///
/// El vocabulario es el del template y no se agrega ni se recorta: los estados que este producto
/// no alcanza —`Reconectando` lo gobierna el framework— quedan declarados igual, para que el
/// inventario siga siendo el mismo en todas las superficies.
/// </summary>
public enum EstadoDeSuperficie
{
    /// <summary>Operación asíncrona en curso: esqueletos.</summary>
    Cargando,

    /// <summary>No hay datos todavía. La acción que se ofrece es crear el primero.</summary>
    Vacio,

    /// <summary>Hay datos y el filtro no encontró ninguno. La acción es limpiar el filtro.</summary>
    FiltradoSinResultados,

    /// <summary>Hay contenido para presentar.</summary>
    ConDatos,

    /// <summary>El servicio no respondió: lo que se ve puede estar desactualizado.</summary>
    Indisponible,

    /// <summary>Una acción viaja al servidor.</summary>
    Enviando,

    /// <summary>Los datos que se cargaron no cumplen la política.</summary>
    ErrorDeEntrada,

    /// <summary>La operación se rechazó por una razón que no es de entrada.</summary>
    ErrorDeOperacion,

    /// <summary>La acción se completó.</summary>
    Exito,

    /// <summary>El circuito se cortó y se está reconectando.</summary>
    Reconectando
}
