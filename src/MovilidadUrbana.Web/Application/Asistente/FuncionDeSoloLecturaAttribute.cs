namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>
/// Marca un método de <see cref="FuncionesDeLectura" /> como función de sólo lectura. Hoy las
/// invoca el enrutador (el código elige, ARQ-1); la marca deja posible ofrecerlas al modelo más
/// adelante (D-4) y la prueba <c>Funciones_SoloLectura</c> la exige en todas.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FuncionDeSoloLecturaAttribute(string nombre) : Attribute
{
    public string Nombre { get; } = nombre;
}
