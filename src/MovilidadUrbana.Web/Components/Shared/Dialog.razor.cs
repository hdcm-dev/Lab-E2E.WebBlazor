using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MovilidadUrbana.Web.Services;

namespace MovilidadUrbana.Web.Components.Shared;

/// <summary>
/// Diálogo de confirmación. Lo monta el host del layout y lo gobierna el servicio de diálogos: la
/// página pide una confirmación desde donde ocurre la acción y este marcado vive una sola vez.
/// </summary>
public partial class Dialog : ComponentBase
{
    private ElementReference _element;
    private string _typed = string.Empty;

    [Parameter, EditorRequired] public ConfirmationRequest Request { get; set; } = default!;

    /// <summary>Decisión de la persona: confirmó o canceló.</summary>
    [Parameter] public EventCallback<bool> OnClose { get; set; }

    [Inject] private IJSRuntime Js { get; set; } = default!;

    private string TitleId => "dialogo-titulo";

    private string NoticeId => "dialogo-aviso";

    private string FieldId => "dialogo-confirmacion";

    private string ActionClass => Request.EsDestructiva ? "mq-btn--destructivo" : "mq-btn--primario";

    /// <summary>
    /// Segundo grado de confirmación: mientras lo escrito no coincida con el valor esperado, la
    /// acción no se habilita.
    /// </summary>
    private bool IsEnabled =>
        !Request.PideEscritura || string.Equals(_typed.Trim(), Request.ValorEsperado, StringComparison.Ordinal);

    protected override async Task OnAfterRenderAsync(bool primerRender)
    {
        if (primerRender)
        {
            await Js.InvokeVoidAsync("mqDialogo.abrir", _element);
        }
    }

    private async Task CerrarAsync(bool result)
    {
        await Js.InvokeVoidAsync("mqDialogo.cerrar", _element);
        await OnClose.InvokeAsync(result);
    }
}
