using System.Reflection;
using System.Text.RegularExpressions;
using MovilidadUrbana.Web.Application.Abstractions;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Infrastructure.Persistence;

namespace MovilidadUrbana.UnitTests.Arquitectura;

/// <summary>
/// Las capas son carpetas de un ensamblado (ADR 0003): lo que el compilador no impide, lo impide
/// esta prueba. Nombra el SDK de IA a propósito. Mutación registrada: un
/// <c>using Microsoft.Extensions.AI;</c> en <c>Domain/Catalogos.cs</c> pone en rojo
/// <see cref="Domain_SinDependenciasDeIa" />.
/// </summary>
public class ArquitecturaTests
{
    private static readonly string[] Sdk = ["Microsoft.Extensions.AI", "OllamaSharp"];

    private static IEnumerable<string> Lineas(string archivo) =>
        File.ReadLines(archivo).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal) && !l.TrimStart().StartsWith("///", StringComparison.Ordinal) && !l.TrimStart().StartsWith('*'));

    private static List<string> QueNombran(IEnumerable<string> archivos, params string[] textos) =>
        archivos.Where(a => Lineas(a).Any(l => textos.Any(t => l.Contains(t, StringComparison.Ordinal))))
            .Select(a => Path.GetRelativePath(Repositorio.Web, a)).ToList();

    [Test(Description = "REQ-IA-01: el dominio no conoce el SDK de IA")]
    public void Domain_SinDependenciasDeIa()
    {
        Assert.That(QueNombran(Repositorio.Fuentes("Domain"), Sdk), Is.Empty);
        var tipos = typeof(MovilidadUrbana.Web.Domain.Catalogos).Assembly.GetTypes().Where(t => t.Namespace?.StartsWith("MovilidadUrbana.Web.Domain", StringComparison.Ordinal) == true);
        Assert.That(tipos.SelectMany(TiposReferidos).Where(t => Sdk.Any(s => t.Namespace?.StartsWith(s, StringComparison.Ordinal) == true)), Is.Empty);
    }

    [Test(Description = "REQ-IA-02: el SDK sólo aparece en Infrastructure/AI y en la raíz")]
    public void SdkDeIa_SoloEnInfrastructureAIyRaiz()
    {
        var todos = Repositorio.Fuentes(".").ToList();
        var permitidos = todos.Where(a => a.Contains(Path.Combine("Infrastructure", "AI"), StringComparison.Ordinal) || Path.GetFileName(a) == "Program.cs");
        Assert.That(QueNombran(todos.Except(permitidos), Sdk), Is.Empty);
    }

    [Test(Description = "Application no depende de Infrastructure")]
    public void Application_SinInfrastructure() =>
        Assert.That(QueNombran(Repositorio.Fuentes("Application"), "MovilidadUrbana.Web.Infrastructure", "Microsoft.EntityFrameworkCore"), Is.Empty);

    [Test(Description = "REQ-IA-04: toda función de lectura envuelve un caso de uso, nunca un repositorio")]
    public void Funciones_SinRepositorios()
    {
        var prohibidos = new[] { typeof(ILocalidadRepository), typeof(IEncuestaRepository), typeof(AppDbContext) };
        var parametros = typeof(FuncionesDeLectura).GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType);
        Assert.That(parametros.Intersect(prohibidos), Is.Empty);
    }

    [Test(Description = "ADR 0005: las funciones son de sólo lectura y nada en la capa de IA escribe")]
    public void Funciones_SoloLectura()
    {
        var publicas = typeof(FuncionesDeLectura).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(publicas.Where(m => m.GetCustomAttribute<FuncionDeSoloLecturaAttribute>() is null).Select(m => m.Name), Is.Empty);

        var escrituras = new[] { "SaveAsync(", "DeleteAsync(", "AddAsync(", "UpdateAsync(", "SaveChanges", "ExecuteDelete" };
        Assert.That(QueNombran(Repositorio.Fuentes(Path.Combine("Infrastructure", "AI")).Concat(Repositorio.Fuentes(Path.Combine("Application", "Asistente"))), escrituras), Is.Empty);
    }

    [Test(Description = "K2 §8.3: el pipeline no lee los votos")]
    public void Pipeline_NoLeeVotos()
    {
        var directos = typeof(AsistenteService).GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).ToList();
        var indirectos = directos.SelectMany(t => t.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType));
        Assert.That(directos.Concat(indirectos), Has.No.Member(typeof(ILectorDeVotos)));
        Assert.That(typeof(IRegistroDeSenales).GetMethods().Select(m => m.Name), Is.EquivalentTo(new[] { "RegistrarAsync" }));
    }

    [Test(Description = "REQ-IA-31 y C-26: ni nombre de modelo ni dirección del servidor en el repositorio")]
    public void Repo_SinModeloNiEndpoint()
    {
        var patron = new Regex(@"(?i)\b(qwen|llama\d|gpt-oss|gemma\d?|mistral|phi\d|deepseek|nomic-embed)|\b100\.\d{1,3}\.\d{1,3}\.\d{1,3}\b|:11434\b");
        var carpetas = new[] { Path.Combine(Repositorio.Raiz, "src", "MovilidadUrbana.Web"), Path.Combine(Repositorio.Raiz, "docs"), Path.Combine(Repositorio.Raiz, "tools") };
        var hallados = carpetas.Where(Directory.Exists)
            .SelectMany(c => Directory.EnumerateFiles(c, "*", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.EndsWith(".db", StringComparison.Ordinal))
            .Where(f => patron.IsMatch(File.ReadAllText(f)))
            .ToList();
        Assert.That(hallados, Is.Empty);
    }

    private static IEnumerable<Type> TiposReferidos(Type t)
    {
        const BindingFlags todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var f in t.GetFields(todo)) yield return f.FieldType;
        foreach (var p in t.GetProperties(todo)) yield return p.PropertyType;
        foreach (var m in t.GetMethods(todo))
        {
            yield return m.ReturnType;
            foreach (var p in m.GetParameters()) yield return p.ParameterType;
        }
        if (t.BaseType is not null) yield return t.BaseType;
        foreach (var i in t.GetInterfaces()) yield return i;
    }
}
