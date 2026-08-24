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
tests/MovilidadUrbana.E2ETests/
  Infraestructura/
    ServidorDeLaAplicacion.cs   Levanta y baja la aplicación bajo prueba
    PruebaE2E.cs                Base: sesión por prueba, espera de interactividad, menú
    ParalelismoDelEnsamblado.cs Configuración de paralelismo de NUnit
  NavegacionTests.cs    Portada, menú y ruta inexistente
  LocalidadesTests.cs   ABM completo y aislamiento entre sesiones
  EncuestaTests.cs      Asistente completo
scripts/
  dotnet.sh             Ejecuta el SDK de .NET dentro del contenedor oficial
  publicar.sh           Publica el binario autocontenido que usa CI, en `publicacion/`
  pruebas.sh            Corre las E2E sin tener nada instalado (contenedor + SDK local)
Guides/
  Beginner-Guide.md     Guía de estudio para quien nunca escribió una prueba E2E
pruebas.runsettings     Navegador, timeouts y paralelismo de las pruebas
.github/workflows/      CI, workflow reutilizable de E2E y verificación de entornos
```

En la solución, los archivos que no pertenecen a ningún proyecto están agrupados en tres carpetas
virtuales —`github-workflow`, `Guides` y `scripts`—, para poder abrirlos desde el Explorador de
soluciones sin salir de Visual Studio. Son carpetas de solución: no se compilan ni cambian nada del
build.

### Por qué las pruebas E2E son un proyecto de la solución

Se usan las **vinculaciones oficiales de Playwright para .NET** (`Microsoft.Playwright.NUnit`) y no
el runner de JavaScript, con un objetivo concreto: que las pruebas se descubran, se ejecuten y se
**depuren desde Visual Studio**, sin salir del IDE ni del lenguaje de la aplicación.

Es una decisión con costo, y conviene tenerlo a la vista. El Explorador de pruebas de Visual Studio
soporta pruebas de JavaScript, pero solo de Mocha, Jasmine, Tape, Jest y Vitest: Playwright no está
en esa lista, así que con el runner de JavaScript no hay forma de integrarlo. A cambio de ganar el
IDE se pierden funciones que solo existen en `@playwright/test`: `--shard`, el reporter `blob` con
`merge-reports` y el reporte HTML. Acá el reporte de cada configuración es un **TRX** y el
paralelismo lo maneja NUnit.

La alternativa —dejar las E2E fuera de la solución, en una carpeta `e2e/` con specs de
TypeScript— es la que eligió la aplicación de referencia de .NET,
[dotnet/eShop](https://github.com/dotnet/eShop). Las dos son defendibles; esta prioriza el IDE.

## Cómo se corren

### Desde Visual Studio

Abrí `Lab-E2E.WebBlazor.sln` y listo: **Test > Explorador de pruebas** descubre los 22 casos y
podés ejecutarlos o depurarlos de a uno, con puntos de interrupción en el código C# de la prueba.
No hay ningún paso previo.

La primera corrida tarda unos minutos porque baja el navegador; las siguientes, segundos.

**No hace falta publicar la aplicación a mano.** Las pruebas ejercitan la aplicación *publicada*
—no el proyecto compilado—, y publicarla es responsabilidad del propio fixture: antes de la primera
prueba corre `dotnet publish` sobre `publicacion/`. Eso además garantiza que lo que se prueba está
al día: si tocás una página de Blazor y volvés a correr las pruebas, se republica sola.

El navegador corre la misma suerte: el fixture lo instala llamando al instalador que expone el
paquete `Microsoft.Playwright`, en vez de exigir `pwsh playwright.ps1 install` —que además obliga a
tener PowerShell 7, un producto aparte del PowerShell que trae Windows—. Se instala solo el
navegador de la corrida, no los tres. Para desactivarlo: `INSTALAR_NAVEGADORES=false`.

Publicar e instalar se hacen en el fixture y no en el build a propósito. Atado al build, el paso queda a merced de que
el entorno decida compilar —Visual Studio evalúa por su cuenta si el proyecto está al día y cómo
invocar targets de otro proyecto—, y cuando esa decisión no sale como se espera no hay publicación
y todas las pruebas mueren en `OneTimeSetUp`. En el fixture corre siempre y de la misma forma en la
consola, en el IDE y en CI. `dotnet publish` es incremental: cuando no cambió nada tarda un par de
segundos.

Para desactivarlo —CI lo hace, porque allá la aplicación llega como artefacto— se define
`PUBLICAR_ANTES_DE_PROBAR=false`.

El navegador sale de `pruebas.runsettings` (**Test > Configurar archivo de configuración de
ejecución**). Para la configuración móvil, definí la variable de entorno `EMULAR_MOVIL=true`.

### Desde la línea de comandos

```bash
dotnet test tests/MovilidadUrbana.E2ETests --settings pruebas.runsettings
dotnet test tests/MovilidadUrbana.E2ETests --settings pruebas.runsettings -- Playwright.BrowserName=firefox
```

`scripts/publicar.sh` sigue existiendo para producir la publicación **autocontenida** que usa CI,
pero para correr las pruebas no hay que invocarlo.

### Sin nada instalado (con Docker)

`scripts/pruebas.sh` corre todo dentro de la imagen oficial de Playwright —que ya trae las
librerías de sistema que los navegadores necesitan— y le agrega el SDK de .NET en `.dotnet/`. Los
navegadores quedan en `.navegadores/`, así que solo se descargan la primera vez. Ambas carpetas
están ignoradas por git.

```bash
scripts/pruebas.sh                     # chromium
scripts/pruebas.sh firefox
scripts/pruebas.sh webkit
EMULAR_MOVIL=true scripts/pruebas.sh   # chromium emulando un Pixel 7
```

Para probar contra un entorno ya desplegado se define `URL_BASE` y no se levanta nada local:

```bash
URL_BASE=https://ejemplo.test scripts/pruebas.sh chromium
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
con `RendererInfo.IsInteractive`, y `EsperarInteractivoAsync` de
[PruebaE2E.cs](tests/MovilidadUrbana.E2ETests/Infraestructura/PruebaE2E.cs) lo espera antes de
tocar nada.

### 2. El estado es del servidor, así que hay que aislarlo

En el ejemplo estático cada prueba tenía su `localStorage`. Acá hay una única base SQLite y todas
las pruebas —incluidas las que corren en paralelo— la comparten.

La solución es que la aplicación reparta un **espacio de datos por sesión**: una cookie que emite
[MiddlewareDeSesion](src/MovilidadUrbana.Web/Infraestructura/Sesiones/MiddlewareDeSesion.cs), y por
la que filtran todos los repositorios. Cada prueba escribe esa cookie con un valor propio antes de
navegar y recibe su juego de localidades recién sembrado, sin ver nada de las demás. Es lo que
permite correr las clases de prueba en paralelo y que la corrida de chromium termine en segundos.
La prueba *«cada prueba trabaja sobre su propio conjunto de datos»* verifica justamente eso.

Un detalle propio de Blazor Server: un circuito **no tiene acceso a la petición HTTP** que lo
originó, así que la cookie no se puede leer desde una página. El identificador se lee en
`App.razor` —que sí se renderiza dentro de la petición— y se pasa como parámetro al componente raíz
`Routes`, que es el puente entre el render estático y el interactivo.

### 3. La aplicación hay que compilarla antes de probarla

En CI se publica **autocontenida** para `linux-x64`, así el binario no depende del runtime que
tenga instalado quien lo ejecute, y se publica **una sola vez** para que todas las configuraciones
de navegador la reutilicen. En la máquina de quien desarrolla alcanza con una publicación
dependiente del framework, que evita tener que elegir un identificador de plataforma.

El fixture admite las dos formas: usa el ejecutable nativo si está —`MovilidadUrbana.Web.exe` en
Windows, sin extensión en Linux y macOS— y, si no, arranca con `dotnet MovilidadUrbana.Web.dll`.
El nombre fijo de Linux era justamente lo que hacía fallar el descubrimiento en Windows.

Quien las levanta y las baja es
[ServidorDeLaAplicacion](tests/MovilidadUrbana.E2ETests/Infraestructura/ServidorDeLaAplicacion.cs),
un `[SetUpFixture]` de NUnit: el binding de .NET no tiene el equivalente del bloque `webServer` que
ofrece el runner de JavaScript, así que el ciclo de vida del servidor se maneja a mano.

Un detalle que cuesta un rato encontrar: ASP.NET Core toma el **directorio actual** como raíz de
contenido. Si se lanza el binario desde otra carpeta, `wwwroot` no se encuentra, los recursos
estáticos se sirven vacíos —con `200` y `Content-Length: 0`, no con `404`— y el circuito nunca
arranca porque `blazor.web.js` llega en blanco. Por eso el fixture fija `WorkingDirectory` en la
carpeta de la publicación.

### 4. Menos JavaScript, menos intermitencia

En la versión estática hubo que corregir dos defectos alrededor del modal de Bootstrap: el click
que llegaba durante la animación de apertura y el orden del manejador de `data-bs-dismiss`. Acá el
diálogo es marcado propio gobernado por el estado del componente, así que esa clase de carrera no
existe y no hace falta desactivar las animaciones.

### 5. El enlace de datos tiene que escuchar el evento correcto

`FillAsync` dispara `input`, no `change`. Con el `@bind` por defecto —que escucha `onchange`— el
valor no llega al servidor hasta que el campo pierde el foco, y la validación rechaza un
formulario que en pantalla se ve completo. Los campos usan `@bind:event="oninput"`.

### 6. Con el binding de .NET, el paralelismo llega hasta la clase

NUnit no paraleliza por defecto, así que hay que pedirlo. Pero subirlo a
`ParallelScope.Children` —tests en paralelo dentro de una misma clase— rompe la integración de
Playwright, que lleva un registro de servicios por worker: la corrida falla con
`The given key 'Browser' was not present in the dictionary` y
`Collection was modified; enumeration operation may not execute`. El límite práctico es
`ParallelScope.Fixtures`: las clases en paralelo entre sí, los casos de cada clase en secuencia.
Es una diferencia real con `fullyParallel: true` del runner de JavaScript, que reparte caso por
caso.

Otra trampa: un `[SetUpFixture]` cubre **su** namespace y los que cuelgan de él, nunca el de
arriba. Puesto en `MovilidadUrbana.E2ETests.Infraestructura` no se ejecuta para las pruebas de
`MovilidadUrbana.E2ETests`, y el síntoma es desconcertante —la URL base llega vacía y Playwright
se queja de la cookie—.

## Los workflows

### `e2e.yml` — la definición reutilizable

Es el único lugar donde está escrito *cómo* se corren las pruebas. Se dispara de tres maneras:

| Disparador | Para qué |
| --- | --- |
| `workflow_call` | Lo invocan `ci.yml` y `verificacion-entorno.yml`, y podría invocarlo otro repositorio |
| `workflow_dispatch` | Corrida a pedido desde la pestaña *Actions*, eligiendo navegadores y entorno |
| `schedule` | Regresión completa todas las noches sobre la rama por defecto |

Entradas: `navegadores`, `url-base`, `referencia` y `retencion-dias`. Salida: `resultado`.

Cómo trabaja:

1. `publicar` compila la aplicación autocontenida y la sube como artefacto. Se saltea cuando se
   prueba contra un entorno ya desplegado.
2. `preparar` traduce la lista de configuraciones en la matriz del job siguiente.
3. `pruebas` corre cada configuración en paralelo: compila el proyecto de pruebas, instala el
   navegador que le toca, baja el artefacto de la aplicación, ejecuta `dotnet test` y sube el TRX.
4. `reporte` junta los TRX de todas las configuraciones en una única tabla en el resumen de la
   corrida y refleja el resultado de las pruebas.

Desde otro repositorio se invoca así:

```yaml
jobs:
  e2e:
    uses: hdcm-dev/Lab-E2E.WebBlazor/.github/workflows/e2e.yml@main
    with:
      navegadores: chromium,firefox
```

### `ci.yml` — lo que se ata a la protección de rama

| Disparador | Alcance |
| --- | --- |
| `pull_request` hacia `main` o `develop` | Verificación rápida: solo `chromium` |
| `push` a `main` | Verificación completa: las 4 configuraciones |
| `merge_group` | Igual que `push`, al entrar en la cola de merge |

Antes de gastar un runner con navegadores corre `compilacion`, que construye la solución con
`-warnaserror` y lista las pruebas con `dotnet test --list-tests` —el equivalente del
`playwright test --list` del runner de JavaScript: comprueba que el descubrimiento funcione sin
levantar navegadores ni la aplicación—. Al terminar, `comentario-en-pr` deja (o actualiza, no duplica) un comentario con el
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
- Los navegadores los instala el CLI que viene dentro del paquete `Microsoft.Playwright`, que baja
  la build correspondiente a su propia versión: biblioteca y navegador no se pueden desincronizar.
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

Sobre `/dev/shm`: dentro de un contenedor queda en 64 MB, y es la causa clásica de que Chromium
muera a mitad de una corrida. Se midió: con `/dev/shm` limitado a 64 MB las 22 pruebas de chromium
pasan igual, así que a esta escala no hace falta `--disable-dev-shm-usage`. Si alguna vez aparece
esa intermitencia, las dos salidas son ese argumento de lanzamiento o darle más `--shm-size` al
contenedor del runner.

## Evidencia

Estado verificado en esta máquina el 2026-08-23, con las imágenes
`mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.400) y
`mcr.microsoft.com/playwright:v1.62.1-noble`:

```
$ scripts/dotnet.sh dotnet build Lab-E2E.WebBlazor.sln --configuration Release -warnaserror
  Build succeeded.
      0 Warning(s)
      0 Error(s)

$ scripts/pruebas.sh chromium
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 7 s

$ scripts/pruebas.sh firefox
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 13 s

$ scripts/pruebas.sh webkit
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 15 s

$ EMULAR_MOVIL=true scripts/pruebas.sh chromium
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 6 s
```

Son 22 pruebas por cada una de las 4 configuraciones: 88 en total, las mismas que cubría la versión
con el runner de JavaScript. El descubrimiento que usa la CI también se comprobó
(`dotnet test --list-tests`).

Lo que **no** se verificó desde acá: la ejecución desde el Explorador de pruebas de Visual Studio
—no hay Windows ni Visual Studio en esta máquina— y el comportamiento real de los workflows, del
que solo se validó la sintaxis YAML.
