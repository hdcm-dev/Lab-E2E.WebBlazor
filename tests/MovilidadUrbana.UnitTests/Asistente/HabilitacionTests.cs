using Microsoft.Extensions.Configuration;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Infrastructure.AI;

namespace MovilidadUrbana.UnitTests.Asistente;

/// <summary>REQ-IA-29 y REQ-IA-31: el motivo exacto, nombrando la clave y nunca su valor.</summary>
public class HabilitacionTests
{
    private static Habilitacion Con(params (string Clave, string Valor)[] valores) =>
        Habilitacion.Evaluar(new ConfigurationBuilder().AddInMemoryCollection(valores.Select(v => new KeyValuePair<string, string?>(v.Clave, v.Valor))).Build());

    [Test]
    public void SinNada_FaltaEndpoint() => Assert.That(Con().Motivo, Is.EqualTo(MotivoDeHabilitacion.FaltaEndpoint));

    [Test]
    public void SinModelo_MotivoExacto_SinDireccion()
    {
        var h = Con(("OLLAMA_ENDPOINT", "http://servidor.invalid:1234"));
        Assert.Multiple(() =>
        {
            Assert.That(h.Motivo, Is.EqualTo(MotivoDeHabilitacion.FaltaModelo));
            Assert.That(h.Clave, Is.EqualTo("OLLAMA_MODEL"));
            Assert.That(Plantillas.Aviso(h.Motivo, h.Clave), Does.Contain("falta configurar OLLAMA_MODEL").And.Not.Contain("servidor.invalid"));
        });
    }

    [Test]
    public void ClaveVariante_MotivoClaveDesconocida()
    {
        var h = Con(("OLLAMA_ENDPOINT", "http://servidor.invalid"), ("OLLAMA_MODELO", "x"));
        Assert.Multiple(() =>
        {
            Assert.That(h.Motivo, Is.EqualTo(MotivoDeHabilitacion.ClaveDesconocida));
            Assert.That(h.Clave, Is.EqualTo("OLLAMA_MODELO"));
        });
    }

    [Test]
    public void Apagado_SinPanel()
    {
        var h = Con(("Asistente:Habilitado", "false"), ("OLLAMA_ENDPOINT", "http://servidor.invalid"), ("OLLAMA_MODEL", "x"));
        Assert.Multiple(() =>
        {
            Assert.That(h.PanelVisible, Is.False);
            Assert.That(h.Motivo, Is.EqualTo(MotivoDeHabilitacion.ApagadaPorParametro));
        });
    }

    [Test]
    public void Completa_Habilitada()
    {
        var h = Con(("OLLAMA_ENDPOINT", "http://servidor.invalid"), ("OLLAMA_MODEL", "x"), ("OLLAMA_NUM_CTX", "4096"));
        Assert.Multiple(() =>
        {
            Assert.That(h.Motivo, Is.EqualTo(MotivoDeHabilitacion.Habilitada));
            Assert.That(h.PanelVisible, Is.True);
            Assert.That(h.NumCtx, Is.EqualTo(4096));
        });
    }
}
