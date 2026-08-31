---
doc_id: GF-09-07
doc_type: escenario-practico
title: 07 — Cierre, auditoría de convergencia y retrospectiva
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po]
traces: [GF-09, GF-06]
---

# 07 — Cierre y auditoría

## Objetivo

Comprobar que después de seis escenarios el repositorio quedó en un estado sano: nada corregido en
una release quedó sin volver al tronco, no hay ramas huérfanas, y cada versión se puede rastrear
hasta su commit. Y cerrar la capacitación con una retrospectiva que produzca cambios concretos.

**Roles:** los tres, juntos.

## Precondición

Escenarios 00 a 06 completados.

## Pasos

### 1. Auditoría de convergencia

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

### 2. Higiene de ramas

```bash
git ls-remote --heads origin
```

Lo esperable al cierre: `main` y, a lo sumo, las ramas de release vivas. Toda rama corta debería
haber desaparecido al mergear su pull request. Si aparece alguna con semanas de vida, es material
para la retrospectiva. **[F: TBD-2]**

### 3. Trazabilidad de versiones

```bash
git tag --sort=-creatordate | head
git show --no-patch --format='%H %ci %s' v1.0.1
```

Para cada tag hay que poder responder de qué commit salió, qué contiene respecto del anterior y quién
autorizó su despliegue. Si alguna respuesta requiere reconstruir la historia a mano, falta
trazabilidad.

### 4. Comparar releases

```bash
git log --oneline v1.0.0..v1.0.1     # qué agregó el parche
git log --oneline main ^release/1.0  # qué quedó fuera de la release
```

La segunda lista es la más instructiva: son las funcionalidades que **no** se arrastraron al hacer
cherry-pick, que es justo la objeción que el escenario 02 discutía en abstracto.

### 5. Retrospectiva

Cuatro preguntas, treinta minutos, y una salida escrita:

1. ¿Cuál fue el pull request más grande, y cuánto tardó su revisión?
2. ¿Cuánto tiempo pasó entre el despliegue del hotfix y su retorno a `main`?
3. ¿Qué falla detectó el pipeline que la revisión humana no habría detectado?
4. ¿Qué regla del modelo costó más sostener, y qué haría falta para que sea fácil?

La salida no es un acta: son uno o dos cambios concretos —un control nuevo en el pipeline, un ajuste
en los criterios de admisión, un umbral de tamaño de pull request— con responsable.

## Qué observar

- **Que la auditoría encuentre algo.** Si el escenario 05 se hizo completo, no debería; si se saltó
  el retorno a propósito, el control tiene que detectarlo. Vale la pena probar las dos cosas.
- **La diferencia entre comparar por SHA y comparar por contenido.** Los SHA difieren siempre tras un
  cherry-pick; `git cherry` compara el cambio, no el hash.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| `git cherry` marca todo con `+` | Se compara contra la rama equivocada, o `main` está desactualizado | `git fetch` y repetir con las referencias remotas |
| La auditoría automática no detecta un hotfix sin retorno | La rama no cae en el patrón `origin/release/*` que audita el workflow; o el checkout fue superficial y `git cherry` no tiene historia que comparar; o el cambio se reescribió (rebase, squash con contenido distinto) y ya figura como equivalente | Reproducirlo en orden: `git branch -r --list 'origin/release/*'`, después `fetch-depth: 0`, y por último `git cherry -v main <rama>` a mano. **No** es por falta de `-x`: el control compara contenido, no mensajes |
| La auditoría queda en rojo por un retorno legítimo | El retorno se resolvió a mano y el contenido difiere, así que el commit queda marcado `+` para siempre | Declararlo en el mensaje del commit de la release con la línea `Convergencia: <sha-en-main> (retorno con conflicto resuelto)`, que es lo único que la auditoría excluye **[C]** |
| Quedan ramas de release viejas | No se borran al caer en desuso | Borrarlas **[F: TBD-1]** |

## Verificación

Estado final esperado del repositorio de práctica:

1. `git cherry -v main release/1.0` no arroja ningún `+` salvo los que lleven la línea
   `Convergencia:` en su mensaje, que es donde se registra la explicación.
2. Solo quedan `main` y las ramas de release vivas.
3. Cada tag se puede rastrear hasta su commit y su corrida de pipeline.
4. La retrospectiva produjo al menos un cambio concreto con responsable asignado.

---

Vuelve al [índice de la guía práctica](README.md) o al
[índice general](../Estandares-Modelo-Ramas-Guide/README.md).
