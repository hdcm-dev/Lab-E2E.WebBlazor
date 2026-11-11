using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Resultado del ruteo: intención y anillo. Lo decide el código (ARQ-1, F-1).</summary>
public sealed record Ruteo(string Intencion, Anillo Anillo, string? MotivoDeAfuera = null);

/// <summary>
/// Enrutador determinista (REQ-IA-24) y clasificador de anillo en una sola función pura: recibe
/// la pregunta ya normalizada por <see cref="Sinonimos" /> y devuelve la intención. El orden de
/// las reglas es parte del contrato: primero lo que nunca se contesta (datos personales, temas
/// de afuera), después la navegación, la derivación, las acciones, el glosario, los requisitos,
/// las cifras y por último la ayuda general. Lo que no reconoce cae a Afuera, nunca al anillo 1.
/// </summary>
public static class Enrutador
{
    public static Ruteo Clasificar(PreguntaNormalizada p)
    {
        if (p.Texto.Length == 0) return new(Intenciones.Vacia, Anillo.Afuera);

        // 1. Datos personales: no se muestran nunca, se pidan como se pidan.
        if (p.TieneAlguna("edad de", "la edad de", "nombre de la persona", "nombres de las personas", "como se llama", "edades", "quien respondio", "quienes respondieron", "datos de las personas", "datos personales") ||
            System.Text.RegularExpressions.Regex.IsMatch(p.Texto, @"\bpersona \d+\b"))
        {
            return new(Intenciones.DatosPersonales, Anillo.Afuera, "datos_personales");
        }

        // 2. Afuera por tema.
        // «pasá por alto» es una consigna (B46), no un colectivo que pasa por un lugar.
        if (p.TieneAlguna("hora", "horario", "horarios", "a que hora", "recorrido", "recorridos", "parada", "paradas", "frecuencia del", "pasa el")
            || (p.TieneAlguna("pasa por") && !p.TieneAlguna("pasa por alto")))
            return new(Intenciones.Afuera, Anillo.Afuera, "horarios");
        if (p.TieneAlguna("clima", "llueve", "lluvia", "llover", "temperatura", "pronostico", "manana llueve"))
            return new(Intenciones.Afuera, Anillo.Afuera, "clima");
        if (p.TieneAlguna("aumento", "aumentar", "disminuyo", "tendencia", "ano pasado", "el ano", "evolucion", "historico", "comparado con"))
            return new(Intenciones.Afuera, Anillo.Afuera, "tendencia");
        if (p.TieneAlguna("oficial", "oficiales", "indec", "censo", "politica", "politicas", "gobierno", "intendente", "tarifa", "boleto", "precio", "sube"))
            return new(Intenciones.Afuera, Anillo.Afuera, "oficial");

        // 3. Navegación explícita: sólo por claves del catálogo; una ruta escrita nunca es un destino.
        if (p.Empieza("llevame", "llevame a", "ir a", "abri", "abrime", "mostrame la pantalla", "navega", "anda a"))
            return new(Intenciones.Navegacion, Anillo.Operar);

        // 4. Lo de al lado: la API y las pruebas del repositorio.
        if (p.TieneAlguna("api", "apis", "graphql", "rest", "scalar", "swagger", "openapi", "endpoint", "postman", "curl", "json"))
            return new(Intenciones.DerivacionApi, Anillo.LoDeAlLado);
        if (p.TieneAlguna("prueba", "pruebas", "test", "tests", "e2e", "playwright", "bunit", "unitarias"))
            return new(Intenciones.DerivacionPruebas, Anillo.LoDeAlLado);

        var comoOPregunta = p.TieneAlguna("como", "donde", "cual", "que");

        // 5. Acciones desde una frase. El asistente no escribe: lleva a la acción prellenada.
        if (!comoOPregunta && !p.Tiene("encuesta") &&
            p.Empieza("agrega", "agregame", "agregar", "carga", "cargame", "cargar", "crea", "suma", "da de alta", "dar de alta", "alta", "nueva localidad", "anota"))
            return new(Intenciones.AltaDesdeFrase, Anillo.Operar);
        if (!p.Tiene("como") && p.TieneAlguna("borra", "borrame", "borrar", "elimina", "eliminame", "eliminar", "da de baja", "dar de baja", "saca", "quita", "quitar"))
            return new(Intenciones.Baja, Anillo.Operar);

        // 6. Glosario (anillo 2): qué significa algo.
        if (p.Empieza("que es", "que son", "que significa", "que quiere decir", "que cuenta como", "que se entiende por", "definicion de") ||
            p.TieneAlguna("puedo marcar", "por que no veo", "no veo lo", "se comparten", "se comparte", "lo de mi companero", "ve lo mio", "unico por provincia"))
            return new(Intenciones.Glosario, Anillo.Dominio);

        // 7. Requisitos de las reglas.
        if (p.Tiene("codigo postal")) return new(Intenciones.RequisitoCodigoPostal, Anillo.Operar);
        if (p.Tiene("edad")) return new(Intenciones.RequisitoEdad, Anillo.Operar);

        // 8. Cargar una encuesta.
        var cifra = p.TieneAlguna("cuantas", "cuantos", "cuanto", "cantidad", "total", "porcentaje", "promedio", "mas", "menos");
        if (p.Tiene("encuesta") && !cifra && p.TieneAlguna("cargar", "cargo", "completar", "completo", "responder", "respondo", "hacer", "hago", "empezar", "empiezo", "quiero", "donde", "como", "llenar", "lleno"))
            return new(Intenciones.EncuestaComo, Anillo.Operar);

        // 9. Cifras de la sesión: el código elige la función.
        if (p.TieneAlguna("promedio", "promedios", "tarda", "tardan", "demora", "demoran", "mediana", "cuanto tiempo", "cuantos minutos", "cuantos kilometros", "distancia", "distancias", "lejos"))
            return new(Intenciones.Promedios, Anillo.Operar);
        if (p.TieneAlguna("frecuencia", "frecuencias", "seguido", "cada cuanto", "diaria", "semanal", "ocasional"))
            return new(Intenciones.Frecuencias, Anillo.Operar);
        if (p.TieneAlguna("motivo", "motivos", "por que viaja", "por que viajan", "para que viaja", "para que viajan", "razon", "razones") ||
            (cifra && p.TieneAlguna("trabajo", "estudio", "salud")))
            return new(Intenciones.Motivos, Anillo.Operar);
        if (p.FueraDeCatalogo.Count > 0 && (cifra || p.TieneAlguna("van", "viaja", "viajan", "usa", "usan")))
            return new(Intenciones.FueraDeCatalogo, Anillo.Operar);
        if (p.TieneAlguna("medio", "medios", "transporte", "van en", "viajan en", "viaja en") ||
            (cifra && p.TieneAlguna("usa", "usan")) ||
            (p.Canonicos.Any(EsMedio) && (cifra || p.TieneAlguna("van", "viajan", "va"))))
            return new(Intenciones.Medios, Anillo.Operar);
        if ((p.Tiene("encuestas") || p.Tiene("encuesta")) && (cifra || p.TieneAlguna("hay", "tengo")))
            return new(Intenciones.ContarEncuestas, Anillo.Operar);
        if (p.Tiene("habitantes") || p.TieneAlguna("cuantos viven", "poblacion"))
            return new(Intenciones.Habitantes, Anillo.Operar);
        if (p.TieneAlguna("localidades", "localidad", "ciudades") && (cifra || p.TieneAlguna("dame", "mostrame", "lista", "listame", "tengo", "cargadas", "hay", "conta", "contame", "contar")))
            return new(Intenciones.ContarLocalidades, Anillo.Operar);
        if (cifra && p.TieneAlguna("hay", "tengo")) return new(Intenciones.ContarLocalidades, Anillo.Operar);

        // 10. Ayuda general para operar: «¿cómo…?», «¿dónde…?», «¿por qué…?».
        if (p.Empieza("como", "donde", "por que", "puedo", "se puede", "que hago", "que tengo", "que pasa", "cuantos pasos", "que puedo", "para que sirve", "ayuda", "ayudame") ||
            p.TieneAlguna("no me deja", "no me acepta", "no puedo", "error"))
            return new(Intenciones.Ayuda, Anillo.Operar);

        return new(Intenciones.Afuera, Anillo.Afuera, "general");
    }

    public static bool EsMedio(string clave) => Domain.Catalogos.Medios.Any(m => m.Clave == clave);
}
