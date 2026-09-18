using MovilidadUrbana.MAUI.Presentation;
using MovilidadUrbana.MAUI.Presentation.Localidades;

namespace MovilidadUrbana.MAUI.Tests;

[TestFixture]
public class LocalidadesViewModelTests
{
    private TestEnvironment _entorno = default!;

    [SetUp] public void Create() => _entorno = new TestEnvironment();
    [TearDown] public void Clear() => _entorno.Dispose();

    [Test]
    [Description("Al cargar muestra las localidades sembradas, ordenadas por nombre")]
    public async Task CargaLasSembradasOrdenadas()
    {
        var vm = _entorno.Lista();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.That(vm.Visible.Select(l => l.Nombre), Is.EqualTo(new[] { "Corrientes", "Resistencia" }));
        Assert.That(vm.State, Is.EqualTo(ListState.ConDatos));
        Assert.That(vm.Visible[0].HabitantesTexto, Is.EqualTo("346.334 hab."));
    }

    [Test]
    [Description("Un filtro sin coincidencias da FiltradoSinResultados, y limpiarlo vuelve a mostrar todo")]
    public async Task FiltrarSinCoincidenciasYLimpiar()
    {
        var vm = _entorno.Lista();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SearchText = "zzz";
        Assert.That(vm.State, Is.EqualTo(ListState.FiltradoSinResultados));

        vm.ClearFilterCommand.Execute(null);
        Assert.That(vm.State, Is.EqualTo(ListState.ConDatos));
        Assert.That(vm.Visible, Has.Count.EqualTo(2));
    }

    [Test]
    [Description("Filtra por provincia y por código postal")]
    public async Task FiltraPorProvinciaYPorCodigoPostal()
    {
        var vm = _entorno.Lista();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.Provincia = "Chaco";
        Assert.That(vm.Visible.Select(l => l.Nombre), Is.EqualTo(new[] { "Resistencia" }));

        vm.Provincia = LocalidadesViewModel.AllProvincias;
        vm.SearchText = "3400";
        Assert.That(vm.Visible.Select(l => l.Nombre), Is.EqualTo(new[] { "Corrientes" }));
        Assert.That(vm.Resumen, Is.EqualTo("1 de 2 localidades"));
    }

    [Test]
    [Description("Sin ninguna localidad el estado es Vacio, distinto de FiltradoSinResultados")]
    public async Task SinLocalidadesEsVacio()
    {
        foreach (var l in await _entorno.Localidades.GetAllAsync()) await _entorno.Localidades.DeleteAsync(l.Id);
        var vm = _entorno.Lista();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.That(vm.State, Is.EqualTo(ListState.Vacio));
        Assert.That(vm.IsEmpty, Is.True);
    }

    [Test]
    [Description("Agregar abre el editor vacío; tocar una fila lo abre con esa localidad")]
    public async Task AbreElEditor()
    {
        var vm = _entorno.Lista();
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.AddCommand.ExecuteAsync(null);
        await vm.EditCommand.ExecuteAsync(vm.Visible[1]);

        Assert.That(_entorno.Navegador.OpenedEditors, Has.Count.EqualTo(2));
        Assert.That(_entorno.Navegador.OpenedEditors[0], Is.Null);
        Assert.That(_entorno.Navegador.OpenedEditors[1]!.Nombre, Is.EqualTo("Resistencia"));
    }
}
