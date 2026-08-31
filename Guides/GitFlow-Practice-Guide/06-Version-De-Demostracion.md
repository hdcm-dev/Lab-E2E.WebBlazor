---
doc_id: GF-09-06
doc_type: escenario-practico
title: 06 — Versión de demostración
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [devops, po]
traces: [GF-09, GF-07]
---

# 06 — Versión de demostración (E-06)

## Objetivo

Mostrar trabajo todavía no liberado sin crear una tercera línea de código que nadie audite. La
tentación es cortar una rama `demo`; el escenario existe para practicar la alternativa.

**Roles:** I1 es A-OPS, I2 es A-PO y pide la demostración, I3 es A-DEV y verifica qué entra.

## Precondición

`main` tiene trabajo integrado que no está en `release/1.0`. Existe un ambiente de demostración, o al
menos la capacidad de levantar uno efímero.

## Pasos

### 1. Elegir el commit, no la rama (I1 + I3)

```bash
git checkout main
git pull --ff-only
git log --oneline -10
```

Se elige un commit concreto de `main`. Lo que se muestra tiene que ser reproducible: «la punta de
`main` del martes» no es una referencia, un SHA sí.

### 2. Etiquetar con sufijo de precedencia

```bash
git tag -a v1.1.0-demo.1 -m "Demostración para la reunión del 30/08. No soportada."
git push origin v1.1.0-demo.1
```

El sufijo hace que la versión quede **por debajo** de `v1.1.0` en precedencia semántica
**[F: SEMVER-1]**, de modo que ninguna herramienta la confunda con una versión liberada.

### 3. Construir una sola vez y desplegar

El artefacto se construye por el mismo camino que cualquier otro: el build no cambia porque el
destino sea una demostración. Se despliega en el ambiente efímero o en el de demostración.

### 4. Declarar el alcance por escrito (I2)

En el issue o en el anuncio de la demostración:

- **no está soportada**: no recibe hotfix ni parches;
- **no se promociona** a producción bajo ninguna circunstancia;
- su tag **no se reutiliza**: la próxima demostración es `demo.2`;
- lo que se muestra puede cambiar antes de liberarse.

### 5. Dar de baja el ambiente

Terminada la demostración, el ambiente efímero se destruye. El tag queda: es barato y documenta qué
se mostró.

## Qué observar

- **Que no se creó ninguna rama.** `git branch -r` sigue mostrando `main` y `release/1.0`.
- **La precedencia del tag.** Cualquier herramienta que ordene versiones pone `v1.1.0-demo.1` antes
  que `v1.1.0`.
- **Que el artefacto se construyó con el mismo pipeline.** Si hizo falta un build especial «para la
  demo», el build no era hermético.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| Existe una rama `demo` con commits propios | Se cortó una rama en lugar de etiquetar | Llevar lo que valga a `main` por pull request y borrar la rama |
| Piden un arreglo «sobre la demo» | No se declaró que no está soportada | El arreglo va a `main` por el circuito normal y se genera `demo.2` |
| La demo terminó desplegada en producción | Se promocionó un artefacto no autorizado | Revertir; revisar quién puede promocionar a producción |

## Verificación

1. Existe el tag con sufijo y no existe ninguna rama nueva.
2. El artefacto de la demostración salió del mismo pipeline que los demás.
3. El alcance —no soportada, no promocionable— está escrito en algún lado consultable.
4. El ambiente efímero fue dado de baja.

---

Sigue: [07 — Cierre y auditoría](07-Cierre-Y-Auditoria.md).
