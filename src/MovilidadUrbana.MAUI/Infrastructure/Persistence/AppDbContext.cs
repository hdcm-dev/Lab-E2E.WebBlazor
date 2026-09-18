using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MovilidadUrbana.MAUI.Domain.Entities;

namespace MovilidadUrbana.MAUI.Infrastructure.Persistence;

/// <summary>Contexto EF Core sobre SQLite. Es el único lugar que conoce el motor de base de datos.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Localidad> Localidades => Set<Localidad>();

    public DbSet<RespuestaDeEncuesta> Encuestas => Set<RespuestaDeEncuesta>();

    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Session>(entidad =>
        {
            entidad.HasKey(s => s.Id);
            entidad.Property(s => s.Id).HasMaxLength(64);
        });

        model.Entity<Localidad>(entidad =>
        {
            entidad.Property(l => l.SessionId).HasMaxLength(64).IsRequired();
            entidad.Property(l => l.Nombre).HasMaxLength(60).IsRequired();
            entidad.Property(l => l.Provincia).HasMaxLength(60).IsRequired();
            entidad.Property(l => l.CodigoPostal).HasMaxLength(4).IsRequired();
            // Toda consulta del ABM filtra por sesión: el índice acompaña ese acceso.
            entidad.HasIndex(l => l.SessionId);
        });

        model.Entity<RespuestaDeEncuesta>(entidad =>
        {
            entidad.Property(e => e.SessionId).HasMaxLength(64).IsRequired();
            entidad.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entidad.HasIndex(e => e.SessionId);

            // SQLite no tiene tipo lista: los medios se guardan como texto separado por comas.
            entidad.Property(e => e.Medios)
                .HasConversion(
                    new ValueConverter<IReadOnlyList<string>, string>(
                        medios => string.Join(',', medios),
                        texto => texto.Length == 0
                            ? new List<string>()
                            : texto.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()),
                    new ValueComparer<IReadOnlyList<string>>(
                        (a, b) => a!.SequenceEqual(b!),
                        lista => lista.Aggregate(0, (acumulado, valor) => HashCode.Combine(acumulado, valor.GetHashCode())),
                        lista => lista.ToList()));
        });
    }
}
