using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain;
using MovilidadUrbana.Web.Domain.Asistente;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Adaptador de <see cref="IExtractorDeLocalidad" /> (A0 con persona que firma): el modelo propone
/// nombre, provincia, código postal y habitantes desde una frase; el código normaliza la provincia
/// al catálogo y descarta todo valor que no esté en la frase. Un texto que no es JSON no rompe
/// nada: no hay propuesta (REQ-IA-03). La propuesta nunca se guarda: prellena el formulario.
/// </summary>
public sealed partial class ExtractorDeLocalidad(IChatClient cliente, LimitadorDelModelo limitador, OpcionesDelModelo opciones) : IExtractorDeLocalidad
{
    public static readonly string Prompt = RedactorConModelo.LeerPromptPublico("extractor.v1.txt");

    public async Task<Resultado<LocalidadPropuesta>> ExtraerAsync(string frase, CancellationToken cancellationToken = default)
    {
        if (GuardaDeLargo.Excede(frase)) return Resultado<LocalidadPropuesta>.Rechazado("frase sobre el tope");

        using var permiso = limitador.Intentar();
        if (!permiso.IsAcquired) return Resultado<LocalidadPropuesta>.Degradado("ocupado");

        using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tiempo.CancelAfter(opciones.TiempoMaximo);
        string texto;
        try
        {
            var respuesta = await cliente.GetResponseAsync(
                [new ChatMessage(ChatRole.System, Prompt), new ChatMessage(ChatRole.User, frase)],
                opciones.ParaExtraer(), tiempo.Token);
            texto = respuesta.Text ?? string.Empty;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Resultado<LocalidadPropuesta>.Degradado("sin respuesta");
        }
        catch (Exception excepcion) when (excepcion is HttpRequestException or InvalidOperationException)
        {
            return Resultado<LocalidadPropuesta>.Degradado("sin respuesta");
        }

        return Interpretar(texto, frase);
    }

    /// <summary>Lee la salida del modelo y la ata a la frase. Función pura: se prueba sin modelo.</summary>
    public static Resultado<LocalidadPropuesta> Interpretar(string salida, string frase)
    {
        var inicio = salida.IndexOf('{');
        var fin = salida.LastIndexOf('}');
        if (inicio < 0 || fin <= inicio) return Resultado<LocalidadPropuesta>.Rechazado("la salida no es JSON");

        JsonElement raiz;
        try
        {
            using var documento = JsonDocument.Parse(salida[inicio..(fin + 1)]);
            raiz = documento.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Resultado<LocalidadPropuesta>.Rechazado("la salida no es JSON");
        }
        if (raiz.ValueKind != JsonValueKind.Object) return Resultado<LocalidadPropuesta>.Rechazado("la salida no es un objeto");

        var fraseNormalizada = " " + Texto.Normalizar(frase) + " ";
        var numerosDeLaFrase = NumerosDe(frase);

        var nombre = Cadena(raiz, "nombre");
        if (nombre is not null && !fraseNormalizada.Contains(" " + Texto.Normalizar(nombre) + " ", StringComparison.Ordinal)) nombre = null;

        var provincia = NormalizarProvincia(Cadena(raiz, "provincia"));
        if (provincia is not null && !fraseNormalizada.Contains(" " + Texto.Normalizar(provincia) + " ", StringComparison.Ordinal)) provincia = null;

        var cp = Cadena(raiz, "codigoPostal");
        cp = cp is null ? null : new string(cp.Where(char.IsAsciiDigit).ToArray());
        if (string.IsNullOrEmpty(cp) || !numerosDeLaFrase.Contains(cp)) cp = null;

        int? habitantes = null;
        if (raiz.TryGetProperty("habitantes", out var h))
        {
            var crudo = h.ValueKind switch
            {
                JsonValueKind.Number when h.TryGetInt64(out var n) => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonValueKind.String => new string((h.GetString() ?? string.Empty).Where(char.IsAsciiDigit).ToArray()),
                _ => null
            };
            // Un número que no está en la frase es inventado: se descarta (B04).
            if (!string.IsNullOrEmpty(crudo) && numerosDeLaFrase.Contains(crudo) && int.TryParse(crudo, out var valor)) habitantes = valor;
        }

        return Resultado<LocalidadPropuesta>.Ok(new LocalidadPropuesta(nombre, provincia, cp, habitantes));
    }

    public static string? NormalizarProvincia(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var buscado = Texto.Normalizar(valor);
        return Catalogos.Provincias.FirstOrDefault(p => Texto.Normalizar(p) == buscado);
    }

    /// <summary>Números de la frase sin separadores de miles: «90.000» y «90000» son el mismo.</summary>
    public static IReadOnlySet<string> NumerosDe(string frase) =>
        NumeroRx().Matches(frase).Select(m => m.Value.Replace(".", string.Empty).Replace(",", string.Empty)).ToHashSet(StringComparer.Ordinal);

    private static string? Cadena(JsonElement raiz, string nombre) =>
        raiz.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()) ? v.GetString()!.Trim()
        : raiz.TryGetProperty(nombre, out v) && v.ValueKind == JsonValueKind.Number ? v.GetRawText()
        : null;

    [GeneratedRegex(@"\d{1,3}(?:[.,]\d{3})+|\d+")]
    private static partial Regex NumeroRx();
}
