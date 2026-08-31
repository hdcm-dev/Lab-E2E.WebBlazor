---
doc_id: GF-08
doc_type: documento-tematico
title: Pull requests y pruebas automatizadas
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, seguridad]
traces: [GF-06, GF-07, GF-09]
---

# Pull requests y pruebas automatizadas

El problema que originó esta guía es concreto: un pull request contra una rama estable puede romper
funcionalidad que andaba, porque no se dispara ninguna verificación automática que lo detecte. Un
procedimiento de pull request sin pipeline es una conversación entre dos personas sobre un diff; lo
que convierte esa conversación en un control es que **el pipeline corra antes del merge y que el
merge esté bloqueado si el pipeline no está en verde**.

## Ciclo de vida de un pull request

1. Se abre **en borrador** con el primer commit. **[C]** La verificación automática empieza a correr
   temprano y quien revisa el diseño puede mirar el resultado mientras el trabajo avanza.
2. La descripción vincula el issue (`Closes #142`), lo que lo cierra automáticamente al mergear.
3. El pipeline ejecuta lo que está implementado en los workflows del anexo, y nada más: compilación
   de la solución con las advertencias como errores, descubrimiento de las pruebas, y la regresión
   de extremo a extremo. **[C]** Análisis estático, escaneo de dependencias y pruebas unitarias o de
   integración **no** corren hoy: son controles pendientes, no cubiertos. Cada fila de esta lista
   tiene que poder señalarse job por job en
   [Anexos/workflows/ci.yml](Anexos/workflows/ci.yml); si no se puede, no se enuncia.
4. Se marca como listo para revisión.
5. Revisión: una aprobación para cambios normales; dos cuando el pull request toca alguna de las
   rutas sensibles declaradas en `CODEOWNERS` —hoy `.github/workflows/**` y
   `src/**/Persistencia/**`—. El criterio es la ruta tocada, no una apreciación sobre el cambio, y
   se configura como regla del repositorio, no como acuerdo verbal. **[C]** Con un equipo de tres
   personas y el autor excluido, «dos aprobaciones» significa unanimidad de los otros dos: es una
   consecuencia operativa deliberada, no un descuido.
6. **Squash merge**, y la rama se borra automáticamente.

### Por qué squash

Deja **un solo commit por issue en `main`**, lo que hace que el cherry-pick a una release sea de un
único SHA y no falle por commits intermedios. No es una preferencia estética: es lo que sostiene
mecánicamente la regla 4 del [modelo adoptado](06-Modelo-Adoptado.md). **[C]** El borrado de la rama
tras el merge funciona además como prueba de convergencia. **[F: TBD-2]**

## Tamaño del pull request

Es la variable con más impacto sobre la calidad de la revisión, y la que el equipo controla sin
comprar nada.

> **[F: GOOG-1]** El fundamento para preferir cambios chicos: se revisan más rápido y más a fondo,
> tienen menos probabilidad de introducir defectos, desperdician menos trabajo si son rechazados,
> generan menos conflictos al mergear y son más simples de revertir. El tamaño correcto es un cambio
> autocontenido, y quien revisa tiene la potestad de rechazar un pull request únicamente por ser
> demasiado grande.

> **[F: GOOG-2]** La contrapartida del lado de quien revisa es el tiempo de respuesta: un día hábil
> es el máximo para responder a un pedido de revisión, sin interrumpir una tarea de concentración en
> curso. Y si un pull request es tan grande que no se sabe cuándo habrá tiempo de revisarlo, la
> respuesta correcta es pedir que se parta en varios chicos encadenados.

La consecuencia práctica es que «la revisión es el cuello de botella» casi nunca se resuelve
revisando más rápido: se resuelve achicando los cambios.

## Estados del issue **[C]**

**Backlog → Listo para tomar → En curso → En revisión → En homologación → Cerrado**

Dos reglas sobre las transiciones:

- Un issue pasa a *Listo para tomar* solo si tiene criterio de aceptación escrito.
- Un issue pasa a *Cerrado* cuando A-QA lo valida, no cuando se mergea el pull request.

## Qué verifica el pipeline y cuándo

El principio es que **el costo de la verificación crezca con la importancia de la rama**: en un pull
request importa la velocidad de la respuesta, y en la línea principal y en las ramas de release
importa la cobertura.

| Disparador | Alcance de la verificación | Por qué |
|---|---|---|
| `pull_request` a `main` o `release/*` | Comprobaciones rápidas + regresión en **un solo navegador** (chromium) | Respuesta en minutos; es el control que impide romper lo estable. El recorte de latencia es la matriz, no el reparto en shards: el workflow reutilizable de la aplicación no ofrece sharding |
| `push` a `main` | Matriz completa de navegadores | Lo ya integrado se verifica a fondo |
| `push` a `release/*` | Matriz completa | Un cherry-pick que aplica limpio no garantiza que funcione **[F: TBD-1]** |
| `merge_group` | Igual que `push` | La cola de merge verifica la combinación real |
| `schedule` | Matriz completa nocturna | Detecta intermitencias y degradaciones lentas |
| `workflow_dispatch` | A pedido, parametrizado | Diagnóstico y verificación de entornos |

> **[F: TBD-1]** El pipeline que protege al tronco se duplica para proteger también a las ramas de
> release activas. Un cherry-pick que aplica sin conflicto no es garantía de que el resultado
> funcione: hay que verificarlo en el contexto de la release.

### Verificación rápida primero

Antes de gastar un runner con navegadores conviene un job barato que falle en segundos. Sobre esta
aplicación —.NET, con la suite E2E en `tests/MovilidadUrbana.E2ETests`— eso es: restaurar, compilar
la solución con las advertencias tratadas como errores, y listar las pruebas, que detecta pruebas
rotas y filtros de ejecución olvidados. Un pull request que no compila no merece una matriz de
cuatro navegadores.

### Una sola definición de «cómo se corren las pruebas»

La definición vive en un **workflow reutilizable** invocado por los demás. **[F: GHA-1]** GitHub
documenta la reutilización de workflows: uno se declara con el disparador `workflow_call`, recibe
entradas y secretos, devuelve salidas, y otro lo invoca con `uses:`; el llamado puede además estar en
otro repositorio.

El beneficio no es ahorrar líneas: es que la verificación de un pull request, la de `main`, la de una
rama de release y la de un entorno desplegado sean **la misma**, con distintos parámetros. Cuando son
tres definiciones distintas, tarde o temprano una queda atrás y aparece el «en CI pasaba».

Los workflows concretos, listos para copiar, están en
[Anexos/workflows/](Anexos/workflows/README.md).

### Runner

Todos los jobs corren en el runner autoalojado del equipo:

```yaml
runs-on: [self-hosted, i7infra-dev]
```

Los jobs que ejecutan las pruebas lo hacen **dentro del contenedor oficial de Playwright**, que trae
los navegadores y sus dependencias del sistema. **[F: PW-1]** La documentación de integración
continua de Playwright ofrece esa imagen precisamente para eso. Sobre un runner autoalojado la
decisión pesa más que sobre uno alojado: sin contenedor, la máquina acumula versiones de navegadores
que nadie recuerda haber instalado, y la corrida deja de ser reproducible.

## Protección de rama

La configuración es lo que convierte al procedimiento en un control efectivo:

| Control | Ramas | Efecto |
|---|---|---|
| Prohibir push directo | `main`, `release/*` | Todo entra por pull request |
| Verificaciones obligatorias | `main`, `release/*` | Sin pipeline en verde no hay merge |
| Aprobaciones mínimas | `main`, `release/*` | 1 aprobación **[C]** |
| Revisión obligatoria de propietarios | `main`, `release/*` | *Require review from Code Owners*: sin la aprobación del dueño de la ruta no hay merge |
| Segunda aprobación por ruta sensible | `.github/workflows/**`, `src/**/Persistencia/**` | La categoría «infraestructura, seguridad o migraciones» se define por **ruta**, no por juicio: son exactamente las rutas de `CODEOWNERS`. Se instrumenta con una regla adicional (*ruleset*) que exige 2 aprobaciones sobre ese patrón **[C]** |
| Borrado automático de rama | todas | Higiene, y evidencia de convergencia |

Conviene exigir **un único check** en la regla de protección —un job final que resuma a los demás— en
lugar de listar cada job: así la regla no hay que actualizarla cada vez que cambia la matriz. El
check solo puede dar verde si los jobs que resume **efectivamente corrieron**: un job salteado no es
un job aprobado, y tratarlo como tal es la forma silenciosa en que este control se vaciaría.

### Protección del espacio de nombres de tags

La protección de rama no alcanza. El tag `v*` es el disparador de
[`release.yml`](Anexos/workflows/release.yml), que construye y publica el artefacto con permiso de
escritura sobre el repositorio: quien pueda empujar un tag `v*` publica una versión desde cualquier
commit, sin pull request, sin revisión y sin protección de rama. Como según este documento el tag es
la respuesta a «qué hay en producción», eso corta la cadena de custodia del artefacto liberado.

| Control | Alcance | Efecto |
|---|---|---|
| Regla de protección de tags | patrón `v*` | Solo el rol A-OPS puede crear tags de versión **[C]** |
| Tag únicamente sobre commit integrado | `v*` | El commit etiquetado tiene que ser alcanzable desde `main` o desde una `release/*`, nunca desde una rama personal |
| Autorización previa a la versión final | tags sin sufijo (`v1.0.0`) | Registro de A-AUT antes de crear el tag; las candidatas `-rc` no la requieren |

Si el equipo decide no configurar esto, la decisión hay que escribirla como riesgo aceptado y decir
por qué la verificación previa del workflow se considera suficiente. **[C]**

## Aplicación por escenario

| Escenario | Qué agrega el pipeline |
|---|---|
| **E-01** Funcionalidad | Regresión que confirma que lo que andaba sigue andando |
| **E-02** Defecto | La prueba que reproduce el defecto queda como regresión permanente |
| **E-04** Estabilización | Verificación dedicada de la rama de release tras cada cherry-pick |
| **E-05** Emergencia | Verificación acotada; la matriz completa corre después, no bloquea |
| **E-08** Rechazo | El TRX de la corrida fallida como evidencia de por qué se rechazó |

## Ejemplo concreto

Un pull request de corrección sobre `fix/142` contra `main`, en un repositorio con este esquema:

1. Primer commit y apertura en borrador. Arranca la verificación rápida —que corre también en
   borrador— y falla en 40 segundos porque la prueba nueva todavía no compila. Nadie perdió un
   runner con navegadores.
2. Se corrige. La verificación rápida pasa y arranca la regresión en chromium, un solo navegador.
   Falla una prueba de la encuesta: el TRX y los resultados quedan como artefactos de la corrida.
3. Se corrige la causa. Pipeline en verde, revisión aprobada, squash merge. En `main` queda un solo
   commit, `a3f9c21`.
4. `push` a `main` dispara la matriz completa: cuatro navegadores.
5. Con la release abierta, `cherry-pick -x a3f9c21` a `release/1.4` dispara la misma verificación
   sobre esa rama, que es la que confirma que el arreglo funciona **ahí**.

## Preguntas guía

1. ¿Qué pasa hoy si alguien abre un pull request contra una rama de release? ¿Corre algo?

   Conviene comprobarlo en el archivo y no de memoria: abrir el `ci.yml` que trae la aplicación
   sembrada y leer su bloque `on:`. Ahí figuran `push` a `main`, `pull_request` hacia `main` o
   `develop`, y `merge_group`: ninguno alcanza a `release/*`, así que ese pull request no dispara
   corrida alguna y la protección de rama no tiene check que exigir. El
   [`ci.yml`](Anexos/workflows/ci.yml) de esta guía es el que agrega `release/**`.

2. ¿Cuál es el check obligatorio de la regla de protección, y qué jobs resume?

   `CI aprobada` —el job `ci-ok` de [`ci.yml`](Anexos/workflows/ci.yml)—, y resume exactamente dos:
   `verificacion-rapida` y `e2e`. Corre con `if: always()` y recorre ambos resultados; solo
   `success` pasa. Un `skipped` lo hace fallar, que es la parte que sostiene el control: un job
   salteado no verificó este commit.

3. Si la matriz completa tarda demasiado en un pull request, ¿qué se recorta primero y por qué?

   Primero cae la matriz: en el evento `pull_request`, `ci.yml` le pasa a `e2e.yml` la entrada
   `navegadores: chromium`, y reserva los cuatro para lo ya integrado. El reparto en shards no está
   disponible: `e2e.yml` declara cuatro entradas de `workflow_call` y ninguna es `cantidad-shards`;
   pasarla haría que GitHub rechace la corrida como inválida.

4. ¿Dónde queda la evidencia de una corrida que falló, y cuánto tiempo se conserva?

   En la corrida misma: el TRX que `e2e.yml` sube como artefacto. No hay reporte HTML ni trazas de
   Playwright —la suite es el proyecto .NET `tests/MovilidadUrbana.E2ETests`—. El plazo depende de
   quién invoque: [`release.yml`](Anexos/workflows/release.yml) fija `retencion-dias: 30`, mientras
   que `ci.yml` no pasa esa entrada y hereda el valor por omisión de `e2e.yml`.

## Criterios de calidad

Un pipeline de pull request sirve cuando cumple tres cosas: falla rápido y por el motivo correcto,
deja evidencia suficiente para diagnosticar sin reproducir a mano, y no se puede saltear. Si el
equipo aprendió a mergear «igual» porque el pipeline es intermitente, el control ya no existe aunque
el archivo YAML siga ahí.

---

Sigue: [09 — Guía práctica](../GitFlow-Practice-Guide/README.md).
