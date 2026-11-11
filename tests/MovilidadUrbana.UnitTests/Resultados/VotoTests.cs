using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MovilidadUrbana.UnitTests.Dobles;
using MovilidadUrbana.Web.Application.Asistente;
using MovilidadUrbana.Web.Domain.Entities;

namespace MovilidadUrbana.UnitTests.Resultados;

/// <summary>Votos sin texto (E5, REQ-IA-30).</summary>
public class VotoTests
{
    [Test(Description = "El voto sólo tiene campos cerrados: los únicos textos son ids de catálogos")]
    public void Voto_SinTextoNiPersona()
    {
        var textos = typeof(Voto).GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => p.Name);
        Assert.That(textos, Is.EquivalentTo(new[] { nameof(Voto.Intencion), nameof(Voto.Destino), nameof(Voto.VersionGlosario), nameof(Voto.VersionCorpus) }));
        var nombres = typeof(Voto).GetProperties().Select(p => p.Name.ToLowerInvariant());
        Assert.That(nombres, Has.None.Contains("sesion").And.None.Contains("session").And.None.Contains("pregunta").And.None.Contains("respuesta").And.None.Contains("cookie"));
    }

    [Test]
    public async Task Voto_SePersisteYSeLee()
    {
        using var arnes = new Arnes(habilitada: false);
        var r = await arnes.PreguntarAsync("s", "¿Cuántas localidades tengo?");
        using (var alcance = arnes.Sesion("s"))
        {
            await alcance.ServiceProvider.GetRequiredService<AsistenteService>().VotarAsync(r, false, MotivoDelVoto.FaltaInformacion);
        }
        using var lectura = arnes.Sesion("otra");
        var votos = await lectura.ServiceProvider.GetRequiredService<ILectorDeVotos>().GetAllAsync();
        var voto = votos.Single(v => v.Origen == OrigenDelVoto.Persona);
        Assert.Multiple(() =>
        {
            Assert.That(voto.Intencion, Is.EqualTo(Intenciones.ContarLocalidades));
            Assert.That(voto.Destino, Is.EqualTo(Destinos.LocalidadesLista));
            Assert.That(voto.Util, Is.False);
            Assert.That(voto.Motivo, Is.EqualTo(MotivoDelVoto.FaltaInformacion));
            Assert.That(voto.VersionCorpus, Is.EqualTo(CorpusDeAyuda.Version));
        });
    }

    [Test]
    public void Registro_RechazaTextoFueraDelCatalogo()
    {
        using var arnes = new Arnes(habilitada: false);
        using var alcance = arnes.Sesion("s");
        var registro = alcance.ServiceProvider.GetRequiredService<IRegistroDeSenales>();
        Assert.ThrowsAsync<ArgumentException>(() => registro.RegistrarAsync(new Voto { Intencion = "¿cuántas tengo? texto libre", VersionCorpus = "x", VersionGlosario = "y" }));
    }
}
