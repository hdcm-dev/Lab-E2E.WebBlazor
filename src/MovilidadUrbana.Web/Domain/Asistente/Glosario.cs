namespace MovilidadUrbana.Web.Domain.Asistente;

/// <summary>
/// Glosario del dominio como datos versionados (anillo 2). Cada entrada lleva su definición
/// corta, de dónde sale y a qué pantalla del anillo 1 lleva después de explicar.
/// </summary>
public static class Glosario
{
    public const string Version = "glosario.v2";

    public static readonly IReadOnlyList<EntradaDelGlosario> Entradas =
    [
        new("localidad", "Localidad", ["localidad", "ciudad"], "Una ciudad o pueblo que cargás en el ABM, con su provincia, su código postal y sus habitantes.", "LocalidadRules", "localidades.lista"),
        new("provincia", "Provincia", ["provincia"], "Una de las siete provincias del catálogo fijo de la aplicación.", "Catalogos.Provincias", "localidades.lista"),
        new("codigo_postal", "Código postal", ["codigo postal"], "Cuatro dígitos, el código postal clásico; no es el CPA de ocho caracteres.", "LocalidadRules.DigitosDelCodigoPostal", "localidades.alta"),
        new("habitantes", "Habitantes", ["habitantes"], "La cantidad que declarás al cargar la localidad; no es un dato oficial.", "LocalidadRules.HabitantesMinimos", "localidades.lista"),
        new("medio", "Medio de transporte", ["medio", "medios", "transporte"], "Uno de los seis medios de la encuesta: colectivo, auto particular, bicicleta, moto, a pie y tren.", "Catalogos.Medios", "encuesta.inicio"),
        new("multirrespuesta", "Multirrespuesta", ["multirrespuesta", "marcar dos", "marcar varios", "dos medios", "varios medios"], "En el paso 2 podés marcar todos los medios que usás; por eso los conteos de medios pueden sumar más que el total de encuestas.", "EncuestaPolicy.RequisitoDeLosMedios", "encuesta.inicio"),
        new("frecuencia", "Frecuencia", ["frecuencia", "semanal", "diaria", "ocasional"], "Con qué asiduidad usás esos medios: todos los días, algunos días por semana u ocasionalmente.", "Catalogos.Frecuencias", "encuesta.inicio"),
        new("motivo", "Motivo", ["motivo"], "El motivo principal del viaje: trabajo, estudio, salud u otros. Se elige uno solo.", "Catalogos.Motivos", "encuesta.inicio"),
        new("distancia", "Distancia diaria", ["distancia", "kilometros", "km"], "Los kilómetros que recorrés por día, entre 0 y 500.", "EncuestaRules.DistanciaMaxima", "encuesta.inicio"),
        new("minutos", "Minutos de viaje", ["minutos"], "El tiempo de viaje, entre 1 y 600 minutos.", "EncuestaRules.MinutosMaximos", "encuesta.inicio"),
        new("encuesta", "Encuesta", ["encuesta"], "El formulario de tres pasos: datos de la persona, medios y distancia.", "EncuestaRules.TotalDePasos", "encuesta.inicio"),
        new("abm", "ABM", ["abm"], "Alta, baja y modificación: la pantalla de localidades.", "Localidades", "localidades.lista"),
        new("sesion", "Sesión", ["sesion", "companero", "compartir", "comparten", "otro"], "Cada navegador tiene su propia sesión por cookie: ves sólo lo que cargaste vos, y nadie ve lo tuyo.", "SessionMiddleware", "inicio"),
        new("siembra", "Siembra", ["siembra", "sembradas"], "Las dos localidades con las que arranca cada sesión: Corrientes y Resistencia.", "SessionSeeder", "localidades.lista"),
        new("asistente_de_pasos", "Asistente de pasos", ["pasos"], "La encuesta avanza de a un paso y no te deja seguir mientras el paso tenga errores.", "EncuestaService.ValidarPaso", "encuesta.inicio"),
        new("unico_por_provincia", "Único por provincia", ["unico por provincia", "repetir el nombre", "nombre repetido"], "No puede haber dos localidades con el mismo nombre en la misma provincia; en provincias distintas, sí.", "LocalidadRules.MismaLocalidad", "localidades.lista"),
        new("promedio", "Promedio", ["promedio"], "La suma dividida por la cantidad de respuestas; con pocas respuestas es sólo descriptivo.", "ResultadosDeEncuestaService", "resultados.resumen"),
        new("mediana", "Mediana", ["mediana"], "El valor del medio cuando ordenás las respuestas; un valor extremo la mueve menos que al promedio.", "ResultadosDeEncuestaService", "resultados.resumen")
    ];

    /// <summary>La entrada cuyo término aparece en la pregunta normalizada, o null.</summary>
    public static EntradaDelGlosario? Buscar(PreguntaNormalizada pregunta) =>
        Entradas
            .SelectMany(entrada => entrada.Formas.Select(forma => (entrada, forma)))
            .Where(par => pregunta.Tiene(par.forma))
            .OrderByDescending(par => par.forma.Length)
            .Select(par => par.entrada)
            .FirstOrDefault();
}

public sealed record EntradaDelGlosario(
    string Id,
    string Termino,
    IReadOnlyList<string> Formas,
    string Definicion,
    string Fuente,
    string Destino);
