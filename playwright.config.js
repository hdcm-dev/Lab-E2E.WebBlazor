// @ts-check
const { defineConfig, devices } = require('@playwright/test');
const path = require('node:path');

const PUERTO = Number(process.env.PUERTO || 4173);
const URL_BASE = process.env.URL_BASE || `http://127.0.0.1:${PUERTO}`;
const EN_CI = !!process.env.CI;

// El binario autocontenido que deja `scripts/publicar.sh`. Corre igual en la máquina de quien
// desarrolla y dentro del contenedor de Playwright en CI, que no trae el runtime de .NET.
const CARPETA_APLICACION = process.env.CARPETA_APLICACION || 'publicacion';
const EJECUTABLE = process.env.EJECUTABLE || './MovilidadUrbana.Web';
// Ruta absoluta: la aplicación se lanza desde su propia carpeta, no desde la raíz del repositorio.
const BASE_DE_DATOS = path.resolve(process.env.BASE_DE_DATOS || 'datos-e2e/movilidad.db');

// Dentro de un contenedor `/dev/shm` suele quedar en 64 MB —es el caso del runner
// `i7infra-dev`—. Chromium lo agota y muere con «Target closed» a mitad de la corrida; con este
// argumento usa /tmp en su lugar. Firefox y WebKit no lo necesitan.
const CHROMIUM_EN_CONTENEDOR = { launchOptions: { args: ['--disable-dev-shm-usage'] } };

module.exports = defineConfig({
  testDir: './e2e',
  // Cada prueba estrena su cookie de sesión, y con ella su propio conjunto de datos en el
  // servidor: por eso pueden correr en paralelo contra una única instancia de la aplicación.
  fullyParallel: true,
  // En CI un `test.only` olvidado hace fallar la corrida en lugar de saltear el resto.
  forbidOnly: EN_CI,
  retries: EN_CI ? 2 : 0,
  workers: EN_CI ? 2 : undefined,
  timeout: 30_000,
  expect: { timeout: 5_000 },
  reporter: EN_CI
    ? [['blob'], ['github'], ['list']]
    : [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: URL_BASE,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    locale: 'es-AR',
    timezoneId: 'America/Argentina/Buenos_Aires'
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'], ...CHROMIUM_EN_CONTENEDOR } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
    { name: 'mobile-chrome', use: { ...devices['Pixel 7'], ...CHROMIUM_EN_CONTENEDOR } }
  ],
  // Cuando se apunta a un entorno ya desplegado (URL_BASE externa) no se levanta nada.
  webServer: process.env.URL_BASE
    ? undefined
    : {
        command: EJECUTABLE,
        // ASP.NET Core toma el directorio actual como raíz de contenido: si se lo lanza desde
        // otro lado, `wwwroot` no se encuentra y los recursos estáticos salen vacíos.
        cwd: CARPETA_APLICACION,
        url: `http://127.0.0.1:${PUERTO}/`,
        reuseExistingServer: !EN_CI,
        // Arranque en frío: crear el archivo SQLite y su esquema lleva unos segundos.
        timeout: 90_000,
        stdout: 'pipe',
        stderr: 'pipe',
        env: {
          ASPNETCORE_URLS: `http://127.0.0.1:${PUERTO}`,
          ASPNETCORE_ENVIRONMENT: 'Production',
          ConnectionStrings__BaseDeDatos: `Data Source=${BASE_DE_DATOS};Default Timeout=30`,
          Logging__LogLevel__Default: 'Warning'
        }
      }
});
