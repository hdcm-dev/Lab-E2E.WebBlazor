using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MovilidadUrbana.Web.Servicios;

namespace MovilidadUrbana.Web.Components.Componentes;

/// <summary>
/// Diálogo de confirmación. Lo monta el host del layout y lo gobierna el servicio de diálogos: la
/// página pide una confirmación desde donde ocurre la acción y este marcado vive una sola vez.
/// </summary>
public partial class Dialogo : ComponentBase
{
    private ElementReference _elemento;
    private string _escrito = string.Empty;

    [Parameter, EditorRequired] public PedidoDeConfirmacion Pedido { get; set; } = default!;

    /// <summary>Decisión de la persona: confirmó o canceló.</summary>
    [Parameter] public EventCallback<bool> AlCerrar { get; set; }

    [Inject] private IJSRuntime Js { get; set; } = default!;

    private string IdDelTitulo => "dialogo-titulo";

    private string IdDelAviso => "dialogo-aviso";

    private string IdDelCampo => "dialogo-confirmacion";

    private string ClaseDeLaAccion => Pedido.EsDestructiva ? "mq-btn--destructivo" : "mq-btn--primario";

    /// <summary>
    /// Segundo grado de confirmación: mientras lo escrito no coincida con el valor esperado, la
    /// acción no se habilita.
    /// </summary>
    private bool EstaHabilitada =>
        !Pedido.PideEscritura || string.Equals(_escrito.Trim(), Pedido.ValorEsperado, StringComparison.Ordinal);

    protected override async Task OnAfterRenderAsync(bool primerRender)
    {
        if (primerRender)
        {
            await Js.InvokeVoidAsync("mqDialogo.abrir", _elemento);
        }
    }

    private async Task CerrarAsync(bool resultado)
    {
        await Js.InvokeVoidAsync("mqDialogo.cerrar", _elemento);
        await AlCerrar.InvokeAsync(resultado);
    }
}
