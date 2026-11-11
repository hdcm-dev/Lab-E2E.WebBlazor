using System.Reflection;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MovilidadUrbana.Web.Application.Asistente;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Adaptador de <see cref="IRedactorDeAyuda" /> sobre <see cref="IChatClient" />: una conversación
/// nueva por llamada, <b>sin herramientas</b> (REQ-IA-24), tiempo máximo y sin reintentos. Al
/// modelo viaja sólo el <see cref="PedidoDeRedaccion" />; su salida pasa por el limpiador de URLs
/// y el validador de cifras, citas y primera persona antes de llegar a la pantalla.
/// </summary>
public sealed class RedactorConModelo(
    IChatClient cliente,
    LimitadorDelModelo limitador,
    OpcionesDelModelo opciones,
    ILogger<RedactorConModelo> registro) : IRedactorDeAyuda
{
    public static readonly string Prompt = LeerPrompt("redactor.v1.txt");

    public async Task<Resultado<string>> RedactarAsync(PedidoDeRedaccion pedido, CancellationToken cancellationToken = default)
    {
        if (GuardaDeLargo.Excede(pedido.Pregunta)) return Resultado<string>.Rechazado("pregunta sobre el tope");

        using var permiso = limitador.Intentar();
        if (!permiso.IsAcquired) return Resultado<string>.Degradado("ocupado");

        using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tiempo.CancelAfter(opciones.TiempoMaximo);
        try
        {
            var respuesta = await cliente.GetResponseAsync(
                [new ChatMessage(ChatRole.System, Prompt), new ChatMessage(ChatRole.User, ArmarMensaje(pedido))],
                opciones.ParaRedactar(),
                tiempo.Token);

            var texto = LimpiadorDeUrls.Limpiar(respuesta.Text ?? string.Empty);
            var falla = ValidadorDeCifras.Validar(texto, pedido);
            if (falla is not null)
            {
                registro.LogInformation("Redacción rechazada: {Motivo} ({Intencion})", falla, pedido.Intencion);
                return Resultado<string>.Rechazado(falla);
            }

            return Resultado<string>.Ok(texto);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Resultado<string>.Degradado("sin respuesta");
        }
        catch (Exception excepcion) when (excepcion is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException)
        {
            registro.LogWarning("El modelo de redacción no respondió: {Tipo}", excepcion.GetType().Name);
            return Resultado<string>.Degradado("sin respuesta");
        }
    }

    /// <summary>El mensaje del usuario: sólo los campos declarados del pedido (REQ-IA-15).</summary>
    public static string ArmarMensaje(PedidoDeRedaccion pedido)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PREGUNTA: " + pedido.Pregunta);
        sb.AppendLine();
        sb.AppendLine("FUENTES:");
        if (pedido.Fragmentos.Count > 0)
        {
            for (var i = 0; i < pedido.Fragmentos.Count; i++)
            {
                sb.AppendLine($"[{i + 1}] {pedido.Fragmentos[i].Titulo}: {pedido.Fragmentos[i].Texto}");
            }
        }
        else
        {
            sb.Append("[1] Cifras calculadas por la aplicación: ");
            sb.AppendLine(string.Join("; ", pedido.Cifras.Select(c => $"{MarcarTerceros(c.Etiqueta)} = {Plantillas.N(c.Valor)} ({c.Nota})")));
        }
        sb.AppendLine();
        sb.AppendLine("RESPUESTA FIJA DE REFERENCIA (podés mejorar su claridad, no su contenido): " + pedido.Plantilla);
        return sb.ToString();
    }

    /// <summary>
    /// Un nombre de localidad lo escribió una persona: viaja marcado como dato (REQ-IA-19), con
    /// los signos que podrían cerrar la marca neutralizados.
    /// </summary>
    public static string MarcarTerceros(string etiqueta)
    {
        const string prefijo = "habitantes de ";
        if (!etiqueta.StartsWith(prefijo, StringComparison.Ordinal)) return etiqueta;
        var nombre = etiqueta[prefijo.Length..].Replace("«", "\"").Replace("»", "\"");
        return $"{prefijo}«dato cargado: {nombre}»";
    }

    private static string LeerPrompt(string nombre)
    {
        var ensamblado = typeof(RedactorConModelo).Assembly;
        var recurso = ensamblado.GetManifestResourceNames().Single(r => r.EndsWith(".Prompts." + nombre, StringComparison.Ordinal));
        using var flujo = ensamblado.GetManifestResourceStream(recurso)!;
        using var lector = new StreamReader(flujo, Encoding.UTF8);
        return lector.ReadToEnd();
    }

    internal static string LeerPromptPublico(string nombre) => LeerPrompt(nombre);
}
