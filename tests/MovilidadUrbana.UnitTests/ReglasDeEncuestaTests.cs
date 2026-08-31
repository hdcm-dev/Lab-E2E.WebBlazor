using MovilidadUrbana.Web.Dominio.Reglas;

namespace MovilidadUrbana.UnitTests;

/// <summary>
/// Los rangos de la encuesta, con foco en los bordes: son los valores que una prueba por navegador
/// tardaría minutos en recorrer y acá cuestan milisegundos.
/// </summary>
[TestFixture]
public class ReglasDeEncuestaTests
{
    [TestCase("Ana Pérez", ExpectedResult = true)]
    [TestCase("Ana", ExpectedResult = true, Description = "El mínimo son 3 caracteres")]
    [TestCase("An", ExpectedResult = false)]
    [TestCase("   ", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool NombreValido(string? nombre) => ReglasDeEncuesta.NombreValido(nombre);

    [TestCase(16, ExpectedResult = true, Description = "Borde inferior admitido")]
    [TestCase(34, ExpectedResult = true)]
    [TestCase(110, ExpectedResult = true, Description = "Borde superior admitido")]
    [TestCase(15, ExpectedResult = false)]
    [TestCase(111, ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool EdadValida(int? edad) => ReglasDeEncuesta.EdadValida(edad);

    [TestCase(0d, ExpectedResult = true, Description = "Cero kilómetros es válido: se puede no viajar")]
    [TestCase(12.5d, ExpectedResult = true)]
    [TestCase(500d, ExpectedResult = true)]
    [TestCase(-0.1d, ExpectedResult = false)]
    [TestCase(500.1d, ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool DistanciaValida(double? distancia) => ReglasDeEncuesta.DistanciaValida(distancia);

    [TestCase(1, ExpectedResult = true)]
    [TestCase(45, ExpectedResult = true)]
    [TestCase(600, ExpectedResult = true)]
    [TestCase(0, ExpectedResult = false, Description = "A diferencia de la distancia, cero minutos no es válido")]
    [TestCase(601, ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool MinutosValidos(int? minutos) => ReglasDeEncuesta.MinutosValidos(minutos);

    [Test]
    [Description("El asistente tiene tres pasos, y de ese número dependen la barra de progreso y el indicador")]
    public void TotalDePasos() => Assert.That(ReglasDeEncuesta.TotalDePasos, Is.EqualTo(3));
}
