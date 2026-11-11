using System.Text;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Ids del catálogo de destinos. Un chip sólo puede apuntar a uno de estos.</summary>
public static class Destinos
{
    public const string Inicio = "inicio";
    public const string LocalidadesLista = "localidades.lista";
    public const string LocalidadesAlta = "localidades.alta";
    public const string LocalidadesBaja = "localidades.baja";
    public const string EncuestaInicio = "encuesta.inicio";
    public const string ResultadosResumen = "resultados.resumen";
    public const string ResultadosMedios = "resultados.medios";
    public const string ResultadosMotivos = "resultados.motivos";
    public const string Medicion = "asistente.medicion";
}

/// <summary>
/// Un destino tipado (K6 §14): plantilla relativa a una ruta <c>@page</c> real, consulta fija y
/// los parámetros que el código puede completar. Ninguna URL sale del texto del modelo.
/// </summary>
public sealed record DefinicionDeDestino(
    string Id,
    string Rotulo,
    string Plantilla,
    IReadOnlyDictionary<string, string> Consulta,
    IReadOnlyList<string> Parametros,
    IReadOnlyList<string> Claves);

/// <summary>Un chip listo para mostrar: id del catálogo, rótulo y href ya resuelto por el código.</summary>
public sealed record Chip(string DestinoId, string Rotulo, string Href);

/// <summary>
/// Registro del catálogo. Se valida al arrancar contra las rutas reales: un catálogo inválido
/// corta el arranque en vez de ofrecer un chip que no abre lo que promete.
/// </summary>
public sealed class CatalogoDeDestinos
{
    private static readonly IReadOnlyDictionary<string, string> SinConsulta = new Dictionary<string, string>();

    public static readonly IReadOnlyList<DefinicionDeDestino> Definiciones =
    [
        new(Destinos.Inicio, "Ir al inicio", "", SinConsulta, [], ["inicio", "principal", "home"]),
        new(Destinos.LocalidadesLista, "Abrir localidades", "localidades", SinConsulta, ["provincia"], ["localidades", "abm", "lista", "ciudades"]),
        new(Destinos.LocalidadesAlta, "Dar de alta una localidad", "localidades", new Dictionary<string, string> { ["alta"] = "1" },
            ["nombre", "provincia", "cp", "habitantes", "propuesta"], ["alta", "nueva", "agregar localidad", "cargar localidad"]),
        new(Destinos.LocalidadesBaja, "Eliminar una localidad", "localidades", SinConsulta, ["baja"], ["baja", "borrar", "eliminar"]),
        new(Destinos.EncuestaInicio, "Empezar una encuesta", "encuesta/1", SinConsulta, [], ["encuesta", "cargar encuesta", "responder"]),
        new(Destinos.ResultadosResumen, "Ver resultados", "resultados", SinConsulta, ["vista"], ["resultados", "estadisticas", "graficos"]),
        new(Destinos.ResultadosMedios, "Ver medios más usados", "resultados", new Dictionary<string, string> { ["vista"] = "medios" }, [], ["medios", "transporte"]),
        new(Destinos.ResultadosMotivos, "Ver motivos de viaje", "resultados", new Dictionary<string, string> { ["vista"] = "motivos" }, [], ["motivos"]),
        new(Destinos.Medicion, "Ver la medición del asistente", "asistente/medicion", SinConsulta, [], ["medicion", "votos"])
    ];

    private readonly Dictionary<string, DefinicionDeDestino> _porId;

    public CatalogoDeDestinos() : this(Definiciones) { }

    public CatalogoDeDestinos(IReadOnlyList<DefinicionDeDestino> definiciones)
    {
        _porId = new Dictionary<string, DefinicionDeDestino>(StringComparer.Ordinal);
        foreach (var d in definiciones)
        {
            if (!_porId.TryAdd(d.Id, d)) throw new InvalidOperationException($"Destino duplicado: {d.Id}.");
            if (d.Plantilla.StartsWith('/') || d.Plantilla.Contains("//", StringComparison.Ordinal) ||
                d.Plantilla.Contains("http", StringComparison.OrdinalIgnoreCase) || d.Plantilla.Contains(':'))
            {
                throw new InvalidOperationException($"La plantilla del destino {d.Id} no es una ruta relativa: «{d.Plantilla}».");
            }
        }
    }

    public IEnumerable<DefinicionDeDestino> Todos => _porId.Values;

    public bool Existe(string id) => _porId.ContainsKey(id);

    public DefinicionDeDestino Obtener(string id) =>
        _porId.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException($"Destino desconocido: {id}.");

    /// <summary>
    /// Valida el catálogo contra las plantillas de ruta reales (las <c>@page</c>). Lo invoca el
    /// host al arrancar; si falla, la aplicación no levanta.
    /// </summary>
    public void ValidarContra(IEnumerable<string> plantillasDeRuta)
    {
        var rutas = plantillasDeRuta.Select(r => r.Trim('/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var d in _porId.Values)
        {
            var ruta = d.Plantilla;
            var coincide = rutas.Contains(ruta) || rutas.Any(r => CoincideConParametros(r, ruta));
            if (!coincide) throw new InvalidOperationException($"El destino {d.Id} apunta a «{ruta}», que no es una ruta de la aplicación.");
        }
    }

    /// <summary>Arma el chip: sólo con los parámetros que el destino declara; el resto se descarta.</summary>
    public Chip Resolver(string id, IReadOnlyDictionary<string, string?>? parametros = null, string? rotulo = null)
    {
        var d = Obtener(id);
        var consulta = new List<KeyValuePair<string, string>>(d.Consulta);
        if (parametros is not null)
        {
            foreach (var (clave, valor) in parametros)
            {
                if (string.IsNullOrWhiteSpace(valor) || !d.Parametros.Contains(clave)) continue;
                consulta.Add(new(clave, valor));
            }
        }

        var href = new StringBuilder(d.Plantilla);
        for (var i = 0; i < consulta.Count; i++)
        {
            href.Append(i == 0 ? '?' : '&')
                .Append(Uri.EscapeDataString(consulta[i].Key)).Append('=').Append(Uri.EscapeDataString(consulta[i].Value));
        }

        var final = href.ToString();
        return new Chip(id, rotulo ?? d.Rotulo, final.Length == 0 ? "./" : final);
    }

    /// <summary>El destino cuyas claves coinciden con palabras normalizadas («llevame a localidades»).</summary>
    public DefinicionDeDestino? PorClaves(string preguntaNormalizada)
    {
        var texto = " " + preguntaNormalizada + " ";
        return _porId.Values
            .SelectMany(d => d.Claves.Select(c => (d, c)))
            .Where(par => texto.Contains(" " + par.c + " ", StringComparison.Ordinal))
            .OrderByDescending(par => par.c.Length)
            .Select(par => par.d)
            .FirstOrDefault();
    }

    private static bool CoincideConParametros(string ruta, string plantilla)
    {
        var a = ruta.Split('/');
        var b = plantilla.Split('/');
        if (a.Length != b.Length) return false;
        return a.Zip(b).All(p => string.Equals(p.First, p.Second, StringComparison.OrdinalIgnoreCase) ||
                                 (p.First.StartsWith('{') && p.First.EndsWith('}')));
    }
}
