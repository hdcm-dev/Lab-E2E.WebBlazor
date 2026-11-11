using System.Reflection;

namespace MovilidadUrbana.UnitTests.Arquitectura;

/// <summary>Ubica la raíz del repositorio y el código de la web, para las pruebas que leen fuentes.</summary>
public static class Repositorio
{
    public static string Raiz { get; } = Path.GetFullPath(
        typeof(Repositorio).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(a => a.Key == "RaizDelRepositorio").Value!);

    public static string Web => Path.Combine(Raiz, "src", "MovilidadUrbana.Web");

    public static IEnumerable<string> Fuentes(string carpetaRelativa, params string[] extensiones)
    {
        var carpeta = Path.Combine(Web, carpetaRelativa);
        if (!Directory.Exists(carpeta)) return [];
        var ext = extensiones.Length == 0 ? [".cs", ".razor"] : extensiones;
        return Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories)
            .Where(f => ext.Contains(Path.GetExtension(f)) && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }
}
