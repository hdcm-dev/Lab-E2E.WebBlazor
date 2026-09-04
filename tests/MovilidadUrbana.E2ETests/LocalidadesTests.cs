using Microsoft.Playwright;

namespace MovilidadUrbana.E2ETests;

[TestFixture]
public class LocalidadesTests : PruebaE2E
{
    [SetUp]
    public async Task AbrirElAbmAsync() => await IrAAsync("/localidades");

    /// <summary>
    /// La presentación visible de una localidad. La grilla lleva la tabla y las tarjetas apiladas
    /// las dos siempre en el marcado —las conmuta el único punto de quiebre del CSS—, así que las
    /// acciones se buscan sobre la que está a la vista y el mismo caso sirve en escritorio y en
    /// móvil.
    /// </summary>
    private ILocator PresentacionDe(string nombre) =>
        Page.Locator("[data-testid=fila], [data-testid=tarjeta]")
            .Filter(new() { HasText = nombre })
            .Filter(new() { Visible = true });

    [Test]
    [Description("Muestra el listado sembrado")]
    public async Task MuestraElListadoSembrado()
    {
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(2);
        await Expect(Page.GetByTestId("contador")).ToHaveTextAsync("2");
        await Expect(Page.GetByTestId("sin-datos")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("cuerpo-tabla")).ToContainTextAsync("Corrientes");
    }

    [Test]
    [Description("Rechaza el alta con campos inválidos")]
    public async Task RechazaElAltaConCamposInvalidos()
    {
        await Page.GetByTestId("campo-nombre").FillAsync("Ab");
        await Page.GetByTestId("campo-codigo-postal").FillAsync("34");
        await Page.GetByTestId("boton-guardar").ClickAsync();

        await Expect(Page.GetByTestId("aviso")).ToHaveTextAsync("Revise los campos marcados en rojo.");
        await Expect(Page.GetByTestId("error-nombre")).ToHaveTextAsync(new Regex("al menos 3 caracteres"));
        await Expect(Page.GetByTestId("error-provincia")).ToHaveTextAsync(new Regex("Seleccione una provincia"));
        await Expect(Page.GetByTestId("error-codigo-postal")).ToHaveTextAsync(new Regex("4 dígitos"));
        await Expect(Page.GetByTestId("error-habitantes")).ToHaveTextAsync(new Regex("mayor a cero"));
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(2);
    }

    [Test]
    [Description("Da de alta una localidad y la persiste tras recargar")]
    public async Task DaDeAltaUnaLocalidadYLaPersisteTrasRecargar()
    {
        await Page.GetByTestId("campo-nombre").FillAsync("Goya");
        await Page.GetByTestId("campo-provincia").SelectOptionAsync("Corrientes");
        await Page.GetByTestId("campo-codigo-postal").FillAsync("3450");
        await Page.GetByTestId("campo-habitantes").FillAsync("89000");
        await Page.GetByTestId("boton-guardar").ClickAsync();

        await Expect(Page.GetByTestId("aviso")).ToHaveTextAsync("Se agregó la localidad Goya.");
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(3);
        await Expect(Page.GetByTestId("contador")).ToHaveTextAsync("3");

        var fila = Page.GetByTestId("fila").Filter(new() { HasText = "Goya" });
        await Expect(fila.GetByTestId("celda-codigo-postal")).ToHaveTextAsync("3450");
        // El número se formatea en el servidor con la cultura es-AR.
        await Expect(fila.GetByTestId("celda-habitantes")).ToHaveTextAsync("89.000");

        // El formulario vuelve a modo alta.
        await Expect(Page.GetByTestId("titulo-formulario")).ToHaveTextAsync("Alta de localidad");
        await Expect(Page.GetByTestId("campo-nombre")).ToHaveValueAsync("");

        // Recargar prueba que el dato quedó en la base y no en el estado del circuito anterior.
        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(3);
        await Expect(Page.GetByTestId("cuerpo-tabla")).ToContainTextAsync("Goya");
    }

    [Test]
    [Description("No permite duplicar nombre dentro de la misma provincia")]
    public async Task NoPermiteDuplicarNombreDentroDeLaMismaProvincia()
    {
        await Page.GetByTestId("campo-nombre").FillAsync("corrientes");
        await Page.GetByTestId("campo-provincia").SelectOptionAsync("Corrientes");
        await Page.GetByTestId("campo-codigo-postal").FillAsync("3400");
        await Page.GetByTestId("campo-habitantes").FillAsync("1000");
        await Page.GetByTestId("boton-guardar").ClickAsync();

        await Expect(Page.GetByTestId("error-nombre")).ToHaveTextAsync(new Regex("Ya existe una localidad"));
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(2);
    }

    [Test]
    [Description("Modifica una localidad existente")]
    public async Task ModificaUnaLocalidadExistente()
    {
        await PresentacionDe("Resistencia").GetByTestId("boton-editar").ClickAsync();

        await Expect(Page.GetByTestId("titulo-formulario")).ToHaveTextAsync("Editar localidad");
        await Expect(Page.GetByTestId("boton-guardar")).ToHaveTextAsync("Actualizar");
        await Expect(Page.GetByTestId("campo-codigo-postal")).ToHaveValueAsync("3500");

        await Page.GetByTestId("campo-habitantes").FillAsync("300000");
        await Page.GetByTestId("boton-guardar").ClickAsync();

        await Expect(Page.GetByTestId("aviso")).ToHaveTextAsync("Se actualizó la localidad Resistencia.");
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(2);
        await Expect(Page.GetByTestId("fila").Filter(new() { HasText = "Resistencia" })
            .GetByTestId("celda-habitantes")).ToHaveTextAsync("300.000");
    }

    [Test]
    [Description("Cancelar la baja deja la tabla intacta")]
    public async Task CancelarLaBajaDejaLaTablaIntacta()
    {
        await PresentacionDe("Corrientes").GetByTestId("boton-eliminar").ClickAsync();

        await Expect(Page.GetByTestId("dialogo-titulo")).ToContainTextAsync("Corrientes");
        await Page.GetByTestId("boton-cancelar-dialogo").ClickAsync();

        await Expect(Page.GetByTestId("dialogo")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(2);
    }

    [Test]
    [Description("Confirmar la baja elimina la fila")]
    public async Task ConfirmarLaBajaEliminaLaFila()
    {
        await PresentacionDe("Corrientes").GetByTestId("boton-eliminar").ClickAsync();
        await Page.GetByTestId("boton-confirmar-dialogo").ClickAsync();

        await Expect(Page.GetByTestId("aviso")).ToHaveTextAsync("Se eliminó la localidad Corrientes.");
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("cuerpo-tabla")).Not.ToContainTextAsync("Corrientes");
    }

    [Test]
    [Description("Al borrar todas las localidades avisa que no hay datos")]
    public async Task AlBorrarTodasLasLocalidadesAvisaQueNoHayDatos()
    {
        // Al quedar la colección vacía la tabla deja de estar: en su lugar se dibuja el estado
        // vacío, así que lo que se cuenta son las filas y no el texto del cuerpo de la tabla.
        var restantes = 2;
        foreach (var nombre in new[] { "Corrientes", "Resistencia" })
        {
            await PresentacionDe(nombre).GetByTestId("boton-eliminar").ClickAsync();
            await Page.GetByTestId("boton-confirmar-dialogo").ClickAsync();
            restantes -= 1;
            await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(restantes);
        }
        await Expect(Page.GetByTestId("sin-datos")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("contador")).ToHaveTextAsync("0");

        // La siembra inicial no se repite: la sesión ya quedó marcada como sembrada.
        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(0);
    }

    [Test]
    [Description("Cada prueba trabaja sobre su propio conjunto de datos")]
    public async Task CadaPruebaTrabajaSobreSuPropioConjuntoDeDatos()
    {
        await Page.GetByTestId("campo-nombre").FillAsync("Mercedes");
        await Page.GetByTestId("campo-provincia").SelectOptionAsync("Corrientes");
        await Page.GetByTestId("campo-codigo-postal").FillAsync("3470");
        await Page.GetByTestId("campo-habitantes").FillAsync("40000");
        await Page.GetByTestId("boton-guardar").ClickAsync();
        await Expect(Page.GetByTestId("fila")).ToHaveCountAsync(3);

        // Otra sesión, mismo servidor y misma base: no ve nada de lo anterior.
        await Context.ClearCookiesAsync();
        var otra = await Context.NewPageAsync();
        await otra.GotoAsync($"{ServidorDeLaAplicacion.UrlBase}/localidades");
        await Expect(otra.GetByTestId("fila")).ToHaveCountAsync(2);
        await Expect(otra.GetByTestId("cuerpo-tabla")).Not.ToContainTextAsync("Mercedes");
    }
}
