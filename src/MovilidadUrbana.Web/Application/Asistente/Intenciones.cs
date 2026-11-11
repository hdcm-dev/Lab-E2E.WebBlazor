namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Catálogo cerrado de intenciones del enrutador. El voto sólo guarda uno de estos ids.</summary>
public static class Intenciones
{
    public const string AltaComo = "localidad.alta.como";
    public const string AltaDesdeFrase = "localidad.alta.desde_frase";
    public const string Baja = "localidad.baja";
    public const string ContarLocalidades = "contar_localidades";
    public const string Habitantes = "habitantes_por_provincia";
    public const string ContarEncuestas = "contar_encuestas";
    public const string Medios = "distribucion_medios";
    public const string Motivos = "distribucion_motivos";
    public const string Frecuencias = "distribucion_frecuencias";
    public const string Promedios = "promedios_viaje";
    public const string FueraDeCatalogo = "fuera_de_catalogo";
    public const string EncuestaComo = "encuesta.como";
    public const string RequisitoCodigoPostal = "requisito.localidad.cp";
    public const string RequisitoEdad = "requisito.encuesta.edad";
    public const string Ayuda = "ayuda";
    public const string Glosario = "glosario";
    public const string DerivacionApi = "derivacion.api";
    public const string DerivacionPruebas = "derivacion.pruebas";
    public const string Navegacion = "navegacion";
    public const string Ambigua = "aclaracion";
    public const string Afuera = "afuera";
    public const string DatosPersonales = "afuera.datos_personales";
    public const string Vacia = "vacia";
    public const string Larga = "larga";

    /// <summary>
    /// Las únicas intenciones que pasan por el redactor con modelo: las distribuciones de cifras
    /// (medios, motivos y frecuencias), donde la redacción ordena varias categorías en una frase. En
    /// la lectura a ciegas de la ronda 2 el texto del modelo fue peor que la plantilla en el glosario,
    /// el «cómo», los promedios y el conteo (B09, B14, B24, B25, B26): ahí queda la plantilla fija.
    /// </summary>
    public static readonly IReadOnlySet<string> ConRedaccion = new HashSet<string>([Medios, Motivos, Frecuencias], StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> Todas = typeof(Intenciones)
        .GetFields()
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);
}
