using System.Threading.RateLimiting;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Un solo límite de pedidos al modelo (REQ-IA-21), compartido por el redactor y el extractor:
/// un permiso y sin cola. La segunda consulta simultánea no espera: degrada a «ocupado».
/// </summary>
public sealed class LimitadorDelModelo(int permisos = 1) : IDisposable
{
    private readonly ConcurrencyLimiter _limitador = new(new ConcurrencyLimiterOptions
    {
        PermitLimit = permisos,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

    public RateLimitLease Intentar() => _limitador.AttemptAcquire();

    public void Dispose() => _limitador.Dispose();
}
