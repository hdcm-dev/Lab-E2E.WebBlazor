const { test, expect, irA } = require('./apoyo');

/** Completa el paso indicado con datos válidos, sin avanzar. */
async function completarPaso1(page) {
  await page.getByTestId('campo-nombre').fill('Ana Pérez');
  await page.getByTestId('campo-edad').fill('34');
  await page.getByTestId('campo-localidad').selectOption('Corrientes');
}

async function completarPaso2(page) {
  await page.getByTestId('medio-colectivo').check();
  await page.getByTestId('medio-bicicleta').check();
  await page.getByTestId('campo-frecuencia').selectOption('diaria');
}

async function completarPaso3(page) {
  await page.getByTestId('campo-distancia').fill('12.5');
  await page.getByTestId('campo-minutos').fill('45');
  await page.getByTestId('campo-motivo').selectOption('trabajo');
}

test.beforeEach(async ({ page }) => {
  await irA(page, '/encuesta');
});

test.describe('Asistente de encuesta', () => {
  test('arranca en el paso 1 con el anterior deshabilitado', async ({ page }) => {
    await expect(page.getByTestId('paso-1')).toBeVisible();
    await expect(page.getByTestId('paso-2')).toBeHidden();
    await expect(page.getByTestId('paso-3')).toBeHidden();
    await expect(page.getByTestId('indicador-paso')).toHaveText('1/3');
    await expect(page.getByTestId('boton-anterior')).toBeDisabled();
    await expect(page.getByTestId('boton-finalizar')).toBeHidden();
    await expect(page.getByTestId('contador-encuestas')).toHaveText('Registradas: 0');
  });

  test('el desplegable de localidades se alimenta del ABM', async ({ page }) => {
    const opciones = page.getByTestId('campo-localidad').locator('option');
    await expect(opciones).toHaveCount(3); // marcador de posición + 2 localidades sembradas
    await expect(opciones.nth(1)).toHaveText('Corrientes (Corrientes)');
  });

  test('no avanza del paso 1 con datos inválidos', async ({ page }) => {
    await page.getByTestId('campo-edad').fill('12');
    await page.getByTestId('boton-siguiente').click();

    await expect(page.getByTestId('aviso')).toHaveText('Complete los datos del paso antes de continuar.');
    await expect(page.getByTestId('error-nombre')).toHaveText(/mínimo 3 caracteres/);
    await expect(page.getByTestId('error-edad')).toHaveText(/entre 16 y 110/);
    await expect(page.getByTestId('error-localidad')).toHaveText(/Seleccione una localidad/);
    await expect(page.getByTestId('paso-1')).toBeVisible();
    await expect(page.getByTestId('indicador-paso')).toHaveText('1/3');
  });

  test('no avanza del paso 2 sin medios ni frecuencia', async ({ page }) => {
    await completarPaso1(page);
    await page.getByTestId('boton-siguiente').click();
    await expect(page.getByTestId('paso-2')).toBeVisible();

    await page.getByTestId('boton-siguiente').click();
    await expect(page.getByTestId('error-medios')).toHaveText('Seleccione al menos un medio de transporte.');
    await expect(page.getByTestId('error-frecuencia')).toHaveText(/Seleccione la frecuencia/);
    await expect(page.getByTestId('indicador-paso')).toHaveText('2/3');
  });

  test('permite volver atrás conservando lo cargado', async ({ page }) => {
    await completarPaso1(page);
    await page.getByTestId('boton-siguiente').click();
    await completarPaso2(page);
    await page.getByTestId('boton-siguiente').click();

    await expect(page.getByTestId('paso-3')).toBeVisible();
    await expect(page.getByTestId('boton-siguiente')).toBeHidden();
    await expect(page.getByTestId('boton-finalizar')).toBeVisible();

    await page.getByTestId('boton-anterior').click();
    await expect(page.getByTestId('paso-2')).toBeVisible();
    await expect(page.getByTestId('medio-colectivo')).toBeChecked();
    await expect(page.getByTestId('campo-frecuencia')).toHaveValue('diaria');

    await page.getByTestId('boton-anterior').click();
    await expect(page.getByTestId('campo-nombre')).toHaveValue('Ana Pérez');
    await expect(page.getByTestId('boton-anterior')).toBeDisabled();
  });

  test('la barra de progreso acompaña el avance', async ({ page }) => {
    const progreso = page.getByTestId('progreso-contenedor');
    await expect(progreso).toHaveAttribute('aria-valuenow', '33');

    await completarPaso1(page);
    await page.getByTestId('boton-siguiente').click();
    await expect(progreso).toHaveAttribute('aria-valuenow', '67');

    await completarPaso2(page);
    await page.getByTestId('boton-siguiente').click();
    await expect(progreso).toHaveAttribute('aria-valuenow', '100');
  });

  test('no finaliza con el paso 3 incompleto', async ({ page }) => {
    await completarPaso1(page);
    await page.getByTestId('boton-siguiente').click();
    await completarPaso2(page);
    await page.getByTestId('boton-siguiente').click();

    await page.getByTestId('campo-distancia').fill('900');
    await page.getByTestId('boton-finalizar').click();

    await expect(page.getByTestId('error-distancia')).toHaveText(/entre 0 y 500 km/);
    await expect(page.getByTestId('error-minutos')).toHaveText(/entre 1 y 600 minutos/);
    await expect(page.getByTestId('resumen')).toBeHidden();
  });

  test('recorre los tres pasos, muestra el resumen y registra la respuesta', async ({ page }) => {
    await completarPaso1(page);
    await page.getByTestId('boton-siguiente').click();
    await completarPaso2(page);
    await page.getByTestId('boton-siguiente').click();
    await completarPaso3(page);
    await page.getByTestId('boton-finalizar').click();

    await expect(page.getByTestId('mensaje-envio')).toBeVisible();
    await expect(page.getByTestId('resumen-persona')).toHaveText('Ana Pérez (34 años)');
    await expect(page.getByTestId('resumen-localidad')).toHaveText('Corrientes');
    await expect(page.getByTestId('resumen-medios')).toHaveText('Colectivo, Bicicleta');
    await expect(page.getByTestId('resumen-frecuencia')).toHaveText('Todos los días');
    await expect(page.getByTestId('resumen-distancia')).toHaveText('12,5 km');
    await expect(page.getByTestId('resumen-minutos')).toHaveText('45 min');
    await expect(page.getByTestId('resumen-motivo')).toHaveText('Trabajo');

    await expect(page.getByTestId('formulario')).toBeHidden();
    await expect(page.getByTestId('boton-reiniciar')).toBeVisible();
    await expect(page.getByTestId('etiqueta-paso')).toHaveText('Encuesta completada');

    // La respuesta quedó guardada en el servidor: el contador lo confirma tras recargar.
    await expect(page.getByTestId('contador-encuestas')).toHaveText('Registradas: 1');
    await page.reload();
    await expect(page.getByTestId('contador-encuestas')).toHaveText('Registradas: 1');
  });

  test('«Nueva encuesta» devuelve el asistente al paso 1', async ({ page }) => {
    await completarPaso1(page);
    await page.getByTestId('boton-siguiente').click();
    await completarPaso2(page);
    await page.getByTestId('boton-siguiente').click();
    await completarPaso3(page);
    await page.getByTestId('boton-finalizar').click();

    await page.getByTestId('boton-reiniciar').click();

    await expect(page.getByTestId('paso-1')).toBeVisible();
    await expect(page.getByTestId('indicador-paso')).toHaveText('1/3');
    await expect(page.getByTestId('campo-nombre')).toHaveValue('');
    await expect(page.getByTestId('contador-encuestas')).toHaveText('Registradas: 1');
  });
});
