---
doc_id: GF-09-ESCENARIOS
doc_type: guia-practica
title: Guía práctica de GitFlow — los ocho escenarios en un solo documento
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po, autoridad-de-cambio]
traces: [GF-06, GF-07, GF-08]
---

# Guía práctica de GitFlow

Este documento contiene la práctica completa: los ocho escenarios ejecutables del modelo adoptado,
con sus precondiciones, comandos, verificaciones y errores frecuentes. Se lee y se ejecuta sin
depender de ningún otro documento de esta carpeta.

La práctica se hace sobre el repositorio [`Lab-GitFlow`](https://github.com/hdcm-dev/Lab-GitFlow),
con la aplicación de `Lab-E2E.WebBlazor` como sistema bajo prueba, y está pensada para un equipo de
**tres personas** que rotan por los roles.

---

## Tabla de contenido

- [1. Cómo usar esta guía](#1-cómo-usar-esta-guía)
  - [1.1 Los tres integrantes y su rotación](#11-los-tres-integrantes-y-su-rotación)
  - [1.2 Estructura de cada escenario](#12-estructura-de-cada-escenario)
  - [1.3 Orden de ejecución](#13-orden-de-ejecución)
  - [1.4 Qué hace falta](#14-qué-hace-falta)
  - [1.5 Los escenarios de un vistazo](#15-los-escenarios-de-un-vistazo)
- [2. Escenario 00 — Preparación](#2-escenario-00--preparación)
- [3. Escenario 01 — Funcionalidad nueva (E-01)](#3-escenario-01--funcionalidad-nueva-e-01)
- [4. Escenario 02 — Defecto con release abierta (E-02)](#4-escenario-02--defecto-con-release-abierta-e-02)
- [5. Escenario 03 — Corte de release (E-03 y E-04)](#5-escenario-03--corte-de-release-e-03-y-e-04)
- [6. Escenario 04 — Pull request que rompe la regresión (E-08)](#6-escenario-04--pull-request-que-rompe-la-regresión-e-08)
- [7. Escenario 05 — Emergencia en producción (E-05)](#7-escenario-05--emergencia-en-producción-e-05)
- [8. Escenario 06 — Versión de demostración (E-06)](#8-escenario-06--versión-de-demostración-e-06)
- [9. Escenario 07 — Cierre y auditoría](#9-escenario-07--cierre-y-auditoría)

---

## 1. Cómo usar esta guía

Cada escenario se puede hacer de a uno, pero rinde mucho más con las tres personas en simultáneo:
buena parte de lo que hay que aprender —esperar una revisión, descubrir que alguien mergeó antes,
decidir si una corrección entra a la release— solo aparece cuando hay más de una persona tocando el
repositorio.

### 1.1 Los tres integrantes y su rotación

Para no atar los roles a las personas, la guía los nombra **I1**, **I2** e **I3**, y los rota por
escenario. La correspondencia con los actores de
[01 — Marco de referencia](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#1-marco-de-referencia) es esta:

| Escenario | I1 | I2 | I3 |
|---|---|---|---|
| [01 Funcionalidad nueva](#3-escenario-01--funcionalidad-nueva-e-01) | A-DEV | A-REV | A-QA |
| [02 Defecto con release abierta](#4-escenario-02--defecto-con-release-abierta-e-02) | A-QA | A-DEV | A-REV + A-OPS |
| [03 Corte de release](#5-escenario-03--corte-de-release-e-03-y-e-04) | A-OPS | A-PO | A-QA |
| [04 PR que rompe la regresión](#6-escenario-04--pull-request-que-rompe-la-regresión-e-08) | A-DEV | A-REV | A-QA |
| [05 Emergencia en producción](#7-escenario-05--emergencia-en-producción-e-05) | A-DEV | A-OPS | A-AUT |
| [06 Versión de demostración](#8-escenario-06--versión-de-demostración-e-06) | A-OPS | A-PO | A-DEV |
| [07 Cierre y auditoría](#9-escenario-07--cierre-y-auditoría) | los tres | | |

La rotación es deliberada: quien nunca cortó una release no entiende por qué el tamaño de los pull
requests le importa a otro.

### 1.2 Estructura de cada escenario

Todos los escenarios tienen la misma estructura, y conviene respetarla:

1. **Objetivo** — qué se aprende, en una línea.
2. **Precondición** — en qué estado tiene que estar el repositorio antes de empezar.
3. **Pasos** — los comandos y las acciones en GitHub, en orden.
4. **Qué observar** — lo que hay que mirar mientras corre; es la parte formativa.
5. **Errores frecuentes** — lo que suele salir mal y qué significa.
6. **Verificación** — cómo se comprueba que el escenario quedó bien resuelto.

### 1.3 Orden de ejecución

**El orden de lectura no es el orden de ejecución.** La numeración agrupa por tema; las
precondiciones mandan. El único orden ejecutable es este, y es el que hay que seguir:

**00 → 01 → 03 → 02 → 04 → 05 → 06 → 07**

El 03 va antes que el 02 porque el 02 exige `release/1.0` con su candidata, y el único escenario que
la crea es el 03. El 05 exige además la versión `v1.0.0` liberada, que produce el paso 6 del 03.
Cada escenario deja el repositorio en el estado que el siguiente de **esta** secuencia necesita.

Una advertencia sobre el tiempo: los escenarios 01 a 05 llevan una jornada de trabajo si se hacen
completos y con las esperas reales de revisión. Comprimirlos en dos horas es posible, pero se pierde
justamente lo que se quería practicar.

### 1.4 Qué hace falta

- Acceso de escritura al repositorio de práctica y permiso para configurar protección de rama.
- El runner autoalojado `i7infra-dev` disponible, o un runner alojado sustituyendo el `runs-on:` de
  los workflows.
- Docker en la máquina de cada integrante, para correr las pruebas sin instalar .NET ni Node.

### 1.5 Los escenarios de un vistazo

| # | Escenario | Ejercita |
|---|---|---|
| [00](#2-escenario-00--preparación) | Preparación | Repositorio, protección de rama, pipeline, `CODEOWNERS` |
| [01](#3-escenario-01--funcionalidad-nueva-e-01) | Funcionalidad nueva | E-01: rama corta, pull request, revisión, squash merge |
| [02](#4-escenario-02--defecto-con-release-abierta-e-02) | Defecto con release abierta | E-02: prueba que falla primero, cherry-pick, nueva candidata |
| [03](#5-escenario-03--corte-de-release-e-03-y-e-04) | Corte de release y liberación | E-03 y E-04: corte retroactivo, candidata, criterios de admisión, autorización, tag de versión final y promoción |
| [04](#6-escenario-04--pull-request-que-rompe-la-regresión-e-08) | Pull request que rompe la regresión | E-08: el control que motivó esta guía |
| [05](#7-escenario-05--emergencia-en-producción-e-05) | Emergencia en producción | E-05: hotfix desde el tag y retorno obligatorio |
| [06](#8-escenario-06--versión-de-demostración-e-06) | Versión de demostración | E-06: artefacto identificable y desechable |
| [07](#9-escenario-07--cierre-y-auditoría) | Cierre y auditoría | Convergencia, higiene de ramas, retrospectiva |

---

## 2. Escenario 00 — Preparación

### Objetivo

Dejar el repositorio de práctica con una aplicación real, pruebas automatizadas que corren, y los
controles que el resto de los escenarios va a ejercitar. Sin esto, los escenarios siguientes son
teatro: no hay nada que se pueda romper ni ninguna verificación que lo detecte.

### Precondición

- `Lab-GitFlow` existe y tiene solo el commit inicial.
- Los tres repositorios están clonados **como hermanos** bajo un mismo directorio de trabajo, que
  todos los bloques de comandos de esta guía dan por posición actual salvo aviso:

  ```
  <directorio-de-trabajo>/
    Lab-GitFlow/                    ← el repositorio de práctica
    Lab-GitFlow.Documentacion/      ← este cuerpo documental
    Lab-E2E.WebBlazor/              ← la aplicación bajo prueba
  ```

- Docker instalado.
- `Lab-GitFlow` es **privado y sin colaboradores externos**. No es un detalle administrativo: el
  pipeline corre sobre un runner autoalojado persistente y, en el evento `pull_request`, ejecuta el
  workflow *de la rama del pull request* antes de cualquier revisión. Ver
  [el anexo de workflows](../Estandares-Modelo-Ramas-Guide/Anexos/workflows/README.md).
- Los equipos de GitHub `@equipo/devops` y `@equipo/datos` existen en la organización dueña del
  repositorio, con los tres integrantes repartidos. Sin ellos, el `CODEOWNERS` del paso 5 queda sin
  efecto.

### Pasos

#### 1. Sembrar la aplicación

La aplicación no se escribe para esta práctica: se toma la de `Lab-E2E.WebBlazor`, que ya tiene la
solución .NET, las pruebas de extremo a extremo con Playwright y los scripts de contenedor.

```bash
cd <directorio-de-trabajo>/Lab-GitFlow
git checkout -b chore/1-sembrar-aplicacion

# Copiar por lista positiva lo que la práctica necesita. Enumerar exclusiones a mano no sirve:
# la aplicación trae `.dotnet/` y `.navegadores/` (SDK y navegadores descargados, ~1,9 GB) que
# ninguna lista escrita de memoria contempla.
#
# Las dos exclusiones sí son necesarias: `src/` y `tests/` arrastran sus `bin/` y `obj/` de la
# última compilación —medido acá, 472 MB contra 524 KB sin ellos—. Git los ignora, así que el
# commit sale igual, pero la copia de trabajo queda con casi medio giga de salida de compilación
# ajena que después confunde al primer `dotnet build`.
rsync -a --relative --exclude='bin/' --exclude='obj/' \
      ../Lab-E2E.WebBlazor/./{Lab-E2E.WebBlazor.sln,pruebas.runsettings,.gitignore,README.md} \
      ../Lab-E2E.WebBlazor/./{src,tests,scripts,.github} \
      ./

# Comprobación de que se sembró lo que debía y nada más:
du -sh .          # esperado: menos de 1 MB; si da cientos de MB, se colaron bin/ y obj/
test -x scripts/pruebas.sh && test -f Lab-E2E.WebBlazor.sln && echo "siembra ok"

git add -A
git commit -m "chore: sembrar la aplicación de práctica y sus pruebas E2E"
git push -u origin chore/1-sembrar-aplicacion
```

Se integra por pull request, no por push directo: es la primera oportunidad de ver el circuito
completo antes de que haya protección que lo obligue. **Abrir el pull request, aprobarlo y mergearlo
con squash ahora**, antes de seguir: el paso 3 corta su rama desde `main` y necesita que este
trabajo ya esté ahí.

#### 2. Comprobar que las pruebas corren localmente

La suite de extremo a extremo de esta aplicación es el proyecto **.NET**
`tests/MovilidadUrbana.E2ETests`, que usa el binding de Playwright para .NET y se configura con
`pruebas.runsettings`. No hay `package.json`, ni carpeta `e2e/`, ni `playwright.config.js`, ni un
`scripts/e2e.sh`: los scripts que provee la aplicación son `dotnet.sh`, `pruebas.sh` y
`publicar.sh`.

```bash
# Comprobar primero que la interfaz de pruebas es la que esta guía supone:
ls scripts/                  # esperado: dotnet.sh  pruebas.sh  publicar.sh

scripts/pruebas.sh chromium  # publica la aplicación y corre tests/MovilidadUrbana.E2ETests
```

**No hay que anteponer `scripts/publicar.sh`.** El fixture `ServidorDeLaAplicacion` publica por su
cuenta antes de la primera prueba, sin identificador de plataforma y dependiente del framework. Si
`publicacion/` ya trae el binario **autocontenido** que deja `publicar.sh`, esa segunda publicación
se superpone: reescribe `runtimeconfig.json` dejando las bibliotecas del runtime autocontenido en la
carpeta, y el anfitrión de .NET resuelve la propia carpeta de la aplicación como ubicación del
runtime, no encuentra ningún framework ahí y el proceso muere antes de escuchar. El síntoma son las
22 pruebas fallando en `OneTimeSetUp` con *«La aplicación terminó sola con código 150 antes de
escuchar»*. **[E: corrida local del 2026-08-24 sobre el repositorio sembrado]**

Para ejercitar el artefacto autocontenido —el mismo que se despliega— hay que desactivar el paso del
fixture, y entonces sí las dos órdenes conviven:

```bash
scripts/publicar.sh
PUBLICAR_ANTES_DE_PROBAR=false scripts/pruebas.sh chromium
```

Las tres combinaciones se probaron sobre el repositorio de práctica sembrado: sola, 22 en verde; con
la variable, 22 en verde; publicando antes sin la variable, 22 en rojo. **[E]**

El navegador se elige por argumento (`scripts/pruebas.sh firefox`) o por variable
(`NAVEGADOR=webkit scripts/pruebas.sh`); no hay `--project`. Los resultados quedan en
`resultados/` como TRX.

Si esto no pasa en verde en la máquina de cada integrante, no tiene sentido seguir: los escenarios
siguientes distinguen «la prueba falla porque el cambio está mal» de «la prueba falla porque el
entorno está mal», y esa distinción requiere una línea base verde.

#### 3. Instalar los workflows del procedimiento

Los del laboratorio de E2E cubren el pull request y la línea principal. Faltan los que el
procedimiento de release necesita: verificación de las ramas `release/*`, corte de versión y
auditoría de convergencia. Están en
[../Anexos/workflows/](../Estandares-Modelo-Ramas-Guide/Anexos/workflows/README.md). El `ci.yml` de esa carpeta
**reemplaza** al que vino con la aplicación: aquel se dispara sobre `main` y sobre `develop` —una rama que este modelo no usa— y ninguno de sus disparadores alcanza a `release/*`.

```bash
# La segunda rama nace de `main` con el paso 1 ya mergeado, no de la primera rama.
git checkout main
git pull --ff-only

git checkout -b chore/2-workflows-de-gitflow
cp ../Lab-GitFlow.Documentacion/Analisis/Estandares-Modelo-Ramas-Guide/Anexos/workflows/*.yml \
   .github/workflows/
git add .github/workflows
git commit -m "chore: agregar los workflows de release y auditoría de convergencia"
git push -u origin chore/2-workflows-de-gitflow
```

Este pull request también se abre, se aprueba y se mergea con squash antes de seguir. Si se saltea,
la rama del paso 5 va a nacer de esta y su pull request va a mostrar archivos ajenos, que es
exactamente el «error frecuente» del
[escenario 01](#3-escenario-01--funcionalidad-nueva-e-01).

#### 4. Configurar la protección de rama

En *Settings → Branches* del repositorio, sobre `main` y sobre el patrón `release/*`:

| Control | Valor |
|---|---|
| Require a pull request before merging | sí, con 1 aprobación |
| Require review from Code Owners | **sí** — sin esto el `CODEOWNERS` del paso 5 solo sugiere revisor, no controla nada |
| Require status checks to pass | sí, check obligatorio: `CI aprobada` |
| Require branches to be up to date | sí |
| Do not allow bypassing | sí, incluidos administradores |
| Automatically delete head branches | sí (*Settings → General*) |

Y una regla adicional (*ruleset*) que exige **2 aprobaciones** sobre los patrones
`.github/workflows/**` y `src/**/Persistencia/**`, que es como se instrumenta la regla de
[08](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#8-pull-requests-y-pruebas-automatizadas): la categoría «infraestructura, seguridad o migraciones» se
decide por ruta tocada, no por juicio.

Sobre el espacio de nombres de tags, en *Settings → Tags*: regla sobre el patrón `v*` que restringe
la creación a quien cumple A-OPS. Sin ella, cualquiera con permiso de escritura publica una versión
desde una rama personal, porque el disparador de `release.yml` es el tag, no el merge.

Se exige **un solo check** —el job resumen— y no la lista completa de jobs: así la regla no hay que
tocarla cada vez que cambia la matriz de navegadores.

#### Permisos y vía de excepción sobre la protección **[C]**

Desactivar el bypass sin decir quién puede levantar la protección y cómo, garantiza que en la
primera emergencia alguien la levante sin dejar rastro. Queda escrito:

| Actor | Permiso sobre `Lab-GitFlow` |
|---|---|
| A-DEV, A-REV, A-QA | Write |
| A-OPS | Admin |
| A-AUT | Write, más la autorización registrada fuera del repositorio |

La única vía de excepción es que **A-OPS** desactive temporalmente la regla, y solo si el pipeline no
puede correr por una causa de infraestructura —el runner apagado o sin la etiqueta `i7infra-dev`—
durante una emergencia en curso. Se registra en el incidente antes de hacerlo: quién, qué regla, por
qué y hasta cuándo. La regla se vuelve a activar el mismo día, y el pull request afectado se
reverifica cuando el runner vuelve. Esto entra en la lista de emergencia y en la revisión posterior
a la implementación.

#### 5. Declarar dueños de los archivos sensibles

Los equipos `@equipo/devops` y `@equipo/datos` tienen que existir **antes** en la organización dueña
del repositorio (*Organization → Teams*), con acceso al repositorio: `CODEOWNERS` que nombra un
equipo inexistente es silenciosamente inerte.

```bash
git checkout main
git pull --ff-only
git checkout -b chore/3-codeowners
mkdir -p .github
cat > .github/CODEOWNERS <<'EOF'
.github/workflows/   @equipo/devops
src/**/Persistencia/ @equipo/datos
EOF
git add .github/CODEOWNERS
git commit -m "chore: declarar dueños de los archivos sensibles"
git push -u origin chore/3-codeowners
```

Este pull request también se aprueba y se mergea con squash: recién ahí la verificación del escenario
—«`git ls-remote --heads origin` muestra solo `main`»— puede cumplirse.

Son los dos lugares donde un error no se arregla con un revert: el pipeline y las migraciones de
datos.

### Qué observar

- El primer pull request corre el pipeline **antes** de que exista la protección: ver la diferencia
  entre «el pipeline informa» y «el pipeline bloquea» es el punto del
  [escenario 04](#6-escenario-04--pull-request-que-rompe-la-regresión-e-08).
- Cuánto tarda la verificación rápida frente a la matriz completa. Esa diferencia es la que justifica
  separarlas.
- Que el reporte de las pruebas quede como artefacto de la corrida, y por cuántos días.

### Errores frecuentes

| Síntoma | Causa habitual |
|---|---|
| Los jobs quedan en cola para siempre | El runner autoalojado no tiene la etiqueta `i7infra-dev`, o está apagado |
| Las pruebas pasan localmente y fallan en el runner | La aplicación no se publicó antes de correr; el artefacto no llegó al job |
| El check obligatorio nunca aparece en la lista | El nombre configurado no coincide **exactamente** con el `name:` del job |

### Verificación

El escenario está resuelto cuando se cumplen las siete condiciones:

1. `git ls-remote --heads origin` muestra solo `main` —los tres pull requests se mergearon—.
2. Un push directo a `main` es rechazado por el servidor, y también uno a una rama `release/*`.
3. Un pull request de prueba dispara la verificación rápida y la regresión, y el botón de merge queda
   bloqueado hasta que terminan.
4. La corrida deja el reporte de pruebas como artefacto descargable.
5. `scripts/pruebas.sh chromium` pasa en verde en la máquina de cada
   integrante.
6. El contrato de los workflows se verificó contra el archivo real:
   `grep -c 'cantidad-shards' .github/workflows/*.yml` no devuelve ninguna coincidencia.
7. Un pull request que toca `.github/workflows/` pide dos aprobaciones y la revisión del equipo
   propietario.

---

## 3. Escenario 01 — Funcionalidad nueva (E-01)

### Objetivo

Recorrer el circuito completo de un cambio: issue con criterio de aceptación, rama corta, pull
request en borrador, revisión, squash merge, y cierre del issue por quien verifica —no por quien
programa—.

**Roles:** I1 es A-DEV, I2 es A-REV, I3 es A-QA y escribe el criterio junto a A-PO.

### Precondición

[Escenario 00](#2-escenario-00--preparación) terminado. `main` protegida y sin release abierta:
contexto **C-1**.

### Pasos

#### 1. El issue, antes que el código (I3 + A-PO)

Se crea el issue **#107 — Filtrar el listado de localidades por provincia** con criterio de
aceptación explícito:

> Dado un valor seleccionado en el filtro de provincia, el listado muestra únicamente las localidades
> de esa provincia. Con el filtro vacío, muestra todas. Si no hay coincidencias, se muestra el mensaje
> de listado vacío.

Un issue sin este bloque no pasa a *Listo para tomar*. La razón es práctica: ese texto es lo que I3
va a ejecutar después, y lo que I1 va a convertir en pruebas.

#### 2. Rama corta desde la línea principal (I1)

```bash
git checkout main
git pull --ff-only
git checkout -b feature/107-filtro-por-provincia
```

El `--ff-only` es deliberado: si la copia local divergió del remoto, conviene que falle de manera
ruidosa en lugar de generar un merge silencioso.

#### 3. Pull request en borrador con el primer commit

```bash
git commit -m "feat: agregar el filtro de provincia al listado de localidades

Refs #107"
git push -u origin feature/107-filtro-por-provincia
```

Se abre el pull request **en borrador**, con la
[plantilla](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#anexo-b--plantillas) completa. El pipeline arranca
ahí, no al final.

#### 4. Las pruebas, con el cambio

La funcionalidad se acompaña de pruebas de extremo a extremo sobre los tres casos del criterio: filtro
con resultados, filtro sin resultados, filtro vacío. Los selectores van por `data-testid`, siguiendo
la convención de la aplicación bajo prueba.

#### 5. Revisión (I2)

I2 revisa dentro del día hábil. **[F: GOOG-2]** Si el pull request es demasiado grande para saber
cuándo habrá tiempo de revisarlo, la respuesta correcta es pedir que se parta, no dejarlo esperando.

#### 6. Squash merge

Con el pipeline en verde y la aprobación registrada, se mergea con **squash**. La rama se borra
automáticamente. En `main` queda **un solo commit** con el número de issue en el cuerpo.

#### 7. Cierre (I3)

I3 verifica el criterio de aceptación sobre el ambiente de integración y recién entonces cierra el
issue. Si el pull request decía `Closes #107`, el cierre es automático al mergear, y en ese caso I3
lo reabre si la verificación no pasa. Mergeado no es verificado.

### Qué observar

- **El commit único en `main`.** `git log --oneline -3` después del merge: un commit por issue.
  Anotar ese SHA, pero por el motivo contrario al que suena: **no** va a viajar a la release. Es una
  funcionalidad, y una funcionalidad no se cherry-pickea a una release abierta salvo que estuviera
  en su alcance —[06](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#6-modelo-adoptado)—. Es el commit que el
  [escenario 02](#4-escenario-02--defecto-con-release-abierta-e-02) va a mostrar en
  `git log --oneline main ^release/1.0` como ejemplo concreto de lo que **no** se arrastró. El que
  viaja es el commit del fix #142, y lo produce el propio escenario 02.
- **Cuándo empieza a correr el pipeline.** Con el pull request en borrador, no al marcarlo listo.
- **El tamaño del diff.** Anotar cuántas líneas tuvo y cuánto tardó la revisión; el
  [escenario 04](#6-escenario-04--pull-request-que-rompe-la-regresión-e-08) usa esa comparación.
- **Qué pasa si I2 aprueba y mientras tanto alguien mergeó otra cosa.** Con «require branches to be
  up to date» activo, hay que actualizar la rama y el pipeline vuelve a correr.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El pull request muestra archivos que I1 no tocó | La rama no nació de `main` actualizado | Rehacer la rama desde `main` y volver a aplicar el cambio |
| El issue queda abierto tras el merge | Falta la palabra clave `Closes #107` en la descripción | Cerrarlo a mano; corregir la plantilla la próxima vez |
| Quedan tres commits en `main` | Se mergeó sin squash | Es tarde para revertirlo sin reescribir; anotarlo y ver el impacto en el escenario 02 |

### Verificación

1. `main` tiene exactamente un commit nuevo, con `#107` en el mensaje.
2. La rama remota `feature/107-filtro-por-provincia` ya no existe.
3. Las pruebas nuevas corren en la matriz completa disparada por el push a `main`.
4. El issue está cerrado por I3, con una nota de qué se verificó y dónde.

---

## 4. Escenario 02 — Defecto con release abierta (E-02)

### Objetivo

Practicar la regla que sostiene todo el modelo: el defecto se reproduce y se corrige en la línea
principal, con una prueba, y recién después viaja a la release por cherry-pick. Al terminar, la
corrección está en los dos lugares y hay evidencia de que así fue.

**Roles:** I1 es A-QA y reporta, I2 es A-DEV y corrige, I3 revisa y hace de A-OPS.

### Precondición

[Escenario 03](#5-escenario-03--corte-de-release-e-03-y-e-04) hecho —es el que produce este estado—:
existe `release/1.0` con la candidata `v1.0.0-rc1` promocionada. Contexto **C-2**. El orden de
ejecución es 00 → 01 → 03 → 02 → 04 → 05 → 06 → 07; ver
[1.3 Orden de ejecución](#13-orden-de-ejecución).

### Pasos

#### 1. Reportar con evidencia (I1)

El issue **#142** describe el defecto con pasos exactos, el resultado esperado, el observado y **la
versión donde se vio** —`v1.0.0-rc1`, no «en homologación»—.

Paso cero, antes de tocar código: si el defecto no se puede reproducir, el issue vuelve al reportante
pidiendo el dato que falta. No se depura a ciegas.

#### 2. Ramar desde la línea principal, no desde la release (I2)

```bash
git checkout main
git pull --ff-only
git checkout -b fix/142-filtro-ignora-mayusculas
```

Acá aparece la objeción más común, y conviene enunciarla en voz alta antes de seguir: *«si ramo de
`main`, ¿no arrastro a la release las funcionalidades que entraron después del corte?»*. No, porque
la release no recibe un merge de `main`: recibe **un commit** por cherry-pick. La objeción es válida
contra el merge de rama a rama; no contra el cherry-pick.

#### 3. Primero la prueba que falla

```bash
# El fixture publica la aplicación antes de la primera prueba; anteponer `publicar.sh`
# rompe la corrida (ver escenario 00, paso 2).
scripts/pruebas.sh chromium  # tests/MovilidadUrbana.E2ETests, navegador por argumento
```

El resultado queda en `resultados/*.trx`; ahí tiene que figurar la prueba nueva como fallida.

La prueba nueva **tiene que fallar**. Si pasa a la primera, no se entendió el defecto: se está
probando otra cosa. Este paso no es un agregado de esta guía; es parte de la práctica recomendada.
**[F: TBD-1]**

#### 4. Corregir, y solo eso

Nada de refactores oportunistas en el mismo pull request. Un cambio de corrección que toca quince
archivos es imposible de revisar y, sobre todo, imposible de revertir sin perder el arreglo.

```bash
git commit -m "fix: comparar la provincia sin distinguir mayúsculas

El filtro comparaba la cadena tal cual venía del formulario, de modo
que una provincia seleccionada con otra capitalización no coincidía.

Refs #142"
```

El cuerpo explica **por qué**; el *qué* ya está en el diff.

#### 5. Pull request, revisión, squash merge

Queda un único commit en `main`. Anotar su SHA: es el que viaja.

#### 6. Decidir la admisión (I3 + I1)

Con la release abierta, la corrección **no** entra por defecto. Se contrasta con los criterios de
admisión escritos en el [escenario 03](#5-escenario-03--corte-de-release-e-03-y-e-04). Si entra,
sigue el paso 7; si no, queda para la próxima versión y se registra la decisión en el issue.

#### 7. Cherry-pick a la release

`release/1.0` está protegida igual que `main`: no admite push directo, tampoco para un cherry-pick.
La vía de escritura es una rama corta cortada **desde la propia release**, y un pull request contra
ella.

```bash
git checkout release/1.0
git pull --ff-only
git checkout -b cherry/142-filtro-mayusculas
git cherry-pick -x <sha-del-fix>
git push -u origin cherry/142-filtro-mayusculas
# Pull request contra release/1.0, pipeline en verde, aprobación, squash merge
```

Si el `git push` directo a `release/1.0` no es rechazado, la protección del
[escenario 00](#2-escenario-00--preparación) está mal configurada: es el mismo control que la guía
existe para instalar.

El `-x` deja el SHA original en el mensaje del commit nuevo. Sirve para que una persona rastree el
origen leyendo la historia; **no** es lo que verifica la auditoría de convergencia del
[escenario 07](#9-escenario-07--cierre-y-auditoría), que compara por contenido con `git cherry`.

#### 8. Nueva candidata y revalidación

```bash
git tag -a v1.0.0-rc2 -m "Candidata 2: incluye la corrección de #142"
git push origin v1.0.0-rc2
```

I1 revalida el caso sobre `rc2` y cierra el issue.

### Qué observar

- **El SHA cambia.** `git log -1 release/1.0` muestra un commit con contenido idéntico y hash
  distinto, con la línea `(cherry picked from commit …)`. Comparar ramas por SHA no sirve; por eso
  existe la auditoría.
- **El pipeline de la rama de release corre otra vez.** Un cherry-pick que aplica limpio no garantiza
  que el resultado funcione en el contexto de la release. **[F: TBD-1]**
- **Lo que NO viajó.** `git log --oneline main ^release/1.0` lista los commits que quedaron fuera:
  ahí se ve, concretamente, que las funcionalidades posteriores al corte no se arrastraron.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El cherry-pick no aplica limpio | El tronco divergió mucho de la release | Resolver el conflicto puntualmente, y tomar nota: la ventana de estabilización es demasiado larga |
| El cherry-pick arrastra más de lo esperado | El merge no fue con squash y el issue quedó en varios commits | Cherry-pickear el rango completo, y volver a la disciplina de squash |
| La corrección quedó solo en la release | Se corrigió directamente ahí «porque era más rápido» | Llevarla a `main` hoy mismo; si no, el defecto reaparece en la próxima versión **[F: GL-1]** |

### Verificación

1. La prueba que reproducía el defecto existe en `main` y falla si se revierte la corrección.
2. `git log --oneline release/1.0` muestra el commit con la referencia al SHA de `main`.
3. `v1.0.0-rc2` disparó una corrida completa sobre la rama de release, en verde.
4. El issue lo cerró I1 tras revalidar, no I2 al mergear.

---

## 5. Escenario 03 — Corte de release (E-03 y E-04)

### Objetivo

Cortar una rama de release desde el commit que corresponde —no necesariamente el último—, numerar la
candidata, y fijar por escrito qué se admite en ella. Es el escenario que habilita al
[02](#4-escenario-02--defecto-con-release-abierta-e-02) y al
[05](#7-escenario-05--emergencia-en-producción-e-05).

**Roles:** I1 es A-OPS, I2 es A-PO y define el alcance, I3 es A-QA y prepara el plan de pruebas.

### Precondición

[Escenario 01](#3-escenario-01--funcionalidad-nueva-e-01) terminado, con al menos tres commits en
`main`. Al menos uno de ellos debe ser trabajo que **no** se quiere liberar todavía: es lo que
vuelve interesante el corte.

### Pasos

#### 1. Decidir el alcance (I2) y el punto de corte (I1)

```bash
git checkout main
git pull --ff-only
git log --oneline -8
```

I2 marca hasta qué commit va la versión 1.0. Si el trabajo no deseado quedó **después** de ese
commit, el corte es directo. Si quedó en el medio, hay dos opciones: cortar antes y cherry-pickear
lo que sí va, o cortar en la punta y revertir lo que no va. Para la práctica conviene el primero.

#### 2. Cortar, incluso hacia atrás

```bash
# Corte retroactivo: desde un SHA elegido, no desde la punta.
git checkout -b release/1.0 <sha-elegido>
git push -u origin release/1.0
```

> **[F: TBD-1]** La rama de release se crea *just in time* y se puede cortar retroactivamente desde
> un commit anterior conocido como bueno. No hace falta que nadie congele nada.

#### 3. Numerar la candidata

```bash
git tag -a v1.0.0-rc1 -m "Candidata 1 de la versión 1.0.0"
git push origin v1.0.0-rc1
```

El tag dispara el build **único** del artefacto. Ese artefacto es el que se promociona a homologación
y, si se aprueba, el mismo que va a producción.

#### 4. Escribir los criterios de admisión (I1 + I2)

Se registra en la descripción de la rama o en el issue de release, con la
[plantilla de registro de release](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#anexo-b--plantillas). Los dos tramos se anclan a fechas, no a
duraciones relativas: I1 e I2 fijan acá y ahora la **fecha de congelamiento** y la **fecha de pase**.
Del corte al congelamiento (exclusive) se admite cualquier defecto reportado por QA; del
congelamiento al pase, solo bloqueantes. **[C]** Ver
[07](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#7-integración-y-versionado).

#### 5. Plan de pruebas (I3)

I3 arma qué se va a verificar sobre la candidata: los criterios de aceptación de lo que entró, más el
recorrido exploratorio. La regresión automatizada ya corre sola sobre la rama; lo manual es lo que
I3 planifica.

#### 6. Estabilizar y liberar la candidata (E-04)

El escenario no termina en la candidata: E-04 termina cuando **la versión se libera con su tag, o se
descarta**. Este tramo es el que produce el estado que los escenarios
[05](#7-escenario-05--emergencia-en-producción-e-05) y
[07](#9-escenario-07--cierre-y-auditoría) dan por hecho, y es el más delicado del modelo, así que se
practica igual que el resto.

1. **Decisión de A-QA (I3).** I3 ejecuta el plan sobre la candidata promocionada a homologación y
   emite un veredicto escrito sobre `v1.0.0-rc1`: apta, o con defectos que van al
   [escenario 02](#4-escenario-02--defecto-con-release-abierta-e-02). Si hay defectos, se vuelve acá
   con `rc2` antes de seguir.
2. **Autorización de A-AUT (I2, con el criterio de riesgo).** Queda registrada en el issue de
   release: quién autoriza, sobre qué candidata y con qué criterio.
3. **Tag de versión final sobre el mismo commit de la candidata aprobada (I1).**

   ```bash
   git fetch --tags
   # El tag final va sobre el MISMO commit que la candidata: si apunta a otro, lo liberado
   # no es lo que aprobó A-QA.
   git tag -a v1.0.0 -m "Versión 1.0.0" "$(git rev-list -n1 v1.0.0-rc1)"
   git push origin v1.0.0
   test "$(git rev-list -n1 v1.0.0)" = "$(git rev-list -n1 v1.0.0-rc1)" && echo "mismo commit ok"
   ```

4. **Promoción del artefacto (I1).** Se despliega a producción **el binario de `v1.0.0-rc1`**, no una
   recompilación: se compara el `sha256sum` del binario desplegado contra el digest registrado para
   esa candidata. Ver [07](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#7-integración-y-versionado).

Al terminar, `v1.0.0` existe, apunta al commit de la candidata aprobada, y hay una versión liberada
que el escenario 05 puede parchear.

### Qué observar

- **El pipeline de `release/1.0`.** Debe correr la matriz completa igual que `main`. Si no corre, la
  rama de release está menos protegida que el tronco, que es exactamente al revés de lo que se busca.
- **Que `main` no se detuvo.** Mientras la release se estabiliza, el resto sigue integrando. Es la
  propiedad que justifica el modelo.
- **La precedencia del tag.** `v1.0.0-rc1` es anterior a `v1.0.0` para cualquier herramienta que
  compare versiones semánticas.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La release incluye trabajo no deseado | Se cortó en la punta por costumbre | Borrar la rama —todavía no hay nada encima— y volver a cortar desde el SHA correcto |
| Nadie sabe qué entra a la release | No se escribieron los criterios de admisión | Escribirlos antes del primer pedido de cherry-pick, no después |
| Hay tres ramas `release/*` vivas | No se borraron las que cayeron en desuso | Borrar; con más de dos, el riesgo de cherry-pickear a la equivocada es real **[F: TBD-1]** |

### Verificación

1. `release/1.0` existe en el remoto y su punta es el SHA elegido, no la punta de `main`.
2. El tag `v1.0.0-rc1` existe y disparó una corrida que produjo un artefacto.
3. La protección de rama aplica también sobre `release/*`: un `git push` directo a `release/1.0` es
   rechazado.
4. Los criterios de admisión están escritos, con fecha de congelamiento y de pase, y son accesibles
   para los tres integrantes.
5. El tag `v1.0.0` existe y apunta **al mismo commit** que `v1.0.0-rc1`:
   `test "$(git rev-list -n1 v1.0.0)" = "$(git rev-list -n1 v1.0.0-rc1)"`.
6. Está registrada la autorización de A-AUT y el digest del artefacto promocionado a producción.

---

## 6. Escenario 04 — Pull request que rompe la regresión (E-08)

### Objetivo

Provocar deliberadamente el problema que originó esta guía —un cambio que rompe funcionalidad que
andaba— y comprobar que el control lo detiene **antes** del merge. Es el escenario más importante de
la práctica, y el único que se hace rompiendo algo a propósito.

**Roles:** I1 es A-DEV y rompe, I2 es A-REV, I3 es A-QA y observa el reporte.

### Precondición

[Escenario 01](#3-escenario-01--funcionalidad-nueva-e-01) terminado. La aplicación sembrada ya trae
las pruebas de extremo a extremo que cubren el listado de localidades (`LocalidadesTests`), el
asistente de encuesta (`EncuestaTests`) y la navegación (`NavegacionTests`); son las que este
escenario va a poner a trabajar.

### Pasos

#### 1. Un cambio plausible que rompe otra cosa (I1)

La clave es que el cambio **parezca razonable** y que la prueba que rompe sea de **otra pantalla**.
El ejemplo se elige a partir del comportamiento real de la aplicación sembrada, no de una regla
inventada: el listado del ABM de localidades se ordena por antigüedad
(`RepositorioDeLocalidades.ListarAsync` usa `.OrderBy(l => l.Id)`), y el desplegable de localidades
de la **encuesta** se alimenta de ese mismo listado.

El cambio: *mostrar primero las altas más recientes en el ABM*, es decir `.OrderByDescending(l =>
l.Id)`. Es una mejora de usabilidad defendible y, sobre todo, **no rompe ninguna prueba del ABM**:
`LocalidadesTests` localiza sus filas por texto (`Filter(HasText = "Goya")`), no por posición.

Lo que rompe está en la otra pantalla: `EncuestaTests.ElDesplegableDeLocalidadesSeAlimentaDelAbm`
afirma que la primera opción real del desplegable es `"Corrientes (Corrientes)"` usando
`opciones.Nth(1)`. Invertido el orden, la primera pasa a ser `"Resistencia (Chaco)"` y esa prueba
—y solo esa— falla.

```bash
git checkout main
git pull --ff-only
git checkout -b feature/151-listado-mas-recientes-primero
# src/MovilidadUrbana.Web/Infraestructura/Persistencia/RepositorioDeLocalidades.cs
#   .OrderBy(l => l.Id)  →  .OrderByDescending(l => l.Id)
# ... más la prueba propia del ABM, en verde ...
git push -u origin feature/151-listado-mas-recientes-primero
```

Antes de dictar la práctica conviene confirmar el rojo esperado corriendo la suite con el cambio
aplicado: la única prueba fallida tiene que ser `ElDesplegableDeLocalidadesSeAlimentaDelAbm`.

#### 2. Abrir el pull request y esperar el pipeline

Sin tocar nada más. Lo que sigue es lo que hay que mirar.

#### 3. Leer el reporte antes que el código (I3)

La evidencia de la corrida es el **TRX** de cada configuración, que el workflow sube como artefacto
`resultados-<configuracion>`, más la tabla de contadores que `e2e.yml` escribe en el resumen de la
corrida. El TRX trae, por cada caso fallido, el mensaje de la aserción y su pila: para esta rotura
dice qué texto esperaba y cuál encontró, que es exactamente lo que hace falta para decidir sin
abrir el código.

Conviene saber qué **no** hay, porque el reflejo de buscarlo cuesta tiempo: con el binding de .NET
no existen el reporte HTML ni la traza navegable que genera el runner de JavaScript. El proyecto
sembrado no instrumenta `Context.Tracing`, así que no se producen trazas ni capturas. Si el equipo
las quiere, hay que agregarlas explícitamente en la clase base de las pruebas y subirlas como un
artefacto más; es una mejora razonable, pero es trabajo, no una casilla de configuración.

#### 4. Decidir qué está mal: el cambio o la prueba (los tres)

Es la discusión formativa del escenario, y no tiene respuesta única:

- Si el orden nuevo es el correcto, la prueba de la encuesta estaba afirmando por posición algo que
  nunca fue una regla de negocio: se corrige la prueba —que localice la opción por texto, como hacen
  las del ABM— y se documenta que el orden del listado no es contractual.
- Si el desplegable de la encuesta sí depende del orden del ABM, el cambio está mal o está
  incompleto: se corrige el cambio, o se ordena el desplegable por su cuenta.

Lo que **no** es una opción es mergear con la regresión en rojo, ni marcar la prueba como salteada
para desbloquear el merge. Una prueba salteada es una regresión que nadie va a mirar.

#### 5. Corregir y volver a la cola

Se ajusta lo que corresponda, el pipeline vuelve a correr y recién con todo en verde se mergea.

### Qué observar

- **El botón de merge bloqueado.** Es la diferencia entre un pipeline que informa y un pipeline que
  controla. Sin la protección del [escenario 00](#2-escenario-00--preparación), esto mismo habría
  sido un comentario que alguien podía ignorar.
- **Qué falló y qué no.** La prueba propia del cambio pasa; la que se rompe es de otra pantalla. Ese
  es exactamente el caso que la revisión humana no detecta leyendo el diff.
- **Cuánto tardó en detectarse.** Comparar con el tiempo que habría tardado en aparecer si el cambio
  se descubría en homologación tres días después.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La prueba falla en el runner pero no localmente | El runner corre la matriz completa; localmente se corrió un solo navegador | Reproducir con el mismo proyecto antes de concluir que es intermitencia |
| Se marca la prueba como salteada para desbloquear | Presión de tiempo | Revertir el salteo; si el cambio es urgente, se revierte el cambio, no el control |
| El pipeline queda en rojo por una intermitencia real | Espera fija o dependencia de orden entre pruebas | Corregir la prueba: una regresión intermitente termina siendo ignorada, y ahí se pierde el control entero |

### Verificación

1. Quedó registro de una corrida en rojo, con el TRX de la configuración fallida descargable.
2. El merge estuvo bloqueado mientras el pipeline estuvo en rojo.
3. La decisión —corregir el cambio o corregir la prueba— está escrita en el pull request, con su
   motivo.
4. Ninguna prueba quedó salteada.

---

## 7. Escenario 05 — Emergencia en producción (E-05)

### Objetivo

Ejercitar la única excepción del modelo —ramar desde el tag de producción— y, sobre todo, el paso que
se olvida: el retorno de la corrección a la línea principal el mismo día. Es el único error de este
modelo que sale realmente caro.

**Roles:** I1 es A-DEV, I2 es A-OPS, I3 es A-AUT y aprueba con criterio de emergencia.

### Precondición

La versión `v1.0.0` está liberada —tag creado y artefacto promocionado— y `main` ya avanzó con
trabajo posterior al corte. Ese estado lo produce el **paso 6 del
[escenario 03](#5-escenario-03--corte-de-release-e-03-y-e-04)**; si no se hizo, hay que hacerlo
ahora, porque sin el tag `v1.0.0` el primer comando de este escenario falla con «pathspec did not
match». Contexto **C-3**.

### Pasos

#### 1. Confirmar que es una emergencia

La vía de excepción se activa **solo** si hay usuarios afectados ahora —servicio caído o degradado—
o hay una vulnerabilidad siendo explotada. Las dos condiciones se responden con sí o no mirando un
hecho registrado: un incidente abierto, una alerta, un aviso de seguridad. Un defecto molesto pero
tolerable no califica: va por el circuito normal del
[escenario 02](#4-escenario-02--defecto-con-release-abierta-e-02).

Que un cherry-pick desde `main` no aplique limpio **no** es una emergencia: es un problema técnico de
portabilidad, se resuelve conflicto por conflicto dentro del circuito normal, y se anota que la
ventana de estabilización se está haciendo larga. Ver
[06](../Estandares-Modelo-Ramas-Guide/Estandares-Modelo-Ramas.md#6-modelo-adoptado).

Para la práctica: simular que la aplicación agota el tiempo de espera al listar localidades cuando la
base tiene muchos registros.

#### 2. Ramar desde el TAG, no desde la punta de la release

```bash
git fetch --tags
git checkout -b hotfix/199-timeout-listado v1.0.0
```

El motivo de que sea el tag y no `release/1.0`: la punta de la rama de release puede tener
correcciones ya mergeadas pero **todavía no liberadas**. Si se rama de ahí, el hotfix arrastra a
producción cambios que nadie autorizó.

#### 3. Corrección mínima, con su prueba

Lo mínimo que resuelve el incidente. Nada más. Cualquier mejora adicional entra después por el
circuito normal.

```bash
git push -u origin hotfix/199-timeout-listado
```

#### 4. Pull request contra la rama de release, aprobación de emergencia (I3)

> **[F: ITIL-1]** La autoridad de aprobación se asigna según el riesgo del cambio. Una aprobación de
> emergencia es legítima: lo que no es opcional es la revisión posterior a la implementación.

La verificación automática corre igual, aunque acotada: la matriz completa puede correr después, sin
bloquear el despliegue.

#### 5. Nueva versión de parche y despliegue (I2)

Ramar desde el tag no sirve de nada si después se etiqueta la punta de la release: la punta puede
tener correcciones mergeadas y **no liberadas**, y el artefacto de `v1.0.1` las llevaría a producción
igual. Así que la punta se etiqueta solo si está probado que no hay nada de más; si hay, el parche se
etiqueta sobre el commit del hotfix.

```bash
git checkout release/1.0
git pull --ff-only

# Compuerta: qué hay en la punta que no esté liberado en v1.0.0, sin contar el hotfix recién
# mergeado. Si esto imprime algo, la punta NO se puede etiquetar.
git log --oneline v1.0.0..release/1.0 --invert-grep --grep="#199"

# Caso A — la lista está vacía: la punta es el hotfix y nada más.
git tag -a v1.0.1 -m "Parche: tiempo de espera al listar localidades"

# Caso B — la lista NO está vacía: el tag va sobre el commit del hotfix, no sobre la punta.
# git tag -a v1.0.1 -m "Parche: tiempo de espera al listar localidades" <sha-del-hotfix-en-release>

git push origin v1.0.1
```

#### 6. El retorno, el mismo día

```bash
git checkout main
git pull --ff-only
git checkout -b fix/199-retorno-timeout
git cherry-pick -x <sha-del-hotfix>
git push -u origin fix/199-retorno-timeout
# Pull request a main, revisión normal
```

Sin este paso, el defecto reaparece en la próxima versión y nadie va a entender por qué. La auditoría
del [escenario 07](#9-escenario-07--cierre-y-auditoría) existe precisamente para detectar cuando este
paso falta.

#### 7. Revisión posterior a la implementación (los tres)

Media hora, con una sola pregunta de fondo: por qué no se detectó antes. La salida esperada no es un
culpable, es una prueba de regresión nueva o un control de pipeline nuevo.

### Qué observar

- **Qué contiene el tag y qué contiene la punta de la release.** `git log --oneline v1.0.0..release/1.0`
  muestra la diferencia; si esa lista no está vacía, ramar de la punta habría desplegado eso también.
- **El registro de la aprobación de emergencia.** Quién, cuándo y con qué alcance.
- **La ventana entre el despliegue del parche y el retorno a `main`.** El objetivo es horas, no días.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El hotfix arrastró cambios no autorizados | Se ramó de `release/1.0` en lugar del tag | Rehacer desde el tag; verificar qué se desplegó |
| El retorno a `main` quedó para «mañana» | La urgencia terminó al desplegar | Hacerlo antes de cerrar el incidente: es parte del incidente, no una tarea posterior |
| El retorno genera conflicto | `main` ya cambió esa zona del código | Resolverlo a mano; el resultado importa más que la limpieza del historial |

### Verificación

1. Existe el tag `v1.0.1`, apunta a un commit de `release/1.0`, y
   `git log --oneline v1.0.0..v1.0.1` contiene **solo** el hotfix: ningún cambio no autorizado viajó
   a producción con el parche.
2. El commit del hotfix figura en `main` con la referencia a su SHA original.
3. La auditoría de convergencia pasa en verde
   ([escenario 07](#9-escenario-07--cierre-y-auditoría)).
4. Quedó registrada la aprobación de emergencia y el acta breve de la revisión posterior.

---

## 8. Escenario 06 — Versión de demostración (E-06)

### Objetivo

Mostrar trabajo todavía no liberado sin crear una tercera línea de código que nadie audite. La
tentación es cortar una rama `demo`; el escenario existe para practicar la alternativa.

**Roles:** I1 es A-OPS, I2 es A-PO y pide la demostración, I3 es A-DEV y verifica qué entra.

### Precondición

`main` tiene trabajo integrado que no está en `release/1.0`. Existe un ambiente de demostración, o al
menos la capacidad de levantar uno efímero.

### Pasos

#### 1. Elegir el commit, no la rama (I1 + I3)

```bash
git checkout main
git pull --ff-only
git log --oneline -10
```

Se elige un commit concreto de `main`. Lo que se muestra tiene que ser reproducible: «la punta de
`main` del martes» no es una referencia, un SHA sí.

#### 2. Etiquetar con sufijo de precedencia

```bash
git tag -a v1.1.0-demo.1 -m "Demostración para la reunión del 30/08. No soportada."
git push origin v1.1.0-demo.1
```

El sufijo hace que la versión quede **por debajo** de `v1.1.0` en precedencia semántica
**[F: SEMVER-1]**, de modo que ninguna herramienta la confunda con una versión liberada.

#### 3. Construir una sola vez y desplegar

El artefacto se construye por el mismo camino que cualquier otro: el build no cambia porque el
destino sea una demostración. Se despliega en el ambiente efímero o en el de demostración.

#### 4. Declarar el alcance por escrito (I2)

En el issue o en el anuncio de la demostración:

- **no está soportada**: no recibe hotfix ni parches;
- **no se promociona** a producción bajo ninguna circunstancia;
- su tag **no se reutiliza**: la próxima demostración es `demo.2`;
- lo que se muestra puede cambiar antes de liberarse.

#### 5. Dar de baja el ambiente

Terminada la demostración, el ambiente efímero se destruye. El tag queda: es barato y documenta qué
se mostró.

### Qué observar

- **Que no se creó ninguna rama.** `git branch -r` sigue mostrando `main` y `release/1.0`.
- **La precedencia del tag.** Cualquier herramienta que ordene versiones pone `v1.1.0-demo.1` antes
  que `v1.1.0`.
- **Que el artefacto se construyó con el mismo pipeline.** Si hizo falta un build especial «para la
  demo», el build no era hermético.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| Existe una rama `demo` con commits propios | Se cortó una rama en lugar de etiquetar | Llevar lo que valga a `main` por pull request y borrar la rama |
| Piden un arreglo «sobre la demo» | No se declaró que no está soportada | El arreglo va a `main` por el circuito normal y se genera `demo.2` |
| La demo terminó desplegada en producción | Se promocionó un artefacto no autorizado | Revertir; revisar quién puede promocionar a producción |

### Verificación

1. Existe el tag con sufijo y no existe ninguna rama nueva.
2. El artefacto de la demostración salió del mismo pipeline que los demás.
3. El alcance —no soportada, no promocionable— está escrito en algún lado consultable.
4. El ambiente efímero fue dado de baja.

---

## 9. Escenario 07 — Cierre y auditoría

### Objetivo

Comprobar que después de seis escenarios el repositorio quedó en un estado sano: nada corregido en
una release quedó sin volver al tronco, no hay ramas huérfanas, y cada versión se puede rastrear
hasta su commit. Y cerrar la capacitación con una retrospectiva que produzca cambios concretos.

**Roles:** los tres, juntos.

### Precondición

Escenarios 00 a 06 completados.

### Pasos

#### 1. Auditoría de convergencia

Es el control que detecta el error más caro del modelo: un hotfix que nunca volvió a la línea
principal.

```bash
git fetch --all --tags

# Commits presentes en la release y ausentes en main, comparando por contenido:
git cherry -v main release/1.0
```

`git cherry` marca con `+` los commits de `release/1.0` cuyo cambio **no** está en `main`, y con `-`
los que sí. Todo `+` es un candidato a hotfix sin retorno y hay que explicarlo uno por uno.

La versión automatizada del mismo control está en
[../Anexos/workflows/auditoria-convergencia.yml](../Estandares-Modelo-Ramas-Guide/Anexos/workflows/auditoria-convergencia.yml), y
corre exactamente este `git cherry`: la detección es por **contenido**. El mensaje del commit
interviene después y para una sola cosa —descartar los que llevan el encabezado `Convergencia:`, la
forma declarada de explicar un retorno resuelto a mano—, nunca para decidir si el cambio está en
`main`. El `-x` no participa de ninguno de los dos pasos; sirve para que una persona rastree el SHA
de origen al leer la historia. Son dos justificaciones distintas y conviene no mezclarlas: el día que se mezclan, un
control que alerta se diagnostica buscando un `-x` que nunca tuvo nada que ver.

#### 2. Higiene de ramas

```bash
git ls-remote --heads origin
```

Lo esperable al cierre: `main` y, a lo sumo, las ramas de release vivas. Toda rama corta debería
haber desaparecido al mergear su pull request. Si aparece alguna con semanas de vida, es material
para la retrospectiva. **[F: TBD-2]**

#### 3. Trazabilidad de versiones

```bash
git tag --sort=-creatordate | head
git show --no-patch --format='%H %ci %s' v1.0.1
```

Para cada tag hay que poder responder de qué commit salió, qué contiene respecto del anterior y quién
autorizó su despliegue. Si alguna respuesta requiere reconstruir la historia a mano, falta
trazabilidad.

#### 4. Comparar releases

```bash
git log --oneline v1.0.0..v1.0.1     # qué agregó el parche
git log --oneline main ^release/1.0  # qué quedó fuera de la release
```

La segunda lista es la más instructiva: son las funcionalidades que **no** se arrastraron al hacer
cherry-pick, que es justo la objeción que el
[escenario 02](#4-escenario-02--defecto-con-release-abierta-e-02) discutía en abstracto.

#### 5. Retrospectiva

Cuatro preguntas, treinta minutos, y una salida escrita:

1. ¿Cuál fue el pull request más grande, y cuánto tardó su revisión?
2. ¿Cuánto tiempo pasó entre el despliegue del hotfix y su retorno a `main`?
3. ¿Qué falla detectó el pipeline que la revisión humana no habría detectado?
4. ¿Qué regla del modelo costó más sostener, y qué haría falta para que sea fácil?

La salida no es un acta: son uno o dos cambios concretos —un control nuevo en el pipeline, un ajuste
en los criterios de admisión, un umbral de tamaño de pull request— con responsable.

### Qué observar

- **Que la auditoría encuentre algo.** Si el
  [escenario 05](#7-escenario-05--emergencia-en-producción-e-05) se hizo completo, no debería; si se
  saltó el retorno a propósito, el control tiene que detectarlo. Vale la pena probar las dos cosas.
- **La diferencia entre comparar por SHA y comparar por contenido.** Los SHA difieren siempre tras un
  cherry-pick; `git cherry` compara el cambio, no el hash.

### Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| `git cherry` marca todo con `+` | Se compara contra la rama equivocada, o `main` está desactualizado | `git fetch` y repetir con las referencias remotas |
| La auditoría automática no detecta un hotfix sin retorno | La rama no cae en el patrón `origin/release/*` que audita el workflow; o el checkout fue superficial y `git cherry` no tiene historia que comparar; o el cambio se reescribió (rebase, squash con contenido distinto) y ya figura como equivalente | Reproducirlo en orden: `git branch -r --list 'origin/release/*'`, después `fetch-depth: 0`, y por último `git cherry -v main <rama>` a mano. **No** es por falta de `-x`: el control compara contenido, no mensajes |
| La auditoría queda en rojo por un retorno legítimo | El retorno se resolvió a mano y el contenido difiere, así que el commit queda marcado `+` para siempre | Declararlo en el mensaje del commit de la release con la línea `Convergencia: <sha-en-main> (retorno con conflicto resuelto)`, que es lo único que la auditoría excluye **[C]** |
| Quedan ramas de release viejas | No se borran al caer en desuso | Borrarlas **[F: TBD-1]** |

### Verificación

Estado final esperado del repositorio de práctica:

1. `git cherry -v main release/1.0` no arroja ningún `+` salvo los que lleven la línea
   `Convergencia:` en su mensaje, que es donde se registra la explicación.
2. Solo quedan `main` y las ramas de release vivas.
3. Cada tag se puede rastrear hasta su commit y su corrida de pipeline.
4. La retrospectiva produjo al menos un cambio concreto con responsable asignado.

---

Índice general del cuerpo documental:
[Estándares y modelo de ramas](../Estandares-Modelo-Ramas-Guide/README.md).
