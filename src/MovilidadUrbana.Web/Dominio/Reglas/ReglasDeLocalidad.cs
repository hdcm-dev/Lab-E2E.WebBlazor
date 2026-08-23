using System.Text.RegularExpressions;

namespace MovilidadUrbana.Web.Dominio.Reglas;

/// <summary>
/// Reglas de negocio del ABM de localidades. Viven en el dominio, no en atributos del modelo
/// de pantalla, para que la validación no dependa de la interfaz que la invoque.
/// </summary>
public static partial class ReglasDeLocalidad
{
    public const int LargoMinimoDelNombre = 3;
    public const int LargoMaximoDelNombre = 60;
    public const int HabitantesMinimos = 1;

    public static bool NombreValido(string? nombre) =>
        (nombre ?? string.Empty).Trim().Length >= LargoMinimoDelNombre;

    public static bool ProvinciaValida(string? provincia) => !string.IsNullOrWhiteSpace(provincia);

    public static bool CodigoPostalValido(string? codigoPostal) =>
        CodigoPostal().IsMatch((codigoPostal ?? string.Empty).Trim());

    public static bool HabitantesValidos(int? habitantes) =>
        habitantes is not null && habitantes >= HabitantesMinimos;

    /// <summary>Dos localidades son la misma si comparten nombre —sin distinguir mayúsculas— y provincia.</summary>
    public static bool MismaLocalidad(string nombreA, string provinciaA, string nombreB, string provinciaB) =>
        string.Equals(nombreA.Trim(), nombreB.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(provinciaA, provinciaB, StringComparison.Ordinal);

    [GeneratedRegex(@"^\d{4}$")]
    private static partial Regex CodigoPostal();
}
