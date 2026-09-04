using Microsoft.AspNetCore.Components;

namespace MovilidadUrbana.Web.Components.Componentes;

/// <summary>
/// Grilla de listado: concentra el marcado de la tabla y de las tarjetas apiladas, con
/// <c>&lt;caption&gt;</c> accesible, <c>scope</c> en todos los encabezados y los estados del ciclo
/// de datos. Escribir la tabla en cada página que lista algo es lo que garantiza deriva entre
/// superficies, así que el patrón vive acá y en un solo lugar.
/// </summary>
/// <typeparam name="TItem">Tipo de cada elemento de la colección.</typeparam>
public partial class Grilla<TItem> : ComponentBase
{
    private readonly List<ColumnaDeGrilla<TItem>> _columnas = [];

    /// <summary>Elementos que se presentan. Ya vienen filtrados por la superficie.</summary>
    [Parameter] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Estado vigente de la colección.</summary>
    [Parameter] public EstadoDeSuperficie Estado { get; set; } = EstadoDeSuperficie.ConDatos;

    /// <summary>Texto del <c>&lt;caption&gt;</c>: dice qué compara la tabla, para quien no la ve.</summary>
    [Parameter, EditorRequired] public string TituloAccesible { get; set; } = default!;

    /// <summary>Declaración de las columnas, con <see cref="ColumnaDeGrilla{TItem}" />.</summary>
    [Parameter, EditorRequired] public RenderFragment Columnas { get; set; } = default!;

    /// <summary>Acciones de cada fila. Lo que el estado no admite no se dibuja.</summary>
    [Parameter] public RenderFragment<TItem>? Acciones { get; set; }

    /// <summary>Bloque del estado vacío: no hay datos todavía.</summary>
    [Parameter] public RenderFragment? Vacio { get; set; }

    /// <summary>Bloque del filtro sin resultados: hay datos y ninguno coincide.</summary>
    [Parameter] public RenderFragment? SinResultados { get; set; }

    /// <summary>Clave estable de cada elemento, para que el diferenciador no rearme las filas.</summary>
    [Parameter] public Func<TItem, object> Clave { get; set; } = elemento => elemento!;

    [Parameter] public string RotuloDeAcciones { get; set; } = "Operaciones";

    [Parameter] public string? TestidCuerpo { get; set; }

    [Parameter] public string? TestidFila { get; set; }

    [Parameter] public string? TestidTarjeta { get; set; }

    private IReadOnlyList<ColumnaDeGrilla<TItem>> Definiciones => _columnas;

    /// <summary>Columnas que van al cuerpo de la tarjeta apilada, en el orden declarado.</summary>
    private IEnumerable<ColumnaDeGrilla<TItem>> Detalles =>
        _columnas.Where(columna => !columna.EsEncabezadoDeFila);

    /// <summary>Título de la tarjeta apilada: la columna que encabeza la fila.</summary>
    private RenderFragment Encabezado(TItem elemento) => constructor =>
    {
        var columna = _columnas.FirstOrDefault(c => c.EsEncabezadoDeFila) ?? _columnas.FirstOrDefault();
        if (columna is not null)
        {
            constructor.AddContent(0, columna.Plantilla(elemento));
        }
    };

    /// <summary>
    /// Registro de una columna. Las columnas se declaran como componentes hijos, así que la
    /// primera pasada de render de la grilla todavía no las conoce: al registrarse piden una
    /// pasada más, que es la que dibuja la tabla completa.
    /// </summary>
    internal void Agregar(ColumnaDeGrilla<TItem> columna)
    {
        if (_columnas.Contains(columna)) return;

        _columnas.Add(columna);
        StateHasChanged();
    }
}
