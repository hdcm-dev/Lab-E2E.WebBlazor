namespace MovilidadUrbana.ApiWeb.Contratos;

/// <summary>Los tres pasos de la encuesta, en un solo envío.</summary>
public sealed record SolicitudDeEncuesta(
    string? Nombre,
    int? Edad,
    string? Localidad,
    IReadOnlyList<string>? Medios,
    string? Frecuencia,
    double? Distancia,
    int? Minutos,
    string? Motivo);

/// <summary>Una fila de la ficha de revisión, ya formateada.</summary>
public sealed record CampoDeResumenDto(string Clave, string Etiqueta, string Valor);

/// <summary>La encuesta registrada y su ficha de revisión.</summary>
public sealed record EncuestaRegistradaDto(int Id, DateTimeOffset RegistradaEn, IReadOnlyList<CampoDeResumenDto> Resumen);

/// <summary>Resultado de validar un paso sin registrar nada.</summary>
public sealed record ValidacionDePasoDto(int Paso, bool Valido, IReadOnlyDictionary<string, string> Errores);

/// <summary>Cuántas encuestas registró esta sesión.</summary>
public sealed record ContadorDeEncuestasDto(int Cantidad);
