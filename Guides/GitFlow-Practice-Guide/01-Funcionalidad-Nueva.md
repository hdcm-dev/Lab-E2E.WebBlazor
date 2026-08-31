---
doc_id: GF-09-01
doc_type: escenario-practico
title: 01 — Funcionalidad nueva
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, po]
traces: [GF-09, GF-06, GF-08]
---

# 01 — Funcionalidad nueva (E-01)

## Objetivo

Recorrer el circuito completo de un cambio: issue con criterio de aceptación, rama corta, pull
request en borrador, revisión, squash merge, y cierre del issue por quien verifica —no por quien
programa—.

**Roles:** I1 es A-DEV, I2 es A-REV, I3 es A-QA y escribe el criterio junto a A-PO.

## Precondición

Escenario 00 terminado. `main` protegida y sin release abierta: contexto **C-1**.

## Pasos

### 1. El issue, antes que el código (I3 + A-PO)

Se crea el issue **#107 — Filtrar el listado de localidades por provincia** con criterio de
aceptación explícito:

> Dado un valor seleccionado en el filtro de provincia, el listado muestra únicamente las localidades
> de esa provincia. Con el filtro vacío, muestra todas. Si no hay coincidencias, se muestra el mensaje
> de listado vacío.

Un issue sin este bloque no pasa a *Listo para tomar*. La razón es práctica: ese texto es lo que I3
va a ejecutar después, y lo que I1 va a convertir en pruebas.

### 2. Rama corta desde la línea principal (I1)

```bash
git checkout main
git pull --ff-only
git checkout -b feature/107-filtro-por-provincia
```

El `--ff-only` es deliberado: si la copia local divergió del remoto, conviene que falle de manera
ruidosa en lugar de generar un merge silencioso.

### 3. Pull request en borrador con el primer commit

```bash
git commit -m "feat: agregar el filtro de provincia al listado de localidades

Refs #107"
git push -u origin feature/107-filtro-por-provincia
```

Se abre el pull request **en borrador**, con la [plantilla](../Estandares-Modelo-Ramas-Guide/Anexos/Plantillas.md) completa. El
pipeline arranca ahí, no al final.

### 4. Las pruebas, con el cambio

La funcionalidad se acompaña de pruebas de extremo a extremo sobre los tres casos del criterio: filtro
con resultados, filtro sin resultados, filtro vacío. Los selectores van por `data-testid`, siguiendo
la convención de la aplicación bajo prueba.

### 5. Revisión (I2)

I2 revisa dentro del día hábil. **[F: GOOG-2]** Si el pull request es demasiado grande para saber
cuándo habrá tiempo de revisarlo, la respuesta correcta es pedir que se parta, no dejarlo esperando.

### 6. Squash merge

Con el pipeline en verde y la aprobación registrada, se mergea con **squash**. La rama se borra
automáticamente. En `main` queda **un solo commit** con el número de issue en el cuerpo.

### 7. Cierre (I3)

I3 verifica el criterio de aceptación sobre el ambiente de integración y recién entonces cierra el
issue. Si el pull request decía `Closes #107`, el cierre es automático al mergear, y en ese caso I3
lo reabre si la verificación no pasa. Mergeado no es verificado.

## Qué observar

- **El commit único en `main`.** `git log --oneline -3` después del merge: un commit por issue.
  Anotar ese SHA, pero por el motivo contrario al que suena: **no** va a viajar a la release. Es una
  funcionalidad, y una funcionalidad no se cherry-pickea a una release abierta salvo que estuviera
  en su alcance —[06](../Estandares-Modelo-Ramas-Guide/06-Modelo-Adoptado.md)—. Es el commit que el escenario 02 va a mostrar en
  `git log --oneline main ^release/1.0` como ejemplo concreto de lo que **no** se arrastró. El que
  viaja es el commit del fix #142, y lo produce el propio escenario 02.
- **Cuándo empieza a correr el pipeline.** Con el pull request en borrador, no al marcarlo listo.
- **El tamaño del diff.** Anotar cuántas líneas tuvo y cuánto tardó la revisión; el escenario 04 usa
  esa comparación.
- **Qué pasa si I2 aprueba y mientras tanto alguien mergeó otra cosa.** Con «require branches to be
  up to date» activo, hay que actualizar la rama y el pipeline vuelve a correr.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El pull request muestra archivos que I1 no tocó | La rama no nació de `main` actualizado | Rehacer la rama desde `main` y volver a aplicar el cambio |
| El issue queda abierto tras el merge | Falta la palabra clave `Closes #107` en la descripción | Cerrarlo a mano; corregir la plantilla la próxima vez |
| Quedan tres commits en `main` | Se mergeó sin squash | Es tarde para revertirlo sin reescribir; anotarlo y ver el impacto en el escenario 02 |

## Verificación

1. `main` tiene exactamente un commit nuevo, con `#107` en el mensaje.
2. La rama remota `feature/107-filtro-por-provincia` ya no existe.
3. Las pruebas nuevas corren en la matriz completa disparada por el push a `main`.
4. El issue está cerrado por I3, con una nota de qué se verificó y dónde.

---

Sigue: [02 — Defecto con release abierta](02-Defecto-Con-Release-Abierta.md).
