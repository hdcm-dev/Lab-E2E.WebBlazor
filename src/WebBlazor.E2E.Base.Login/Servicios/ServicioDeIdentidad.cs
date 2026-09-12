using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebBlazor.E2E.Base.Login.Servicios;

/// <summary>
/// Identidad de laboratorio: una sola credencial fija, la que este proyecto base usa
/// para demostrar el circuito de ingreso de punta a punta.
/// </summary>
/// <remarks>
/// El esquema de credenciales de un producto real —hash del secreto, control de
/// intentos, política de sesión— es materia de la postura de seguridad, no de la
/// capa de presentación. Acá se sostiene sólo lo que la presentación necesita: que
/// el rechazo sea indiferenciado y que no exponga parámetros de la política.
/// </remarks>
public sealed class ServicioDeIdentidad : IServicioDeIdentidad
{
    private const string IdentificadorAdmitido = "admin";
    private const string SecretoAdmitido = "admin";

    /// <inheritdoc />
    public ResultadoDeAutenticacion Autenticar(string? identificador, string? secreto)
    {
        if (string.IsNullOrWhiteSpace(identificador) || string.IsNullOrWhiteSpace(secreto))
        {
            return new ResultadoDeAutenticacion(false, CatalogoDeResultados.DatosIncompletos);
        }

        // Un solo desenlace de rechazo: no se distingue cuál de los dos campos falló.
        if (!string.Equals(identificador, IdentificadorAdmitido, StringComparison.Ordinal)
            || !string.Equals(secreto, SecretoAdmitido, StringComparison.Ordinal))
        {
            return new ResultadoDeAutenticacion(false, CatalogoDeResultados.CredencialesRechazadas);
        }

        var identidad = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, identificador)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ResultadoDeAutenticacion(true, string.Empty, new ClaimsPrincipal(identidad));
    }
}
