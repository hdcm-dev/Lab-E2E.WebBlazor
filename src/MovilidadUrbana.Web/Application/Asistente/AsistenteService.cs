using Microsoft.Extensions.Logging;
using MovilidadUrbana.Web.Application.Resultados;
using MovilidadUrbana.Web.Domain;
using MovilidadUrbana.Web.Domain.Asistente;
using MovilidadUrbana.Web.Domain.Entities;
using MovilidadUrbana.Web.Domain.Rules;

namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>
/// El asistente: un pipeline de ocho pasos donde el código elige y el modelo, cuando está, sólo
/// redacta (K2 §3). 1 contexto de pantalla · 2 alcance (la sesión, y lo que la pregunta nombra) ·
/// 3 rol (la sesión) · 4 anillo e intención · 5 función o recuperación · 6 redacción · 7
/// validación (en el adaptador) · 8 destino revalidado en el turno.
/// No depende del lector de votos: lo que vota la gente no cambia lo que se responde (K2 §8.3).
/// </summary>
public sealed class AsistenteService(
    FuncionesDeLectura funciones,
    RecuperadorDeAyuda recuperador,
    CorpusDeAyuda corpus,
    CatalogoDeDestinos catalogo,
    IRedactorDeAyuda redactor,
    IExtractorDeLocalidad extractor,
    IRegistroDeSenales senales,
    IEstadoDelAsistente estado,
    TimeProvider reloj,
    ILogger<AsistenteService> registro)
{
    public async Task<RespuestaDelAsistente> ResponderAsync(string pregunta, string? rutaActual = null, CancellationToken cancellationToken = default)
    {
        var inicio = reloj.GetTimestamp();
        RespuestaDelAsistente respuesta;
        if (GuardaDeLargo.Excede(pregunta))
        {
            respuesta = Fija(Intenciones.Larga, Anillo.Afuera, Forma.F8, ResultadoDelTurno.Rechazado, Plantillas.PreguntaLarga);
        }
        else
        {
            var p = Sinonimos.Aplicar(pregunta);
            var ruteo = Enrutador.Clasificar(p);
            var contexto = Plantillas.ContextoDe(rutaActual)?.Contexto;
            respuesta = await ResponderAsync(pregunta, p, ruteo, contexto, cancellationToken);
        }

        if (estado.Motivo != MotivoDeHabilitacion.Habilitada)
        {
            respuesta = respuesta with { Aviso = Plantillas.Aviso(estado.Motivo, estado.Clave) };
        }

        // Telemetría sin contenido (REQ-IA-09): intención, anillo, forma, resultado y duración. Nunca la pregunta.
        registro.LogInformation("Turno del asistente: {Intencion} {Anillo} {Forma} {Resultado} {Milisegundos} ms",
            respuesta.Intencion, respuesta.Anillo, respuesta.Forma, respuesta.Resultado, (int)reloj.GetElapsedTime(inicio).TotalMilliseconds);

        if (respuesta.Forma is Forma.F5 or Forma.F7)
        {
            await RegistrarSenalAsync(respuesta, cancellationToken);
        }

        return respuesta;
    }

    /// <summary>La huella de un turno, sin texto: lo que guarda un voto o una señal.</summary>
    public Voto Huella(RespuestaDelAsistente r, OrigenDelVoto origen, bool util, MotivoDelVoto? motivo) => new()
    {
        Intencion = r.Intencion,
        Anillo = r.Anillo,
        Forma = r.Forma,
        Destino = r.Destinos.FirstOrDefault()?.DestinoId,
        Resultado = r.Resultado,
        Origen = origen,
        Util = util,
        Motivo = util ? null : motivo,
        VersionGlosario = Glosario.Version,
        VersionCorpus = CorpusDeAyuda.Version,
        Dia = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime)
    };

    public Task VotarAsync(RespuestaDelAsistente r, bool util, MotivoDelVoto? motivo, CancellationToken cancellationToken = default) =>
        senales.RegistrarAsync(Huella(r, OrigenDelVoto.Persona, util, motivo), cancellationToken);

    private async Task RegistrarSenalAsync(RespuestaDelAsistente r, CancellationToken cancellationToken)
    {
        try
        {
            await senales.RegistrarAsync(Huella(r, OrigenDelVoto.Senal, util: false, motivo: null), cancellationToken);
        }
        catch (Exception excepcion)
        {
            // La señal es medición: si falla, la respuesta sigue.
            registro.LogWarning(excepcion, "No se pudo registrar la señal del turno.");
        }
    }

    private async Task<RespuestaDelAsistente> ResponderAsync(string original, PreguntaNormalizada p, Ruteo ruteo, string? contexto, CancellationToken ct)
    {
        switch (ruteo.Intencion)
        {
            case Intenciones.Vacia:
                return Fija(ruteo.Intencion, Anillo.Afuera, Forma.F7, ResultadoDelTurno.Afuera, "Escribí una pregunta sobre las localidades, la encuesta o sus resultados.") with { Sugerencias = Plantillas.SugerenciasDeAfuera };
            case Intenciones.Afuera:
            case Intenciones.DatosPersonales:
                return Fija(ruteo.Intencion, Anillo.Afuera, Forma.F7, ResultadoDelTurno.Afuera, Plantillas.Afuera(ruteo.MotivoDeAfuera)) with { Sugerencias = Plantillas.SugerenciasDeAfuera };
            case Intenciones.Navegacion:
                return Navegar(p);
            case Intenciones.DerivacionApi:
            case Intenciones.DerivacionPruebas:
                return await AyudaAsync(p, ruteo, contexto, f => f.Anillo == Anillo.LoDeAlLado, [Destinos.LocalidadesLista], ct);
            case Intenciones.AltaDesdeFrase:
                return await AltaDesdeFraseAsync(original, ct);
            case Intenciones.Baja:
                return await BajaAsync(p, ct);
            case Intenciones.Glosario:
                return await GlosarioAsync(p, ruteo, ct);
            case Intenciones.RequisitoCodigoPostal:
                return await AyudaAsync(p, ruteo, contexto, f => f.Pagina == "requisitos-de-la-localidad", [Destinos.LocalidadesAlta], ct);
            case Intenciones.RequisitoEdad:
                return await AyudaAsync(p, ruteo, contexto, f => f.Pagina == "requisitos-de-la-encuesta", [Destinos.EncuestaInicio], ct);
            case Intenciones.EncuestaComo:
                return await EncuestaComoAsync(p, ruteo, contexto, ct);
            case Intenciones.ContarLocalidades:
            case Intenciones.Habitantes:
                return await LocalidadesAsync(p, ruteo, ct);
            case Intenciones.ContarEncuestas:
                return await EncuestasAsync(p, ct);
            case Intenciones.FueraDeCatalogo:
                return Fija(ruteo.Intencion, Anillo.Operar, Forma.F5, ResultadoDelTurno.Abstencion, Plantillas.FueraDeCatalogo(p.FueraDeCatalogo)) with { Destinos = [catalogo.Resolver(Destinos.ResultadosMedios)] };
            case Intenciones.Medios:
            case Intenciones.Motivos:
            case Intenciones.Frecuencias:
                return await DistribucionAsync(p, ruteo, ct);
            case Intenciones.Promedios:
                return await PromediosAsync(p, ct);
            default:
                var destinos = p.TieneAlguna("alta", "agrego", "agregar", "cargo", "cargar", "nueva") && p.TieneAlguna("localidad", "ciudad")
                    ? new[] { Destinos.LocalidadesAlta } : [];
                var intencion = destinos.Length > 0 ? Intenciones.AltaComo : Intenciones.Ayuda;
                return await AyudaAsync(p, ruteo with { Intencion = intencion }, contexto, null, destinos, ct);
        }
    }

    // ---- Anillo 1: navegación y acciones ------------------------------------------------------

    private RespuestaDelAsistente Navegar(PreguntaNormalizada p)
    {
        // Una ruta escrita («/admin») o una URL nunca se convierten en destino (K6 §18).
        var conRuta = p.Texto.Contains('/') || p.Texto.Contains("http", StringComparison.Ordinal);
        var destino = conRuta ? null : catalogo.PorClaves(p.Texto);
        if (destino is null)
        {
            return Fija(Intenciones.Navegacion, Anillo.Afuera, Forma.F7, ResultadoDelTurno.Afuera,
                "Sólo te puedo llevar a las pantallas de la aplicación: inicio, localidades, encuesta y resultados.") with { Sugerencias = Plantillas.SugerenciasDeAfuera };
        }

        return Fija(Intenciones.Navegacion, Anillo.Operar, Forma.F4, ResultadoDelTurno.Ok, $"Abrí «{destino.Rotulo}» con el botón de abajo.") with { Destinos = [catalogo.Resolver(destino.Id)] };
    }

    private async Task<RespuestaDelAsistente> AltaDesdeFraseAsync(string frase, CancellationToken ct)
    {
        var intencion = Intenciones.AltaDesdeFrase;
        if (estado.Motivo != MotivoDeHabilitacion.Habilitada)
        {
            return Fija(intencion, Anillo.Operar, Forma.F6, ResultadoDelTurno.Degradado,
                "Leer los datos de una frase necesita la ayuda con IA, que está apagada. Abrí el alta y completá los datos en el formulario.") with { Destinos = [catalogo.Resolver(Destinos.LocalidadesAlta)] };
        }

        // Al extractor viaja sólo la frase, tal como la escribió la persona (C-13).
        var resultado = await extractor.ExtraerAsync(frase, ct);
        if (resultado.Tipo != TipoDeResultado.Ok || resultado.Valor is null)
        {
            var texto = resultado.Tipo == TipoDeResultado.Degradado
                ? "La ayuda con IA no pudo leer la frase. Abrí el alta y completá los datos en el formulario."
                : $"No hay una propuesta confiable para esa frase ({resultado.Motivo}). Abrí el alta y completá los datos en el formulario.";
            return Fija(intencion, Anillo.Operar, Forma.F6, resultado.Tipo == TipoDeResultado.Degradado ? ResultadoDelTurno.Degradado : ResultadoDelTurno.Rechazado, texto)
                with { Destinos = [catalogo.Resolver(Destinos.LocalidadesAlta)] };
        }

        var propuesta = resultado.Valor;
        var faltan = new List<string>();
        if (!LocalidadRules.NombreValido(propuesta.Nombre)) faltan.Add($"nombre ({Localidades.LocalidadPolicy.RequisitoDelNombre})");
        if (!LocalidadRules.ProvinciaValida(propuesta.Provincia)) faltan.Add($"provincia ({Localidades.LocalidadPolicy.RequisitoDeLaProvincia})");
        if (!LocalidadRules.CodigoPostalValido(propuesta.CodigoPostal)) faltan.Add($"código postal ({Localidades.LocalidadPolicy.RequisitoDelCodigoPostal})");
        if (!LocalidadRules.HabitantesValidos(propuesta.Habitantes)) faltan.Add($"habitantes ({Localidades.LocalidadPolicy.RequisitoDeLosHabitantes})");

        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(propuesta.Nombre)) partes.Add(propuesta.Nombre!);
        if (!string.IsNullOrWhiteSpace(propuesta.Provincia)) partes.Add(propuesta.Provincia!);
        if (!string.IsNullOrWhiteSpace(propuesta.CodigoPostal)) partes.Add($"CP {propuesta.CodigoPostal}");
        if (propuesta.Habitantes is int h) partes.Add($"{Plantillas.N(h)} habitantes");

        var mensaje = $"Propuesta para el alta: {string.Join(", ", partes)}. Revisala en el formulario y guardala vos con «Guardar»."
                      + (faltan.Count > 0 ? $" Falta completar: {string.Join("; ", faltan)}." : string.Empty);

        var chip = catalogo.Resolver(Destinos.LocalidadesAlta, new Dictionary<string, string?>
        {
            ["nombre"] = propuesta.Nombre,
            ["provincia"] = LocalidadRules.ProvinciaValida(propuesta.Provincia) ? propuesta.Provincia : null,
            ["cp"] = propuesta.CodigoPostal,
            ["habitantes"] = propuesta.Habitantes?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["propuesta"] = "ia"
        }, "Revisar el alta prellenada");

        var cifras = new List<Cifra>();
        if (propuesta.Habitantes is int hab) cifras.Add(new Cifra("habitantes de la frase", hab, "de la frase"));
        if (int.TryParse(propuesta.CodigoPostal, out var cp)) cifras.Add(new Cifra("código postal de la frase", cp, "de la frase"));

        return Fija(intencion, Anillo.Operar, Forma.F1, ResultadoDelTurno.Ok, mensaje) with { Destinos = [chip], Cifras = cifras };
    }

    private async Task<RespuestaDelAsistente> BajaAsync(PreguntaNormalizada p, CancellationToken ct)
    {
        var sesion = await funciones.ListarLocalidadesAsync(ct);
        var mencion = ResolutorDeCandidatas.Resolver(p, sesion);

        if (mencion.Ambigua)
        {
            var loc = mencion.Localidades.First(l => Texto.Normalizar(l.Nombre) == Texto.Normalizar(mencion.Provincia!));
            return Fija(Intenciones.Baja, Anillo.Operar, Forma.F3, ResultadoDelTurno.Ok,
                $"«{mencion.Provincia}» es una localidad de tu sesión y también una provincia. ¿Cuál querés decir?") with
            {
                Opciones =
                [
                    new($"La localidad {loc.Nombre} ({loc.Provincia})", $"Borrá la localidad {loc.Nombre}"),
                    new($"Las localidades de la provincia {mencion.Provincia}", $"Dame las localidades de la provincia {mencion.Provincia}")
                ]
            };
        }

        if (mencion.Localidades.Count == 0)
        {
            return Fija(Intenciones.Baja, Anillo.Operar, Forma.F5, ResultadoDelTurno.Abstencion, Plantillas.NoEncuentroLocalidad) with { Destinos = [catalogo.Resolver(Destinos.LocalidadesLista)] };
        }

        if (mencion.Localidades.Count > 1)
        {
            return Fija(Intenciones.Baja, Anillo.Operar, Forma.F3, ResultadoDelTurno.Ok, "Hay más de una localidad con ese nombre en tu sesión. ¿Cuál querés eliminar?") with
            {
                Opciones = mencion.Localidades.Select(l => new Opcion($"{l.Nombre} ({l.Provincia}, {Plantillas.N(l.Habitantes)} hab.)", $"Borrá {l.Nombre} {l.Provincia}")).ToList()
            };
        }

        var l = mencion.Localidades[0];
        if (!Permisos.PuedeEliminar(sesion.Select(s => s.Id), l.Id))
        {
            return Fija(Intenciones.Baja, Anillo.Operar, Forma.F5, ResultadoDelTurno.Abstencion, Plantillas.NoEncuentroLocalidad) with { Destinos = [catalogo.Resolver(Destinos.LocalidadesLista)] };
        }

        var chip = catalogo.Resolver(Destinos.LocalidadesBaja, new Dictionary<string, string?> { ["baja"] = l.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            $"Eliminar «{l.Nombre}» ({l.Provincia})");
        return Fija(Intenciones.Baja, Anillo.Operar, Forma.F1, ResultadoDelTurno.Ok,
            $"{l.Nombre} ({l.Provincia}), con {Plantillas.N(l.Habitantes)} habitantes. Para eliminarla, abrí la confirmación con el botón de abajo: la baja se confirma ahí.")
            with { Destinos = [chip], Cifras = [new Cifra($"habitantes de {l.Nombre}", l.Habitantes, "declarados en el ABM")] };
    }

    // ---- Anillo 1: cifras -------------------------------------------------------------------

    private async Task<RespuestaDelAsistente> LocalidadesAsync(PreguntaNormalizada p, Ruteo ruteo, CancellationToken ct)
    {
        var sesion = await funciones.ListarLocalidadesAsync(ct);
        var mencion = ResolutorDeCandidatas.Resolver(p, sesion);

        if (mencion.Ambigua) return AclaracionLocalidadOProvincia(ruteo.Intencion, mencion, sesion, p);

        if (mencion.Localidades.Count == 1 && (ruteo.Intencion == Intenciones.Habitantes || mencion.Provincia is null))
        {
            var l = mencion.Localidades[0];
            var texto = $"{l.Nombre} ({l.Provincia}) tiene {Plantillas.N(l.Habitantes)} habitantes declarados en el ABM, con código postal {l.CodigoPostal}. No es un dato oficial.";
            return await RedactarCifrasAsync(ruteo.Intencion, Forma.F1, p, texto,
                [new Cifra($"habitantes de {l.Nombre}", l.Habitantes, "declarados en el ABM, no datos oficiales")],
                [catalogo.Resolver(Destinos.LocalidadesLista, new Dictionary<string, string?> { ["provincia"] = l.Provincia })], ct);
        }

        if (mencion.Localidades.Count == 0 && mencion.Provincia is null && ruteo.Intencion == Intenciones.Habitantes && p.TieneAlguna("tiene", "de la localidad"))
        {
            return Fija(ruteo.Intencion, Anillo.Operar, Forma.F5, ResultadoDelTurno.Abstencion, Plantillas.NoEncuentroLocalidad) with { Destinos = [catalogo.Resolver(Destinos.LocalidadesLista)] };
        }

        var (total, porProvincia) = await funciones.ContarLocalidadesAsync(mencion.Provincia, ct);
        if (ruteo.Intencion == Intenciones.Habitantes)
        {
            var suma = sesion.Where(l => mencion.Provincia is null || l.Provincia == mencion.Provincia).Sum(l => l.Habitantes);
            var donde = mencion.Provincia is null ? "en tu sesión" : $"en la provincia {mencion.Provincia}";
            return await RedactarCifrasAsync(ruteo.Intencion, Forma.F1, p,
                $"Las {total} localidades {donde} suman {Plantillas.N(suma)} habitantes declarados en el ABM. No son datos oficiales.",
                [new Cifra("localidades", total, donde), new Cifra("habitantes declarados", suma, "declarados en el ABM, no datos oficiales")],
                [catalogo.Resolver(Destinos.LocalidadesLista, new Dictionary<string, string?> { ["provincia"] = mencion.Provincia })], ct);
        }

        if (mencion.Provincia is not null)
        {
            var nombres = sesion.Where(l => l.Provincia == mencion.Provincia).Select(l => l.Nombre).ToList();
            var texto = total == 0
                ? $"No tenés localidades cargadas en la provincia {mencion.Provincia} en tu sesión."
                : $"En la provincia {mencion.Provincia} tenés {total} {(total == 1 ? "localidad cargada" : "localidades cargadas")} en tu sesión: {string.Join(", ", nombres)}.";
            return Fija(ruteo.Intencion, Anillo.Operar, Forma.F1, ResultadoDelTurno.Ok, texto) with
            {
                Cifras = [new Cifra($"localidades en {mencion.Provincia}", total, "localidades cargadas en tu sesión")],
                Grafico = FormaDeGrafico.Numero,
                Destinos = [catalogo.Resolver(Destinos.LocalidadesLista, new Dictionary<string, string?> { ["provincia"] = mencion.Provincia })]
            };
        }

        if (p.TieneAlguna("por provincia", "por provincias", "desglose", "desglosado", "abrilo"))
        {
            var detalle = string.Join("; ", porProvincia.Select(pp => $"{pp.Provincia}: {pp.Cantidad}"));
            return Fija(ruteo.Intencion, Anillo.Operar, Forma.F1, ResultadoDelTurno.Ok,
                $"Tenés {total} localidades cargadas en tu sesión. Por provincia: {detalle}.") with
            {
                Cifras = [new Cifra("localidades", total, "localidades cargadas en tu sesión"), .. porProvincia.Select(pp => new Cifra(pp.Provincia, pp.Cantidad, "localidades por provincia"))],
                Destinos = [catalogo.Resolver(Destinos.LocalidadesLista)]
            };
        }

        // F2: el total, la nota y la oferta de desglose.
        return Fija(ruteo.Intencion, Anillo.Operar, Forma.F2, ResultadoDelTurno.Ok,
            $"Tenés {total} {(total == 1 ? "localidad cargada" : "localidades cargadas")} en tu sesión.") with
        {
            Cifras = [new Cifra("localidades", total, "localidades cargadas en tu sesión")],
            Grafico = FormaDeGrafico.Numero,
            Opciones = porProvincia.Count > 1 ? [new Opcion("¿Lo abro por provincia?", "Contá mis localidades por provincia")] : [],
            Destinos = [catalogo.Resolver(Destinos.LocalidadesLista)]
        };
    }

    private RespuestaDelAsistente AclaracionLocalidadOProvincia(string intencion, Mencion mencion, IReadOnlyList<LocalidadVista> sesion, PreguntaNormalizada p)
    {
        var nombre = mencion.Provincia!;
        var loc = mencion.Localidades.First(l => Texto.Normalizar(l.Nombre) == Texto.Normalizar(nombre));
        var enProvincia = sesion.Count(l => l.Provincia == nombre);
        var (pLoc, pProv) = intencion switch
        {
            Intenciones.ContarEncuestas => ($"¿Cuántas encuestas hay en la localidad {loc.Nombre}?", $"¿Cuántas encuestas hay en la provincia {nombre}?"),
            Intenciones.Glosario => ($"¿Qué es la localidad {loc.Nombre}?", $"¿Qué es la provincia {nombre}?"),
            _ => ($"¿Cuántos habitantes tiene la localidad {loc.Nombre}?", $"Dame las localidades de la provincia {nombre}")
        };
        return Fija(intencion, intencion == Intenciones.Glosario ? Anillo.Dominio : Anillo.Operar, Forma.F3, ResultadoDelTurno.Ok,
            $"¿{nombre} la localidad ({Plantillas.N(loc.Habitantes)} hab.) o la provincia ({enProvincia} {(enProvincia == 1 ? "localidad cargada" : "localidades cargadas")})?") with
        {
            Opciones = [new($"La localidad {loc.Nombre}", pLoc), new($"La provincia {nombre}", pProv)]
        };
    }

    private async Task<RespuestaDelAsistente> EncuestasAsync(PreguntaNormalizada p, CancellationToken ct)
    {
        var sesion = await funciones.ListarLocalidadesAsync(ct);
        var mencion = ResolutorDeCandidatas.Resolver(p, sesion);
        if (mencion.Ambigua) return AclaracionLocalidadOProvincia(Intenciones.ContarEncuestas, mencion, sesion, p);

        var total = await funciones.ContarEncuestasAsync(ct);
        if (total == 0) return SinEncuestas(Intenciones.ContarEncuestas, sesion.Count);

        if (!mencion.Vacia)
        {
            var porLocalidad = await funciones.EncuestasPorLocalidadAsync(ct);
            var nombres = mencion.Localidades.Count > 0
                ? mencion.Localidades.Select(l => l.Nombre).ToHashSet(StringComparer.Ordinal)
                : sesion.Where(l => l.Provincia == mencion.Provincia).Select(l => l.Nombre).ToHashSet(StringComparer.Ordinal);
            var n = porLocalidad.Filas.Where(f => nombres.Contains(f.Grupo)).Sum(f => f.Encuestas);
            var donde = mencion.Localidades.Count > 0 ? $"en la localidad {mencion.Localidades[0].Nombre}" : $"en la provincia {mencion.Provincia}";
            return await RedactarCifrasAsync(Intenciones.ContarEncuestas, Forma.F1, p,
                $"Hay {n} de {total} encuestas de tu sesión {donde}." + (n is > 0 and < 5 ? " " + Plantillas.NChico : string.Empty),
                [new Cifra($"encuestas {donde}", n, "de tu sesión"), new Cifra("encuestas", total, "de tu sesión")],
                [catalogo.Resolver(Destinos.ResultadosResumen)], ct);
        }

        return await RedactarCifrasAsync(Intenciones.ContarEncuestas, Forma.F1, p,
            $"Hay {total} {(total == 1 ? "encuesta registrada" : "encuestas registradas")} en tu sesión." + (total < 5 ? " " + Plantillas.NChico : string.Empty),
            [new Cifra("encuestas", total, "encuestas registradas en tu sesión")],
            [catalogo.Resolver(Destinos.ResultadosResumen)], ct);
    }

    private RespuestaDelAsistente SinEncuestas(string intencion, int localidades) =>
        Fija(intencion, Anillo.Operar, Forma.F5, ResultadoDelTurno.Abstencion, Plantillas.SinEncuestas) with
        {
            Destinos = [catalogo.Resolver(Permisos.PuedeEncuestar(localidades) ? Destinos.EncuestaInicio : Destinos.LocalidadesAlta)]
        };

    private async Task<RespuestaDelAsistente> DistribucionAsync(PreguntaNormalizada p, Ruteo ruteo, CancellationToken ct)
    {
        var d = ruteo.Intencion switch
        {
            Intenciones.Medios => await funciones.DistribucionMediosAsync(ct),
            Intenciones.Motivos => await funciones.DistribucionMotivosAsync(ct),
            _ => await funciones.DistribucionFrecuenciasAsync(ct)
        };

        if (d.Encuestas == 0)
        {
            var sesion = await funciones.ListarLocalidadesAsync(ct);
            return SinEncuestas(ruteo.Intencion, sesion.Count);
        }

        var nombrada = d.Filas.FirstOrDefault(f => p.Canonicos.Contains(f.Clave));
        var mayor = d.Filas.OrderByDescending(f => f.Cantidad).First();
        var nota = d.Sumable ? "" : " Una persona puede marcar varios medios, así que no suman 100 %.";
        string texto;
        if (nombrada is not null)
        {
            var explicada = p.Explicadas.FirstOrDefault(e => e.Canonico == nombrada.Clave);
            var aclaracion = explicada.Forma is null ? string.Empty : $"«{Plantillas.Capitalizar(explicada.Forma)}» se entiende como {nombrada.Etiqueta.ToLowerInvariant()}. ";
            texto = $"{aclaracion}{nombrada.Etiqueta}: {nombrada.Cantidad} de {d.Encuestas} encuestas de tu sesión.{nota}";
        }
        else
        {
            var variable = ruteo.Intencion switch { Intenciones.Medios => "El medio más usado", Intenciones.Motivos => "El motivo más frecuente", _ => "La frecuencia más común" };
            texto = $"{variable} en tu sesión es {mayor.Etiqueta}: {mayor.Cantidad} de {d.Encuestas} encuestas.{nota}";
        }
        if (d.Encuestas < 5) texto += " " + Plantillas.NChico;

        var destino = ruteo.Intencion switch
        {
            Intenciones.Medios => catalogo.Resolver(Destinos.ResultadosMedios),
            Intenciones.Motivos => catalogo.Resolver(Destinos.ResultadosMotivos),
            _ => catalogo.Resolver(Destinos.ResultadosResumen, new Dictionary<string, string?> { ["vista"] = "frecuencias" }, "Ver frecuencias")
        };

        var cifras = d.Filas.Select(f => new Cifra(f.Etiqueta, f.Cantidad, d.Nota)).Append(new Cifra("encuestas", d.Encuestas, d.Nota)).ToList();
        var r = await RedactarCifrasAsync(ruteo.Intencion, Forma.F1, p, texto, cifras, [destino], ct);
        return r with { Distribucion = d, Grafico = ReglaDeForma.Decidir(TipoDeDato.Distribucion, d.Filas.Count, d.Sumable) };
    }

    private async Task<RespuestaDelAsistente> PromediosAsync(PreguntaNormalizada p, CancellationToken ct)
    {
        var medio = p.Canonicos.FirstOrDefault(Enrutador.EsMedio);
        var promedios = await funciones.PromediosViajeAsync(medio is null ? AgrupacionDePromedios.Ninguno : AgrupacionDePromedios.Medio, ct);
        if (promedios.Filas.Count == 0 && medio is null)
        {
            var sesion = await funciones.ListarLocalidadesAsync(ct);
            return SinEncuestas(Intenciones.Promedios, sesion.Count);
        }

        var fila = medio is null ? promedios.Filas[0] : promedios.Filas.FirstOrDefault(f => f.Grupo == medio);
        var destino = catalogo.Resolver(Destinos.ResultadosResumen, new Dictionary<string, string?> { ["vista"] = "distancias" }, "Ver distancias y tiempos");
        if (fila is null)
        {
            return Fija(Intenciones.Promedios, Anillo.Operar, Forma.F5, ResultadoDelTurno.Abstencion,
                $"Ninguna encuesta de tu sesión marcó {Catalogos.EtiquetaDeMedio(medio!).ToLowerInvariant()}.") with { Destinos = [destino] };
        }

        var quienes = medio is null ? "Las personas encuestadas" : $"Quienes marcaron {fila.Etiqueta.ToLowerInvariant()}";
        var texto = $"{quienes} tardan en promedio {Plantillas.N(fila.MinutosPromedio)} minutos (mediana {Plantillas.N(fila.MinutosMediana)}) y recorren {Plantillas.N(fila.DistanciaPromedio)} km por día (mediana {Plantillas.N(fila.DistanciaMediana)}), sobre {fila.Encuestas} respuestas de tu sesión."
                    + (fila.Encuestas < 5 ? " " + Plantillas.NChico : string.Empty);
        var cifras = new List<Cifra>
        {
            new("minutos promedio", fila.MinutosPromedio, promedios.Nota), new("minutos mediana", fila.MinutosMediana, promedios.Nota),
            new("km promedio", fila.DistanciaPromedio, promedios.Nota), new("km mediana", fila.DistanciaMediana, promedios.Nota),
            new("respuestas", fila.Encuestas, promedios.Nota)
        };
        var r = await RedactarCifrasAsync(Intenciones.Promedios, Forma.F1, p, texto, cifras, [destino], ct);
        return r with { Promedios = promedios with { Filas = [fila] }, Grafico = ReglaDeForma.Decidir(TipoDeDato.Comparacion) };
    }

    // ---- Anillos 1, 2 y 3: ayuda con cita ----------------------------------------------------

    private async Task<RespuestaDelAsistente> EncuestaComoAsync(PreguntaNormalizada p, Ruteo ruteo, string? contexto, CancellationToken ct)
    {
        var sesion = await funciones.ListarLocalidadesAsync(ct);
        if (!Permisos.PuedeEncuestar(sesion.Count))
        {
            return Fija(ruteo.Intencion, Anillo.Operar, Forma.F6, ResultadoDelTurno.Ok, Plantillas.PrimeroUnaLocalidad) with { Destinos = [catalogo.Resolver(Destinos.LocalidadesAlta)] };
        }

        return await AyudaAsync(p, ruteo, contexto, f => f.Pagina == "cargar-una-encuesta", [Destinos.EncuestaInicio], ct, "cargar-una-encuesta");
    }

    private async Task<RespuestaDelAsistente> GlosarioAsync(PreguntaNormalizada p, Ruteo ruteo, CancellationToken ct)
    {
        var entrada = Glosario.Buscar(p);
        if (entrada is null)
        {
            var sesion = await funciones.ListarLocalidadesAsync(ct);
            var mencion = ResolutorDeCandidatas.Resolver(p, sesion);
            if (mencion.Ambigua) return AclaracionLocalidadOProvincia(Intenciones.Glosario, mencion, sesion, p);
            if (mencion.Localidades.Count == 1) return await LocalidadesAsync(p, new Ruteo(Intenciones.Habitantes, Anillo.Operar), ct);
            if (mencion.Provincia is not null)
            {
                entrada = Glosario.Entradas.First(e => e.Id == "provincia");
            }
            else
            {
                return NoEstaEnLaAyuda(Intenciones.Glosario, Anillo.Dominio);
            }
        }

        var recuperados = recuperador.Buscar(p.Texto, f => f.Anillo == Anillo.Dominio).Select(r => r.Fragmento).ToList();
        var propio = corpus.Vigentes.FirstOrDefault(f => f.Pagina == "glosario" && f.Seccion == entrada.Termino);
        var primeros = entrada.Id == "sesion" ? recuperados.Where(f => f.Pagina == "por-que-no-veo-lo-de-otro").ToList() : [];
        var fragmentos = primeros
            .Concat(propio is null ? [] : [propio])
            .Concat(recuperados)
            .DistinctBy(f => f.Id)
            .ToList();
        if (fragmentos.Count == 0) return NoEstaEnLaAyuda(Intenciones.Glosario, Anillo.Dominio);
        fragmentos = fragmentos.Take(RecuperadorDeAyuda.TopK).ToList();

        var destinoId = entrada.Destino;
        if (entrada.Id == "provincia")
        {
            var sesion = await funciones.ListarLocalidadesAsync(ct);
            var prov = ResolutorDeCandidatas.Resolver(p, sesion).Provincia;
            var texto = prov is null ? $"{entrada.Definicion} [1]" : $"{prov} es una de las provincias del catálogo de la aplicación. {entrada.Definicion} [1]";
            return await RedactarAyudaAsync(Intenciones.Glosario, Anillo.Dominio, p, texto, fragmentos,
                [catalogo.Resolver(Destinos.LocalidadesLista, new Dictionary<string, string?> { ["provincia"] = prov })], ct);
        }

        var plantilla = fragmentos[0].Pagina == "glosario" ? $"{entrada.Termino}: {entrada.Definicion} [1]" : $"{fragmentos[0].Texto} [1]";
        return await RedactarAyudaAsync(Intenciones.Glosario, Anillo.Dominio, p, plantilla, fragmentos, [catalogo.Resolver(destinoId)], ct);
    }

    private async Task<RespuestaDelAsistente> AyudaAsync(
        PreguntaNormalizada p, Ruteo ruteo, string? contexto, Func<FragmentoDeAyuda, bool>? filtro, IReadOnlyList<string> destinosPrimero, CancellationToken ct, string? paginaFija = null)
    {
        var recuperados = recuperador.Buscar(p.Texto, filtro);
        if (contexto is not null)
        {
            recuperados = recuperados.OrderByDescending(r => r.Cobertura).ThenByDescending(r => r.Fragmento.Contexto == contexto).ThenByDescending(r => r.Puntaje).ToList();
        }
        if (recuperados.Count == 0 && paginaFija is not null)
        {
            // La intención ya eligió la página (lo decidió el código): sin coincidencia léxica,
            // se cita su primer fragmento en vez de abstenerse.
            var primero = corpus.Vigentes.FirstOrDefault(f => f.Pagina == paginaFija);
            if (primero is not null) recuperados = [new Recuperado(primero, 0, 0)];
        }
        if (recuperados.Count == 0) return NoEstaEnLaAyuda(ruteo.Intencion, ruteo.Anillo);

        var fragmentos = recuperados.Select(r => r.Fragmento).ToList();
        var principal = fragmentos[0];
        var anillo = ruteo.Anillo == Anillo.Operar && principal.Anillo != Anillo.Operar ? principal.Anillo : ruteo.Anillo;

        // Destino por intención (K6 §15): la intención reordena los destinos que declara la ficha.
        var ids = destinosPrimero.Concat(principal.Destinos).Where(catalogo.Existe).Distinct().Take(2).ToList();
        if (anillo == Anillo.LoDeAlLado && ids.Count == 0) ids.Add(Destinos.Inicio);
        var sesion = ids.Contains(Destinos.EncuestaInicio) ? await funciones.ListarLocalidadesAsync(ct) : [];
        var chips = ids.Select(id => id == Destinos.EncuestaInicio && !Permisos.PuedeEncuestar(sesion.Count) ? catalogo.Resolver(Destinos.LocalidadesAlta) : catalogo.Resolver(id))
            .DistinctBy(c => c.Href).ToList();

        var plantilla = $"{principal.Texto} [1]";
        var intencion = ruteo.Intencion == Intenciones.Ayuda && principal.Pagina == "alta-de-una-localidad" ? Intenciones.AltaComo : ruteo.Intencion;
        return await RedactarAyudaAsync(intencion, anillo, p, plantilla, fragmentos, chips, ct);
    }

    private RespuestaDelAsistente NoEstaEnLaAyuda(string intencion, Anillo anillo) =>
        Fija(intencion, anillo, Forma.F5, ResultadoDelTurno.Abstencion, Plantillas.NoEstaEnLaAyuda) with
        {
            Sugerencias = Plantillas.SugerenciasDeAfuera,
            Destinos = [catalogo.Resolver(Destinos.Inicio)]
        };

    // ---- Paso 6: redacción, con la plantilla como respaldo ------------------------------------

    private async Task<RespuestaDelAsistente> RedactarAyudaAsync(
        string intencion, Anillo anillo, PreguntaNormalizada p, string plantilla, IReadOnlyList<FragmentoDeAyuda> fragmentos, IReadOnlyList<Chip> chips, CancellationToken ct)
    {
        var citas = fragmentos.Select((f, i) => new Cita(i + 1, f.Id, f.Titulo)).ToList();
        var basica = Fija(intencion, anillo, Forma.F4, ResultadoDelTurno.Ok, plantilla) with { Destinos = chips, Citas = citas.Take(1).ToList() };
        return await RedactarAsync(basica, new PedidoDeRedaccion(intencion, anillo, p.Texto, [], fragmentos, plantilla), citas, ct);
    }

    private async Task<RespuestaDelAsistente> RedactarCifrasAsync(
        string intencion, Forma forma, PreguntaNormalizada p, string plantilla, IReadOnlyList<Cifra> cifras, IReadOnlyList<Chip> chips, CancellationToken ct)
    {
        var basica = Fija(intencion, Anillo.Operar, forma, ResultadoDelTurno.Ok, plantilla) with { Cifras = cifras, Destinos = chips, Grafico = FormaDeGrafico.Numero };
        return await RedactarAsync(basica, new PedidoDeRedaccion(intencion, Anillo.Operar, p.Texto, cifras, [], plantilla), [], ct);
    }

    private async Task<RespuestaDelAsistente> RedactarAsync(RespuestaDelAsistente basica, PedidoDeRedaccion pedido, IReadOnlyList<Cita> citas, CancellationToken ct)
    {
        if (estado.Motivo != MotivoDeHabilitacion.Habilitada) return basica with { Resultado = ResultadoDelTurno.Degradado };
        // Fuera de las distribuciones la plantilla fija es la respuesta: el modelo no se llama.
        if (!Intenciones.ConRedaccion.Contains(pedido.Intencion)) return basica;

        var redactado = await redactor.RedactarAsync(pedido, ct);
        return redactado.Tipo switch
        {
            TipoDeResultado.Ok => basica with { Texto = redactado.Valor!, RedactadaConIa = true, Citas = citas.Count > 0 ? citas : basica.Citas },
            TipoDeResultado.Rechazado => basica with { Resultado = ResultadoDelTurno.Rechazado, MotivoDelResultado = redactado.Motivo },
            _ => basica with { Resultado = ResultadoDelTurno.Degradado, MotivoDelResultado = redactado.Motivo, Aviso = redactado.Motivo == "ocupado" ? Plantillas.Ocupado : null }
        };
    }

    private static RespuestaDelAsistente Fija(string intencion, Anillo anillo, Forma forma, ResultadoDelTurno resultado, string texto) => new()
    {
        Intencion = intencion,
        Anillo = anillo,
        Forma = forma,
        Resultado = resultado,
        Texto = texto
    };
}
