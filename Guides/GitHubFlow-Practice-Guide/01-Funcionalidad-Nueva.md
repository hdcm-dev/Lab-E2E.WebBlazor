---
doc_id: GHF-01
doc_type: escenario-practico
title: 01 — Funcionalidad nueva
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, qa, po]
traces: [GHF-IDX, GF-08]
---

# 01 — Funcionalidad nueva (E-01)

## Objetivo

Recorrer los seis pasos documentados del modelo sobre un cambio real, y ver que en GitHub Flow el
merge es el final del recorrido: no hay ninguna etapa posterior donde el cambio espere.

**Roles:** I1 es A-DEV, I2 es A-REV, I3 es A-QA y escribe el criterio junto a A-PO.

## Precondición

Escenario 00 terminado, `main` protegida, sin ninguna otra rama viva.

## Pasos

Los seis pasos son los de la documentación del modelo **[F: GH-1]**, y acá se ejecutan con la
aplicación sembrada.

### 1. El issue, antes que el código (I3 + A-PO)

**#107 — Filtrar el listado de localidades por provincia**, con criterio de aceptación explícito:

> Dado un valor seleccionado en el filtro de provincia, el listado muestra únicamente las
> localidades de esa provincia. Con el filtro vacío, muestra todas. Si no hay coincidencias, se
> muestra el mensaje de listado vacío.

El modelo no exige el issue: es convención de este equipo **[C]**, y sostiene el paso 6, donde
alguien tiene que verificar contra algo escrito.

### 2. Rama con nombre corto y descriptivo (I1)

```bash
git checkout main
git pull --ff-only
git checkout -b filtro-por-provincia
```

La documentación pide un nombre **corto y descriptivo** **[F: GH-1]**; no impone prefijos. Este
equipo mantiene igual la convención `feature/`, `fix/`, `chore/` con el número de issue adelante,
porque es lo que hace rastreable un commit hasta su ticket sin abrir el tablero **[C]**. Elegí una
de las dos formas y sostenela: mezclarlas hace inútil cualquier filtro por nombre de rama.

### 3. Cambios, commits y pull request en borrador

```bash
git commit -m "feat: agregar el filtro de provincia al listado de localidades

Refs #107"
git push -u origin filtro-por-provincia
```

El pull request se abre **en borrador** con el primer commit. La documentación contempla
explícitamente marcarlo como borrador cuando se busca opinión temprana **[F: GH-1]**, y en este
laboratorio hay un motivo adicional: el pipeline arranca ahí, no al final.

### 4. Las pruebas, con el cambio

Tres casos del criterio —filtro con resultados, sin resultados y vacío—, con selectores por
`data-testid`, como el resto de la suite. En este modelo esa suite es lo único que separa un merge
de una regresión en producción: no hay homologación después.

### 5. Revisión (I2)

I2 revisa dentro del día hábil. **[F: GOOG-2]** Un pull request demasiado grande para saber cuándo
habrá tiempo de revisarlo se pide partido, no se deja esperando.

### 6. Merge y borrado de la rama

Con el pipeline en verde y la aprobación registrada, se mergea y **la rama se borra**. Los dos
últimos pasos del ciclo documentado son exactamente esos **[F: GH-1]**, y el borrado no es
cosmético: es lo que mantiene el repositorio con pocas ramas activas, que es la condición asociada
al buen desempeño de entrega **[F: DORA-1]**.

### 7. Cierre (I3)

I3 verifica el criterio sobre el ambiente donde quedó desplegado el merge y recién entonces cierra
el issue. Mergeado no es verificado, y en este modelo la distancia entre una cosa y la otra se mide
en minutos: razón de más para que la verificación exista.

## Qué observar

- **Que después del merge no queda nada pendiente.** En la guía de GitFlow, el commit de una
  funcionalidad quedaba esperando a ver si entraba o no en la release abierta. Acá esa pregunta no
  existe. Anotar el contraste: es el beneficio central del modelo.
- **Cuánto tiempo pasó entre el primer commit y el merge.** Con ramas de uno o dos días el modelo
  funciona; con ramas de dos semanas, la rama se vuelve la rama larga que el modelo dice no tener.
- **Qué pasa si I2 aprueba y mientras tanto alguien mergeó otra cosa.** Con «require branches to be
  up to date» activo, hay que actualizar la rama y el pipeline vuelve a correr.
- **Qué se desplegó, exactamente.** El modelo asume que lo mergeado se despliega, pero no dice
  quién lo hace ni cómo se sabe qué versión está arriba. Si el equipo no lo definió, esa pregunta
  queda sin respuesta y conviene registrarla para el escenario 07.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El pull request muestra archivos que I1 no tocó | La rama no nació de `main` actualizado | Rehacer la rama desde `main` y volver a aplicar el cambio |
| La rama sobrevive al merge | Falta *Automatically delete head branches* | Activarlo y borrarla a mano esta vez |
| El issue queda abierto tras el merge | Falta `Closes #107` en la descripción | Cerrarlo a mano; corregir la plantilla la próxima vez |

## Verificación

1. `main` tiene el commit de la funcionalidad y las pruebas nuevas.
2. La rama remota ya no existe.
3. El pipeline corrió sobre `main` después del merge, con la matriz completa.
4. El issue está cerrado por I3, con nota de qué verificó y dónde.

---

Sigue: [02 — Corrección hacia adelante](02-Correccion-Hacia-Adelante.md).
