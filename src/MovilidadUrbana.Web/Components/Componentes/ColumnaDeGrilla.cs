using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MovilidadUrbana.Web.Components.Componentes;

/// <summary>
/// Declaración de una columna de <see cref="Grilla{TItem}" />. No emite marcado: se registra en la
/// grilla, que es la que dibuja la tabla y las tarjetas apiladas con las mismas columnas.
/// </summary>
/// <typeparam name="TItem">Tipo de cada elemento de la colección.</typeparam>
public sealed class ColumnaDeGrilla<TItem> : ComponentBase
{
    [CascadingParameter] private Grilla<TItem>? Grilla { get; set; }

    /// <summary>Encabezado de la columna.</summary>
    [Parameter, EditorRequired] public string Titulo { get; set; } = default!;

    /// <summary>Contenido de la celda para un elemento.</summary>
    [Parameter, EditorRequired] public RenderFragment<TItem> Plantilla { get; set; } = default!;

    /// <summary>
    /// La celda de esta columna es el encabezado de su fila: se dibuja como
    /// <c>&lt;th scope="row"&gt;</c> y es el título de la tarjeta apilada.
    /// </summary>
    [Parameter] public bool EsEncabezadoDeFila { get; set; }

    /// <summary>Columna numérica: alineada a la derecha y con cifras tabulares.</summary>
    [Parameter] public bool EsNumerica { get; set; }

    /// <summary>Identificador de la celda para las pruebas de extremo a extremo.</summary>
    [Parameter] public string? Testid { get; set; }

    protected override void OnInitialized()
    {
        if (Grilla is null)
        {
            throw new InvalidOperationException(
                "ColumnaDeGrilla solo se puede usar dentro del bloque <Columnas> de una Grilla.");
        }

        Grilla.Agregar(this);
    }

    /// <summary>No emite marcado propio: la grilla dibuja sus celdas.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }
}
