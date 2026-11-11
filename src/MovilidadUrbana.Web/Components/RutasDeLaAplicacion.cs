using Microsoft.AspNetCore.Components;

namespace MovilidadUrbana.Web.Components;

/// <summary>Las plantillas de ruta reales: las <c>@page</c> de los componentes del ensamblado.</summary>
public static class RutasDeLaAplicacion
{
    public static IEnumerable<string> Plantillas() =>
        typeof(RutasDeLaAplicacion).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes(typeof(RouteAttribute), inherit: false).Cast<RouteAttribute>())
            .Select(r => r.Template.Trim('/'));

    /// <summary>Las claves que la página de una ruta lee de la consulta.</summary>
    public static IReadOnlySet<string> ParametrosDeConsulta(string plantilla)
    {
        var tipo = typeof(RutasDeLaAplicacion).Assembly.GetTypes().FirstOrDefault(t =>
            t.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>()
                .Any(r => CoincideConParametros(r.Template.Trim('/'), plantilla)));
        if (tipo is null) return new HashSet<string>();
        return tipo.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            .Select(p => (p, a: p.GetCustomAttributes(typeof(SupplyParameterFromQueryAttribute), true).Cast<SupplyParameterFromQueryAttribute>().FirstOrDefault()))
            .Where(par => par.a is not null)
            .Select(par => (par.a!.Name ?? par.p.Name).ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool CoincideConParametros(string ruta, string plantilla)
    {
        var a = ruta.Split('/');
        var b = plantilla.Split('/');
        return a.Length == b.Length && a.Zip(b).All(p => string.Equals(p.First, p.Second, StringComparison.OrdinalIgnoreCase) || p.First.StartsWith('{'));
    }
}
