using System.Globalization;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>
/// Textos fijos de las formas F1–F8, en voseo y en segunda persona. Son la línea base: sin
/// modelo, el asistente responde con estas plantillas y las cifras del código. Ninguna dice que
/// el asistente hizo algo: lleva a la acción, y la acción la confirma la persona.
/// </summary>
public static class Plantillas
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public static string N(double valor) => valor % 1 == 0 ? valor.ToString("N0", Cultura) : valor.ToString("N1", Cultura);

    public const string NoEstaEnLaAyuda = "No está en la ayuda de Movilidad Urbana.";
    public const string NoEncuentroLocalidad = "No encuentro esa localidad en tu sesión.";
    public const string SinEncuestas = "Todavía no hay encuestas en tu sesión.";
    public const string NChico = "Con menos de 5 respuestas, la cifra es sólo descriptiva.";
    public const string PrimeroUnaLocalidad = "Para cargar una encuesta, primero cargá una localidad: el paso 1 te pide elegirla de las que tenés en el ABM.";
    public const string PreguntaLarga = "Escribí una pregunta más corta, de hasta 300 caracteres.";
    public const string Ocupado = "La ayuda con IA está ocupada con otra consulta: te muestro la respuesta fija.";

    public static string Afuera(string? motivo) => motivo switch
    {
        "horarios" => "No tengo horarios ni recorridos de colectivos: la aplicación sólo conoce lo que cargás en tu sesión.",
        "clima" => "No tengo información del clima. Acá te ayudo con las localidades, la encuesta y sus resultados.",
        "tendencia" => "La encuesta no guarda una serie en el tiempo, así que no hay con qué comparar el año pasado.",
        "oficial" => "No tengo estadísticas ni datos oficiales: sólo las cifras de las encuestas de tu sesión.",
        "datos_personales" => "No se muestran datos de las personas encuestadas, ni nombres ni edades: sólo cifras agregadas.",
        _ => "Eso queda fuera de lo que responde este asistente. Acá te ayudo con las localidades, la encuesta y sus resultados."
    };

    public static readonly IReadOnlyList<string> SugerenciasDeAfuera =
    [
        "¿Qué medio usa más la gente?",
        "¿Cómo doy de alta una localidad?",
        "¿Cuántas localidades tengo?"
    ];

    public static string FueraDeCatalogo(IEnumerable<string> formas) =>
        $"{Capitalizar(string.Join(" y ", formas))} no está entre los 6 medios de la encuesta: colectivo, auto particular, bicicleta, moto, a pie y tren.";

    public static string Aviso(MotivoDeHabilitacion motivo, string? clave) => motivo switch
    {
        MotivoDeHabilitacion.Habilitada => string.Empty,
        MotivoDeHabilitacion.ApagadaPorParametro => "La ayuda con IA está apagada por configuración. Las cifras y los destinos siguen funcionando.",
        MotivoDeHabilitacion.ClaveDesconocida => $"La ayuda con IA está apagada: la clave {clave} no es una de las que se conocen. Las cifras y los destinos siguen funcionando.",
        MotivoDeHabilitacion.SinRespuesta => "La ayuda con IA no respondió. Las cifras y los destinos siguen funcionando.",
        _ => $"La ayuda con IA está apagada: falta configurar {clave}. Las cifras y los destinos siguen funcionando."
    };

    public static string Capitalizar(string texto) => texto.Length == 0 ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];

    /// <summary>Sugerencias por pantalla y persona (REQ-IA-32): todas realizables en la sesión.</summary>
    public static IReadOnlyList<string> SugerenciasPara(string? ruta)
    {
        var r = (ruta ?? string.Empty).Split('?')[0].Trim('/');
        return r switch
        {
            "localidades" => ["¿Cómo doy de alta una localidad?", "¿Por qué no me acepta el código postal?", "Borrá Resistencia"],
            var e when e.StartsWith("encuesta", StringComparison.Ordinal) => ["¿Qué cuenta como frecuencia semanal?", "¿Puedo marcar dos medios?", "¿Qué edad tengo que tener para responder?"],
            "resultados" => ["¿Qué medio usa más la gente?", "¿Por qué viaja la gente?", "¿Por qué los medios no suman 100 %?"],
            _ => ["¿Qué puedo hacer acá?", "¿Qué es una sesión?", "¿Cómo cargo una localidad por la API?"]
        };
    }

    /// <summary>El contexto de la ayuda que corresponde a la pantalla («Sobre: Localidades»).</summary>
    public static (string Contexto, string Rotulo)? ContextoDe(string? ruta)
    {
        var r = (ruta ?? string.Empty).Split('?')[0].Trim('/');
        if (r == "localidades") return ("localidades", "Localidades");
        if (r.StartsWith("encuesta", StringComparison.Ordinal)) return ("encuesta", "Encuesta");
        if (r == "resultados") return ("resultados", "Resultados");
        return null;
    }

    /// <summary>
    /// Nota de multirrespuesta según lo que muestra la respuesta: con porcentajes, «no suman 100 %»;
    /// con conteos («4 de 20»), que los conteos pueden sumar más que las encuestas (B17, B42).
    /// </summary>
    public static string NotaDeMultirrespuesta(bool conPorcentajes) => conPorcentajes ? NotaConPorcentajes : NotaConConteos;

    public const string NotaConConteos = "Una persona puede marcar varios medios, así que los conteos pueden sumar más que el total de encuestas.";

    public const string NotaConPorcentajes = "Una persona puede marcar varios medios, así que los porcentajes no suman 100 %.";
}
