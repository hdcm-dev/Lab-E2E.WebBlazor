# Lab-E2E.WebBlazor

Laboratorio para practicar **pruebas de extremo a extremo con Playwright** sobre una aplicación
**.NET Blazor con render interactive server**, y para ver cómo se integran esas pruebas en la
cadena de desarrollo con GitHub Actions.

Es el mismo ejemplo de *movilidad urbana* que [Lab-E2E.StaticHtml](../Lab-E2E.StaticHtml),
replanteado sobre una aplicación real: el estado ya no vive en `localStorage` sino en el servidor,
en una base **SQLite**, y cada interacción viaja por el circuito de Blazor.

| Pantalla | Componente | Qué ejercita |
| --- | --- | --- |
| ABM de localidades | [Localidades.razor](src/MovilidadUrbana.Web/Components/Pages/Localidades.razor) | Alta, modificación y baja sobre una tabla, validación en el servidor, diálogo de confirmación y persistencia |
| Encuesta de transporte | [Encuesta.razor](src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor) | Asistente de tres pasos: datos de la persona, medios que utiliza para viajar y distancia recorrida |

El diseño es **Bootstrap 5.3.8** vendorizado en
[wwwroot/vendor/bootstrap/](src/MovilidadUrbana.Web/wwwroot/vendor/bootstrap/), sin CDN y **sin el
bundle de JavaScript**: el menú colapsable y el diálogo modal se resuelven con estado del
componente, que es lo natural en una aplicación interactiva.

## Estructura

Un único proyecto en la solución, con las capas de **Clean Architecture** separadas en carpetas y
las dependencias apuntando siempre hacia adentro:

```
Lab-E2E.WebBlazor.sln
src/MovilidadUrbana.Web/
  Dominio/              Entidades y reglas de negocio. No depende de nada.
    Entidades/          Localidad, RespuestaDeEncuesta, Sesion
    Reglas/             ReglasDeLocalidad, ReglasDeEncuesta
    Catalogos.cs        Provincias, medios, frecuencias y motivos
  Aplicacion/           Casos de uso. Depende solo de Dominio.
    Abstracciones/      IRepositorioDeLocalidades, IRepositorioDeEncuestas, IContextoDeSesion
    Localidades/        ServicioDeLocalidades + su modelo de pantalla
    Encuestas/          ServicioDeEncuestas + su modelo de pantalla
    Resultado.cs        Salida de un caso de uso: aviso y errores por campo
  Infraestructura/      Implementa las abstracciones de Aplicacion.
    Persistencia/       EF Core sobre SQLite, repositorios y siembra inicial
    Sesiones/           Cookie de sesión y su middleware
  Components/           Presentación (Blazor). Depende de Aplicacion.
    App.razor, Routes.razor, Layout/, Pages/
  wwwroot/              Bootstrap vendorizado y estilos propios
  Program.cs            Composición: es el único lugar que conoce todas las capas
scripts/
  dotnet.sh             Ejecuta el SDK de .NET dentro del contenedor oficial
  publicar.sh           Publica el binario autocontenido en `publicacion/`
  e2e.sh                Ejecuta npm/Playwright dentro del contenedor oficial
e2e/                    Las pruebas de extremo a extremo, fuera de la solución
  apoyo.js              Fixtures: cookie de sesión por prueba, espera de interactividad, menú
  navegacion.spec.js    Portada, menú y ruta inexistente
  localidades.spec.js   ABM completo y aislamiento entre sesiones
  encuesta.spec.js      Asistente completo
playwright.config.js    Navegadores, reportes y arranque de la aplicación bajo prueba
.github/workflows/      CI, workflow reutilizable de E2E y verificación de entornos
```

### Por qué las pruebas E2E no son un proyecto de la solución

Es la convención de la aplicación de referencia de .NET, [dotnet/eShop](https://github.com/dotnet/eShop),
que en su raíz tiene `src/` con la aplicación, `tests/` con los proyectos C# de pruebas unitarias y
funcionales (`Basket.UnitTests`, `Catalog.FunctionalTests`, …) y una carpeta `e2e/` aparte con las
specs de Playwright, más `playwright.config.ts` y `package.json` en la raíz. Las E2E viven al lado
de la solución, no adentro.

Acá se sigue el mismo reparto: `e2e/` queda fuera del `.sln` y `tests/` queda libre para cuando
haga falta agregar proyectos de pruebas en C#, con la convención de nombre `<Proyecto>.Tests` que
usan los tutoriales de .NET.

## Cómo correrlo

### Con el SDK y Node instalados

```bash
dotnet run --project src/MovilidadUrbana.Web     # aplicación en http://localhost:5232
npm ci
npx playwright install --with-deps               # navegadores, solo la primera vez

# Antes de cada corrida: las pruebas ejercitan el binario publicado, no `dotnet run`.
dotnet publish src/MovilidadUrbana.Web -c Release -r linux-x64 --self-contained -o publicacion

npm test                                         # las 4 configuraciones de navegador
npm run test:e2e                                 # alias, la convención de dotnet/eShop
npm run test:chromium                            # solo chromium, más rápido
npm run reporte                                  # abre el reporte HTML de la última corrida
```

### Sin nada instalado (con Docker)

Los dos scripts ejecutan cualquier comando dentro de la imagen oficial correspondiente:

```bash
scripts/publicar.sh                              # usa mcr.microsoft.com/dotnet/sdk:10.0
scripts/e2e.sh npm ci                            # usa mcr.microsoft.com/playwright:v1.62.1-noble
scripts/e2e.sh npx playwright test
scripts/e2e.sh npx playwright test --project=chromium
```

> **Nota sobre `dotnet run`.** Dentro de la imagen `mcr.microsoft.com/dotnet/sdk:10.0` el
> servidor de desarrollo responde `500` al pedir `/_framework/blazor.web.js`, con lo que el
> circuito nunca se establece. Se comprobó que la plantilla `dotnet new blazor` sin modificar hace
> exactamente lo mismo en esa imagen, así que no es algo de este código; en una máquina con el SDK
> instalado no se verificó. Lo que sí quedó verificado acá es la aplicación **publicada**, que es
> justamente lo que ejercitan las pruebas.

`playwright.config.js` arranca la aplicación publicada antes de las pruebas y la baja al terminar.
Para probar contra un entorno ya desplegado se define `URL_BASE` y no se levanta nada local:

```bash
URL_BASE=https://ejemplo.test scripts/e2e.sh npx playwright test --project=chromium
```

## Lo que cambia respecto del ejemplo estático

Las decisiones de fondo —selectores por `data-testid`, nada de esperas fijas, cuatro
configuraciones de navegador con una móvil— se mantienen. Lo que sigue es lo que **aparece recién
cuando la aplicación tiene servidor**.

### 1. Hay que esperar a que la página sea interactiva

Una aplicación *interactive server* se entrega primero como HTML prerenderizado y recién después
se establece el circuito por WebSocket. En el medio los botones **se ven pero no responden**: un
click que llegue antes de la conexión se pierde sin dejar rastro, y el síntoma es una prueba que
falla de manera intermitente y solo en las máquinas cargadas.

[MainLayout.razor](src/MovilidadUrbana.Web/Components/Layout/MainLayout.razor) publica un testigo
con `RendererInfo.IsInteractive`, y la fixture `esperarInteractivo` de
[e2e/apoyo.js](e2e/apoyo.js) lo espera antes de tocar nada.

### 2. El estado es del servidor, así que hay que aislarlo

En el ejemplo estático cada prueba tenía su `localStorage`. Acá hay una única base SQLite y todas
las pruebas —incluidas las que corren en paralelo— la comparten.

La solución es que la aplicación reparta un **espacio de datos por sesión**: una cookie que emite
[MiddlewareDeSesion](src/MovilidadUrbana.Web/Infraestructura/Sesiones/MiddlewareDeSesion.cs), y por
la que filtran todos los repositorios. Cada prueba escribe esa cookie con un valor propio antes de
navegar y recibe su juego de localidades recién sembrado, sin ver nada de las demás. Es lo que
permite dejar `fullyParallel: true` y que la corrida de chromium termine en segundos.
La prueba *«cada prueba trabaja sobre su propio conjunto de datos»* verifica justamente eso.

Un detalle propio de Blazor Server: un circuito **no tiene acceso a la petición HTTP** que lo
originó, así que la cookie no se puede leer desde una página. El identificador se lee en
`App.razor` —que sí se renderiza dentro de la petición— y se pasa como parámetro al componente raíz
`Routes`, que es el puente entre el render estático y el interactivo.

### 3. La aplicación hay que compilarla antes de probarla

Se publica **autocontenida** para `linux-x64`: el contenedor de Playwright trae los navegadores
pero no el runtime de .NET, y de este modo el mismo artefacto corre en las dos imágenes sin
instalar nada. En CI se publica **una sola vez** y todos los shards reutilizan ese artefacto.

Un detalle que cuesta un rato encontrar: ASP.NET Core toma el **directorio actual** como raíz de
contenido. Si se lanza el binario desde otra carpeta, `wwwroot` no se encuentra, los recursos
estáticos se sirven vacíos —con `200` y `Content-Length: 0`, no con `404`— y el circuito nunca
arranca porque `blazor.web.js` llega en blanco. Por eso `playwright.config.js` fija
`cwd: 'publicacion'`.

### 4. Menos JavaScript, menos intermitencia

En la versión estática hubo que corregir dos defectos alrededor del modal de Bootstrap: el click
que llegaba durante la animación de apertura y el orden del manejador de `data-bs-dismiss`. Acá el
diálogo es marcado propio gobernado por el estado del componente, así que esa clase de carrera no
existe y no hace falta `reducedMotion: 'reduce'`.

### 5. El enlace de datos tiene que escuchar el evento correcto

`fill()` de Playwright dispara `input`, no `change`. Con el `@bind` por defecto —que escucha
`onchange`— el valor no llega al servidor hasta que el campo pierde el foco, y la validación
rechaza un formulario que en pantalla se ve completo. Los campos usan `@bind:event="oninput"`.

## Los workflows

### `e2e.yml` — la definición reutilizable

Es el único lugar donde está escrito *cómo* se corren las pruebas. Se dispara de tres maneras:

| Disparador | Para qué |
| --- | --- |
| `workflow_call` | Lo invocan `ci.yml` y `verificacion-entorno.yml`, y podría invocarlo otro repositorio |
| `workflow_dispatch` | Corrida a pedido desde la pestaña *Actions*, eligiendo navegadores, shards y entorno |
| `schedule` | Regresión completa todas las noches sobre la rama por defecto |

Entradas: `navegadores`, `cantidad-shards`, `url-base`, `referencia` y `retencion-dias`. Salida:
`resultado`.

Cómo trabaja:

1. `publicar` compila la aplicación autocontenida y la sube como artefacto. Se saltea cuando se
   prueba contra un entorno ya desplegado.
2. `preparar` traduce las entradas en la matriz `navegadores × shards`.
3. `pruebas` corre cada combinación en paralelo, instala el navegador que le toca, baja el
   artefacto de la aplicación y sube su reporte parcial (`blob`) y, si falla, las trazas y capturas.
4. `reporte` une los parciales con `playwright merge-reports` en un único reporte HTML, escribe el
   resumen de la corrida y refleja el resultado de las pruebas.

Desde otro repositorio se invoca así:

```yaml
jobs:
  e2e:
    uses: hdcm-dev/Lab-E2E.WebBlazor/.github/workflows/e2e.yml@main
    with:
      navegadores: chromium,firefox
      cantidad-shards: 2
```

### `ci.yml` — lo que se ata a la protección de rama

| Disparador | Alcance |
| --- | --- |
| `pull_request` hacia `main` o `develop` | Verificación rápida: solo `chromium`, en 2 shards |
| `push` a `main` | Verificación completa: los 4 navegadores, en 4 shards |
| `merge_group` | Igual que `push`, al entrar en la cola de merge |

Antes de gastar un runner con navegadores corren dos comprobaciones baratas: `compilacion`, que
construye la solución con `-warnaserror`, y `verificacion-rapida`, que hace `node --check` sobre el
JavaScript de las pruebas y `playwright test --list`, que detecta specs rotas y `test.only`
olvidados. Al terminar, `comentario-en-pr` deja (o actualiza, no duplica) un comentario con el
resultado y el enlace a la corrida, y `ci-ok` resume todos los jobs en un único check —que es el
que conviene exigir en la regla de protección de rama, para no tener que actualizarla cada vez que
cambia la matriz—.

### `verificacion-entorno.yml` — el mismo motor, otro objetivo

Prueba de humo a pedido contra un entorno ya desplegado, reutilizando `e2e.yml` con `url-base`.

### Prácticas aplicadas

- **`concurrency`** por rama, cancelando la corrida anterior en los pull requests y conservándola en
  `main`.
- **`permissions` mínimos**: `contents: read` en general, y `pull-requests: write` únicamente en el
  job que comenta.
- El comentario en el PR se limita a ramas del propio repositorio: un fork no recibe permisos de
  escritura, y así el job no falla.
- **Un paso comprueba que el SDK del runner coincida** con el `TargetFramework` del `.csproj`. Si
  divergen, la corrida falla con un mensaje claro en lugar de un error de compilación confuso.
- Los navegadores los instala `playwright install`, que baja la build correspondiente a la versión
  de `@playwright/test` del `package.json`: biblioteca y navegador no se pueden desincronizar.
- **Compilar una vez, probar muchas**: la aplicación se publica en un job y se reutiliza como
  artefacto en toda la matriz.
- **`paths-ignore`** para no disparar la CI por cambios de documentación.
- **`timeout-minutes`** en todos los jobs y **`fail-fast: false`** en la matriz, para ver todas las
  combinaciones que fallan y no solo la primera.

### Runner

Los jobs corren en el runner autoalojado `runs-on: [self-hosted, i7infra-dev]`, **directamente y no
dentro de un contenedor**. El motivo es concreto: ese runner es él mismo un contenedor y no tiene
montado el socket de Docker, así que un job con `container:` ni siquiera llega a arrancar —falla en
*Initialize containers* con `failed to connect to the docker API at unix:///var/run/docker.sock`—.

No hace falta: el runner corre Ubuntu 24.04 y ya trae el SDK de .NET 10.0.400 y Node 24, que es
todo lo que necesita la compilación. Los navegadores los instala Playwright en el propio job y
quedan cacheados para las corridas siguientes.

El único ajuste que exige ese entorno está en `playwright.config.js`: dentro de un contenedor
`/dev/shm` queda en 64 MB, Chromium lo agota y muere a mitad de la corrida, así que los proyectos
basados en Chromium se lanzan con `--disable-dev-shm-usage`.

## Evidencia

Estado verificado en esta máquina el 2026-08-23, con las imágenes
`mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.400) y
`mcr.microsoft.com/playwright:v1.62.1-noble`:

```
$ scripts/dotnet.sh dotnet build Lab-E2E.WebBlazor.sln --configuration Release -warnaserror
  Build succeeded.
      0 Warning(s)
      0 Error(s)

$ scripts/e2e.sh npx playwright test --project=chromium
  22 passed (11.1s)

$ scripts/e2e.sh npx playwright test
  88 passed (1.2m)
```

Son 22 pruebas por cada una de las 4 configuraciones de navegador. Los workflows, en cambio, no se
ejecutaron: se validó su sintaxis YAML, pero su comportamiento real solo puede comprobarse en
GitHub Actions con el runner `i7infra-dev` disponible.
