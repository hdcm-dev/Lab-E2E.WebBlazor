using MovilidadUrbana.Web.Application.Resultados;
using MovilidadUrbana.Web.Domain.Asistente;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Una cifra que salió de una función de lectura, con su nota de métrica (REQ-IA-42).</summary>
public sealed record Cifra(string Etiqueta, double Valor, string Nota);

/// <summary>Un botón de aclaración (F3): el rótulo y la pregunta, ya sin ambigüedad, que dispara.</summary>
public sealed record Opcion(string Rotulo, string Pregunta);

/// <summary>Una cita a un fragmento de la ayuda.</summary>
public sealed record Cita(int Numero, string FragmentoId, string Titulo);

/// <summary>
/// La respuesta de un turno. La arma el código; el modelo, cuando está, sólo aporta el
/// <see cref="Texto" /> y pasa por el validador. Todo lo demás —cifras, gráfico, opciones,
/// destinos— es del código.
/// </summary>
public sealed record RespuestaDelAsistente
{
    public required string Intencion { get; init; }
    public required Anillo Anillo { get; init; }
    public required Forma Forma { get; init; }
    public required ResultadoDelTurno Resultado { get; init; }
    public required string Texto { get; init; }
    public bool RedactadaConIa { get; init; }
    public IReadOnlyList<Cifra> Cifras { get; init; } = [];
    public Distribucion? Distribucion { get; init; }
    public Promedios? Promedios { get; init; }
    public FormaDeGrafico Grafico { get; init; }
    public IReadOnlyList<Opcion> Opciones { get; init; } = [];
    public IReadOnlyList<Chip> Destinos { get; init; } = [];
    public IReadOnlyList<Cita> Citas { get; init; } = [];
    public IReadOnlyList<string> Sugerencias { get; init; } = [];

    /// <summary>Las intenciones del mensaje, en el orden en que se respondieron (marco §4).</summary>
    public IReadOnlyList<string> Partes { get; init; } = [];

    /// <summary>Por qué la redacción cayó a la plantilla (control que falló, o «sin respuesta»), para la medición.</summary>
    public string? MotivoDelResultado { get; init; }

    /// <summary>Aviso de degradación (F8) que acompaña la respuesta, si lo hay.</summary>
    public string? Aviso { get; init; }

    public string Rotulo => RedactadaConIa ? "Redactado con IA a partir de la ayuda" : "Respuesta fija";
}
