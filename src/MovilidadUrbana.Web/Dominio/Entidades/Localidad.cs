namespace MovilidadUrbana.Web.Dominio.Entidades;

/// <summary>
/// Localidad del ABM. `SesionId` aísla los datos de cada visitante: es lo que en la versión
/// estática de este laboratorio resolvía `localStorage`, y lo que permite que varias pruebas
/// E2E corran en paralelo contra el mismo servidor sin pisarse.
/// </summary>
public class Localidad
{
    public int Id { get; set; }

    public string SesionId { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Provincia { get; set; } = string.Empty;

    public string CodigoPostal { get; set; } = string.Empty;

    public int Habitantes { get; set; }
}
