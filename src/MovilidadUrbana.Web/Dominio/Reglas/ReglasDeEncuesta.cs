namespace MovilidadUrbana.Web.Dominio.Reglas;

/// <summary>Rangos aceptados por la encuesta de transporte, paso por paso.</summary>
public static class ReglasDeEncuesta
{
    public const int TotalDePasos = 3;

    public const int EdadMinima = 16;
    public const int EdadMaxima = 110;

    public const double DistanciaMinima = 0;
    public const double DistanciaMaxima = 500;

    public const int MinutosMinimos = 1;
    public const int MinutosMaximos = 600;

    public static bool NombreValido(string? nombre) => (nombre ?? string.Empty).Trim().Length >= 3;

    public static bool EdadValida(int? edad) => edad is >= EdadMinima and <= EdadMaxima;

    public static bool DistanciaValida(double? distancia) =>
        distancia is not null && distancia >= DistanciaMinima && distancia <= DistanciaMaxima;

    public static bool MinutosValidos(int? minutos) => minutos is >= MinutosMinimos and <= MinutosMaximos;
}
