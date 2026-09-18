using MovilidadUrbana.MAUI.Presentation.Encuestas;

namespace MovilidadUrbana.MAUI.Tests;

[TestFixture]
public class EncuestaViewModelTests
{
    private TestEnvironment _entorno = default!;

    [SetUp] public void Create() => _entorno = new TestEnvironment();
    [TearDown] public void Clear() => _entorno.Dispose();

    private static void CompletarPaso1(EncuestaViewModel vm) { vm.Nombre = "Ana Pérez"; vm.Edad = "34"; vm.Localidad = "Corrientes"; }
    private static void CompletarPaso2(EncuestaViewModel vm)
    {
        vm.Medios.Single(m => m.Clave == "colectivo").IsSelected = true;
        vm.Medios.Single(m => m.Clave == "bicicleta").ToggleCommand.Execute(null);
        vm.Frecuencia = "diaria";
    }
    private static void CompletarPaso3(EncuestaViewModel vm) { vm.Distancia = "12,5"; vm.Minutos = "45"; vm.Motivo = "trabajo"; }

    [Test]
    [Description("Carga las localidades del ABM y arranca en el paso 1 con cero registradas")]
    public async Task CargaLasLocalidadesDelAbm()
    {
        var vm = _entorno.Encuesta();

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.That(vm.Localidades, Is.EqualTo(new[] { "Corrientes", "Resistencia" }));
        Assert.That(vm.StepLabel, Is.EqualTo("Paso 1 de 3"));
        Assert.That(vm.IsFirstStep, Is.True);
        Assert.That(vm.Registradas, Is.Zero);
    }

    [Test]
    [Description("No avanza del paso 1 con datos inválidos y muestra los errores")]
    public async Task NoAvanzaDelPaso1ConDatosInvalidos()
    {
        var vm = _entorno.Encuesta();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.Edad = "12";

        vm.NextCommand.Execute(null);

        Assert.That(vm.Paso, Is.EqualTo(1));
        Assert.That(vm.Notice, Is.EqualTo(EncuestaViewModel.IncompleteStepNotice));
        Assert.That(vm.NombreError, Does.Contain("mínimo 3 caracteres"));
        Assert.That(vm.EdadError, Does.Contain("entre 16 y 110"));
        Assert.That(vm.LocalidadError, Is.Not.Null);
    }

    [Test]
    [Description("Marcar un medio borra el error de medios; elegir la frecuencia, el suyo")]
    public async Task CorregirElPaso2BorraSusErrores()
    {
        var vm = _entorno.Encuesta();
        await vm.LoadCommand.ExecuteAsync(null);
        CompletarPaso1(vm); vm.NextCommand.Execute(null);
        vm.NextCommand.Execute(null);
        Assert.That(vm.MediosError, Is.Not.Null);

        vm.Medios[0].ToggleCommand.Execute(null);
        vm.Frecuencia = "semanal";

        Assert.That(vm.MediosError, Is.Null);
        Assert.That(vm.FrecuenciaError, Is.Null);
    }

    [Test]
    [Description("Volver atrás conserva lo cargado")]
    public async Task VolverAtrasConservaLoCargado()
    {
        var vm = _entorno.Encuesta();
        await vm.LoadCommand.ExecuteAsync(null);
        CompletarPaso1(vm); vm.NextCommand.Execute(null);
        CompletarPaso2(vm); vm.NextCommand.Execute(null);
        Assert.That(vm.ShowRegistrar, Is.True);

        vm.PreviousCommand.Execute(null);
        vm.PreviousCommand.Execute(null);

        Assert.That(vm.Paso, Is.EqualTo(1));
        Assert.That(vm.Nombre, Is.EqualTo("Ana Pérez"));
        Assert.That(vm.Medios.Where(m => m.IsSelected).Select(m => m.Clave), Is.EquivalentTo(new[] { "colectivo", "bicicleta" }));
    }

    [Test]
    [Description("Recorre los tres pasos, registra y muestra el resumen con el contador actualizado")]
    public async Task RecorreYRegistra()
    {
        var vm = _entorno.Encuesta();
        await vm.LoadCommand.ExecuteAsync(null);
        CompletarPaso1(vm); vm.NextCommand.Execute(null);
        CompletarPaso2(vm); vm.NextCommand.Execute(null);
        CompletarPaso3(vm);

        await vm.RegistrarCommand.ExecuteAsync(null);

        Assert.That(vm.Completada, Is.True);
        Assert.That(vm.StepLabel, Is.EqualTo("Encuesta completada"));
        Assert.That(vm.Resumen.Single(c => c.Clave == "medios").Valor, Is.EqualTo("Colectivo, Bicicleta"));
        Assert.That(vm.Resumen.Single(c => c.Clave == "distancia").Valor, Is.EqualTo("12,5 km"));
        Assert.That(vm.Registradas, Is.EqualTo(1));
    }

    [Test]
    [Description("No registra con el paso 3 incompleto")]
    public async Task NoRegistraConElPaso3Incompleto()
    {
        var vm = _entorno.Encuesta();
        await vm.LoadCommand.ExecuteAsync(null);
        CompletarPaso1(vm); vm.NextCommand.Execute(null);
        CompletarPaso2(vm); vm.NextCommand.Execute(null);
        vm.Distancia = "900";

        await vm.RegistrarCommand.ExecuteAsync(null);

        Assert.That(vm.Completada, Is.False);
        Assert.That(vm.DistanciaError, Does.Contain("entre 0 y 500 km"));
        Assert.That(await _entorno.Encuestas.CountAsync(), Is.Zero);
    }

    [Test]
    [Description("«Cargar otra encuesta» vuelve al paso 1 vacío y conserva el contador")]
    public async Task NuevaEncuestaReinicia()
    {
        var vm = _entorno.Encuesta();
        await vm.LoadCommand.ExecuteAsync(null);
        CompletarPaso1(vm); vm.NextCommand.Execute(null);
        CompletarPaso2(vm); vm.NextCommand.Execute(null);
        CompletarPaso3(vm);
        await vm.RegistrarCommand.ExecuteAsync(null);

        vm.NuevaEncuestaCommand.Execute(null);

        Assert.That(vm.Paso, Is.EqualTo(1));
        Assert.That(vm.Completada, Is.False);
        Assert.That(vm.Nombre, Is.Empty);
        Assert.That(vm.Medios.Any(m => m.IsSelected), Is.False);
        Assert.That(vm.Registradas, Is.EqualTo(1));
    }
}
