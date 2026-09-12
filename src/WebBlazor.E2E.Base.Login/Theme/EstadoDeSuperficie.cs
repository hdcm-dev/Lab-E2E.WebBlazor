namespace WebBlazor.E2E.Base.Login.Theme;

/// <summary>
/// Vocabulario de estados de una superficie. Es el mismo del template de maqueta,
/// sin agregados ni recortes: lo que allá se declara con <c>data-mq-estado</c>,
/// acá se realiza como esta propiedad y un <c>@if</c> por bloque.
/// </summary>
/// <remarks>
/// <see cref="FiltradoSinResultados" /> es un estado DISTINTO de <see cref="Vacio" />:
/// en el primero hay datos y el filtro no encontró nada, y la acción es limpiar el
/// filtro; en el segundo no hay datos, y la acción es crear el primero. Confundirlos
/// le ofrece a la persona la acción equivocada.
/// </remarks>
public enum EstadoDeSuperficie
{
    /// <summary>La colección está en camino.</summary>
    Cargando,

    /// <summary>No hay datos todavía.</summary>
    Vacio,

    /// <summary>Hay datos, y ninguno cumple el filtro.</summary>
    FiltradoSinResultados,

    /// <summary>Hay contenido para mostrar.</summary>
    ConDatos,

    /// <summary>La superficie no pudo traer lo suyo.</summary>
    Indisponible,

    /// <summary>Una acción primaria está viajando al servidor.</summary>
    Enviando,

    /// <summary>Lo ingresado no cumple lo que la superficie enuncia.</summary>
    ErrorDeEntrada,

    /// <summary>La operación fue rechazada.</summary>
    ErrorDeOperacion,

    /// <summary>La operación se concretó.</summary>
    Exito,

    /// <summary>El circuito se está restableciendo.</summary>
    Reconectando
}
