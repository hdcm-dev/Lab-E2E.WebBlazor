---
doc_id: GHF-07
doc_type: escenario-practico
title: 07 — Cierre y auditoría
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, qa, devops, po]
traces: [GHF-IDX, GF-05, GF-06]
---

# 07 — Cierre y auditoría

## Objetivo

Revisar en qué estado quedó el repositorio, comprobar los pocos controles que este modelo admite, y
—lo más formativo— decidir con evidencia propia si GitHub Flow le sirve a este equipo.

**Roles:** los tres, juntos.

## Precondición

Escenarios 00 a 06 terminados.

## Pasos

### 1. Higiene de ramas

```bash
git fetch --all --prune
git ls-remote --heads origin
```

Lo esperable: **solo `main`**. Cualquier otra cosa es un hallazgo. En este modelo el criterio es más
estricto que en el de tronco con releases, donde conviven las ramas de release vivas; acá no hay
excepción posible, y una rama corta con más de una semana de vida se revisa en la reunión de equipo
**[C]**.

### 2. Los controles que acá no existen

Vale la pena recorrer explícitamente lo que **no** se puede auditar, porque es la contracara del
modelo:

| Control de la guía de GitFlow | Estado en GitHub Flow |
|---|---|
| Auditoría de convergencia release → tronco | Sin sentido: no hay ramas de release de las que algo pueda no volver |
| Trazabilidad de qué artefacto está en producción | Sin respuesta, salvo que el equipo registre despliegues por su cuenta **[C]** |
| Criterios de admisión a una versión | Sin objeto: no hay versión que cerrar |
| Autorización previa al pase | Sin lugar en el modelo: el merge es el pase |

Las dos últimas filas son las que deciden si este modelo es viable para un equipo con homologación
formal y autoridad de cambio. En este caso no lo es, y por eso la guía de estudio adopta otro; el
ejercicio sirve para que esa conclusión sea propia y no heredada.

### 3. Medir lo que pasó (los tres)

Con el repositorio a la vista, completar una tabla con datos, no con impresiones:

| Medición | Cómo se obtiene |
|---|---|
| Vida media de una rama | Fecha del primer commit contra fecha del merge, en los pull requests de los escenarios 01 a 04 |
| Tiempo del pipeline | Duración de las corridas en la pestaña *Actions* |
| Cuántas veces bloqueó el check | Corridas en rojo sobre pull requests: acá alcanza con el escenario 03 |
| Cuánto tardó una corrección de producción | Escenario 02, del reporte al despliegue |
| Cuánto tardó una reversión | Escenario 05, de la decisión al sistema sano |

### 4. La conversación que cierra la práctica

Tres preguntas, con las mediciones sobre la mesa:

1. **¿La suite alcanza?** El escenario 03 detuvo un cambio que rompía otra pantalla. ¿Qué defectos
   reales de los últimos meses habría detenido, y cuáles no? Lo que no cubre es lo que en otro
   modelo haría una etapa de homologación.
2. **¿El tiempo del pipeline permite integrar varias veces por día?** Si no, el modelo empuja a
   acumular, y una rama que acumula es la rama larga que este modelo dice no tener.
3. **¿Alguien necesita una versión anterior?** Con una sola respuesta afirmativa, GitHub Flow queda
   descartado: soporta una sola versión viva.

## Qué observar

- **Cuánto menos hubo que configurar.** Comparado con la preparación de la guía de GitFlow: sin
  workflow de release, sin auditoría de convergencia, sin protección sobre `release/*`, sin
  `CODEOWNERS` para archivos de release.
- **Qué preguntas quedaron sin respuesta** durante los escenarios. Cada una es un requisito que
  este modelo no cubre y que el equipo tendría que resolver por convención propia.
- **Si alguien extrañó la ventana de estabilización.** Es la pregunta central, y conviene
  responderla con el escenario 02 fresco.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| Quedan ramas viejas en el remoto | *Automatically delete head branches* apagado, o merges hechos a mano | Activarlo y borrarlas |
| Se concluye «GitHub Flow no sirve» sin datos | Se discutió con impresiones | Volver al paso 3: sin las mediciones, la discusión la gana quien habla más fuerte |
| Se concluye «GitHub Flow alcanza» ignorando la autorización de cambio | Se practicó en un laboratorio sin autoridad de cambio real | Revisar la tabla del paso 2: el modelo no tiene dónde ubicarla |

## Verificación

1. `git ls-remote --heads origin` muestra únicamente `main`.
2. La tabla de mediciones del paso 3 está completa, con números.
3. Las tres preguntas del paso 4 tienen respuesta escrita, con el dato que la sostiene.
4. Quedó registrada la conclusión del equipo sobre si este modelo le sirve, y en qué condiciones
   cambiaría.

---

Volver al [índice de la guía](README.md), o comparar con la
[guía práctica de GitFlow](../GitFlow-Practice-Guide/README.md).
