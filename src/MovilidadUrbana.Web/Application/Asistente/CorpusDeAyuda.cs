using System.Reflection;
using System.Text;
using MovilidadUrbana.Web.Application.Encuestas;
using MovilidadUrbana.Web.Application.Localidades;
using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Un fragmento recuperable: una sección <c>##</c> de una página de ayuda.</summary>
public sealed record FragmentoDeAyuda(
    string Id,
    string Pagina,
    string TituloDePagina,
    string Seccion,
    string Contexto,
    Anillo Anillo,
    string Estado,
    IReadOnlyList<string> Preguntas,
    IReadOnlyList<string> Destinos,
    string Texto)
{
    public string Titulo => $"{TituloDePagina} › {Seccion}";
}

/// <summary>
/// El corpus de ayuda, «docs as code»: páginas Markdown embebidas en el ensamblado
/// (<c>Ayuda/*.md</c>) más dos páginas que se <b>generan</b> desde <see cref="LocalidadPolicy" />
/// y <see cref="EncuestaPolicy" />, y el glosario, que sale de <see cref="Glosario" />. Si cambia
/// una regla, cambia la ayuda: los requisitos no se copian a mano.
/// </summary>
public sealed class CorpusDeAyuda
{
    public const string Version = "corpus.v1";
    public const int LargoMaximoDelFragmento = 500;

    public IReadOnlyList<FragmentoDeAyuda> Fragmentos { get; }

    public CorpusDeAyuda() : this(LeerEmbebidas().Concat(Generadas())) { }

    public CorpusDeAyuda(IEnumerable<(string Nombre, string Contenido)> paginas)
    {
        Fragmentos = paginas.SelectMany(p => Trocear(p.Nombre, p.Contenido)).ToList();
    }

    /// <summary>Sólo los fragmentos vigentes son candidatos (el filtro va antes del top-k).</summary>
    public IEnumerable<FragmentoDeAyuda> Vigentes => Fragmentos.Where(f => f.Estado == "vigente");

    public static IEnumerable<(string Nombre, string Contenido)> LeerEmbebidas()
    {
        var ensamblado = typeof(CorpusDeAyuda).Assembly;
        foreach (var recurso in ensamblado.GetManifestResourceNames().Where(r => r.Contains(".Ayuda.", StringComparison.Ordinal) && r.EndsWith(".md", StringComparison.Ordinal)).Order())
        {
            using var flujo = ensamblado.GetManifestResourceStream(recurso)!;
            using var lector = new StreamReader(flujo, Encoding.UTF8);
            var nombre = recurso[(recurso.IndexOf(".Ayuda.", StringComparison.Ordinal) + 7)..^3];
            yield return (nombre, lector.ReadToEnd());
        }
    }

    public static IEnumerable<(string Nombre, string Contenido)> Generadas()
    {
        yield return ("requisitos-de-la-localidad", $"""
            ---
            titulo: Requisitos de la localidad
            contexto: localidades
            anillo: 1
            estado: vigente
            preguntas: ¿Por qué no me acepta el código postal? | ¿Cuántos dígitos lleva el código postal? | ¿Puedo repetir el nombre? | ¿Qué provincia puedo poner?
            destinos: localidades.alta
            ---
            ## Nombre
            El nombre de la localidad: {LocalidadPolicy.RequisitoDelNombre}
            ## Provincia
            La provincia de la localidad: {LocalidadPolicy.RequisitoDeLaProvincia}
            ## Código postal
            El código postal: {LocalidadPolicy.RequisitoDelCodigoPostal} Es el código postal clásico de cuatro dígitos, no el CPA de ocho caracteres.
            ## Habitantes
            Los habitantes de la localidad: {LocalidadPolicy.RequisitoDeLosHabitantes}
            """);

        yield return ("requisitos-de-la-encuesta", $"""
            ---
            titulo: Requisitos de la encuesta
            contexto: encuesta
            anillo: 1
            estado: vigente
            preguntas: ¿Por qué no me deja seguir? | ¿Qué edad se acepta? | ¿Cuántos minutos puedo poner? | ¿Cuántos kilómetros puedo poner?
            destinos: encuesta.inicio
            ---
            ## Nombre
            El nombre y apellido de quien responde: {EncuestaPolicy.RequisitoDelNombre}
            ## Edad
            La edad de quien responde: {EncuestaPolicy.RequisitoDeLaEdad}
            ## Localidad
            La localidad donde vive: {EncuestaPolicy.RequisitoDeLaLocalidad}
            ## Medios
            Los medios de transporte que usa: {EncuestaPolicy.RequisitoDeLosMedios}
            ## Frecuencia
            La frecuencia de uso: {EncuestaPolicy.RequisitoDeLaFrecuencia}
            ## Distancia
            La distancia diaria: {EncuestaPolicy.RequisitoDeLaDistancia}
            ## Minutos
            Los minutos de viaje: {EncuestaPolicy.RequisitoDeLosMinutos}
            ## Motivo
            El motivo del viaje: {EncuestaPolicy.RequisitoDelMotivo}
            """);

        var glosario = new StringBuilder("""
            ---
            titulo: Glosario
            contexto:
            anillo: 2
            estado: vigente
            preguntas: ¿Qué es la frecuencia? | ¿Qué es una sesión? | ¿Qué significa multirrespuesta?
            destinos: inicio
            ---

            """);
        foreach (var e in Glosario.Entradas) glosario.Append("## ").AppendLine(e.Termino).AppendLine(e.Definicion);
        yield return ("glosario", glosario.ToString());
    }

    private static IEnumerable<FragmentoDeAyuda> Trocear(string pagina, string contenido)
    {
        var lineas = contenido.Replace("\r", string.Empty).Split('\n').Select(l => l.Trim()).ToList();
        var meta = new Dictionary<string, string>(StringComparer.Ordinal);
        var inicio = 0;
        if (lineas.Count > 0 && lineas[0] == "---")
        {
            var fin = lineas.IndexOf("---", 1);
            foreach (var l in lineas[1..fin])
            {
                var dos = l.IndexOf(':');
                if (dos > 0) meta[l[..dos].Trim()] = l[(dos + 1)..].Trim();
            }
            inicio = fin + 1;
        }

        string Meta(string clave, string defecto = "") => meta.TryGetValue(clave, out var v) ? v : defecto;
        var titulo = Meta("titulo", pagina);
        var anillo = (Anillo)int.Parse(Meta("anillo", "1"), System.Globalization.CultureInfo.InvariantCulture);
        var preguntas = Meta("preguntas").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var destinos = Meta("destinos").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? seccion = null;
        var cuerpo = new StringBuilder();
        IEnumerable<FragmentoDeAyuda> Cerrar()
        {
            if (seccion is null) yield break;
            var texto = cuerpo.ToString().Trim();
            if (texto.Length > LargoMaximoDelFragmento) texto = texto[..LargoMaximoDelFragmento];
            var id = $"{pagina}#{Texto.Normalizar(seccion).Replace(' ', '-')}";
            yield return new FragmentoDeAyuda(id, pagina, titulo, seccion, Meta("contexto"), anillo, Meta("estado", "borrador"), preguntas, destinos, texto);
        }

        foreach (var l in lineas.Skip(inicio))
        {
            if (l.StartsWith("## ", StringComparison.Ordinal))
            {
                foreach (var f in Cerrar()) yield return f;
                seccion = l[3..].Trim();
                cuerpo.Clear();
            }
            else if (seccion is not null && l.Length > 0 && !l.StartsWith('#'))
            {
                cuerpo.Append(l).Append(' ');
            }
        }
        foreach (var f in Cerrar()) yield return f;
    }
}
