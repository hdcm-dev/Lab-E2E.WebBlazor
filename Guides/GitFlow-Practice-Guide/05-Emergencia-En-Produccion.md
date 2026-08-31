---
doc_id: GF-09-05
doc_type: escenario-practico
title: 05 — Emergencia en producción
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, devops, autoridad-de-cambio]
traces: [GF-09, GF-06]
---

# 05 — Emergencia en producción (E-05)

## Objetivo

Ejercitar la única excepción del modelo —ramar desde el tag de producción— y, sobre todo, el paso que
se olvida: el retorno de la corrección a la línea principal el mismo día. Es el único error de este
modelo que sale realmente caro.

**Roles:** I1 es A-DEV, I2 es A-OPS, I3 es A-AUT y aprueba con criterio de emergencia.

## Precondición

La versión `v1.0.0` está liberada —tag creado y artefacto promocionado— y `main` ya avanzó con
trabajo posterior al corte. Ese estado lo produce el **paso 6 del escenario 03**; si no se hizo, hay
que hacerlo ahora, porque sin el tag `v1.0.0` el primer comando de este escenario falla con
«pathspec did not match». Contexto **C-3**.

## Pasos

### 1. Confirmar que es una emergencia

La vía de excepción se activa **solo** si hay usuarios afectados ahora —servicio caído o degradado—
o hay una vulnerabilidad siendo explotada. Las dos condiciones se responden con sí o no mirando un
hecho registrado: un incidente abierto, una alerta, un aviso de seguridad. Un defecto molesto pero
tolerable no califica: va por el circuito normal del escenario 02.

Que un cherry-pick desde `main` no aplique limpio **no** es una emergencia: es un problema técnico de
portabilidad, se resuelve conflicto por conflicto dentro del circuito normal, y se anota que la
ventana de estabilización se está haciendo larga. Ver
[06](../Estandares-Modelo-Ramas-Guide/06-Modelo-Adoptado.md).

Para la práctica: simular que la aplicación agota el tiempo de espera al listar localidades cuando la
base tiene muchos registros.

### 2. Ramar desde el TAG, no desde la punta de la release

```bash
git fetch --tags
git checkout -b hotfix/199-timeout-listado v1.0.0
```

El motivo de que sea el tag y no `release/1.0`: la punta de la rama de release puede tener
correcciones ya mergeadas pero **todavía no liberadas**. Si se rama de ahí, el hotfix arrastra a
producción cambios que nadie autorizó.

### 3. Corrección mínima, con su prueba

Lo mínimo que resuelve el incidente. Nada más. Cualquier mejora adicional entra después por el
circuito normal.

```bash
git push -u origin hotfix/199-timeout-listado
```

### 4. Pull request contra la rama de release, aprobación de emergencia (I3)

> **[F: ITIL-1]** La autoridad de aprobación se asigna según el riesgo del cambio. Una aprobación de
> emergencia es legítima: lo que no es opcional es la revisión posterior a la implementación.

La verificación automática corre igual, aunque acotada: la matriz completa puede correr después, sin
bloquear el despliegue.

### 5. Nueva versión de parche y despliegue (I2)

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

### 6. El retorno, el mismo día

```bash
git checkout main
git pull --ff-only
git checkout -b fix/199-retorno-timeout
git cherry-pick -x <sha-del-hotfix>
git push -u origin fix/199-retorno-timeout
# Pull request a main, revisión normal
```

Sin este paso, el defecto reaparece en la próxima versión y nadie va a entender por qué. La auditoría
del escenario 07 existe precisamente para detectar cuando este paso falta.

### 7. Revisión posterior a la implementación (los tres)

Media hora, con una sola pregunta de fondo: por qué no se detectó antes. La salida esperada no es un
culpable, es una prueba de regresión nueva o un control de pipeline nuevo.

## Qué observar

- **Qué contiene el tag y qué contiene la punta de la release.** `git log --oneline v1.0.0..release/1.0`
  muestra la diferencia; si esa lista no está vacía, ramar de la punta habría desplegado eso también.
- **El registro de la aprobación de emergencia.** Quién, cuándo y con qué alcance.
- **La ventana entre el despliegue del parche y el retorno a `main`.** El objetivo es horas, no días.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El hotfix arrastró cambios no autorizados | Se ramó de `release/1.0` en lugar del tag | Rehacer desde el tag; verificar qué se desplegó |
| El retorno a `main` quedó para «mañana» | La urgencia terminó al desplegar | Hacerlo antes de cerrar el incidente: es parte del incidente, no una tarea posterior |
| El retorno genera conflicto | `main` ya cambió esa zona del código | Resolverlo a mano; el resultado importa más que la limpieza del historial |

## Verificación

1. Existe el tag `v1.0.1`, apunta a un commit de `release/1.0`, y
   `git log --oneline v1.0.0..v1.0.1` contiene **solo** el hotfix: ningún cambio no autorizado viajó
   a producción con el parche.
2. El commit del hotfix figura en `main` con la referencia a su SHA original.
3. La auditoría de convergencia pasa en verde (escenario 07).
4. Quedó registrada la aprobación de emergencia y el acta breve de la revisión posterior.

---

Sigue: [06 — Versión de demostración](06-Version-De-Demostracion.md).
