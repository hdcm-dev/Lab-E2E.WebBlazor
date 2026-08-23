const { test, expect, irA } = require('./apoyo');

test.beforeEach(async ({ page }) => {
  await irA(page, '/localidades');
});

test.describe('ABM de localidades', () => {
  test('muestra el listado sembrado', async ({ page }) => {
    await expect(page.getByTestId('fila')).toHaveCount(2);
    await expect(page.getByTestId('contador')).toHaveText('2');
    await expect(page.getByTestId('sin-datos')).toBeHidden();
    await expect(page.getByTestId('cuerpo-tabla')).toContainText('Corrientes');
  });

  test('rechaza el alta con campos inválidos', async ({ page }) => {
    await page.getByTestId('campo-nombre').fill('Ab');
    await page.getByTestId('campo-codigo-postal').fill('34');
    await page.getByTestId('boton-guardar').click();

    await expect(page.getByTestId('aviso')).toHaveText('Revise los campos marcados en rojo.');
    await expect(page.getByTestId('error-nombre')).toHaveText(/al menos 3 caracteres/);
    await expect(page.getByTestId('error-provincia')).toHaveText(/Seleccione una provincia/);
    await expect(page.getByTestId('error-codigo-postal')).toHaveText(/4 dígitos/);
    await expect(page.getByTestId('error-habitantes')).toHaveText(/mayor a cero/);
    await expect(page.getByTestId('fila')).toHaveCount(2);
  });

  test('da de alta una localidad y la persiste tras recargar', async ({ page }) => {
    await page.getByTestId('campo-nombre').fill('Goya');
    await page.getByTestId('campo-provincia').selectOption('Corrientes');
    await page.getByTestId('campo-codigo-postal').fill('3450');
    await page.getByTestId('campo-habitantes').fill('89000');
    await page.getByTestId('boton-guardar').click();

    await expect(page.getByTestId('aviso')).toHaveText('Se agregó la localidad Goya.');
    await expect(page.getByTestId('fila')).toHaveCount(3);
    await expect(page.getByTestId('contador')).toHaveText('3');

    const fila = page.getByTestId('fila').filter({ hasText: 'Goya' });
    await expect(fila.getByTestId('celda-codigo-postal')).toHaveText('3450');
    // El número se formatea en el servidor con la cultura es-AR.
    await expect(fila.getByTestId('celda-habitantes')).toHaveText('89.000');

    // El formulario vuelve a modo alta.
    await expect(page.getByTestId('titulo-formulario')).toHaveText('Alta de localidad');
    await expect(page.getByTestId('campo-nombre')).toHaveValue('');

    // Recargar prueba que el dato quedó en la base y no en el estado del circuito anterior.
    await page.reload();
    await expect(page.getByTestId('fila')).toHaveCount(3);
    await expect(page.getByTestId('cuerpo-tabla')).toContainText('Goya');
  });

  test('no permite duplicar nombre dentro de la misma provincia', async ({ page }) => {
    await page.getByTestId('campo-nombre').fill('corrientes');
    await page.getByTestId('campo-provincia').selectOption('Corrientes');
    await page.getByTestId('campo-codigo-postal').fill('3400');
    await page.getByTestId('campo-habitantes').fill('1000');
    await page.getByTestId('boton-guardar').click();

    await expect(page.getByTestId('error-nombre')).toHaveText(/Ya existe una localidad/);
    await expect(page.getByTestId('fila')).toHaveCount(2);
  });

  test('modifica una localidad existente', async ({ page }) => {
    const fila = page.getByTestId('fila').filter({ hasText: 'Resistencia' });
    await fila.getByTestId('boton-editar').click();

    await expect(page.getByTestId('titulo-formulario')).toHaveText('Editar localidad');
    await expect(page.getByTestId('boton-guardar')).toHaveText('Actualizar');
    await expect(page.getByTestId('campo-codigo-postal')).toHaveValue('3500');

    await page.getByTestId('campo-habitantes').fill('300000');
    await page.getByTestId('boton-guardar').click();

    await expect(page.getByTestId('aviso')).toHaveText('Se actualizó la localidad Resistencia.');
    await expect(page.getByTestId('fila')).toHaveCount(2);
    await expect(
      page.getByTestId('fila').filter({ hasText: 'Resistencia' }).getByTestId('celda-habitantes')
    ).toHaveText('300.000');
  });

  test('cancelar la baja deja la tabla intacta', async ({ page }) => {
    await page.getByTestId('fila').filter({ hasText: 'Corrientes' }).getByTestId('boton-eliminar').click();

    await expect(page.getByTestId('modal-nombre')).toHaveText('Corrientes');
    await page.getByTestId('boton-cancelar-baja').click();

    await expect(page.getByTestId('modal-nombre')).toBeHidden();
    await expect(page.getByTestId('fila')).toHaveCount(2);
  });

  test('confirmar la baja elimina la fila', async ({ page }) => {
    await page.getByTestId('fila').filter({ hasText: 'Corrientes' }).getByTestId('boton-eliminar').click();
    await page.getByTestId('boton-confirmar-baja').click();

    await expect(page.getByTestId('aviso')).toHaveText('Se eliminó la localidad Corrientes.');
    await expect(page.getByTestId('fila')).toHaveCount(1);
    await expect(page.getByTestId('cuerpo-tabla')).not.toContainText('Corrientes');
  });

  test('al borrar todas las localidades avisa que no hay datos', async ({ page }) => {
    for (const nombre of ['Corrientes', 'Resistencia']) {
      await page.getByTestId('fila').filter({ hasText: nombre }).getByTestId('boton-eliminar').click();
      await page.getByTestId('boton-confirmar-baja').click();
      await expect(page.getByTestId('cuerpo-tabla')).not.toContainText(nombre);
    }

    await expect(page.getByTestId('fila')).toHaveCount(0);
    await expect(page.getByTestId('sin-datos')).toBeVisible();
    await expect(page.getByTestId('contador')).toHaveText('0');

    // La siembra inicial no se repite: la sesión ya quedó marcada como sembrada.
    await page.reload();
    await expect(page.getByTestId('fila')).toHaveCount(0);
  });

  test('cada prueba trabaja sobre su propio conjunto de datos', async ({ page, context, baseURL }) => {
    await page.getByTestId('campo-nombre').fill('Mercedes');
    await page.getByTestId('campo-provincia').selectOption('Corrientes');
    await page.getByTestId('campo-codigo-postal').fill('3470');
    await page.getByTestId('campo-habitantes').fill('40000');
    await page.getByTestId('boton-guardar').click();
    await expect(page.getByTestId('fila')).toHaveCount(3);

    // Otra sesión, mismo servidor y misma base: no ve nada de lo anterior.
    await context.clearCookies();
    const otra = await context.newPage();
    await otra.goto(new URL('/localidades', baseURL).toString());
    await expect(otra.getByTestId('fila')).toHaveCount(2);
    await expect(otra.getByTestId('cuerpo-tabla')).not.toContainText('Mercedes');
  });
});
