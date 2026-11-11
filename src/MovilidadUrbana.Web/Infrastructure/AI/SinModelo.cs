using MovilidadUrbana.Web.Application.Asistente;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>Redactor de la IA apagada: siempre degradado, y el asistente responde con la plantilla.</summary>
public sealed class RedactorSinModelo : IRedactorDeAyuda
{
    public Task<Resultado<string>> RedactarAsync(PedidoDeRedaccion pedido, CancellationToken cancellationToken = default) =>
        Task.FromResult(Resultado<string>.Degradado("sin modelo"));
}

/// <summary>Extractor de la IA apagada: sin propuesta; el chip abre el alta vacía.</summary>
public sealed class ExtractorSinModelo : IExtractorDeLocalidad
{
    public Task<Resultado<LocalidadPropuesta>> ExtraerAsync(string frase, CancellationToken cancellationToken = default) =>
        Task.FromResult(Resultado<LocalidadPropuesta>.Degradado("sin modelo"));
}
