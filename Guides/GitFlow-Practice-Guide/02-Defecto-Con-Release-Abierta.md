---
doc_id: GF-09-02
doc_type: escenario-practico
title: 02 — Defecto con release abierta
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops]
traces: [GF-09, GF-06, GF-03]
---

# 02 — Defecto con release abierta (E-02)

## Objetivo

Practicar la regla que sostiene todo el modelo: el defecto se reproduce y se corrige en la línea
principal, con una prueba, y recién después viaja a la release por cherry-pick. Al terminar, la
corrección está en los dos lugares y hay evidencia de que así fue.

**Roles:** I1 es A-QA y reporta, I2 es A-DEV y corrige, I3 revisa y hace de A-OPS.

## Precondición

Escenario 03 hecho —es el que produce este estado—: existe `release/1.0` con la candidata
`v1.0.0-rc1` promocionada. Contexto **C-2**. El orden de ejecución es 00 → 01 → 03 → 02 → 04 → 05 →
06 → 07; ver el [índice de la guía](README.md).

## Pasos

### 1. Reportar con evidencia (I1)

El issue **#142** describe el defecto con pasos exactos, el resultado esperado, el observado y **la
versión donde se vio** —`v1.0.0-rc1`, no «en homologación»—.

Paso cero, antes de tocar código: si el defecto no se puede reproducir, el issue vuelve al reportante
pidiendo el dato que falta. No se depura a ciegas.

### 2. Ramar desde la línea principal, no desde la release (I2)

```bash
git checkout main
git pull --ff-only
git checkout -b fix/142-filtro-ignora-mayusculas
```

Acá aparece la objeción más común, y conviene enunciarla en voz alta antes de seguir: *«si ramo de
`main`, ¿no arrastro a la release las funcionalidades que entraron después del corte?»*. No, porque
la release no recibe un merge de `main`: recibe **un commit** por cherry-pick. La objeción es válida
contra el merge de rama a rama; no contra el cherry-pick.

### 3. Primero la prueba que falla

```bash
# El fixture publica la aplicación antes de la primera prueba; anteponer `publicar.sh`
# rompe la corrida (ver escenario 00, paso 2).
scripts/pruebas.sh chromium  # tests/MovilidadUrbana.E2ETests, navegador por argumento
```

El resultado queda en `resultados/*.trx`; ahí tiene que figurar la prueba nueva como fallida.

La prueba nueva **tiene que fallar**. Si pasa a la primera, no se entendió el defecto: se está
probando otra cosa. Este paso no es un agregado de esta guía; es parte de la práctica recomendada.
**[F: TBD-1]**

### 4. Corregir, y solo eso

Nada de refactores oportunistas en el mismo pull request. Un cambio de corrección que toca quince
archivos es imposible de revisar y, sobre todo, imposible de revertir sin perder el arreglo.

```bash
git commit -m "fix: comparar la provincia sin distinguir mayúsculas

El filtro comparaba la cadena tal cual venía del formulario, de modo
que una provincia seleccionada con otra capitalización no coincidía.

Refs #142"
```

El cuerpo explica **por qué**; el *qué* ya está en el diff.

### 5. Pull request, revisión, squash merge

Queda un único commit en `main`. Anotar su SHA: es el que viaja.

### 6. Decidir la admisión (I3 + I1)

Con la release abierta, la corrección **no** entra por defecto. Se contrasta con los criterios de
admisión escritos en el escenario 03. Si entra, sigue el paso 7; si no, queda para la próxima
versión y se registra la decisión en el issue.

### 7. Cherry-pick a la release

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

Si el `git push` directo a `release/1.0` no es rechazado, la protección del escenario 00 está mal
configurada: es el mismo control que la guía existe para instalar.

El `-x` deja el SHA original en el mensaje del commit nuevo. Sirve para que una persona rastree el
origen leyendo la historia; **no** es lo que verifica la auditoría de convergencia del escenario 07,
que compara por contenido con `git cherry`.

### 8. Nueva candidata y revalidación

```bash
git tag -a v1.0.0-rc2 -m "Candidata 2: incluye la corrección de #142"
git push origin v1.0.0-rc2
```

I1 revalida el caso sobre `rc2` y cierra el issue.

## Qué observar

- **El SHA cambia.** `git log -1 release/1.0` muestra un commit con contenido idéntico y hash
  distinto, con la línea `(cherry picked from commit …)`. Comparar ramas por SHA no sirve; por eso
  existe la auditoría.
- **El pipeline de la rama de release corre otra vez.** Un cherry-pick que aplica limpio no garantiza
  que el resultado funcione en el contexto de la release. **[F: TBD-1]**
- **Lo que NO viajó.** `git log --oneline main ^release/1.0` lista los commits que quedaron fuera:
  ahí se ve, concretamente, que las funcionalidades posteriores al corte no se arrastraron.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El cherry-pick no aplica limpio | El tronco divergió mucho de la release | Resolver el conflicto puntualmente, y tomar nota: la ventana de estabilización es demasiado larga |
| El cherry-pick arrastra más de lo esperado | El merge no fue con squash y el issue quedó en varios commits | Cherry-pickear el rango completo, y volver a la disciplina de squash |
| La corrección quedó solo en la release | Se corrigió directamente ahí «porque era más rápido» | Llevarla a `main` hoy mismo; si no, el defecto reaparece en la próxima versión **[F: GL-1]** |

## Verificación

1. La prueba que reproducía el defecto existe en `main` y falla si se revierte la corrección.
2. `git log --oneline release/1.0` muestra el commit con la referencia al SHA de `main`.
3. `v1.0.0-rc2` disparó una corrida completa sobre la rama de release, en verde.
4. El issue lo cerró I1 tras revalidar, no I2 al mergear.

---

Sigue: [03 — Corte de release](03-Corte-De-Release.md) si todavía no se hizo; si no,
[04 — Pull request que rompe la regresión](04-PR-Que-Rompe-La-Regresion.md).
