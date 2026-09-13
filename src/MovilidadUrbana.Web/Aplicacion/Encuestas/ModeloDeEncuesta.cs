namespace MovilidadUrbana.Web.Aplicacion.Encuestas;

/// <summary>Lo que el asistente va acumulando a lo largo de los tres pasos.</summary>
public sealed class ModeloDeEncuesta
{
    // Paso 1 — datos de la persona
    public string Nombre { get; set; } = string.Empty;
    public int? Edad { get; set; }
    public string Localidad { get; set; } = string.Empty;

    // Paso 2 — medios que utiliza para viajar
    public HashSet<string> Medios { get; } = [];
    public string Frecuencia { get; set; } = string.Empty;

    // Paso 3 — distancia recorrida
    public double? Distancia { get; set; }
    public int? Minutos { get; set; }
    public string Motivo { get; set; } = string.Empty;

    public void AlternarMedio(string clave, bool elegido)
    {
        if (elegido) Medios.Add(clave);
        else Medios.Remove(clave);
    }
}
