namespace MovilidadUrbana.Web.Services;

/// <summary>
/// Lo que una confirmación necesita saber para dibujarse. El aviso de consecuencia es obligatorio:
/// una confirmación que no dice qué pasa al aceptar no confirma nada.
/// </summary>
/// <param name="Titulo">Encabezado del diálogo, que nombra la acción y su objeto.</param>
/// <param name="Aviso">Consecuencia de aceptar, en una frase.</param>
/// <param name="RotuloDeAccion">Verbo exacto de la acción, nunca «Aceptar».</param>
/// <param name="EsDestructiva">Dibuja la acción con el tratamiento destructivo.</param>
/// <param name="RotuloDelCampo">Rótulo del campo de confirmación escrita. Nulo si no la pide.</param>
/// <param name="ValorEsperado">Valor que hay que escribir para habilitar la acción.</param>
public sealed record ConfirmationRequest(
    string Title,
    string Aviso,
    string RotuloDeAccion,
    bool EsDestructiva = true,
    string? RotuloDelCampo = null,
    string? ValorEsperado = null)
{
    /// <summary>Segundo grado de confirmación: la acción arrastra dependientes y hay que escribir.</summary>
    public bool PideEscritura => RotuloDelCampo is not null && ValorEsperado is not null;
}

/// <summary>
/// Abre un diálogo desde donde ocurre la acción y devuelve el resultado que el llamador espera.
/// El marcado del diálogo vive una sola vez, en el host que el layout monta.
/// </summary>
public interface IDialogService
{
    /// <summary>Confirmación pendiente, o nulo si no hay ninguna abierta.</summary>
    ConfirmationRequest? Current { get; }

    /// <summary>Avisa al host que la confirmación vigente cambió.</summary>
    event Action? OnChange;

    /// <summary>Abre la confirmación y espera la decisión de la persona.</summary>
    Task<bool> ConfirmAsync(ConfirmationRequest request);

    /// <summary>Cierra la confirmación vigente con la decisión tomada.</summary>
    void Resolve(bool confirmado);
}
