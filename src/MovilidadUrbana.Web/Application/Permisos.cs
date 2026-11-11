namespace MovilidadUrbana.Web.Application;

/// <summary>
/// Criterio único de «puede hacer» por acción (K6 §16). Lo usan el botón, el deeplink y el
/// destino del asistente: si cambia, cambia en los tres lugares a la vez.
/// </summary>
public static class Permisos
{
    /// <summary>Sólo se elimina una localidad que existe en la sesión actual.</summary>
    public static bool PuedeEliminar(IEnumerable<int> idsDeLaSesion, int id) => id > 0 && idsDeLaSesion.Contains(id);

    /// <summary>El paso 1 de la encuesta pide una localidad del ABM: sin localidades no se puede encuestar.</summary>
    public static bool PuedeEncuestar(int localidadesDeLaSesion) => localidadesDeLaSesion > 0;

    /// <summary>Los resultados se ven siempre; con n = 0 la página muestra su estado vacío.</summary>
    public static bool PuedeVerResultados() => true;
}
