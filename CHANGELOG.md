# Changelog

Cambios relevantes de este laboratorio. El formato sigue
[Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

Las entradas se agrupan por fecha y no por número de versión: lo que se publica acá es un
laboratorio que se lee y se corre entero, no un artefacto que alguien instala en una versión
determinada. La documentación de estudio vive en
[`Lab-E2E.WebBlazor.Documentacion`](https://github.com/hdcm-dev/Lab-E2E.WebBlazor.Documentacion) y
tiene su propio registro.

## [Sin publicar] - 2026-09-12

### Añadido

- **`MovilidadUrbana.ApiWeb`** — la misma aplicación como API REST: `LocalidadesController` y
  `EncuestasController` sobre los mismos casos de uso, repositorios y base que la web. Sesión por
  encabezado `X-Sesion-Id` —el equivalente de la cookie—, `ValidationProblemDetails` (RFC 9457) con las
  mismas claves de error que la web, `201 Created` con `Location`, OpenAPI 3.1 en Development.
  Escucha en `http://localhost:5250`.
- **`MovilidadUrbana.ApiWeb.Tests`** — 11 casos en proceso con `WebApplicationFactory` sobre una base
  SQLite propia de la corrida: siembra por sesión, alta con `Location`, 400 por campo, duplicado,
  modificar y dar de baja, aislamiento entre sesiones, encuesta completa con resumen, encuesta
  incompleta, validación de un paso, paso inexistente. `ci.yml` los corre junto a las unitarias.
- **`evidencia/2026-09-12-capas-y-api/`** — Movilidad Urbana 22/22 tras extraer las capas; la API
  11/11, una falsificación (200 en vez de 201 pone un caso en rojo) y una corrida real sobre Kestrel
  con el OpenAPI y el flujo por `curl`.

### Cambiado

- **Las capas de Movilidad Urbana pasan a proyectos propios**: `MovilidadUrbana.Dominio`,
  `MovilidadUrbana.Aplicacion` y `MovilidadUrbana.Infraestructura`, con los espacios de nombres sin
  el `.Web.`. Hacía falta para que la API y la web compartieran las reglas sin duplicarlas ni
  referenciar un proyecto web desde otro. Cada capa registra sus servicios —`AgregarAplicacion()`,
  `AgregarInfraestructura(cadena)`— y los dos `Program.cs` solo componen. `MovilidadUrbana.UnitTests`
  referencia ahora `Dominio`, que es lo único que prueba. La web y sus 22 E2E no cambiaron de
  comportamiento; `Infraestructura` referencia el framework de ASP.NET Core solo por el middleware de
  sesión. La solución queda en doce proyectos.

- **Los proyectos mudados pierden el prefijo `E2E.Base`**: `WebBlazor.E2E.Base.HolaMundo` pasa a
  `WebBlazor.HolaMundo` y `WebBlazor.E2E.Base.Login` a `WebBlazor.Login`, y con ellos sus proyectos
  de prueba, `WebBlazor.HolaMundo.E2ETests` y `WebBlazor.Login.E2ETests`. Cambian carpetas,
  `.csproj`, espacios de nombres, la solución, los dos workflows, `scripts/pruebas.sh` y el README.
  El prefijo nombraba al repositorio de origen, que ya no existe. Los registros de `evidencia/`
  anteriores conservan el nombre viejo: son históricos. Verificado en
  `evidencia/2026-09-12-renombre/`: la solución compila en Release con `-warnaserror` y las dos
  baterías pasan con el script.

### Añadido

- **Hola Mundo y Login, desde `Lab-E2E.WebBlazor.Base`**, que se retira: sus dos proyectos web y sus
  pruebas E2E llegan a `src/` y `tests/`, idénticos byte a byte, y entran en la solución. El
  laboratorio queda con tres aplicaciones de complejidad creciente —Hola Mundo, Login, Movilidad
  Urbana— para estudiar la temática de a un escalón.
- **Un workflow E2E por proyecto web**, independientes y escalonados: `e2e-holamundo.yml` (un job,
  un navegador, levanta la app y prueba), `e2e-login.yml` (publica una vez y reparte a una matriz) y
  el `e2e.yml` de siempre para Movilidad Urbana. Se retira `e2e_2.yml`, que era el workflow de Base
  sin cambios: tenía 0 corridas exitosas de 4, no levantaba la aplicación y compartía `name` y nombre
  de artefacto con `e2e.yml`.
- **`scripts/pruebas.sh` elige el proyecto con `PROYECTO`** (`holamundo`, `login`; sin variable,
  Movilidad Urbana como antes) y suma `REPETIR`. Para los dos sin fixture levanta la aplicación en la
  URL que la prueba tiene escrita.
- **`evidencia/`**, con las dos carpetas rescatadas de Base —la del testigo de hidratación, que
  enlaza una guía, y la de la aplicación del template, que cita `Template-SDD-Aplicado.md`— y la de
  esta unificación. `.gitignore` deja pasar sus `.log`.

### Cambiado

- **Las pruebas de Hola Mundo y Login pasan de https a http**, en `http://localhost:5027` y
  `http://localhost:5181` —los perfiles http de sus `launchSettings`—. Con https no podían correr en
  CI sin confiar un certificado; se verificó que, escuchando solo http, ninguna de las dos redirige.
- **Playwright 1.62.0 en los tres proyectos de prueba**: los mudados venían en 1.52.0.
- **README**: estructura, cómo se corren los tres proyectos, los workflows en escalera y el conteo
  real de casos (22 + 10 + 1 E2E, 49 unitarios).

### Verificado

Registros en `evidencia/2026-09-12-unificacion/`: solución en Release con `-warnaserror` sin
advertencias; Hola Mundo 3 de 3 y Login 3 de 3 con el script; Movilidad Urbana 22/22 por el camino
por defecto; Login como lo corre `e2e-login.yml` —binario autocontenido en Production— 10/10 en
chromium y en firefox; y las dos baterías **fallan** sin la aplicación levantada.

### Corregido

- **La sección *Runner* del README** decía que todos los jobs corren en `ubuntu-latest`, pero el job
  `publicar` de `e2e.yml` (línea 89) tiene activo el runner propio `[self-hosted, i7infra-dev]`. Se
  corrige el texto, no el workflow: esa combinación es la que acumula 23 corridas en verde.

## [Sin publicar] - 2026-09-09

### Quitado

- **`Guides/`** — toda la documentación de estudio se muda a
  [`Lab-E2E.WebBlazor.Documentacion`](https://github.com/hdcm-dev/Lab-E2E.WebBlazor.Documentacion),
  a `Guides/E2E-Guide/` las de pruebas y a `Guides/` las de ramas e integración continua. El motivo
  es que las mismas guías vivían repartidas entre este repositorio y `Lab-E2E.WebBlazor.Base`, con
  dos copias del mismo texto que podían divergir sin que nada avisara. Una sola copia sirve ahora a
  los dos laboratorios.

### Cambiado

- **`Lab-E2E.WebBlazor.sln`** — se retiran las carpetas de solución `Guides` y `E2E-Guide` con sus
  siete elementos, y el anidamiento de la segunda dentro de la primera. Los documentos ya no están
  en el disco de este repositorio, así que el Explorador de soluciones los mostraría rotos.
  Se conservan `github-workflow`, `scripts` y `Solution Items`.
- **`README.md`** — la sección «Guías» deja de listar documento por documento y remite al
  repositorio de documentación, con dos entradas por carpeta y la advertencia de que los documentos
  citan el código de acá por ruta relativa, así que conviene clonar los repositorios como carpetas
  hermanas. Se retiran de paso los enlaces a `Guides/Estandares-Modelo-Ramas-Guide/` y
  `Guides/GitFlow-Practice-Guide/`, que apuntaban a una estructura de una carpeta por guía con
  archivos numerados que este repositorio nunca tuvo: estaban rotos desde antes de la mudanza.

## [Sin publicar] - 2026-09-04

### Añadido

- **`Guides/E2E-Guide/Caso-Encuesta-Page.md`** — cómo se **diseñan** los casos de la superficie
  Encuesta, que es lo que las dos guías anteriores no cubren: `Beginner-Guide` explica cómo se
  escribe una prueba y `Quick-Guide-ABM` cómo se monta la de un ABM; acá está con qué criterios se
  decide qué probar. El caso obliga a responder una pregunta que no es obvia —**¿tres pasos son
  tres superficies?**— y la respuesta es que no: el `Asistente` declara en su primera línea que es
  «solo para actos divisibles», y ninguno de los tres pasos promete nada por sí solo, así que es
  **una** superficie con estados. La señal para distinguir un acto divisible de un recorrido es si
  al abandonar en el medio queda algo hecho. Quedan escritos además por qué el `Asistente` —109
  líneas, con lógica y estados propios— **no es** una superficie, cómo se prueba una promesa sobre
  la memoria —yendo y volviendo, nunca leyendo el modelo—, por qué el caso central **recarga la
  página** para distinguir lo persistido de lo que vive en el circuito, y a quién le pertenece cada
  identificador de prueba: los del `Asistente` son contrato reutilizable y los de la superficie son
  de ella sola.

### Cambiado

- **`Lab-E2E.WebBlazor.sln`** suma el documento nuevo a la carpeta de solución `E2E-Guide`. La
  solución declara los archivos sueltos uno por uno, así que un documento que no se agrega existe
  en el repositorio pero no en el explorador de quien abre la solución: queda escrito y sin leer.

### Encontrado, no resuelto

- **Dos promesas del `src` no tienen caso de prueba.** Verificado el 2026-09-04 contra las nueve
  pruebas de `EncuestaTests`. La primera: el paso es direccionable —`/encuesta/{Paso:int}`— y
  `OnParametersSet` declara que «un paso pedido por dirección no puede saltear los anteriores»,
  pero **ninguna prueba navega a `/encuesta/2` ni a `/encuesta/3`**; el `Math.Min(pedido,
  _pasoMaximoAlcanzado)` que impide el salteo no está cubierto. Es llamativo porque el motivo
  declarado de que el paso sea direccionable es «para que se pueda verificar de a uno»: la
  capacidad se construyó para la prueba y la prueba no la usa. La segunda: el `catch` de
  `FinalizarAsync` y el estado `boton-procesando` del `Asistente` no se ejercitan nunca — ahí la
  ausencia tiene motivo, porque provocar el fallo desde el navegador exige un punto de inyección
  que la aplicación hoy no tiene.

### Cambiado

- **La interfaz pasa a ser el template del Framework SDD.** Se aplica
  `Knowledge-Template-HTML-SDD-Default` y su realización
  `Knowledge-Template-Blazor-Interactive-Server-SDD-Default`: se retira **Bootstrap** —la hoja
  vendorizada y `estilos.css`— y en su lugar quedan `wwwroot/css/Tokens.css`, con los tokens del
  catálogo `Design-Rules-Web-Generico.md` §2 copiados sin cambios, y `wwwroot/css/Componentes.css`
  con los patrones. Ningún `.razor`, `.razor.css` ni `.cs` escribe un color, una tipografía o un
  espaciado, y no queda un solo `style=` en línea.
- **Un componente propio por patrón del catálogo**, en `Components/Componentes/`: `Grilla<T>` con
  `ColumnaDeGrilla<T>`, `Campo`, `Asistente` con `PasoDeAsistente`, `Dialogo` con `DialogoHost`,
  `Insignia`, `Banda`, `EstadoVacio`, `EstadoIndisponible`, `Esqueleto`, `Icono`, `BarraLateral`,
  `SelloDeVersion` y `AvisoDeReconexion`. Las páginas no reimplementan ninguno en línea: el ABM y
  la encuesta quedaron en marcado de superficie más su code-behind.
- **El shell es el de trabajo del template**: barra lateral con el chrome oscuro de marca y el ítem
  activo marcado, contenido con `#mq-main`, enlace «Ir al contenido» y sello de identidad de
  versión al pie, resuelto en el punto de composición por `IIdentidadDeVersion`.
- **Cada superficie declara y resuelve sus estados** con `EstadoDeSuperficie` —el vocabulario del
  template— y un bloque por estado. `Vacio` y `FiltradoSinResultados` son estados distintos, con
  acciones distintas; para que el segundo signifique algo, el ABM suma **barra de filtros** con
  búsqueda por nombre o código postal y filtro por provincia, sobre lo ya traído.
- **La colección se presenta de dos formas, las dos siempre en el marcado**: tabla con `caption`
  accesible y `scope` en todos los encabezados, y tarjetas apiladas debajo de los 768px. Las
  conmuta el único punto de quiebre del CSS.
- **El diálogo de confirmación es el `<dialog>` nativo**, con host único en el layout y
  `IServicioDeDialogos`: confinamiento de foco y cierre por Escape los trae el navegador, y la
  interoperabilidad se reduce a `mq-dialogo.js`. El foco vuelve al control que lo abrió.
- **El asistente de la encuesta pasa al indicador de pasos normado** —círculo, conector, rótulo y
  los tres estados de paso, con contador y anuncio en región activa— y **el paso vigente está en la
  dirección** (`/encuesta/2`), sin permitir saltear los anteriores. Se retira la barra de progreso:
  era un segundo canal que decía lo mismo.
- **Los requisitos de cada campo se muestran antes del intento y se derivan de la política**, con
  `PoliticaDeLocalidades` y `PoliticaDeEncuestas` sobre las constantes de `Dominio/Reglas`; los
  errores siguen decidiéndose en el servicio de aplicación y ahora se asocian al control por
  `aria-describedby`. Los límites que estaban escritos a mano en la vista y en los mensajes
  —60 caracteres, 4 dígitos, 3 caracteres— pasan a constantes de las reglas.
- **La interfaz de reconexión se estiliza acorde a la marca**: `AvisoDeReconexion` reemplaza el
  modal por defecto del template de .NET por una banda de atención con `role="status"`, en español,
  que no bloquea la interacción.
- **La suite E2E se adapta a la interfaz nueva sin aflojar ninguna verificación**: siguen siendo 22
  casos. `IrPorMenuAsync` ya no despliega un menú colapsable; el diálogo se verifica por sus
  identificadores propios; el avance del asistente se verifica sobre el indicador de pasos; y las
  acciones de fila se buscan sobre la presentación visible, con lo que el mismo caso corre en
  escritorio y en móvil. El detalle y su motivo, en la tabla del `README.md`.

### Añadido

- **`README.md` gana una sección [Diseño](README.md#diseño)** con el mapa patrón → componente, lo
  que del template **no aplica** a este laboratorio y por qué —no hay credenciales, así que no hay
  shell de acceso ni endpoints de identidad ni guard en tres capas—, y las cuatro **desviaciones
  declaradas**: render mode en la raíz por el puente de la cookie de sesión, `EditForm` sin
  anotaciones porque la política vive en el dominio, la revisión del asistente como estado de éxito
  en lugar de cuarto paso, y los anchos de contenido en `ch` por no promover tokens al catálogo.

### Pendiente

- **El índice del `README.md` y las carpetas de solución del `.sln` siguen apuntando a los archivos
  borrados** en la consolidación de las guías. Es lo mismo que quedó anotado el 2026-09-03 y no lo
  toca este cambio.

## [Sin publicar] - 2026-09-03

### Cambiado

- **La solución se pone al día con la consolidación de las guías.** `Lab-E2E.WebBlazor.sln` seguía
  declarando la estructura vieja: los nodos `GitFlow-Practice-Guide`, `GitHubFlow-Practice-Guide`,
  `Estandares-Modelo-Ramas-Guide` con sus `Anexos` y `workflows`, y `GitHub-Action-Guide`, con
  **37 archivos que ya no existen**. En Visual Studio eso se ve como una carpeta llena de enlaces
  rotos, y es la clase de cosa que sobrevive meses porque no rompe el build. Los cinco nodos se
  eliminan y los cuatro documentos consolidados —`Estandares-Modelo-Ramas.md`,
  `Guia-Practica-GitFlow.md`, `Guia-Practica-GitHubFlow.md` y `GitHub-Action-Guide.md`— cuelgan
  directo de `Guides`.
- **Se suman a la solución los archivos de la raíz** que nunca habían estado: `README.md`,
  `CHANGELOG.md` y `pruebas.runsettings`, en un nodo `Solution Items`. El `runsettings` es el que
  más se busca y el que menos se encontraba.


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
