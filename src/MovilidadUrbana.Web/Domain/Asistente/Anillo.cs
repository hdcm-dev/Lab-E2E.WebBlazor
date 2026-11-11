namespace MovilidadUrbana.Web.Domain.Asistente;

/// <summary>Frontera de competencia del asistente. La decide el código, nunca el modelo (F-1).</summary>
public enum Anillo { Operar = 1, Dominio = 2, LoDeAlLado = 3, Afuera = 4 }

/// <summary>
/// Formas de respuesta (K5): F1 cifra con nota, F2 total con desglose ofrecido, F3 aclaración con
/// opciones, F4 explicación con cita, F5 abstención con salida, F6 negativa útil, F7 rechazo amable,
/// F8 aviso de degradación.
/// </summary>
public enum Forma { F1 = 1, F2, F3, F4, F5, F6, F7, F8 }
