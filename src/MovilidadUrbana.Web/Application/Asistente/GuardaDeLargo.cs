namespace MovilidadUrbana.Web.Application.Asistente;

/// <summary>Tope de la pregunta (REQ-IA-21): lo que excede no viaja a ningún lado.</summary>
public static class GuardaDeLargo
{
    public const int Maximo = 300;

    public static bool Excede(string? pregunta) => (pregunta?.Length ?? 0) > Maximo;
}
