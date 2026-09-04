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

El diseño **no usa librería de componentes ni framework de CSS**: es el template del Framework
SDD —[Knowledge-Template-HTML-SDD-Default](../../IA/SDD/IA.SDD/Conocimiento/Knowledge-Template-HTML-SDD-Default.md)
y su realización en Blazor,
[Knowledge-Template-Blazor-Interactive-Server-SDD-Default](../../IA/SDD/IA.SDD/Conocimiento/Knowledge-Template-Blazor-Interactive-Server-SDD-Default.md)—.
Los tokens del catálogo viven en
[wwwroot/css/Tokens.css](src/MovilidadUrbana.Web/wwwroot/css/Tokens.css) y los patrones en
[Componentes.css](src/MovilidadUrbana.Web/wwwroot/css/Componentes.css); cada patrón del catálogo
—grilla, asistente, diálogo, insignia, banda, estado vacío— es un componente Razor propio de
[Components/Componentes/](src/MovilidadUrbana.Web/Components/Componentes/), y las páginas no
reimplementan ninguno en línea. Ver [Diseño](#diseño).

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
    App.razor, Routes.razor
    Layout/             MainLayout: shell de trabajo, barra lateral, hosts y sello de versión
    Componentes/        Un componente propio por patrón del catálogo de diseño
    Pages/              Una superficie por pantalla, con su code-behind
  Theme/                Iconos.cs (trazos SVG) y RolesDeIcono.cs (tamaño por rol)
  Servicios/            Diálogos y foco (Scoped) e identidad de versión (Singleton)
  wwwroot/              css/Tokens.css, css/Componentes.css y la interoperabilidad mínima de js/
  Program.cs            Composición: es el único lugar que conoce todas las capas
tests/MovilidadUrbana.E2ETests/
  Infraestructura/
    ServidorDeLaAplicacion.cs   Levanta y baja la aplicación bajo prueba
    PruebaE2E.cs                Base: sesión por prueba, espera de interactividad, menú, traza
    ParalelismoDelEnsamblado.cs Alcance del paralelismo de NUnit
  NavegacionTests.cs    Portada, menú y ruta inexistente
  LocalidadesTests.cs   ABM completo y aislamiento entre sesiones
  EncuestaTests.cs      Asistente completo
tests/MovilidadUrbana.UnitTests/
  ReglasDeLocalidadTests.cs   Bordes de cada validación del ABM, sin navegador
  ReglasDeEncuestaTests.cs    Rangos de la encuesta, paso por paso
scripts/
  dotnet.sh             Ejecuta el SDK de .NET dentro del contenedor oficial
  publicar.sh           Publica el binario autocontenido que usa CI, en `publicacion/`
  pruebas.sh            Corre las E2E sin tener nada instalado (contenedor + SDK local)
Guides/
  E2E-Guide/                      Pruebas de extremo a extremo: guía de estudio y receta de ABM
  Estandares-Modelo-Ramas-Guide/  Modelos de ramas, integración y releases, con sus anexos
  GitFlow-Practice-Guide/         Ocho escenarios del modelo adoptado, sobre un repositorio real
  GitHubFlow-Practice-Guide/      Los mismos ocho, sobre el modelo que no se adoptó
pruebas.runsettings     Navegador, timeouts y paralelismo de las pruebas
.github/workflows/      CI, workflow reutilizable de E2E y verificación de entornos
```

Son tres proyectos: la aplicación, las pruebas de extremo a extremo y las unitarias sobre las
reglas de dominio. La aplicación sigue siendo **un solo proyecto** con las capas en carpetas; lo que
se sumó son proyectos de prueba.

Los archivos que no pertenecen a ningún proyecto están agrupados en carpetas de solución
—`github-workflow`, `Guides` y `scripts`—, para poder abrirlos desde el Explorador de soluciones sin
salir de Visual Studio. `Guides` reproduce el árbol del disco: una carpeta por guía, y los anexos
de la guía de estudio colgando de ella. Son carpetas de solución: no se compilan ni cambian nada
del build.

## Diseño

La interfaz aplica el template del Framework SDD: el
[template HTML de maqueta](../../IA/SDD/IA.SDD/Conocimiento/Knowledge-Template-HTML-SDD-Default.md)
y su realización en
[Blazor Interactive Server sin librería de componentes](../../IA/SDD/IA.SDD/Conocimiento/Knowledge-Template-Blazor-Interactive-Server-SDD-Default.md),
que hereda de él. Los valores visuales salen del catálogo
[`Design-Rules-Web-Generico.md`](../../IA/SDD/IA.SDD/SDD/Devs/References/Design/Design-Rules-Web-Generico.md)
§2, copiados sin cambios en `Tokens.css`.

### Un componente propio por patrón

| Patrón del catálogo | Componente | Qué concentra |
| --- | --- | --- |
| §4.1 Navegación lateral | [BarraLateral](src/MovilidadUrbana.Web/Components/Componentes/BarraLateral.razor) | Identidad, ítems con ícono, ítem activo con `aria-current="page"` |
| §4.2 Tarjeta de acceso | `.mq-tarjeta-entrada` en [Inicio](src/MovilidadUrbana.Web/Components/Pages/Inicio.razor) | Toda la tarjeta es el área activable, con el foco en el contenedor |
| §4.3 Grilla de listado | [Grilla](src/MovilidadUrbana.Web/Components/Componentes/Grilla.razor) + [ColumnaDeGrilla](src/MovilidadUrbana.Web/Components/Componentes/ColumnaDeGrilla.cs) | Tabla con `caption` y `scope`, tarjetas apiladas, y los cuatro estados del ciclo de datos |
| §4.4 Formulario de edición | [Campo](src/MovilidadUrbana.Web/Components/Componentes/Campo.razor) | Rótulo visible, requisito antes del intento y error asociado por `aria-describedby` |
| §4.5 Asistente | [Asistente](src/MovilidadUrbana.Web/Components/Componentes/Asistente.razor) + [PasoDeAsistente](src/MovilidadUrbana.Web/Components/Componentes/PasoDeAsistente.razor) | Círculo, conector y rótulo por paso; tres estados de paso; contador y región activa |
| §4.8 Insignia | [Insignia](src/MovilidadUrbana.Web/Components/Componentes/Insignia.razor) | Par texto + tint; el texto siempre se imprime |
| §5 Estados | [EstadoVacio](src/MovilidadUrbana.Web/Components/Componentes/EstadoVacio.razor), [EstadoIndisponible](src/MovilidadUrbana.Web/Components/Componentes/EstadoIndisponible.razor), [Esqueleto](src/MovilidadUrbana.Web/Components/Componentes/Esqueleto.razor), [Banda](src/MovilidadUrbana.Web/Components/Componentes/Banda.razor) | Vacío, filtrado sin resultados, cargando, indisponible y bandas de resultado |
| §5.4 del template Blazor · diálogo | [Dialogo](src/MovilidadUrbana.Web/Components/Componentes/Dialogo.razor) + [DialogoHost](src/MovilidadUrbana.Web/Components/Componentes/DialogoHost.razor) | `<dialog>` nativo, host único en el layout, dos grados de confirmación |
| §6 Iconografía | [Icono](src/MovilidadUrbana.Web/Components/Componentes/Icono.razor) + [Iconos](src/MovilidadUrbana.Web/Theme/Iconos.cs) | SVG inline con `currentColor`, grilla de 24, trazo 1.75 |
| Identidad de versión | [SelloDeVersion](src/MovilidadUrbana.Web/Components/Componentes/SelloDeVersion.razor) | Versión resuelta en el host, no compuesta en la vista |

El estado de cada superficie es un
[`EstadoDeSuperficie`](src/MovilidadUrbana.Web/Components/Componentes/EstadoDeSuperficie.cs) con un
bloque `@if` por estado: es la realización del `data-mq-estado` de la maqueta, con el mismo
vocabulario. `Vacio` y `FiltradoSinResultados` son estados distintos y ofrecen acciones distintas,
que es lo que hizo aparecer la barra de filtros del ABM.

### Lo que no aplica, declarado

- **Shell de acceso, endpoints de identidad y guard en tres capas.** Este laboratorio no tiene
  credenciales: la sesión la reparte un middleware por cookie para aislar los datos entre pruebas.
  No hay superficie sin sesión, así que no hay segundo layout ni POST de ingreso.
- **Host de avisos efímeros.** Los resultados de acción se muestran como banda en la superficie,
  que es la forma que el catálogo admite («toast/inline»). Una región que se autodescarta haría que
  la evidencia E2E dependiera de un temporizador.
- **Conmutador (toggle) y confirmación escrita.** Ninguna pantalla tiene un interruptor, y la baja
  de una localidad no arrastra dependientes —las respuestas de encuesta guardan el nombre, no la
  clave—, así que la confirmación es del primer grado. El segundo grado está implementado en el
  componente y sin uso.
- **Dataset de maqueta, barra de validación y recarga automática.** Son instrumentos de la maqueta
  y no viajan al producto.

### Desviaciones declaradas

1. **El render mode se declara en la raíz y no por página.** El template pide `@rendermode
   InteractiveServer` en cada superficie interactiva. Acá el identificador de sesión se lee en
   `App.razor` —el único lugar con la petición HTTP a mano— y se pasa como parámetro a `Routes`,
   que por eso tiene que ser el componente interactivo raíz. Es el puente descrito en
   [«El estado es del servidor»](#2-el-estado-es-del-servidor-así-que-hay-que-aislarlo); no hay
   superficies de identidad que necesiten quedar en SSR estático.
2. **`EditForm` sin `DataAnnotationsValidator`.** El template decide anotaciones más
   `ValidationMessage` por campo. Acá la política vive en `Dominio/Reglas` y la valida el servicio
   de aplicación —con sus propias pruebas unitarias—, así que anotar el modelo de pantalla sería
   exactamente el anti-patrón «transcribir la política de validación en la vista». Se conserva lo
   que la regla protege: error por campo, asociado al control y anunciado, y requisito derivado de
   la política en [PoliticaDeLocalidades](src/MovilidadUrbana.Web/Aplicacion/Localidades/PoliticaDeLocalidades.cs)
   y [PoliticaDeEncuestas](src/MovilidadUrbana.Web/Aplicacion/Encuestas/PoliticaDeEncuestas.cs).
3. **El paso de revisión del asistente es el estado de éxito, no un cuarto paso.** `TotalDePasos`
   es una regla de dominio con pruebas propias; la ficha clave/valor de
   [ResumenDeEncuesta](src/MovilidadUrbana.Web/Aplicacion/Encuestas/ResumenDeEncuesta.cs) se
   recorre —no se escribe a mano— y se muestra al registrar.
4. **Anchos de contenido en `ch`.** El catálogo no tiene token de ancho de contenido y promover uno
   nuevo no es decisión de este producto, así que las tres medidas que hacían falta se expresan en
   `ch`, derivadas de la escala tipográfica. Ningún color, tipografía ni espaciado se escribe fuera
   de `Tokens.css`.

### Lo que cambió en las pruebas

La suite sigue siendo de 22 casos y verifica lo mismo. Cuatro puntos se adaptaron a la interfaz
nueva, y ninguno afloja una verificación:

| Antes | Ahora | Por qué |
| --- | --- | --- |
| `IrPorMenuAsync` desplegaba el `navbar-toggler` | Hace click en el enlace | Debajo del punto de quiebre la barra lateral pasa a navegación superior con los enlaces a la vista: no hay menú que desplegar |
| `modal-nombre`, `boton-cancelar-baja`, `boton-confirmar-baja` | `dialogo-titulo`, `boton-cancelar-dialogo`, `boton-confirmar-dialogo` | El diálogo es un componente único gobernado por el servicio, y su marcado vive una sola vez |
| `progreso-contenedor` con `aria-valuenow` | Los `[data-paso]` con `aria-current="step"` y sus clases de estado | El catálogo norma el indicador de pasos; una barra de progreso era un segundo canal que decía lo mismo |
| Las acciones de fila se buscaban en la tabla | Se buscan en la presentación visible (`Filter(Visible = true)`) | La grilla lleva tabla y tarjetas apiladas siempre en el marcado, y el mismo caso corre en escritorio y en móvil |

## Guías

La documentación de estudio vive en [Guides/](Guides/) y se lee desde el repositorio o desde el
Explorador de soluciones. Son dos familias en cuatro carpetas: una sobre pruebas de extremo a
extremo, y otra sobre el modelo de ramas —una guía de estudio y dos guías prácticas—.

### [Guides/E2E-Guide/](Guides/E2E-Guide/) — pruebas de extremo a extremo

| Documento | Para quién | Qué deja |
| --- | --- | --- |
| [Beginner-Guide.md](Guides/E2E-Guide/Beginner-Guide.md) | Quien nunca escribió una prueba E2E | Nueve capítulos y seis anexos: qué es una E2E, marco de escenarios y actores, anatomía del proyecto en .NET, qué testear, cómo se escribe y estabiliza un caso, lo propio de una aplicación con servidor, y la integración con GitHub Actions |
| [Quick-Guide-ABM.md](Guides/E2E-Guide/Quick-Guide-ABM.md) | Quien ya escribió pruebas E2E | La receta corta para montar las de un ABM: siete pasos, las trampas de Blazor *interactive server* y una lista de verificación |

### [Guides/Estandares-Modelo-Ramas-Guide/](Guides/Estandares-Modelo-Ramas-Guide/) — ramas, integración y releases

Cómo se organiza el trabajo alrededor del código que estas pruebas verifican: qué rama recibe qué
cambio, cuándo se corta una versión y qué tiene que estar en verde para que un merge ocurra.

La carpeta se llama *Estandares-Modelo-Ramas* y no *GitFlow* a propósito: lo que documenta es la
**elección** entre modelos. GitFlow es uno de los comparados —y tiene su [capítulo
propio](Guides/Estandares-Modelo-Ramas-Guide/04-GitFlow.md)—, pero el modelo adoptado es otro:
tronco con ramas de release.

| # | Documento | De qué trata |
| --- | --- | --- |
| 01 | [Marco de referencia](Guides/Estandares-Modelo-Ramas-Guide/01-Marco-De-Referencia.md) | Escenarios, contextos y actores: el vocabulario que usa todo lo demás |
| 02 | [Mapa conceptual](Guides/Estandares-Modelo-Ramas-Guide/02-Mapa-Conceptual.md) | Entradas por escenario, por rol y por artefacto |
| 03 | [Fundamentos de Git](Guides/Estandares-Modelo-Ramas-Guide/03-Fundamentos-De-Git.md) | Merge, squash, rebase, cherry-pick y tags |
| 04 | [GitFlow](Guides/Estandares-Modelo-Ramas-Guide/04-GitFlow.md) | El modelo original, sus reglas y la nota de 2020 de su autor |
| 05 | [Cómo elegir el modelo](Guides/Estandares-Modelo-Ramas-Guide/05-Como-Elegir-El-Modelo.md) | GitHub Flow, GitFlow, GitLab Flow y tronco: comparación y criterio |
| 06 | [Modelo adoptado](Guides/Estandares-Modelo-Ramas-Guide/06-Modelo-Adoptado.md) | Las siete reglas, guardarraíles y antipatrones |
| 07 | [Integración y versionado](Guides/Estandares-Modelo-Ramas-Guide/07-Integracion-Y-Versionado.md) | Ambientes, promoción, versionado semántico y releases |
| 08 | [Pull requests y pruebas](Guides/Estandares-Modelo-Ramas-Guide/08-Pull-Requests-Y-Pruebas.md) | Ciclo del pull request, protección de rama y qué verifica el pipeline |

Los [anexos](Guides/Estandares-Modelo-Ramas-Guide/Anexos/) suman
[glosario](Guides/Estandares-Modelo-Ramas-Guide/Anexos/Glosario.md),
[plantillas](Guides/Estandares-Modelo-Ramas-Guide/Anexos/Plantillas.md),
[listas de verificación](Guides/Estandares-Modelo-Ramas-Guide/Anexos/Listas-De-Verificacion.md),
[preguntas que forman criterio](Guides/Estandares-Modelo-Ramas-Guide/Anexos/Preguntas-Frecuentes.md),
[fuentes](Guides/Estandares-Modelo-Ramas-Guide/Anexos/Fuentes.md) y tres
[workflows de ejemplo](Guides/Estandares-Modelo-Ramas-Guide/Anexos/workflows/) listos para copiar.

### Las dos guías prácticas

Cada una es un recorrido de ocho escenarios ejecutables sobre un repositorio real, para un equipo
de tres personas que rotan por los roles. Se practican sobre
[`Lab-GitFlow`](https://github.com/hdcm-dev/Lab-GitFlow), con la aplicación de este laboratorio como
sistema bajo prueba.

| Guía | Qué ejercita |
| --- | --- |
| [GitFlow-Practice-Guide/](Guides/GitFlow-Practice-Guide/README.md) | El modelo adoptado, de la [preparación](Guides/GitFlow-Practice-Guide/00-Preparacion.md) al [cierre y auditoría](Guides/GitFlow-Practice-Guide/07-Cierre-Y-Auditoria.md), incluido el [PR que rompe la regresión](Guides/GitFlow-Practice-Guide/04-PR-Que-Rompe-La-Regresion.md), que es donde las E2E de este laboratorio entran en la historia |
| [GitHubFlow-Practice-Guide/](Guides/GitHubFlow-Practice-Guide/README.md) | El modelo que **no** se adoptó, como línea de base: una sola rama de vida larga, [corrección hacia adelante](Guides/GitHubFlow-Practice-Guide/02-Correccion-Hacia-Adelante.md), [feature flag](Guides/GitHubFlow-Practice-Guide/04-Cambio-Grande-Con-Feature-Flag.md) y [reversión](Guides/GitHubFlow-Practice-Guide/05-Reversion.md) en lugar de releases. Sirve para medir qué agrega cada pieza del modelo adoptado |

Las dos familias se leen bien juntas, y el punto de contacto es concreto: la [guía de estudio
E2E](Guides/E2E-Guide/Beginner-Guide.md) explica qué verifica cada prueba y cómo se ata al pipeline;
[pull requests y pruebas](Guides/Estandares-Modelo-Ramas-Guide/08-Pull-Requests-Y-Pruebas.md)
explica cuándo esa verificación bloquea un merge y quién decide.

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
dotnet test tests/MovilidadUrbana.UnitTests                    # reglas de dominio, sin navegador
dotnet test tests/MovilidadUrbana.E2ETests --settings pruebas.runsettings
dotnet test tests/MovilidadUrbana.E2ETests --settings pruebas.runsettings -- Playwright.BrowserName=firefox
```

`scripts/publicar.sh` sigue existiendo para producir la publicación **autocontenida** que usa CI,
pero para correr las pruebas no hay que invocarlo.

Cuando un caso E2E falla queda su **traza de Playwright** en `resultados/trazas/<caso>.zip`, con el
DOM paso a paso, la red y la consola. Se abre con `playwright show-trace <archivo>` o subiéndola a
[trace.playwright.dev](https://trace.playwright.dev). Los casos que pasan no dejan nada, y la
grabación se apaga con `TRAZAR=false`.

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
diálogo es el `<dialog>` nativo, abierto y cerrado por el
[servicio de diálogos](src/MovilidadUrbana.Web/Servicios/ServicioDeDialogos.cs) con la
interoperabilidad mínima de [mq-dialogo.js](src/MovilidadUrbana.Web/wwwroot/js/mq-dialogo.js): el
confinamiento de foco y el cierre por Escape los trae el navegador, no hay animación de apertura
que esperar y esa clase de carrera no existe.

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
`-warnaserror`, ejecuta las **pruebas unitarias** —las reglas de dominio, en milisegundos— y lista
las E2E con `dotnet test --list-tests` —el equivalente del `playwright test --list` del runner de
JavaScript: comprueba que el descubrimiento funcione sin levantar navegadores ni la aplicación—. Al terminar, `comentario-en-pr` deja (o actualiza, no duplica) un comentario con el
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

Los jobs corren en los runners alojados por GitHub, `runs-on: ubuntu-latest`. Encima de cada uno
quedó comentada la línea del runner autoalojado del laboratorio:

```yaml
    # runs-on: [self-hosted, i7infra-dev]
    runs-on: ubuntu-latest
```

Descomentar una y comentar la otra alcanza para volver al runner propio; la etiqueta `i7infra-dev`
es la que lo identifica en el repositorio.

Nada corre dentro de un contenedor de job. El motivo viene del runner autoalojado, y conviene
dejarlo escrito porque no es evidente: ese runner es él mismo un contenedor y no tiene montado el
socket de Docker, así que un job con `container:` ni siquiera llega a arrancar —falla en
*Initialize containers* con `failed to connect to the docker API at unix:///var/run/docker.sock`—.

El cambio de runner trajo dos ajustes. **El SDK se pide explícitamente** con `actions/setup-dotnet`:
el runner autoalojado ya traía .NET 10, pero la imagen de GitHub no garantiza esa versión. **Los
navegadores se cachean** con `actions/cache`: el runner autoalojado es un contenedor de larga vida
y conservaba la caché entre corridas, mientras que los de GitHub arrancan limpios y sin caché
bajarían el navegador en cada job de la matriz.

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

El 2026-08-24, con las pruebas unitarias y la captura de traza ya incorporadas:

```
$ scripts/dotnet.sh dotnet test tests/MovilidadUrbana.UnitTests --configuration Release
  Passed! - Failed: 0, Passed: 49, Skipped: 0, Total: 49, Duration: 27 ms

$ scripts/pruebas.sh chromium
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 9 s
```

La traza se comprobó con un caso que falla a propósito, agregado y quitado para la prueba: dejó
`resultados/trazas/…FallaAProposito.zip` de 138 KB con `trace.trace`, `trace.network` y los
recursos de pantalla. Una corrida en verde no crea la carpeta. La grabación permanente cuesta unos
2 segundos sobre los 22 casos de chromium.

El 2026-09-04, con el template del Framework SDD aplicado a la interfaz:

```
$ dotnet build Lab-E2E.WebBlazor.sln --configuration Release -warnaserror
  Build succeeded.
      0 Warning(s)

$ dotnet test tests/MovilidadUrbana.UnitTests --configuration Release
  Passed! - Failed: 0, Passed: 49, Skipped: 0, Total: 49, Duration: 32 ms

$ scripts/pruebas.sh chromium
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 9 s

$ EMULAR_MOVIL=true scripts/pruebas.sh chromium
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 8 s

$ scripts/pruebas.sh firefox
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 17 s

$ scripts/pruebas.sh webkit
  Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 13 s
```

La corrida móvil es la que ejercita las tarjetas apiladas: debajo de los 768px la tabla se oculta y
las acciones de fila se activan sobre la tarjeta.

Un detalle del entorno, que no es del laboratorio: en esta máquina el límite de instancias de
`inotify` del kernel (`fs.inotify.max_user_instances = 128`) estaba agotado, y la aplicación moría
con código 134 al construir su configuración. Las corridas de arriba llevan
`DOTNET_USE_POLLING_FILE_WATCHER=1`. Si aparece «La aplicación terminó sola con código 134 antes de
escuchar», es eso y no la publicación.

Lo que **no** se verificó desde acá: la ejecución desde el Explorador de pruebas de Visual Studio
—no hay Windows ni Visual Studio en esta máquina— y el comportamiento real de los workflows, del
que solo se validó la sintaxis YAML.
