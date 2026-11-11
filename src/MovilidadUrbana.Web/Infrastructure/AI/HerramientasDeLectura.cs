using System.Reflection;
using Microsoft.Extensions.AI;
using MovilidadUrbana.Web.Application.Asistente;

namespace MovilidadUrbana.Web.Infrastructure.AI;

/// <summary>
/// Las funciones de lectura expresadas como <see cref="AIFunction" />, cerradas sobre la instancia
/// del circuito. Hoy no se ofrecen al modelo (ADR 0004: las invoca el enrutador); existen para que
/// la comparación por los dos caminos (D-4) no pida reescribir nada.
/// </summary>
public static class HerramientasDeLectura
{
    public static IReadOnlyList<AIFunction> Crear(FuncionesDeLectura funciones) =>
        typeof(FuncionesDeLectura).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => (m, a: m.GetCustomAttribute<FuncionDeSoloLecturaAttribute>()))
            .Where(par => par.a is not null)
            .Select(par => AIFunctionFactory.Create(par.m, funciones, new AIFunctionFactoryOptions { Name = par.a!.Nombre }))
            .ToList();
}
