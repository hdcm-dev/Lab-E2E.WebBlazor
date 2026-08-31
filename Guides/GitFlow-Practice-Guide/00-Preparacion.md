---
doc_id: GF-09-00
doc_type: escenario-practico
title: 00 — Preparación del repositorio de práctica
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, devops]
traces: [GF-09, GF-08]
---

# 00 — Preparación

## Objetivo

Dejar el repositorio de práctica con una aplicación real, pruebas automatizadas que corren, y los
controles que el resto de los escenarios va a ejercitar. Sin esto, los escenarios siguientes son
teatro: no hay nada que se pueda romper ni ninguna verificación que lo detecte.

## Precondición

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

## Pasos

### 1. Sembrar la aplicación

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

### 2. Comprobar que las pruebas corren localmente

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

### 3. Instalar los workflows del procedimiento

Los del laboratorio de E2E cubren el pull request y la línea principal. Faltan los que el
procedimiento de release necesita: verificación de las ramas `release/*`, corte de versión y
auditoría de convergencia. Están en [../Anexos/workflows/](../Estandares-Modelo-Ramas-Guide/Anexos/workflows/README.md). El `ci.yml` de esa carpeta
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
exactamente el «error frecuente» del escenario 01.

### 4. Configurar la protección de rama

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
[08](../Estandares-Modelo-Ramas-Guide/08-Pull-Requests-Y-Pruebas.md): la categoría «infraestructura, seguridad o migraciones» se
decide por ruta tocada, no por juicio.

Sobre el espacio de nombres de tags, en *Settings → Tags*: regla sobre el patrón `v*` que restringe
la creación a quien cumple A-OPS. Sin ella, cualquiera con permiso de escritura publica una versión
desde una rama personal, porque el disparador de `release.yml` es el tag, no el merge.

Se exige **un solo check** —el job resumen— y no la lista completa de jobs: así la regla no hay que
tocarla cada vez que cambia la matriz de navegadores.

### Permisos y vía de excepción sobre la protección **[C]**

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

### 5. Declarar dueños de los archivos sensibles

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

## Qué observar

- El primer pull request corre el pipeline **antes** de que exista la protección: ver la diferencia
  entre «el pipeline informa» y «el pipeline bloquea» es el punto del escenario 04.
- Cuánto tarda la verificación rápida frente a la matriz completa. Esa diferencia es la que justifica
  separarlas.
- Que el reporte de las pruebas quede como artefacto de la corrida, y por cuántos días.

## Errores frecuentes

| Síntoma | Causa habitual |
|---|---|
| Los jobs quedan en cola para siempre | El runner autoalojado no tiene la etiqueta `i7infra-dev`, o está apagado |
| Las pruebas pasan localmente y fallan en el runner | La aplicación no se publicó antes de correr; el artefacto no llegó al job |
| El check obligatorio nunca aparece en la lista | El nombre configurado no coincide **exactamente** con el `name:` del job |

## Verificación

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

Sigue: [01 — Funcionalidad nueva](01-Funcionalidad-Nueva.md).
