---
doc_id: GF-09-03
doc_type: escenario-practico
title: 03 — Corte de release y candidata
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [devops, po, qa]
traces: [GF-09, GF-07]
---

# 03 — Corte de release (E-03 y E-04)

## Objetivo

Cortar una rama de release desde el commit que corresponde —no necesariamente el último—, numerar la
candidata, y fijar por escrito qué se admite en ella. Es el escenario que habilita al 02 y al 05.

**Roles:** I1 es A-OPS, I2 es A-PO y define el alcance, I3 es A-QA y prepara el plan de pruebas.

## Precondición

Escenario 01 terminado, con al menos tres commits en `main`. Al menos uno de ellos debe ser trabajo
que **no** se quiere liberar todavía: es lo que vuelve interesante el corte.

## Pasos

### 1. Decidir el alcance (I2) y el punto de corte (I1)

```bash
git checkout main
git pull --ff-only
git log --oneline -8
```

I2 marca hasta qué commit va la versión 1.0. Si el trabajo no deseado quedó **después** de ese
commit, el corte es directo. Si quedó en el medio, hay dos opciones: cortar antes y cherry-pickear
lo que sí va, o cortar en la punta y revertir lo que no va. Para la práctica conviene el primero.

### 2. Cortar, incluso hacia atrás

```bash
# Corte retroactivo: desde un SHA elegido, no desde la punta.
git checkout -b release/1.0 <sha-elegido>
git push -u origin release/1.0
```

> **[F: TBD-1]** La rama de release se crea *just in time* y se puede cortar retroactivamente desde
> un commit anterior conocido como bueno. No hace falta que nadie congele nada.

### 3. Numerar la candidata

```bash
git tag -a v1.0.0-rc1 -m "Candidata 1 de la versión 1.0.0"
git push origin v1.0.0-rc1
```

El tag dispara el build **único** del artefacto. Ese artefacto es el que se promociona a homologación
y, si se aprueba, el mismo que va a producción.

### 4. Escribir los criterios de admisión (I1 + I2)

Se registra en la descripción de la rama o en el issue de release, con la
[plantilla de registro de release](../Estandares-Modelo-Ramas-Guide/Anexos/Plantillas.md). Los dos tramos se anclan a fechas, no a
duraciones relativas: I1 e I2 fijan acá y ahora la **fecha de congelamiento** y la **fecha de pase**.
Del corte al congelamiento (exclusive) se admite cualquier defecto reportado por QA; del
congelamiento al pase, solo bloqueantes. **[C]** Ver
[07](../Estandares-Modelo-Ramas-Guide/07-Integracion-Y-Versionado.md).

### 5. Plan de pruebas (I3)

I3 arma qué se va a verificar sobre la candidata: los criterios de aceptación de lo que entró, más el
recorrido exploratorio. La regresión automatizada ya corre sola sobre la rama; lo manual es lo que
I3 planifica.

### 6. Estabilizar y liberar la candidata (E-04)

El escenario no termina en la candidata: E-04 termina cuando **la versión se libera con su tag, o se
descarta**. Este tramo es el que produce el estado que los escenarios 05 y 07 dan por hecho, y es el
más delicado del modelo, así que se practica igual que el resto.

1. **Decisión de A-QA (I3).** I3 ejecuta el plan sobre la candidata promocionada a homologación y
   emite un veredicto escrito sobre `v1.0.0-rc1`: apta, o con defectos que van al escenario 02. Si
   hay defectos, se vuelve acá con `rc2` antes de seguir.
2. **Autorización de A-AUT (I2, con el criterio de riesgo).** Queda registrada en el issue de
   release: quién autoriza, sobre qué candidata y con qué criterio.
3. **Tag de versión final sobre el mismo commit de la candidata aprobada (I1).**

   ```bash
   git fetch --tags
   # El tag final va sobre el MISMO commit que la candidata: si apunta a otro, lo liberado
   # no es lo que aprobó A-QA.
   git tag -a v1.0.0 -m "Versión 1.0.0" "$(git rev-list -n1 v1.0.0-rc1)"
   git push origin v1.0.0
   test "$(git rev-list -n1 v1.0.0)" = "$(git rev-list -n1 v1.0.0-rc1)" && echo "mismo commit ok"
   ```

4. **Promoción del artefacto (I1).** Se despliega a producción **el binario de `v1.0.0-rc1`**, no una
   recompilación: se compara el `sha256sum` del binario desplegado contra el digest registrado para
   esa candidata. Ver [07](../Estandares-Modelo-Ramas-Guide/07-Integracion-Y-Versionado.md).

Al terminar, `v1.0.0` existe, apunta al commit de la candidata aprobada, y hay una versión liberada
que el escenario 05 puede parchear.

## Qué observar

- **El pipeline de `release/1.0`.** Debe correr la matriz completa igual que `main`. Si no corre, la
  rama de release está menos protegida que el tronco, que es exactamente al revés de lo que se busca.
- **Que `main` no se detuvo.** Mientras la release se estabiliza, el resto sigue integrando. Es la
  propiedad que justifica el modelo.
- **La precedencia del tag.** `v1.0.0-rc1` es anterior a `v1.0.0` para cualquier herramienta que
  compare versiones semánticas.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La release incluye trabajo no deseado | Se cortó en la punta por costumbre | Borrar la rama —todavía no hay nada encima— y volver a cortar desde el SHA correcto |
| Nadie sabe qué entra a la release | No se escribieron los criterios de admisión | Escribirlos antes del primer pedido de cherry-pick, no después |
| Hay tres ramas `release/*` vivas | No se borraron las que cayeron en desuso | Borrar; con más de dos, el riesgo de cherry-pickear a la equivocada es real **[F: TBD-1]** |

## Verificación

1. `release/1.0` existe en el remoto y su punta es el SHA elegido, no la punta de `main`.
2. El tag `v1.0.0-rc1` existe y disparó una corrida que produjo un artefacto.
3. La protección de rama aplica también sobre `release/*`: un `git push` directo a `release/1.0` es
   rechazado.
4. Los criterios de admisión están escritos, con fecha de congelamiento y de pase, y son accesibles
   para los tres integrantes.
5. El tag `v1.0.0` existe y apunta **al mismo commit** que `v1.0.0-rc1`:
   `test "$(git rev-list -n1 v1.0.0)" = "$(git rev-list -n1 v1.0.0-rc1)"`.
6. Está registrada la autorización de A-AUT y el digest del artefacto promocionado a producción.

---

Sigue: [04 — Pull request que rompe la regresión](04-PR-Que-Rompe-La-Regresion.md).
