namespace MovilidadUrbana.Web.Dominio.Entidades;

/// <summary>
/// Deja constancia de que una sesión ya recibió su juego de datos inicial. Sin esta marca,
/// borrar todas las localidades volvería a sembrarlas en la siguiente lectura.
/// </summary>
public class Sesion
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreadaEn { get; set; }
}
