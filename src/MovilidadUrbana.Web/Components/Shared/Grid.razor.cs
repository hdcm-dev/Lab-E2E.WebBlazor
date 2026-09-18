using Microsoft.AspNetCore.Components;

namespace MovilidadUrbana.Web.Components.Shared;

/// <summary>
/// Grilla de listado: concentra el marcado de la tabla y de las tarjetas apiladas, con
/// <c>&lt;caption&gt;</c> accesible, <c>scope</c> en todos los encabezados y los estados del ciclo
/// de datos. Escribir la tabla en cada página que lista algo es lo que garantiza deriva entre
/// superficies, así que el patrón vive acá y en un solo lugar.
/// </summary>
/// <typeparam name="TItem">Tipo de cada elemento de la colección.</typeparam>
public partial class Grid<TItem> : ComponentBase
{
    private readonly List<GridColumn<TItem>> _columns = [];

    /// <summary>Elementos que se presentan. Ya vienen filtrados por la superficie.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Estado vigente de la colección.</summary>
    [Parameter] public SurfaceState State { get; set; } = SurfaceState.ConDatos;

    /// <summary>Texto del <c>&lt;caption&gt;</c>: dice qué compara la tabla, para quien no la ve.</summary>
    [Parameter, EditorRequired] public string AccessibleTitle { get; set; } = default!;

    /// <summary>Declaración de las columnas, con <see cref="ColumnaDeGrilla{TItem}" />.</summary>
    [Parameter, EditorRequired] public RenderFragment Columns { get; set; } = default!;

    /// <summary>Acciones de cada fila. Lo que el estado no admite no se dibuja.</summary>
    [Parameter] public RenderFragment<TItem>? Actions { get; set; }

    /// <summary>Bloque del estado vacío: no hay datos todavía.</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>Bloque del filtro sin resultados: hay datos y ninguno coincide.</summary>
    [Parameter] public RenderFragment? NoResults { get; set; }

    /// <summary>Clave estable de cada elemento, para que el diferenciador no rearme las filas.</summary>
    [Parameter] public Func<TItem, object> Clave { get; set; } = elemento => elemento!;

    [Parameter] public string ActionsLabel { get; set; } = "Operaciones";

    [Parameter] public string? BodyTestid { get; set; }

    [Parameter] public string? RowTestid { get; set; }

    [Parameter] public string? CardTestid { get; set; }

    private IReadOnlyList<GridColumn<TItem>> Definitions => _columns;

    /// <summary>Columnas que van al cuerpo de la tarjeta apilada, en el orden declarado.</summary>
    private IEnumerable<GridColumn<TItem>> Details =>
        _columns.Where(column => !column.IsRowHeader);

    /// <summary>Título de la tarjeta apilada: la columna que encabeza la fila.</summary>
    private RenderFragment HeaderName(TItem elemento) => constructor =>
    {
        var column = _columns.FirstOrDefault(c => c.IsRowHeader) ?? _columns.FirstOrDefault();
        if (column is not null)
        {
            constructor.AddContent(0, column.Template(elemento));
        }
    };

    /// <summary>
    /// Registro de una columna. Las columnas se declaran como componentes hijos, así que la
    /// primera pasada de render de la grilla todavía no las conoce: al registrarse piden una
    /// pasada más, que es la que dibuja la tabla completa.
    /// </summary>
    internal void Agregar(GridColumn<TItem> column)
    {
        if (_columns.Contains(column)) return;

        _columns.Add(column);
        StateHasChanged();
    }
}
