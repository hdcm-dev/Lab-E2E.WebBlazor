/**
 * Fixtures propias del laboratorio.
 *
 * Diferencia central con la versión de páginas estáticas: acá el estado no vive en el navegador
 * sino en el servidor, en una única base SQLite compartida. Para que las pruebas sigan siendo
 * independientes —y puedan correr en paralelo— cada una estrena su cookie de sesión antes de
 * navegar, y la aplicación le da su propio conjunto de datos, ya sembrado.
 */
const base = require('@playwright/test');
const { randomUUID } = require('node:crypto');

const COOKIE_DE_SESION = 'sesion-movilidad';

const LOCALIDADES_SEMBRADAS = [
  { nombre: 'Corrientes', provincia: 'Corrientes', codigoPostal: '3400', habitantes: 346334 },
  { nombre: 'Resistencia', provincia: 'Chaco', codigoPostal: '3500', habitantes: 291720 }
];

const test = base.test.extend({
  page: async ({ page, baseURL }, use) => {
    await page.context().addCookies([
      { name: COOKIE_DE_SESION, value: randomUUID().replace(/-/g, ''), url: baseURL }
    ]);
    await use(page);
  }
});

/**
 * Espera a que el circuito de Blazor esté conectado.
 *
 * Una aplicación interactive server se entrega primero como HTML prerenderizado: los botones ya
 * se ven, pero todavía no hay nadie del otro lado que escuche el click. `MainLayout` publica un
 * testigo que pasa a `true` cuando el circuito quedó establecido; sin esta espera, las pruebas
 * fallan de manera intermitente y solo en las máquinas más lentas.
 */
async function esperarInteractivo(page) {
  await base.expect(page.getByTestId('estado-app')).toHaveAttribute('data-interactivo', 'true');
}

/** Navega a una ruta y devuelve el control recién cuando la página responde a la interacción. */
async function irA(page, ruta) {
  await page.goto(ruta);
  await esperarInteractivo(page);
}

/**
 * Navega usando la barra superior. En viewport chico el menú viene colapsado, así que primero hay
 * que desplegarlo: sin esto la misma prueba pasa en escritorio y falla en `mobile-chrome`.
 */
async function irPorMenu(page, testid) {
  const alternador = page.locator('.navbar-toggler');
  if (await alternador.isVisible()) {
    await alternador.click();
    await base.expect(page.locator('#menu')).toHaveClass(/show/);
  }
  await page.getByTestId(testid).click();
}

module.exports = {
  test,
  expect: base.expect,
  COOKIE_DE_SESION,
  LOCALIDADES_SEMBRADAS,
  esperarInteractivo,
  irA,
  irPorMenu
};
