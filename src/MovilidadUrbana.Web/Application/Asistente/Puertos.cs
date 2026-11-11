using MovilidadUrbana.Web.Domain.Asistente;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.Web.Application.Asistente;

// Puertos de la capa de IA. Viven en Application y no nombran ningún SDK: los adaptadores, en
// Infrastructure/AI, son los únicos que conocen Microsoft.Extensions.AI.

/// <summary>Lo único que viaja al modelo de redacción (R-02): ni sesión, ni nombre, ni edad.</summary>
public sealed record PedidoDeRedaccion(
    string Intencion,
    Anillo Anillo,
    string Pregunta,
    IReadOnlyList<Cifra> Cifras,
    IReadOnlyList<FragmentoDeAyuda> Fragmentos,
    string Plantilla);

public enum TipoDeResultado { Ok, Degradado, Rechazado }

/// <summary>Los tres resultados de un puerto: ok, degradado (sin modelo) o rechazado (no pasó un control).</summary>
public sealed record Resultado<T>(TipoDeResultado Tipo, T? Valor, string? Motivo)
{
    public static Resultado<T> Ok(T valor) => new(TipoDeResultado.Ok, valor, null);
    public static Resultado<T> Degradado(string motivo) => new(TipoDeResultado.Degradado, default, motivo);
    public static Resultado<T> Rechazado(string motivo) => new(TipoDeResultado.Rechazado, default, motivo);
}

public interface IRedactorDeAyuda
{
    Task<Resultado<string>> RedactarAsync(PedidoDeRedaccion pedido, CancellationToken cancellationToken = default);
}

/// <summary>Lo que el extractor propone desde una frase. Todo anulable: lo que falta, falta.</summary>
public sealed record LocalidadPropuesta(string? Nombre, string? Provincia, string? CodigoPostal, int? Habitantes);

public interface IExtractorDeLocalidad
{
    Task<Resultado<LocalidadPropuesta>> ExtraerAsync(string frase, CancellationToken cancellationToken = default);
}

/// <summary>Escribe votos y señales. El pipeline lo usa para registrar; nunca para leer.</summary>
public interface IRegistroDeSenales
{
    Task RegistrarAsync(Voto voto, CancellationToken cancellationToken = default);
}

/// <summary>Lee los votos para el tablero. <see cref="AsistenteService" /> no depende de este puerto (K2 §8.3).</summary>
public interface ILectorDeVotos
{
    Task<IReadOnlyList<Voto>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>Catálogo cerrado de motivos de habilitación (REQ-IA-29).</summary>
public enum MotivoDeHabilitacion { Habilitada, ApagadaPorParametro, FaltaEndpoint, FaltaModelo, ClaveDesconocida, SinRespuesta }

/// <summary>Estado de la capa de IA tal como lo ve la presentación: motivo y la clave que lo explica.</summary>
public interface IEstadoDelAsistente
{
    /// <summary>Si el panel existe. Con <c>Asistente__Habilitado=false</c>, no hay panel.</summary>
    bool PanelVisible { get; }

    MotivoDeHabilitacion Motivo { get; }

    /// <summary>Nombre de la clave de configuración que explica el motivo; nunca su valor.</summary>
    string? Clave { get; }

    /// <summary>Si la búsqueda usa sólo el léxico (sin embeddings).</summary>
    bool SoloLexico { get; }

    /// <summary>
    /// Si las distribuciones pasan por el redactor con modelo (<c>Asistente__Redaccion__Habilitada</c>).
    /// Apagada por omisión (ADR 0008): con el modelo medido la redacción sólo agregaba citas a la
    /// plantilla. Las cifras, los destinos, las aclaraciones y el glosario no dependen de esto.
    /// </summary>
    bool RedaccionHabilitada { get; }
}
