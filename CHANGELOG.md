# Changelog

Cambios relevantes de este laboratorio. El formato sigue
[Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

Las entradas se agrupan por fecha y no por número de versión: lo que se publica acá es un
laboratorio que se lee y se corre entero, no un artefacto que alguien instala en una versión
determinada. La documentación de estudio vive en
[`Lab-E2E.WebBlazor.Documentacion`](https://github.com/hdcm-dev/Lab-E2E.WebBlazor.Documentacion) y
tiene su propio registro.

## [Sin publicar] - 2026-09-03

### Cambiado

- **Cada guía del modelo de ramas pasa a ser un solo documento**, tomando la consolidación hecha en
  [`Lab-Documentos.Documentacion`](https://github.com/hdcm-dev/Lab-Documentos.Documentacion): los
  ocho documentos de estudio y los cinco anexos de `Estandares-Modelo-Ramas-Guide/` se unifican en
  `Estandares-Modelo-Ramas.md` (2029 líneas), y los ocho escenarios de cada guía práctica en
  `Guia-Practica-GitFlow.md` (1178) y `Guia-Practica-GitHubFlow.md` (1067). El texto es el mismo: lo
  que cambia es que las referencias cruzadas pasan a ser anclas internas y desaparecen las líneas de
  navegación «Sigue: …». Se borran los 27 archivos que reemplazan, los `README.md` de esas tres
  carpetas incluidos. `E2E-Guide/` conserva sus dos documentos, porque son dos guías distintas y no
  dos capítulos. Fuera del consolidado quedan `Anexos/` —los cinco archivos sueltos y los tres
  workflows de ejemplo con su `README.md`—.
- **`.github/workflows/e2e.yml` vuelve al runner autoalojado**: `runs-on: [self-hosted,
  i7infra-dev]` deja de estar comentado y `ubuntu-latest` pasa a ser el ejemplo comentado. Es la
  configuración de este laboratorio; el repositorio es público, así que la aprobación de workflows
  desde forks queda como guardarraíl del lado de GitHub.

### Pendiente

- **El índice del `README.md` y las carpetas de solución del `.sln` siguen apuntando a los archivos
  borrados** —`01-Marco-De-Referencia.md`, `00-Preparacion.md` y los demás—. Hasta que se
  reescriban, el Explorador de soluciones muestra archivos faltantes y los enlaces del README
  quedan rotos.

## [Sin publicar] - 2026-08-31

### Añadido

- **Guía de GitHub Actions** en `Guides/GitHub-Action-Guide/GitHub-Action-Guide.md`: guía de estudio
  que va del marco conceptual —qué es un pipeline, una puerta y qué significa «continuo»— a la
  anatomía de un workflow sección por sección, y de ahí a escenarios completos (compilar y probar,
  E2E, publicar un paquete, subir un sitio, imagen de contenedor, app móvil). Los ejemplos salen de
  workflows que existen y corren en este workspace —los de `.github/workflows/` de este laboratorio
  entre ellos—; lo ilustrativo se marca como tal con su fuente. Queda al lado de las guías del
  modelo de ramas porque explica la maquinaria que ejecuta las puertas que aquéllas describen.
- **Carpeta de solución `GitHub-Action-Guide`**, colgando de `Guides` igual que el resto, para
  abrir la guía desde el Explorador de soluciones sin salir del IDE.

## [Sin publicar] - 2026-08-30

### Añadido

- **Proyecto de pruebas unitarias** `tests/MovilidadUrbana.UnitTests/`: 49 casos sobre las reglas de
  dominio —`ReglasDeLocalidad` y `ReglasDeEncuesta`—, sin navegador y en milisegundos. Es hermano
  del proyecto de E2E y no parte de él: verifica las reglas por sí solas, mientras que las E2E
  verifican el circuito completo. `ci.yml` lo corre en el job barato, antes de gastar un runner con
  navegadores.
- **Captura de traza de Playwright** en `PruebaE2E`. Se graba en todos los casos y se conserva solo
  en los que fallan, en `resultados/trazas/<caso>.zip`, con el DOM paso a paso, la red y la consola;
  se abre con `playwright show-trace`. El binding de .NET no tiene el `trace: 'on-first-retry'` del
  runner de JavaScript —el adaptador solo lee `BrowserName`, `ExpectTimeout` y `LaunchOptions` del
  `.runsettings`—, así que el ciclo de vida se maneja a mano en un `[TearDown]`. Se apaga con
  `TRAZAR=false`. Cuesta unos 2 segundos sobre los 22 casos de chromium.
- **`ServidorDeLaAplicacion.RaizDelRepositorio`**, que ya usaban la publicación y la base de datos y
  ahora también las trazas.
- **Guías del modelo de ramas** en `Guides/`, tomadas de
  [`Lab-GitFlow.Documentacion`](https://github.com/hdcm-dev/Lab-GitFlow.Documentacion) para poder
  leerlas junto al código que las pruebas verifican:
  `Estandares-Modelo-Ramas-Guide/` —ocho documentos de estudio y sus anexos, con tres workflows de
  ejemplo—, `GitFlow-Practice-Guide/` y `GitHubFlow-Practice-Guide/` —ocho escenarios ejecutables
  cada una—.
- **`CHANGELOG.md`** — este archivo.

### Cambiado

- **`Guides/` pasa a tener una carpeta por guía**: `Beginner-Guide.md` y `Quick-Guide-ABM.md` se
  mudan a `Guides/E2E-Guide/`, que es lo que deja lugar a las tres guías nuevas.
- **Las carpetas de solución de `Guides` reproducen el árbol del disco**: `E2E-Guide`,
  `Estandares-Modelo-Ramas-Guide` —con `Anexos` y `workflows` colgando de ella—,
  `GitFlow-Practice-Guide` y `GitHubFlow-Practice-Guide`. Antes apuntaban a rutas que no existían
  (`Guides\E2E\`, `Guides\Modelo-Ramas\…`) y la guía de GitHub Flow no figuraba, así que el
  Explorador de soluciones mostraba archivos faltantes.
- **La cantidad de workers se declara en un solo lugar.** Se quita
  `[assembly: LevelOfParallelism(3)]`: el *alcance* del paralelismo (`ParallelScope.Fixtures`) es
  una decisión del código y sigue en `ParalelismoDelEnsamblado.cs`, pero el *número* vive solo en
  `NumberOfTestWorkers` de `pruebas.runsettings`. Con los dos declarados, podían divergir sin que
  nada avisara.
- **`ci.yml`** — el job `compilacion` pasa a llamarse «Compilación y unitarias», corre las pruebas
  unitarias y sube su TRX como artefacto `resultados-unitarias`. El comentario en el pull request
  deja de prometer un reporte HTML —que el binding de .NET no produce— y señala los TRX y las
  trazas de los artefactos `resultados-*`.
- **`e2e.yml`** — la cabecera describe el runner alojado por GitHub y por qué el SDK se pide con
  `actions/setup-dotnet` y los navegadores se cachean; el motivo de no usar `container:` queda
  atribuido al runner autoalojado, que es de donde viene.
- **`README.md`** — suma el proyecto de unitarias y las cuatro carpetas de guías con su índice, la
  traza de los casos fallidos, la sección del runner y la evidencia del 2026-08-24.

### Verificado

Sobre las imágenes `mcr.microsoft.com/dotnet/sdk:10.0` y
`mcr.microsoft.com/playwright:v1.62.1-noble`:

- `dotnet build Lab-E2E.WebBlazor.sln --configuration Release -warnaserror` — 0 avisos, 0 errores.
- `dotnet test tests/MovilidadUrbana.UnitTests` — 49 pasadas.
- `scripts/pruebas.sh chromium` — 22 pasadas.
- `dotnet sln list` reconoce los tres proyectos con el `.sln` ya reorganizado.

No se verificó desde acá la ejecución en el Explorador de pruebas de Visual Studio —no hay Windows
en esta máquina— ni el comportamiento real de los workflows, del que solo se validó la sintaxis.
