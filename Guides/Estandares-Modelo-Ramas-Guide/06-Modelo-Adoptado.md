---
doc_id: GF-06
doc_type: documento-tematico
title: Modelo adoptado — tronco con ramas de release
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po]
traces: [GF-05, GF-07, GF-08]
---

# Modelo adoptado

El equipo trabaja sobre una línea principal única a la que se integra por pull request, y corta ramas
de release cuando necesita estabilizar una versión. Las correcciones nacen siempre en la línea
principal y viajan a la release por cherry-pick, nunca al revés. Este documento fija las reglas; los
procedimientos que se derivan de ellas están en [07](07-Integracion-Y-Versionado.md) y
[08](08-Pull-Requests-Y-Pruebas.md).

Cada afirmación lleva una marca: **[F]** cuando está respaldada por una fuente externa listada en
[Anexos/Fuentes.md](Anexos/Fuentes.md), **[C]** cuando es una convención de este equipo. La
separación es deliberada: un documento de proceso pierde autoridad cuando presenta preferencias
propias como si fueran estándares.

## Inventario de ramas

Tres tipos. No hay un cuarto.

| Rama | Vida | Nace de | Quién escribe |
|---|---|---|---|
| `main` | permanente | — | nadie directamente: solo merges de pull request |
| `feature/*`, `fix/*`, `chore/*` | objetivo de diseño: ≤ 2 días; **umbral normativo: > 7 días incumple** **[C]** | `main` | el desarrollador asignado |
| `release/x.y` | semanas, después se borra | `main`, o un commit anterior elegido | nadie directamente: cherry-picks y hotfixes entran por pull request |

Sobre la vida de las ramas cortas hay **dos magnitudes distintas y no intercambiables [C]**:

| Magnitud | Valor | Qué implica |
|---|---|---|
| Objetivo de diseño | vida ≤ 2 días | Es la meta al partir el trabajo; superarla no es un incumplimiento |
| Umbral normativo | vida > 7 días (estrictamente) | Es incumplimiento: la rama se revisa en la reunión de equipo y es alertable automáticamente |

El tramo intermedio —de 2 a 7 días inclusive— está en regla y no requiere acción. Un único operador,
un único umbral: así la revisión semanal y una alerta automática miden lo mismo.

**No existe** una rama `develop`, ni `homologacion`, ni `produccion`. **[C]** Homologación y
producción son ambientes; lo que se mueve entre ellos son artefactos.

```mermaid
gitGraph
   commit id: "v1.3.0"
   branch release-1.3
   checkout release-1.3
   commit id: "corte 1.3" tag: "v1.3.0"
   checkout main
   commit id: "feat-101"
   commit id: "feat-107"
   commit id: "fix-142"
   checkout release-1.3
   cherry-pick id: "fix-142" tag: "v1.3.1"
   checkout main
   commit id: "feat-115"
   branch release-1.4
   checkout release-1.4
   commit id: "corte 1.4" tag: "v1.4.0-rc1"
   checkout main
   commit id: "fix-158"
   checkout release-1.4
   cherry-pick id: "fix-158" tag: "v1.4.0"
```

El desarrollo nunca se detiene en `main`; las ramas de release son ventanas de estabilización; las
correcciones viajan del tronco hacia las releases, jamás en sentido contrario.

## Las siete reglas

Todo el resto del procedimiento es consecuencia de estas siete.

1. **Toda rama nace de `main` actualizado. [C]** Salvo bajo la vía de excepción, definida más
   abajo en «La única excepción».
2. **`main` está protegida. [C]** Sin push directo: se entra por pull request con verificación
   automática en verde y al menos una aprobación.
3. **Un issue → una rama → un pull request → un commit en `main`. [C]** Si un issue necesita dos
   ramas, estaba mal escrito. Salvo bajo la vía de excepción, donde un mismo issue produce
   deliberadamente dos ramas —la de hotfix y la de retorno— y dos pull requests.
4. **Los defectos se reproducen y corrigen en el tronco, con una prueba, y recién después se
   cherry-pickean a la rama de release. [F: TBD-1, SRE-2, GL-1]**
5. **No se corrigen defectos en la rama de release esperando llevarlos de vuelta al tronco. [F: TBD-2]**
   Salvo bajo la vía de excepción, que sí escribe primero en la release y obliga al retorno; lo que
   la regla prohíbe es *esperar* llevarlo después sin plazo ni control, no el retorno mismo.
6. **Se construye una sola vez; se promociona el artefacto, no se recompila por ambiente. [F: SRE-1]**
7. **La configuración depende del ambiente por variable de entorno, nunca de la rama ni de
   compilación condicional. [C]**

### Por qué las reglas 4 y 5 son el corazón del modelo

Tres organizaciones independientes documentan la misma práctica:

- **[F: TBD-1]** La recomendación para equipos de desarrollo basado en tronco es reproducir el
  defecto en el tronco, corregirlo ahí con una prueba, dejar que el servidor de integración lo
  verifique, y después cherry-pickearlo a la rama de release, esperando que la verificación dedicada
  a esa rama lo confirme también.
- **[F: SRE-2]** Google describe que la mayoría de sus proyectos grandes ramifica desde el tronco en
  una revisión específica y **nunca** mergea esa rama de vuelta; las correcciones se envían al tronco
  y se cherry-pickean a la rama de release.
- **[F: GL-1]** GitLab documenta arreglar hacia adelante y después cherry-pickear a la rama de patch
  release, porque el problema clásico es corregir en la versión recién liberada y olvidarse de
  corregir en la principal.

El contrapunto existe y no conviene ocultarlo: **[F: NVIE-1]** GitFlow propone exactamente lo
contrario —estabilizar sobre la rama de release y mergear después hacia la rama de desarrollo—. El
desacuerdo es real y se resuelve por contexto, no por autoridad: el contexto de una aplicación web
con despliegue frecuente y una sola versión viva apunta al modelo de este documento, y el de un
producto instalable con varias versiones soportadas apunta al otro.

## De dónde nace cada rama

```mermaid
flowchart TD
    A["Necesito escribir código"] --> B{"Qué tipo<br/>de trabajo?"}
    B -->|Funcionalidad nueva| C["feature/NNN-desc<br/>desde main"]
    B -->|Defecto| D{"Dónde se<br/>manifiesta?"}
    B -->|Config, dependencias, build| E["chore/NNN-desc<br/>desde main"]
    D -->|En desarrollo| F["fix/NNN-desc<br/>desde main"]
    D -->|En homologación| F
    D -->|En producción| G{"Es emergencia<br/>real?"}
    G -->|No| F
    G -->|Sí| H["hotfix/NNN-desc<br/>desde el TAG de producción"]
    F --> I["PR a main<br/>+ cherry-pick a release"]
    H --> J["PR a release/x.y<br/>+ retorno obligatorio a main"]
```

### Nomenclatura **[C]**

| Prefijo | Uso | Nace de | Ejemplo |
|---|---|---|---|
| `feature/` | Funcionalidad nueva | `main` | `feature/107-filtro-por-partida` |
| `fix/` | Corrección de defecto | `main` | `fix/142-superficie-con-fraccion` |
| `chore/` | Dependencias, build, configuración | `main` | `chore/119-actualizar-sdk` |
| `hotfix/` | Emergencia en producción | tag de producción | `hotfix/199-timeout-consulta` |

El número de issue adelante permite rastrear cualquier commit hasta su ticket sin abrir el tablero.

## La única excepción: emergencia en producción

### Cuándo se activa

Se activa **solo** si se cumple alguna de estas dos condiciones, y ambas se responden con sí o no a
partir de un hecho registrado —incidente abierto, alerta, aviso de seguridad—:

- el servicio está caído o degradado **para los usuarios, ahora** (es el contexto C-3 de
  [01](01-Marco-De-Referencia.md): «¿hay usuarios afectados ahora?»);
- hay una vulnerabilidad de seguridad **siendo explotada**.

Un cherry-pick que no aplica limpio **no** activa nada: es un problema técnico de portabilidad del
arreglo, y su procedimiento es resolver el conflicto puntualmente en el pull request contra la
release —[escenario 02](../GitFlow-Practice-Guide/02-Defecto-Con-Release-Abierta.md)— y anotar que la ventana
de estabilización se está haciendo larga. Confundir «cuesta portarlo» con «es una emergencia»
convierte la excepción en el camino habitual, porque saltea la aprobación normal.

### Qué suspende, y a cambio de qué

Es el único lugar donde las reglas 1, 3 y 5 se suspenden, y solo estas tres:

| Regla | Qué se suspende | Obligación compensatoria |
|---|---|---|
| 1 — toda rama nace de `main` | La rama nace del **tag** de producción | Queda registrado el tag de origen en el pull request |
| 3 — un issue, una rama, un commit | El issue produce dos ramas y dos pull requests | Ambas referencian el mismo número de issue |
| 5 — no se corrige en la release | El arreglo se escribe primero contra `release/x.y` | **Retorno a `main` el mismo día**, con `cherry-pick -x`, antes de cerrar el incidente |

Las reglas 2, 4, 6 y 7 **no** admiten excepción: el pull request, la verificación automática y la
aprobación —de emergencia, pero registrada— siguen siendo obligatorios. **[C]**

```bash
# Desde el TAG, no desde la punta de release/1.3:
# la punta puede tener correcciones ya mergeadas pero todavía no liberadas.
git checkout -b hotfix/199-timeout-consulta v1.3.2
# ... corrección mínima + prueba que la cubre ...
git push -u origin hotfix/199-timeout-consulta
# PR contra release/1.3 → tag v1.3.3 → despliegue
# Y EL MISMO DÍA: PR de retorno a main
```

Si la corrección no vuelve a `main`, el defecto reaparece en la próxima versión. Es el único error de
este modelo que sale realmente caro, y por eso la auditoría de convergencia del
[anexo de workflows](Anexos/workflows/) lo detecta de forma automática. **[C]**

## Aplicación por escenario

La matriz está cerrada: los ocho escenarios de [01](01-Marco-De-Referencia.md) por los dos contextos
que dependen del estado de las ramas. **C-3** (producción comprometida) no es una columna sino un
modificador: se superpone a C-1 o a C-2 y es lo que habilita la vía de excepción; **C-4** (varias
versiones vivas) queda fuera del alcance de este modelo por definición —es el disparador para volver
a [05](05-Como-Elegir-El-Modelo.md) y cambiar de modelo, no una celda de esta tabla—.

| Escenario | Contexto C-1 (sin release abierta) | Contexto C-2 (con release abierta) |
|---|---|---|
| **E-01** Funcionalidad | Rama corta → PR → `main`. Viaja en la próxima versión | Igual, y **no** se cherry-pickea salvo que estuviera en el alcance de la release |
| **E-02** Defecto | Rama corta → PR → `main` | Igual, más cherry-pick `-x` a `release/x.y` por pull request, y nueva candidata |
| **E-03** Corte | Es el escenario que crea la release y hace pasar de C-1 a C-2 | No se corta una segunda release con una viva salvo que la primera esté por borrarse (máximo dos) |
| **E-04** Estabilización | No aplica: no hay candidata que estabilizar | Ciclo candidata → prueba → defecto → nueva candidata, hasta la liberación con tag y autorización |
| **E-05** Emergencia | La rama nace igual del **tag** de producción; como no hay `release/x.y` viva, A-OPS crea `release/x.y` desde ese mismo tag, recibe ahí el PR del hotfix, se etiqueta `vx.y.z+1` y la rama queda viva hasta que se borre por desuso. Nunca se parchea con push directo a `main` ni se resucita una rama borrada | Rama desde el tag → PR a `release/x.y` → retorno a `main` |
| **E-06** Demostración | Tag `-demo.n` sobre un commit de `main` y artefacto desechable; nunca una rama | Igual; la demo no toca la release ni su calendario |
| **E-07** Mantenimiento | Rama `chore/` → PR → `main` | Igual; entra a la release solo si es condición de la liberación |
| **E-08** Rechazo | El pipeline o la revisión bloquean el merge; el cambio vuelve a la cola o se descarta con motivo registrado | Igual, y además puede rechazarse la *admisión* a la release aun con el cambio ya en `main` |

## Guardarraíles

Sin estos controles el modelo se degrada solo en un par de meses.

**Protección de rama.** `main` y `release/*` sin push directo; pull request obligatorio con
verificación en verde y aprobación registrada; archivo `CODEOWNERS` que asigne revisor por carpeta,
con la revisión de propietarios **exigida**, no solo sugerida.

**Una sola vía de escritura sobre `release/*` [C].** No hay excepción de push directo para nadie,
tampoco para los cherry-picks: el cherry-pick se aplica sobre una rama corta
`cherry/NNN-desc` cortada desde la propia `release/x.y`, y entra por pull request contra ella. Es la
misma vía que usa el hotfix. La consecuencia es observable en la configuración del repositorio: si
*Do not allow bypassing* está activo sobre `release/*` y alguien pudo empujar directo, la
configuración está mal, no el procedimiento.
Las migraciones de base de datos y los archivos de pipeline conviene que tengan dueño explícito: son
los dos lugares donde un error no se resuelve con un revert. **[C]**

**Auditoría de convergencia.** Un chequeo automático verifica que todo commit en `release/*` tenga su
equivalente en `main`. El criterio observable es la **equivalencia por contenido del cambio**, no el
mensaje del commit: se implementa con `git cherry origin/main <rama>`, que compara el cambio y no el
SHA —el SHA siempre difiere tras un cherry-pick, y un hotfix escrito en la release nunca lleva línea
`cherry picked from`—. El `-x` es una ayuda de trazabilidad para quien lee la historia, no el
mecanismo de verificación. Un commit marcado `+` es un cambio de release sin equivalente en el
tronco y hay que alertarlo. **[C]**

Un `+` legítimo existe: cuando el retorno se hizo resolviendo un conflicto a mano, el contenido
difiere y el commit queda marcado para siempre. Esos casos se declaran de una sola forma —una línea
`Convergencia: <sha-en-main> (retorno con conflicto resuelto)` en el mensaje del commit de la
release— y la auditoría los excluye. Sin ese mecanismo el control queda en rojo permanente y el
equipo aprende a ignorarlo, que es la única manera real de perderlo. **[C]**

**Higiene de ramas.** Las ramas cortas se borran al mergear **[F: TBD-2]**; las de release se borran
cuando caen en desuso **[F: TBD-1]**; una rama corta con más de una semana de vida se revisa en la
reunión de equipo **[C]**.

## Antipatrones

| Antipatrón | Por qué falla |
|---|---|
| Ramas de ambiente (`homologacion`, `produccion` como ramas) | El código de cada ambiente diverge y deja de ser cierto que se probó lo que se libera |
| Recompilar por ambiente | Se libera un binario distinto del que se probó **[F: SRE-1]** |
| Corregir en la release y prometer el retorno | El retorno se olvida y el defecto regresa **[F: TBD-2]** |
| Pull requests de más de mil líneas | La revisión se vuelve simbólica **[F: GOOG-1]** |
| Cerrar el issue al mergear | Se pierde la trazabilidad de la verificación |
| Enviar todo cambio al comité de cambios | La autoridad de aprobación se asigna según el riesgo, y los cambios estándar están preaprobados **[F: ITIL-1]**; llamarlo «antipatrón» es la lectura de este equipo **[C]** |
| Tres o más releases vivas | Cherry-picks a la rama equivocada **[F: TBD-1]** |
| Refactor oportunista dentro de una corrección | Imposible de revertir sin perder el arreglo |

## Preguntas guía

1. ¿Qué regla se está rompiendo cuando alguien dice «lo arreglo directo en la release, es más rápido»?

   La 5: «No se corrigen defectos en la rama de release esperando llevarlos de vuelta al tronco.
   **[F: TBD-2]**». Conviene mirar qué se ahorra realmente. El arreglo no se escribió más rápido:
   se salteó el orden que fija la regla 4 —tronco, prueba, después cherry-pick— y lo que queda
   pendiente es el retorno, que nadie agenda.

2. Si hay dos releases vivas y llega una corrección, ¿a cuál va? ¿Quién lo decide?

   Primero a ninguna. La regla 4 manda reproducir y corregir «en el tronco, con una prueba, y
   recién después» cherry-pickear **[F: TBD-1, SRE-2, GL-1]**. Recién ahí cada release se evalúa
   por separado, contra el tramo en que esté —estabilización o congelamiento—. La decisión no es
   de quien escribió el arreglo: el criterio y la fecha los fijaron A-OPS y A-PO al cortar.

3. ¿Qué evidencia queda de que un hotfix volvió al tronco?

   La que produce `git cherry origin/main <rama>`: el commit queda marcado `-`, porque la
   comparación es por contenido y el SHA siempre cambia tras un cherry-pick. `auditoria-convergencia.yml`
   corre esa comparación sin que nadie la pida. Si el retorno se resolvió a
   mano, el contenido difiere y la única declaración admitida es la línea `Convergencia:` en el
   mensaje. El `-x` se lee, no se audita.

4. ¿Cuál de las siete reglas es la más difícil de sostener en el equipo propio, y qué la haría fácil?

   No hace falta opinar: el repositorio lo delata. Ramas cortas pasando los siete días, la
   auditoría de convergencia en rojo dos mañanas seguidas, un `push` que entró sin pull request.
   Cada síntoma señala una regla. Y lo que la vuelve sostenible es configuración —check
   obligatorio, *Do not allow bypassing*, alerta automática—, porque una regla que depende de
   acordarse ya falló.

## Criterios de calidad

El modelo está funcionando si se cumplen tres condiciones observables: no hay ramas cortas de más de
una semana, toda rama de release tiene su equivalencia completa en `main`, y nadie necesita preguntar
de dónde ramar. Cuando alguna de las tres falla, la causa está casi siempre en el tamaño de los pull
requests, no en el modelo.

---

Sigue: [07 — Integración y versionado](07-Integracion-Y-Versionado.md).
