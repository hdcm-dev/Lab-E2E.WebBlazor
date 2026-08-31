---
doc_id: GHF-05
doc_type: escenario-practico
title: 05 — Reversión
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, devops, qa]
traces: [GHF-IDX, GF-07]
---

# 05 — Reversión

## Objetivo

Sacar de producción un cambio que rompió algo, cuando corregir hacia adelante no llega a tiempo.
En GitHub Flow la reversión ocupa el lugar que en GitFlow ocupa la rama de hotfix, y conviene
haberla practicado antes de necesitarla.

**Roles:** I1 es A-OPS y decide, I2 es A-DEV y ejecuta, I3 es A-QA y confirma.

## Precondición

Escenario 04 terminado. Un cambio reciente mergeado en `main`, con su pull request identificable.

## Pasos

### 1. La decisión, y su criterio (I1)

Frente a un defecto en producción hay dos caminos, y elegir mal cuesta caro en las dos direcciones:

| Camino | Cuándo | Costo |
|---|---|---|
| Corregir hacia adelante ([escenario 02](02-Correccion-Hacia-Adelante.md)) | La causa está identificada y la corrección es chica y verificable | Un ciclo completo de pull request: reproducir, corregir, revisar, pipeline |
| Revertir | La causa no está clara, o el ciclo de corrección no entra en el tiempo tolerable | Se pierde también lo bueno que traía ese cambio |

El criterio que conviene fijar de antemano, porque en la emergencia no se discute bien: **si la
causa no está identificada en el tiempo que tolera el usuario, se revierte**. **[C]** Revertir no
es admitir derrota; es devolver el sistema a un estado conocido para poder pensar sin apuro.

### 2. Revertir el pull request (I2)

GitHub ofrece el botón *Revert* en el pull request mergeado, que abre uno nuevo con el cambio
inverso. Se prefiere esa vía a un `git revert` local por dos razones: queda enlazado al pull request
original, y pasa por el mismo pipeline que cualquier otro cambio.

```bash
# El equivalente por línea de comandos, si el botón no está disponible:
git checkout main
git pull --ff-only
git checkout -b revert/160-exportar-csv
git revert --no-edit <sha-del-merge> -m 1
git push -u origin revert/160-exportar-csv
```

El `-m 1` indica cuál de los dos padres del commit de merge se conserva: el de la rama principal.
Sin ese parámetro, `git revert` sobre un merge falla porque no puede adivinarlo solo.

### 3. El pipeline corre igual (I2)

Es tentador saltear la verificación «porque solo estamos volviendo atrás», y es un error: el revert
es un cambio como cualquier otro, y puede romper algo por su cuenta si en el medio entraron otros
cambios que dependían de lo revertido. Que la protección no admita excepciones, tampoco acá.

### 4. Confirmar en producción (I3)

I3 comprueba que el síntoma desapareció y que lo demás sigue funcionando.

### 5. Reabrir el trabajo (los tres)

El revert deja el problema resuelto y la funcionalidad perdida. Se reabre el issue original con lo
aprendido y, cuando vuelva, vuelve con la prueba que habría detectado esto. Un revert sin ese paso
garantiza que el mismo cambio se vuelva a mergear igual dentro de dos semanas.

## Qué observar

- **Cuánto tardó desde la decisión hasta el sistema sano.** Ese número es el que hay que comparar
  contra el del escenario 02: define cuál de los dos caminos es realista para este equipo.
- **Qué se perdió además del defecto.** Si el pull request revertido traía tres cosas y solo una
  estaba rota, se perdieron las otras dos. Es el argumento práctico a favor de los pull requests
  chicos: **[F: GOOG-1]** son más simples de revertir.
- **Si el revert fue limpio.** Cuando no lo es, casi siempre significa que otro cambio posterior se
  apoyó en el revertido, y ahí hay una lección sobre acoplamiento.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| `git revert` falla con «commit is a merge but no -m option was given» | Falta indicar el padre a conservar | Agregar `-m 1` |
| El revert entra sin pasar por el pipeline | Alguien levantó la protección «por la urgencia» | Registrar la excepción y reverificar; ver la vía de excepción del escenario 00 |
| La funcionalidad revertida vuelve idéntica semanas después | Nadie reabrió el issue con lo aprendido | El paso 5 no es opcional |

## Verificación

1. El síntoma desapareció de producción, confirmado por I3.
2. El revert entró por pull request, con el pipeline en verde.
3. El issue original está reabierto, con el motivo del revert y la prueba que falta.
4. `git log --oneline -3` en `main` muestra el commit de revert enlazado al pull request original.

---

Sigue: [06 — Vista previa para demostración](06-Vista-Previa-Para-Demostracion.md).
