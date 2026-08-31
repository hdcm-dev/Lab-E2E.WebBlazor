---
doc_id: GF-AX-PL
doc_type: anexo
title: Anexo — plantillas comentadas
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, po]
traces: [GF-08, GF-06]
---

# Plantillas

Cuatro plantillas, cada una con las preguntas que guían sus campos. Copiarlas sin entender qué
resuelve cada bloque las convierte en formulario, que es como mueren los procedimientos.

## Issue

```markdown
## Contexto
Qué problema o necesidad origina este trabajo.

## Criterio de aceptación
Dado <estado inicial>, cuando <acción>, entonces <resultado verificable>.
(Uno o varios. Sin este bloque el issue no pasa a "Listo para tomar".)

## Fuera de alcance
Lo que explícitamente no entra, para que no se discuta en la revisión.
```

**Preguntas que guían el criterio de aceptación:** ¿alguien que no participó de la conversación
podría verificarlo sin preguntar nada? ¿Incluye el caso vacío y el caso de error, o solo el feliz?

## Pull request

```markdown
Closes #142

## Qué cambia
Una o dos líneas.

## Cómo probarlo
Pasos concretos, escritos para quien va a verificar.

## Checklist
- [ ] Pruebas agregadas o actualizadas
- [ ] Sin configuración dependiente del ambiente en el código
- [ ] Migración de datos reversible (o no aplica)
- [ ] El cambio es autocontenido y revertible por sí solo
```

El bloque **cómo probarlo** es el de mayor rendimiento de toda la plantilla: es literalmente el caso
que A-QA va a ejecutar en homologación, escrito por quien más sabe del cambio y en el momento en que
más fresco lo tiene.

**Preguntas que guían el checklist:** si este pull request se revierte mañana, ¿queda algo roto? ¿La
configuración nueva funciona en los tres ambientes sin recompilar?

## Mensaje de commit

Según Conventional Commits **[F: CC-1]**, que es lo que permite derivar el registro de cambios del
historial:

```
fix: contemplar fracción en el cálculo de superficie

La superficie se calculaba sobre el total sin aplicar el porcentaje
de fracción del inmueble, de modo que los lotes fraccionados
informaban una superficie mayor a la real.

Refs #142
```

El cuerpo explica **por qué**; el *qué* ya está en el diff. Tipos en uso: `feat`, `fix`, `chore`,
`docs`, `refactor`, `test`, `ci`.

## Registro de release

Se escribe al cortar la rama, no al liberar:

```markdown
# Release 1.4

**Corte:** commit a3f9c21 del 2026-08-20
**Congelamiento:** 2026-08-28
**Pase previsto:** 2026-08-31
**Alcance:** #107, #115, #119
**Criterios de admisión:** del corte (20/08) al congelamiento (28/08, exclusive),
cualquier defecto reportado por QA; del congelamiento al pase, solo bloqueantes.
**Responsable de release:** <quien cumple A-OPS>
**Plan de pruebas:** <enlace>

## Cherry-picks aplicados
| SHA en main | Issue | Motivo | Candidata |
|---|---|---|---|
| a3f9c21 | #142 | Defecto bloqueante reportado por QA | rc2 |
```

Esa última tabla es la que vuelve trivial la auditoría de convergencia y la conversación de «por qué
esto entró y aquello no».
