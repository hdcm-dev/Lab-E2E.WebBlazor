---
doc_id: GF-09-04
doc_type: escenario-practico
title: 04 — Pull request que rompe la regresión
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops]
traces: [GF-09, GF-08]
---

# 04 — Pull request que rompe la regresión (E-08)

## Objetivo

Provocar deliberadamente el problema que originó esta guía —un cambio que rompe funcionalidad que
andaba— y comprobar que el control lo detiene **antes** del merge. Es el escenario más importante de
la práctica, y el único que se hace rompiendo algo a propósito.

**Roles:** I1 es A-DEV y rompe, I2 es A-REV, I3 es A-QA y observa el reporte.

## Precondición

Escenario 01 terminado. La aplicación sembrada ya trae las pruebas de extremo a extremo que cubren el
listado de localidades (`LocalidadesTests`), el asistente de encuesta (`EncuestaTests`) y la
navegación (`NavegacionTests`); son las que este escenario va a poner a trabajar.

## Pasos

### 1. Un cambio plausible que rompe otra cosa (I1)

La clave es que el cambio **parezca razonable** y que la prueba que rompe sea de **otra pantalla**.
El ejemplo se elige a partir del comportamiento real de la aplicación sembrada, no de una regla
inventada: el listado del ABM de localidades se ordena por antigüedad
(`RepositorioDeLocalidades.ListarAsync` usa `.OrderBy(l => l.Id)`), y el desplegable de localidades
de la **encuesta** se alimenta de ese mismo listado.

El cambio: *mostrar primero las altas más recientes en el ABM*, es decir `.OrderByDescending(l =>
l.Id)`. Es una mejora de usabilidad defendible y, sobre todo, **no rompe ninguna prueba del ABM**:
`LocalidadesTests` localiza sus filas por texto (`Filter(HasText = "Goya")`), no por posición.

Lo que rompe está en la otra pantalla: `EncuestaTests.ElDesplegableDeLocalidadesSeAlimentaDelAbm`
afirma que la primera opción real del desplegable es `"Corrientes (Corrientes)"` usando
`opciones.Nth(1)`. Invertido el orden, la primera pasa a ser `"Resistencia (Chaco)"` y esa prueba
—y solo esa— falla.

```bash
git checkout main
git pull --ff-only
git checkout -b feature/151-listado-mas-recientes-primero
# src/MovilidadUrbana.Web/Infraestructura/Persistencia/RepositorioDeLocalidades.cs
#   .OrderBy(l => l.Id)  →  .OrderByDescending(l => l.Id)
# ... más la prueba propia del ABM, en verde ...
git push -u origin feature/151-listado-mas-recientes-primero
```

Antes de dictar la práctica conviene confirmar el rojo esperado corriendo la suite con el cambio
aplicado: la única prueba fallida tiene que ser `ElDesplegableDeLocalidadesSeAlimentaDelAbm`.

### 2. Abrir el pull request y esperar el pipeline

Sin tocar nada más. Lo que sigue es lo que hay que mirar.

### 3. Leer el reporte antes que el código (I3)

La evidencia de la corrida es el **TRX** de cada configuración, que el workflow sube como artefacto
`resultados-<configuracion>`, más la tabla de contadores que `e2e.yml` escribe en el resumen de la
corrida. El TRX trae, por cada caso fallido, el mensaje de la aserción y su pila: para esta rotura
dice qué texto esperaba y cuál encontró, que es exactamente lo que hace falta para decidir sin
abrir el código.

Conviene saber qué **no** hay, porque el reflejo de buscarlo cuesta tiempo: con el binding de .NET
no existen el reporte HTML ni la traza navegable que genera el runner de JavaScript. El proyecto
sembrado no instrumenta `Context.Tracing`, así que no se producen trazas ni capturas. Si el equipo
las quiere, hay que agregarlas explícitamente en la clase base de las pruebas y subirlas como un
artefacto más; es una mejora razonable, pero es trabajo, no una casilla de configuración.

### 4. Decidir qué está mal: el cambio o la prueba (los tres)

Es la discusión formativa del escenario, y no tiene respuesta única:

- Si el orden nuevo es el correcto, la prueba de la encuesta estaba afirmando por posición algo que
  nunca fue una regla de negocio: se corrige la prueba —que localice la opción por texto, como hacen
  las del ABM— y se documenta que el orden del listado no es contractual.
- Si el desplegable de la encuesta sí depende del orden del ABM, el cambio está mal o está
  incompleto: se corrige el cambio, o se ordena el desplegable por su cuenta.

Lo que **no** es una opción es mergear con la regresión en rojo, ni marcar la prueba como salteada
para desbloquear el merge. Una prueba salteada es una regresión que nadie va a mirar.

### 5. Corregir y volver a la cola

Se ajusta lo que corresponda, el pipeline vuelve a correr y recién con todo en verde se mergea.

## Qué observar

- **El botón de merge bloqueado.** Es la diferencia entre un pipeline que informa y un pipeline que
  controla. Sin la protección del escenario 00, esto mismo habría sido un comentario que alguien
  podía ignorar.
- **Qué falló y qué no.** La prueba propia del cambio pasa; la que se rompe es de otra pantalla. Ese
  es exactamente el caso que la revisión humana no detecta leyendo el diff.
- **Cuánto tardó en detectarse.** Comparar con el tiempo que habría tardado en aparecer si el cambio
  se descubría en homologación tres días después.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La prueba falla en el runner pero no localmente | El runner corre la matriz completa; localmente se corrió un solo navegador | Reproducir con el mismo proyecto antes de concluir que es intermitencia |
| Se marca la prueba como salteada para desbloquear | Presión de tiempo | Revertir el salteo; si el cambio es urgente, se revierte el cambio, no el control |
| El pipeline queda en rojo por una intermitencia real | Espera fija o dependencia de orden entre pruebas | Corregir la prueba: una regresión intermitente termina siendo ignorada, y ahí se pierde el control entero |

## Verificación

1. Quedó registro de una corrida en rojo, con el TRX de la configuración fallida descargable.
2. El merge estuvo bloqueado mientras el pipeline estuvo en rojo.
3. La decisión —corregir el cambio o corregir la prueba— está escrita en el pull request, con su
   motivo.
4. Ninguna prueba quedó salteada.

---

Sigue: [05 — Emergencia en producción](05-Emergencia-En-Produccion.md).
