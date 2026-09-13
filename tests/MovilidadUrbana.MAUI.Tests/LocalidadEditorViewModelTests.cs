namespace MovilidadUrbana.MAUI.Tests;

[TestFixture]
public class LocalidadEditorViewModelTests
{
    private Entorno _entorno = default!;

    [SetUp] public void Crear() => _entorno = new Entorno();
    [TearDown] public void Limpiar() => _entorno.Dispose();

    [Test]
    [Description("Con campos inválidos muestra un error por campo y no vuelve a la lista")]
    public async Task GuardarInvalidoMuestraErrores()
    {
        var vm = _entorno.Editor();
        vm.Preparar(null);
        vm.Nombre = "Go";
        vm.CodigoPostal = "12";

        await vm.GuardarCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorNombre, Is.Not.Null);
        Assert.That(vm.ErrorProvincia, Is.Not.Null);
        Assert.That(vm.ErrorCodigoPostal, Is.Not.Null);
        Assert.That(vm.ErrorHabitantes, Is.Not.Null);
        Assert.That(vm.Aviso, Is.Not.Null);
        Assert.That(_entorno.Navegador.Vueltas, Is.Zero);
    }

    [Test]
    [Description("Corregir un campo borra su error, sin tocar los errores de los demás")]
    public async Task CorregirUnCampoBorraSuError()
    {
        var vm = _entorno.Editor();
        vm.Preparar(null);
        await vm.GuardarCommand.ExecuteAsync(null);

        vm.Provincia = "Corrientes";

        Assert.That(vm.ErrorProvincia, Is.Null);
        Assert.That(vm.ErrorNombre, Is.Not.Null);
    }

    [Test]
    [Description("Un alta válida confirma con un aviso breve, vuelve y la localidad queda guardada")]
    public async Task GuardarNuevaAvisaYVuelve()
    {
        var vm = _entorno.Editor();
        vm.Preparar(null);
        vm.Nombre = "Goya"; vm.Provincia = "Corrientes"; vm.CodigoPostal = "3450"; vm.Habitantes = "90.000";

        await vm.GuardarCommand.ExecuteAsync(null);

        Assert.That(_entorno.Avisos.Mostrados, Is.EqualTo(new[] { "Se agregó la localidad Goya." }));
        Assert.That(_entorno.Navegador.Vueltas, Is.EqualTo(1));
        Assert.That((await _entorno.Localidades.ListarAsync()).Single(l => l.Nombre == "Goya").Habitantes, Is.EqualTo(90000));
    }

    [Test]
    [Description("Modificar conserva el identificador y actualiza los datos")]
    public async Task ModificarActualiza()
    {
        var lista = _entorno.Lista();
        await lista.CargarCommand.ExecuteAsync(null);
        var vm = _entorno.Editor();
        vm.Preparar(lista.Visibles[0]);

        vm.Nombre = "Corrientes Capital";
        await vm.GuardarCommand.ExecuteAsync(null);

        Assert.That(vm.EsEdicion, Is.True);
        Assert.That((await _entorno.Localidades.ListarAsync()).Select(l => l.Nombre), Does.Contain("Corrientes Capital"));
    }

    [Test]
    [Description("Si se cancela la confirmación, la baja no ocurre")]
    public async Task EliminarCanceladoNoBorra()
    {
        var lista = _entorno.Lista();
        await lista.CargarCommand.ExecuteAsync(null);
        var vm = _entorno.Editor();
        vm.Preparar(lista.Visibles[0]);
        _entorno.Avisos.RespuestaAConfirmar = false;

        await vm.EliminarCommand.ExecuteAsync(null);

        Assert.That(await _entorno.Localidades.ListarAsync(), Has.Count.EqualTo(2));
        Assert.That(_entorno.Navegador.Vueltas, Is.Zero);
    }

    [Test]
    [Description("Confirmada, la baja ocurre, avisa y vuelve")]
    public async Task EliminarConfirmadoBorra()
    {
        var lista = _entorno.Lista();
        await lista.CargarCommand.ExecuteAsync(null);
        var vm = _entorno.Editor();
        vm.Preparar(lista.Visibles[0]);

        await vm.EliminarCommand.ExecuteAsync(null);

        Assert.That(await _entorno.Localidades.ListarAsync(), Has.Count.EqualTo(1));
        Assert.That(_entorno.Avisos.Mostrados.Single(), Does.StartWith("Se eliminó la localidad"));
        Assert.That(_entorno.Navegador.Vueltas, Is.EqualTo(1));
    }
}
