---
doc_id: GHF-02
doc_type: escenario-practico
title: 02 — Corrección hacia adelante
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, qa, devops]
traces: [GHF-IDX, GF-05, GF-06]
---

# 02 — Corrección hacia adelante (E-05, sin rama de hotfix)

## Objetivo

Corregir un defecto que ya está en producción usando el único camino que este modelo ofrece: una
rama corta desde la rama principal, con su pull request. Sin rama de hotfix, sin tag del que
partir, sin retorno que recordar.

**Roles:** I1 es A-QA y reporta, I2 es A-DEV y corrige, I3 es A-REV.

## Precondición

Escenario 01 terminado y mergeado. Hay una funcionalidad en producción con la que romper algo.

## Pasos

### 1. El reporte (I1)

**#142 — El filtro de provincia distingue mayúsculas.** Buscando `corrientes` no aparece nada,
mientras que `Corrientes` sí. El reporte lleva pasos de reproducción y qué se esperaba.

En este modelo el reporte no necesita decir «en qué versión»: hay una sola cosa desplegada, la
punta de la rama principal. Es una simplificación real, y también una pérdida —cuando el defecto
aparece y se corrige el mismo día, después nadie puede reconstruir qué estuvo mal y por cuánto
tiempo, salvo que el equipo registre los despliegues por su cuenta **[C]**.

### 2. Reproducir con una prueba que falla (I2)

```bash
git checkout main
git pull --ff-only
git checkout -b fix/142-filtro-ignora-mayusculas
```

Primero la prueba, y tiene que fallar. Si pasa en verde a la primera, no se entendió el defecto: se
está probando otra cosa.

```bash
scripts/pruebas.sh chromium   # la prueba nueva en rojo, el resto en verde
```

### 3. La corrección, y solo eso

Nada de refactores oportunistas en el mismo pull request. Un cambio que toca quince archivos es
imposible de revisar y, sobre todo, imposible de revertir —y en este modelo el revert es el plan de
contingencia, como muestra el [escenario 05](05-Reversion.md)—.

```bash
git commit -m "fix: comparar la provincia sin distinguir mayúsculas

Refs #142"
git push -u origin fix/142-filtro-ignora-mayusculas
```

### 4. Revisión y merge (I3)

Mismo circuito que una funcionalidad: pipeline en verde, una aprobación, merge, rama borrada.

**Acá está la diferencia que da nombre al escenario.** En la guía de GitFlow, esta misma corrección
exigía decidir a qué rama de release iba, hacer el cherry-pick, generar una candidata nueva y
comprobar que el arreglo volviera al tronco —con un workflow dedicado a auditar que no se olvidara—.
Nada de eso ocurre acá: la corrección está en el único lugar donde puede estar, y por eso no puede
perderse. Es literalmente lo que la comparación de la guía de estudio resume en una celda: en
GitHub Flow, un defecto de producción se corrige en la rama principal.

### 5. Verificación en producción (I1)

I1 revalida el caso reportado sobre lo desplegado y cierra el issue.

## Qué observar

- **Cuánto tardó desde el reporte hasta la corrección desplegada.** Comparalo con el escenario 02
  de la guía de GitFlow, que necesita cherry-pick, candidata nueva y promoción. La diferencia es lo
  que este modelo compra.
- **Qué no se pudo hacer.** No hubo forma de corregir la versión anterior sin llevar también todo
  lo que se integró después. Si alguien pide exactamente eso, el modelo no alcanza y la respuesta
  está en [05 — Cómo elegir el modelo](../Estandares-Modelo-Ramas-Guide/05-Como-Elegir-El-Modelo.md):
  GitHub Flow soporta una sola versión viva.
- **Cuánto se confió en la suite.** Nadie probó a mano toda la aplicación antes de desplegar. Esa
  confianza es la condición del modelo, no un descuido.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La prueba nueva pasa antes de aplicar la corrección | No reproduce el defecto reportado | Volver al reporte y reproducir a mano primero |
| El pull request corrige y además refactoriza | Oportunismo | Partirlo: el arreglo primero, el refactor en otro |
| Se corrige sin prueba «porque es urgente» | El escenario 05 es el que corresponde, no este | Si de verdad es urgente, revertir; corregir con prueba después |

## Verificación

1. La prueba que reproduce el defecto está en `main` y falla si se revierte la corrección.
2. La rama se borró al mergear.
3. El issue lo cerró I1 tras revalidar, no I2 al mergear.
4. No existe ninguna rama de mantenimiento: `git ls-remote --heads origin` sigue mostrando solo
   `main`.

---

Sigue: [03 — Pull request que rompe la regresión](03-PR-Que-Rompe-La-Regresion.md).
