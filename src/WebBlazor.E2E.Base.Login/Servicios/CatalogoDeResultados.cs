using WebBlazor.E2E.Base.Login.Theme;

namespace WebBlazor.E2E.Base.Login.Servicios;

/// <summary>
/// Catálogo de códigos de resultado de la identidad. Los mensajes que ve la persona
/// salen de acá y de ningún otro lado: un código sin entrada cae en el mensaje
/// genérico, nunca en el código crudo ni en la traza.
/// </summary>
/// <remarks>
/// El rechazo de credenciales es indiferenciado por diseño: distinguir
/// «identificador inexistente» de «secreto incorrecto» le confirma la existencia
/// de la identidad a quien no debería saberlo.
/// </remarks>
public static class CatalogoDeResultados
{
    /// <summary>Las credenciales no fueron aceptadas. No dice cuál de las dos falló.</summary>
    public const string CredencialesRechazadas = "credenciales-rechazadas";

    /// <summary>La persona cerró la sesión.</summary>
    public const string SesionCerrada = "sesion-cerrada";

    /// <summary>La superficie pedida exige sesión.</summary>
    public const string SesionRequerida = "sesion-requerida";

    /// <summary>Lo enviado no alcanza para intentar el ingreso.</summary>
    public const string DatosIncompletos = "datos-incompletos";

    private static readonly Dictionary<string, Entrada> Entradas = new(StringComparer.Ordinal)
    {
        [CredencialesRechazadas] = new("No pudimos validar el ingreso. Revisá el identificador y el secreto.", Tono.Peligro),
        [SesionCerrada] = new("Cerraste la sesión.", Tono.Exito),
        [SesionRequerida] = new("Ingresá para ver esa superficie.", Tono.Info),
        [DatosIncompletos] = new("Faltan datos para intentar el ingreso.", Tono.Peligro)
    };

    private static readonly Entrada Generica =
        new("No pudimos completar la operación. Volvé a intentarlo.", Tono.Peligro);

    /// <summary>Texto que se le muestra a la persona para el código dado.</summary>
    public static string TextoDe(string? codigo) => Buscar(codigo).Texto;

    /// <summary>Tono con el que se pinta la banda del código dado.</summary>
    public static Tono TonoDe(string? codigo) => Buscar(codigo).Tono;

    private static Entrada Buscar(string? codigo) =>
        codigo is not null && Entradas.TryGetValue(codigo, out var entrada) ? entrada : Generica;

    private sealed record Entrada(string Texto, Tono Tono);
}
