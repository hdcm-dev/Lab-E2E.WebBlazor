using System.Globalization;

namespace MovilidadUrbana.MAUI.Presentacion.Localidades;

/// <summary>Una fila de la lista de localidades, con sus textos ya formateados.</summary>
public sealed record LocalidadItem(int Id, string Nombre, string Provincia, string CodigoPostal, int Habitantes)
{
    public string Detalle => $"{Provincia} · CP {CodigoPostal}";

    public string HabitantesTexto => string.Format(CultureInfo.CurrentCulture, "{0:N0} hab.", Habitantes);
}
