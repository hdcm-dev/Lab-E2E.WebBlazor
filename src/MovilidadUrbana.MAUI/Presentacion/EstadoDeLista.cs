namespace MovilidadUrbana.MAUI.Presentacion;

/// <summary>
/// Los estados de una lista, el mismo vocabulario que la web. <see cref="Vacio"/> y
/// <see cref="FiltradoSinResultados"/> son distintos porque ofrecen salidas distintas: cargar la
/// primera o limpiar el filtro.
/// </summary>
public enum EstadoDeLista
{
    Cargando,
    ConDatos,
    Vacio,
    FiltradoSinResultados
}
