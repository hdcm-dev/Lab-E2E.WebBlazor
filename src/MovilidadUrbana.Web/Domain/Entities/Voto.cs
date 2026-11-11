using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.Web.Domain.Entities;

/// <summary>
/// Un voto 👍/👎 sobre una respuesta del asistente, o una señal automática (F5 y F7) con la misma
/// huella. Sólo campos cerrados: ni la pregunta, ni la respuesta, ni la cookie, ni la sesión. Lo
/// que no se guarda no se puede filtrar.
/// </summary>
public class Voto
{
    public int Id { get; set; }

    /// <summary>Id de intención del enrutador (catálogo cerrado de <c>Intenciones</c>).</summary>
    public string Intencion { get; set; } = string.Empty;

    public Anillo Anillo { get; set; }

    public Forma Forma { get; set; }

    /// <summary>Id del catálogo de destinos, si la respuesta ofreció uno.</summary>
    public string? Destino { get; set; }

    public ResultadoDelTurno Resultado { get; set; }

    public OrigenDelVoto Origen { get; set; }

    public bool Util { get; set; }

    public MotivoDelVoto? Motivo { get; set; }

    public string VersionGlosario { get; set; } = string.Empty;

    public string VersionCorpus { get; set; } = string.Empty;

    public DateOnly Dia { get; set; }
}

public enum ResultadoDelTurno { Ok, Degradado, Rechazado, Abstencion, Afuera }

public enum OrigenDelVoto { Persona, Senal }

public enum MotivoDelVoto { NoEsLoQuePregunte, FaltaInformacion, DatoIncorrecto }
