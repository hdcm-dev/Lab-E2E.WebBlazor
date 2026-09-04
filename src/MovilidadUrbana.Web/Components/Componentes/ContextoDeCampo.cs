namespace MovilidadUrbana.Web.Components.Componentes;

/// <summary>
/// Lo que <c>Campo.razor</c> le pasa al control que envuelve: el identificador, la lista de
/// descriptores que el control tiene que citar en <c>aria-describedby</c> —el requisito y, si lo
/// hay, el error— y la clase que le corresponde según esté válido o no.
///
/// Existe para que la vista no arme esos identificadores a mano: un `aria-describedby` que
/// apunta a un nodo que no está es un error que no se ve mirando la pantalla.
/// </summary>
/// <param name="Id">Identificador del control.</param>
/// <param name="Descriptores">Valor para <c>aria-describedby</c>.</param>
/// <param name="Invalido">El campo tiene un error a la vista.</param>
public readonly record struct ContextoDeCampo(string Id, string Descriptores, bool Invalido)
{
    public string ClaseDeInput => Invalido ? "mq-input mq-input--invalido" : "mq-input";

    public string ClaseDeSelect => Invalido ? "mq-select mq-select--invalido" : "mq-select";
}
