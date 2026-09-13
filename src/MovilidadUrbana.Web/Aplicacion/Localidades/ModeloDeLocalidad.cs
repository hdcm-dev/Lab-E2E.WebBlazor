namespace MovilidadUrbana.Web.Aplicacion.Localidades;

/// <summary>Lo que la pantalla del ABM edita: campos crudos, tal como los tipea la persona.</summary>
public sealed class ModeloDeLocalidad
{
    public int? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Provincia { get; set; } = string.Empty;

    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>Anulable para distinguir «vacío» de «cero», que son dos errores distintos.</summary>
    public int? Habitantes { get; set; }

    public bool EsEdicion => Id is not null;

    public void Limpiar()
    {
        Id = null;
        Nombre = string.Empty;
        Provincia = string.Empty;
        CodigoPostal = string.Empty;
        Habitantes = null;
    }
}
