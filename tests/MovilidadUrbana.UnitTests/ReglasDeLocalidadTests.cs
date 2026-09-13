using MovilidadUrbana.Web.Dominio.Reglas;

namespace MovilidadUrbana.UnitTests;

/// <summary>
/// Cada regla del ABM, caso por caso y sin navegador. Es la contraparte barata de las E2E: acá se
/// cubren los bordes de cada validación y allá queda un único caso que comprueba que el error
/// llega a la pantalla.
/// </summary>
[TestFixture]
public class ReglasDeLocalidadTests
{
    [TestCase("Goya", ExpectedResult = true)]
    [TestCase("  Goya  ", ExpectedResult = true, Description = "Los espacios de los extremos no cuentan")]
    [TestCase("Ab", ExpectedResult = false)]
    [TestCase("  A  ", ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool NombreValido(string? nombre) => ReglasDeLocalidad.NombreValido(nombre);

    [TestCase("Corrientes", ExpectedResult = true)]
    [TestCase("   ", ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool ProvinciaValida(string? provincia) => ReglasDeLocalidad.ProvinciaValida(provincia);

    [TestCase("3400", ExpectedResult = true)]
    [TestCase(" 3400 ", ExpectedResult = true, Description = "Se recorta antes de validar")]
    [TestCase("340", ExpectedResult = false)]
    [TestCase("34000", ExpectedResult = false)]
    [TestCase("34O0", ExpectedResult = false, Description = "Una letra no es un dígito")]
    [TestCase("W3400", ExpectedResult = false, Description = "El código postal alfanumérico no se admite")]
    [TestCase("", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool CodigoPostalValido(string? codigoPostal) => ReglasDeLocalidad.CodigoPostalValido(codigoPostal);

    [TestCase(1, ExpectedResult = true, Description = "El mínimo admitido")]
    [TestCase(346334, ExpectedResult = true)]
    [TestCase(0, ExpectedResult = false)]
    [TestCase(-5, ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool HabitantesValidos(int? habitantes) => ReglasDeLocalidad.HabitantesValidos(habitantes);

    [Test]
    [Description("El duplicado ignora mayúsculas y espacios, pero no cruza provincias")]
    public void MismaLocalidadCompara()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ReglasDeLocalidad.MismaLocalidad("corrientes", "Corrientes", "Corrientes", "Corrientes"), Is.True);
            Assert.That(ReglasDeLocalidad.MismaLocalidad(" Corrientes ", "Corrientes", "Corrientes", "Corrientes"), Is.True);
            Assert.That(ReglasDeLocalidad.MismaLocalidad("Corrientes", "Corrientes", "Corrientes", "Chaco"), Is.False);
            Assert.That(ReglasDeLocalidad.MismaLocalidad("Goya", "Corrientes", "Corrientes", "Corrientes"), Is.False);
        });
    }

    [Test]
    [Description("La comparación de provincias distingue mayúsculas, a diferencia de la de nombres")]
    public void LaProvinciaSeComparaDeFormaOrdinal() =>
        Assert.That(ReglasDeLocalidad.MismaLocalidad("Goya", "corrientes", "Goya", "Corrientes"), Is.False);
}
