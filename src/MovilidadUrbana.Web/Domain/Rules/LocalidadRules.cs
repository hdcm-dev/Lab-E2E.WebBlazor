using System.Text.RegularExpressions;

namespace MovilidadUrbana.Web.Domain.Rules;

/// <summary>
/// Reglas de negocio del ABM de localidades. Viven en el dominio, no en atributos del modelo
/// de pantalla, para que la validación no dependa de la interfaz que la invoque.
/// </summary>
public static partial class LocalidadRules
{
    public const int LargoMinimoDelNombre = 3;
    public const int LargoMaximoDelNombre = 60;
    public const int HabitantesMinimos = 1;

    /// <summary>Dígitos exactos del código postal. El patrón de <see cref="CodigoPostal" /> lo espeja.</summary>
    public const int DigitosDelCodigoPostal = 4;

    // Endurecida a causa de la capa de IA (la propuesta del extractor pasa por acá): antes sólo
    // se aplicaba el mínimo, y el máximo declarado quedaba librado al `maxlength` de la vista.
    public static bool NombreValido(string? nombre) =>
        (nombre ?? string.Empty).Trim().Length is >= LargoMinimoDelNombre and <= LargoMaximoDelNombre;

    /// <summary>La provincia tiene que ser una del catálogo, escrita como el catálogo la escribe.</summary>
    public static bool ProvinciaValida(string? provincia) =>
        !string.IsNullOrWhiteSpace(provincia) && Catalogos.Provincias.Contains(provincia, StringComparer.Ordinal);

    public static bool CodigoPostalValido(string? codigoPostal) =>
        CodigoPostal().IsMatch((codigoPostal ?? string.Empty).Trim());

    public static bool HabitantesValidos(int? habitantes) =>
        habitantes is not null && habitantes >= HabitantesMinimos;

    /// <summary>Dos localidades son la misma si comparten nombre —sin distinguir mayúsculas— y provincia.</summary>
    public static bool MismaLocalidad(string nombreA, string provinciaA, string nombreB, string provinciaB) =>
        string.Equals(nombreA.Trim(), nombreB.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(provinciaA, provinciaB, StringComparison.Ordinal);

    // `[0-9]` y no `\d`: `\d` acepta dígitos de cualquier escritura Unicode («٣٤٠٠»).
    [GeneratedRegex(@"^[0-9]{4}$")]
    private static partial Regex CodigoPostal();
}
