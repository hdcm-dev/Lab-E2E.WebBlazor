namespace MovilidadUrbana.MAUI.Tests;

[TestFixture]
public class LocalidadEditorViewModelTests
{
    private TestEnvironment _entorno = default!;

    [SetUp] public void Create() => _entorno = new TestEnvironment();
    [TearDown] public void Clear() => _entorno.Dispose();

    [Test]
    [Description("Con campos inválidos muestra un error por campo y no vuelve a la lista")]
    public async Task GuardarInvalidoMuestraErrores()
    {
        var vm = _entorno.Editor();
        vm.Initialize(null);
        vm.Nombre = "Go";
        vm.CodigoPostal = "12";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(vm.NombreError, Is.Not.Null);
        Assert.That(vm.ProvinciaError, Is.Not.Null);
        Assert.That(vm.CodigoPostalError, Is.Not.Null);
        Assert.That(vm.HabitantesError, Is.Not.Null);
        Assert.That(vm.Notice, Is.Not.Null);
        Assert.That(_entorno.Navegador.GoBackCount, Is.Zero);
    }

    [Test]
    [Description("Corregir un campo borra su error, sin tocar los errores de los demás")]
    public async Task CorregirUnCampoBorraSuError()
    {
        var vm = _entorno.Editor();
        vm.Initialize(null);
        await vm.SaveCommand.ExecuteAsync(null);

        vm.Provincia = "Corrientes";

        Assert.That(vm.ProvinciaError, Is.Null);
        Assert.That(vm.NombreError, Is.Not.Null);
    }

    [Test]
    [Description("Un alta válida confirma con un aviso breve, vuelve y la localidad queda guardada")]
    public async Task GuardarNuevaAvisaYVuelve()
    {
        var vm = _entorno.Editor();
        vm.Initialize(null);
        vm.Nombre = "Goya"; vm.Provincia = "Corrientes"; vm.CodigoPostal = "3450"; vm.Habitantes = "90.000";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(_entorno.Avisos.Shown, Is.EqualTo(new[] { "Se agregó la localidad Goya." }));
        Assert.That(_entorno.Navegador.GoBackCount, Is.EqualTo(1));
        Assert.That((await _entorno.Localidades.GetAllAsync()).Single(l => l.Nombre == "Goya").Habitantes, Is.EqualTo(90000));
    }

    [Test]
    [Description("Modificar conserva el identificador y actualiza los datos")]
    public async Task ModificarActualiza()
    {
        var lista = _entorno.Lista();
        await lista.LoadCommand.ExecuteAsync(null);
        var vm = _entorno.Editor();
        vm.Initialize(lista.Visible[0]);

        vm.Nombre = "Corrientes Capital";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(vm.IsEdit, Is.True);
        Assert.That((await _entorno.Localidades.GetAllAsync()).Select(l => l.Nombre), Does.Contain("Corrientes Capital"));
    }

    [Test]
    [Description("Si se cancela la confirmación, la baja no ocurre")]
    public async Task EliminarCanceladoNoBorra()
    {
        var lista = _entorno.Lista();
        await lista.LoadCommand.ExecuteAsync(null);
        var vm = _entorno.Editor();
        vm.Initialize(lista.Visible[0]);
        _entorno.Avisos.ConfirmResult = false;

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.That(await _entorno.Localidades.GetAllAsync(), Has.Count.EqualTo(2));
        Assert.That(_entorno.Navegador.GoBackCount, Is.Zero);
    }

    [Test]
    [Description("Confirmada, la baja ocurre, avisa y vuelve")]
    public async Task EliminarConfirmadoBorra()
    {
        var lista = _entorno.Lista();
        await lista.LoadCommand.ExecuteAsync(null);
        var vm = _entorno.Editor();
        vm.Initialize(lista.Visible[0]);

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.That(await _entorno.Localidades.GetAllAsync(), Has.Count.EqualTo(1));
        Assert.That(_entorno.Avisos.Shown.Single(), Does.StartWith("Se eliminó la localidad"));
        Assert.That(_entorno.Navegador.GoBackCount, Is.EqualTo(1));
    }
}
