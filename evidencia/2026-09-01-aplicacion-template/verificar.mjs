import { chromium, devices } from 'playwright';
import fs from 'node:fs';

const salida = '/salida';
fs.mkdirSync(salida, { recursive: true });
const fallas = [];
const ok = (m) => console.log('OK   ' + m);
const mal = (m) => { fallas.push(m); console.log('FALLA ' + m); };

const navegador = await chromium.launch();

// ---------- Proyecto HolaMundo ----------
{
  const ctx = await navegador.newContext({ viewport: { width: 1280, height: 800 } });
  const p = await ctx.newPage();
  const base = 'http://app-holamundo:8080';

  await p.goto(base + '/', { waitUntil: 'networkidle' });
  await p.screenshot({ path: salida + '/holamundo-01-inicio.png', fullPage: true });
  (await p.locator('h1').innerText()) === 'Inicio' ? ok('holamundo inicio') : mal('holamundo inicio h1');

  await p.goto(base + '/HolaMundo', { waitUntil: 'networkidle' });
  await p.screenshot({ path: salida + '/holamundo-02-frase-vacio.png', fullPage: true });
  await p.locator('.mq-vacio').isVisible() ? ok('estado vacio visible') : mal('estado vacio ausente');

  // El recorrido de la prueba E2E existente.
  const frase = 'Hola mundo! - que tal?';
  await p.getByTestId('campo-frase').fill(frase);
  await p.getByTestId('boton-mostrar-frase').click();
  await p.getByTestId('campo-mensaje').waitFor({ state: 'visible', timeout: 10000 });
  const texto = (await p.getByTestId('campo-mensaje').innerText()).trim();
  texto === frase ? ok('campo-mensaje == frase') : mal('campo-mensaje = "' + texto + '"');
  await p.screenshot({ path: salida + '/holamundo-03-frase-con-datos.png', fullPage: true });

  // Estado de error de entrada.
  await p.getByTestId('campo-frase').fill('   ');
  await p.getByTestId('boton-mostrar-frase').click();
  await p.locator('[data-testid="mensaje-error"]').waitFor({ timeout: 10000 }).then(
    () => ok('error de entrada exhibido'), () => mal('error de entrada no exhibido'));
  await p.screenshot({ path: salida + '/holamundo-04-error-de-entrada.png', fullPage: true });

  await p.goto(base + '/ruta-que-no-existe', { waitUntil: 'networkidle' });
  await p.screenshot({ path: salida + '/holamundo-05-no-encontrado.png', fullPage: true });

  // Presentación angosta: el shell colapsa y no hay scroll horizontal a 320px.
  const movil = await navegador.newContext(devices['Pixel 7']);
  const pm = await movil.newPage();
  await pm.goto(base + '/HolaMundo', { waitUntil: 'networkidle' });
  await pm.screenshot({ path: salida + '/holamundo-06-angosto.png', fullPage: true });
  const angosto = await navegador.newContext({ viewport: { width: 320, height: 720 } });
  const pa = await angosto.newPage();
  await pa.goto(base + '/HolaMundo', { waitUntil: 'networkidle' });
  const desborde = await pa.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
  desborde ? mal('scroll horizontal a 320px') : ok('sin scroll horizontal a 320px');
  await pa.screenshot({ path: salida + '/holamundo-07-320px.png', fullPage: true });
}

// ---------- Proyecto Login ----------
{
  const ctx = await navegador.newContext({ viewport: { width: 1280, height: 800 } });
  const p = await ctx.newPage();
  const base = 'http://app-login:8080';

  // Guard: la superficie protegida manda al shell de acceso.
  await p.goto(base + '/', { waitUntil: 'networkidle' });
  p.url().includes('/login') ? ok('guard lleva al ingreso') : mal('guard no redirigió: ' + p.url());
  await p.screenshot({ path: salida + '/login-01-ingreso.png', fullPage: true });

  // Rechazo indiferenciado.
  await p.getByTestId('campo-usuario').fill('admin');
  await p.getByTestId('campo-clave').fill('otra-cosa');
  await p.getByTestId('boton-ingresar').click();
  await p.waitForLoadState('networkidle');
  const rechazo = await p.getByTestId('mensaje-resultado').innerText().catch(() => '');
  rechazo.includes('No pudimos validar') ? ok('rechazo indiferenciado') : mal('rechazo: "' + rechazo + '"');
  await p.screenshot({ path: salida + '/login-02-rechazo.png', fullPage: true });

  // Ingreso aceptado.
  await p.getByTestId('campo-usuario').fill('admin');
  await p.getByTestId('campo-clave').fill('admin');
  await p.getByTestId('boton-ingresar').click();
  await p.waitForLoadState('networkidle');
  const enInicio = await p.locator('h1').innerText().catch(() => '');
  enInicio === 'Inicio' ? ok('ingreso aceptado') : mal('tras ingresar: "' + enInicio + '" url=' + p.url());
  await p.screenshot({ path: salida + '/login-03-inicio-con-sesion.png', fullPage: true });

  await p.goto(base + '/HolaMundo', { waitUntil: 'networkidle' });
  await p.getByTestId('campo-frase').fill('Sesión iniciada');
  await p.getByTestId('boton-mostrar-frase').click();
  await p.getByTestId('campo-mensaje').waitFor({ timeout: 10000 }).then(
    () => ok('superficie protegida operable'), () => mal('superficie protegida no respondió'));
  await p.screenshot({ path: salida + '/login-04-holamundo-protegido.png', fullPage: true });

  // Cierre de sesión por POST desde el chrome.
  await p.getByTestId('boton-cerrar-sesion').click();
  await p.waitForLoadState('networkidle');
  const cierre = await p.getByTestId('mensaje-resultado').innerText().catch(() => '');
  cierre.includes('Cerraste la sesión') ? ok('cierre de sesión por POST') : mal('cierre: "' + cierre + '" url=' + p.url());
  await p.screenshot({ path: salida + '/login-05-sesion-cerrada.png', fullPage: true });
}

await navegador.close();
console.log(fallas.length === 0 ? '\nTODO VERDE' : '\nFALLAS: ' + fallas.length);
process.exit(fallas.length === 0 ? 0 : 1);
