using MovilidadUrbana.Web.Domain.Rules;

namespace MovilidadUrbana.UnitTests;

/// <summary>
/// Reglas endurecidas a causa de la capa de IA (E6): la propuesta del extractor pasa por acá.
/// Las 49 previas siguen en verde sin cambios.
/// </summary>
public class LocalidadRulesEndurecidasTests
{
    [Test]
    public void Nombre_ElMaximoSeAplica()
    {
        Assert.That(LocalidadRules.NombreValido(new string('a', LocalidadRules.LargoMaximoDelNombre)), Is.True);
        Assert.That(LocalidadRules.NombreValido(new string('a', LocalidadRules.LargoMaximoDelNombre + 1)), Is.False);
    }

    [TestCase("Corrientes", ExpectedResult = true)]
    [TestCase("Tucumán", ExpectedResult = false, Description = "No está en el catálogo")]
    [TestCase("corrientes", ExpectedResult = false, Description = "Se escribe como el catálogo")]
    public bool Provincia_DelCatalogo(string provincia) => LocalidadRules.ProvinciaValida(provincia);

    [Test(Description = "Un dígito de otra escritura no es un dígito del código postal")]
    public void CodigoPostal_SoloDigitosAscii() => Assert.That(LocalidadRules.CodigoPostalValido("٣٤٠٠"), Is.False);
}
