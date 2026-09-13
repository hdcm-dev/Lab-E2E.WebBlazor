using MovilidadUrbana.MAUI.Presentacion;
using MovilidadUrbana.MAUI.Presentacion.Localidades;

namespace MovilidadUrbana.MAUI.Tests;

[TestFixture]
public class LocalidadesViewModelTests
{
    private Entorno _entorno = default!;

    [SetUp] public void Crear() => _entorno = new Entorno();
    [TearDown] public void Limpiar() => _entorno.Dispose();

    [Test]
    [Description("Al cargar muestra las localidades sembradas, ordenadas por nombre")]
    public async Task CargaLasSembradasOrdenadas()
    {
        var vm = _entorno.Lista();

        await vm.CargarCommand.ExecuteAsync(null);

        Assert.That(vm.Visibles.Select(l => l.Nombre), Is.EqualTo(new[] { "Corrientes", "Resistencia" }));
        Assert.That(vm.Estado, Is.EqualTo(EstadoDeLista.ConDatos));
        Assert.That(vm.Visibles[0].HabitantesTexto, Is.EqualTo("346.334 hab."));
    }

    [Test]
    [Description("Un filtro sin coincidencias da FiltradoSinResultados, y limpiarlo vuelve a mostrar todo")]
    public async Task FiltrarSinCoincidenciasYLimpiar()
    {
        var vm = _entorno.Lista();
        await vm.CargarCommand.ExecuteAsync(null);

        vm.Texto = "zzz";
        Assert.That(vm.Estado, Is.EqualTo(EstadoDeLista.FiltradoSinResultados));

        vm.LimpiarFiltroCommand.Execute(null);
        Assert.That(vm.Estado, Is.EqualTo(EstadoDeLista.ConDatos));
        Assert.That(vm.Visibles, Has.Count.EqualTo(2));
    }

    [Test]
    [Description("Filtra por provincia y por código postal")]
    public async Task FiltraPorProvinciaYPorCodigoPostal()
    {
        var vm = _entorno.Lista();
        await vm.CargarCommand.ExecuteAsync(null);

        vm.Provincia = "Chaco";
        Assert.That(vm.Visibles.Select(l => l.Nombre), Is.EqualTo(new[] { "Resistencia" }));

        vm.Provincia = LocalidadesViewModel.TodasLasProvincias;
        vm.Texto = "3400";
        Assert.That(vm.Visibles.Select(l => l.Nombre), Is.EqualTo(new[] { "Corrientes" }));
        Assert.That(vm.Resumen, Is.EqualTo("1 de 2 localidades"));
    }

    [Test]
    [Description("Sin ninguna localidad el estado es Vacio, distinto de FiltradoSinResultados")]
    public async Task SinLocalidadesEsVacio()
    {
        foreach (var l in await _entorno.Localidades.ListarAsync()) await _entorno.Localidades.EliminarAsync(l.Id);
        var vm = _entorno.Lista();

        await vm.CargarCommand.ExecuteAsync(null);

        Assert.That(vm.Estado, Is.EqualTo(EstadoDeLista.Vacio));
        Assert.That(vm.EsVacio, Is.True);
    }

    [Test]
    [Description("Agregar abre el editor vacío; tocar una fila lo abre con esa localidad")]
    public async Task AbreElEditor()
    {
        var vm = _entorno.Lista();
        await vm.CargarCommand.ExecuteAsync(null);

        await vm.AgregarCommand.ExecuteAsync(null);
        await vm.EditarCommand.ExecuteAsync(vm.Visibles[1]);

        Assert.That(_entorno.Navegador.EditoresAbiertos, Has.Count.EqualTo(2));
        Assert.That(_entorno.Navegador.EditoresAbiertos[0], Is.Null);
        Assert.That(_entorno.Navegador.EditoresAbiertos[1]!.Nombre, Is.EqualTo("Resistencia"));
    }
}
