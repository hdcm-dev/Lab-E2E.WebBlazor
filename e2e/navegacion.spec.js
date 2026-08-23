const { test, expect, irA, irPorMenu, esperarInteractivo } = require('./apoyo');

test.describe('Navegación general', () => {
  test('la portada ofrece acceso a las dos pantallas', async ({ page }) => {
    await irA(page, '/');

    await expect(page).toHaveTitle(/Inicio/);
    await expect(page.getByTestId('titulo')).toHaveText('Demostración de pruebas E2E');
    await expect(page.getByTestId('ir-localidades')).toBeVisible();
    await expect(page.getByTestId('ir-encuesta')).toBeVisible();
  });

  test('el menú marca la página activa', async ({ page }) => {
    await irA(page, '/localidades');

    await expect(page.getByTestId('nav-localidades')).toHaveAttribute('aria-current', 'page');
    await expect(page.getByTestId('nav-encuesta')).not.toHaveAttribute('aria-current', 'page');
  });

  test('desde la portada se llega al ABM y a la encuesta', async ({ page }) => {
    await irA(page, '/');

    await page.getByTestId('ir-localidades').click();
    await expect(page).toHaveURL(/\/localidades$/);
    await expect(page.getByTestId('titulo')).toHaveText('Localidades');

    // La navegación siguiente ya ocurre dentro del mismo circuito, sin recargar la página.
    await irPorMenu(page, 'nav-encuesta');
    await expect(page).toHaveURL(/\/encuesta$/);
    await expect(page.getByTestId('etiqueta-paso')).toContainText('Paso 1 de 3');
  });

  test('una dirección inexistente muestra la pantalla de no encontrado', async ({ page }) => {
    await page.goto('/ruta-que-no-existe');
    await esperarInteractivo(page);

    await expect(page.getByTestId('titulo')).toHaveText('No encontrado');
  });
});
