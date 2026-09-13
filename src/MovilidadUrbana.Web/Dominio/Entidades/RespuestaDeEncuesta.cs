namespace MovilidadUrbana.Web.Dominio.Entidades;

/// <summary>Una encuesta de transporte completada.</summary>
public class RespuestaDeEncuesta
{
    public int Id { get; set; }

    public string SesionId { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Edad { get; set; }

    public string Localidad { get; set; } = string.Empty;

    /// <summary>Medios de transporte elegidos. Se persiste como texto separado por comas.</summary>
    public IReadOnlyList<string> Medios { get; set; } = [];

    public string Frecuencia { get; set; } = string.Empty;

    public double Distancia { get; set; }

    public int Minutos { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public DateTimeOffset RegistradaEn { get; set; }
}
