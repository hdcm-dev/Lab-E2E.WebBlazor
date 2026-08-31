---
doc_id: GHF-03
doc_type: escenario-practico
title: 03 — Pull request que rompe la regresión
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, qa]
traces: [GHF-IDX, GF-08]
---

# 03 — Pull request que rompe la regresión (E-08)

## Objetivo

Ver al pipeline rechazar un cambio plausible que rompe otra pantalla, y discutir si lo que está mal
es el cambio o la prueba. Es el escenario que motivó todo este cuerpo documental, y en GitHub Flow
importa más que en cualquier otro modelo: es la **única** barrera entre el pull request y
producción.

**Roles:** I1 es A-DEV y rompe a propósito, I2 es A-REV, I3 es A-QA y lee el reporte.

## Precondición

Escenarios 01 y 02 terminados. La suite completa en verde sobre `main`.

## Pasos

### 1. Un cambio plausible que rompe otra cosa (I1)

El ejemplo sale del comportamiento real de la aplicación sembrada. El listado del ABM de
localidades se ordena por antigüedad —`RepositorioDeLocalidades.ListarAsync` usa
`.OrderBy(l => l.Id)`—, y el desplegable de localidades de la **encuesta** se alimenta de ese mismo
listado. **[E]**

El cambio: mostrar primero las altas más recientes, o sea `.OrderByDescending(l => l.Id)`. Es una
mejora de usabilidad defendible y no rompe ninguna prueba del ABM, porque `LocalidadesTests` ubica
sus filas por texto con `Filter(HasText = "Goya")` y no por posición. **[E]**

Lo que rompe está en la otra pantalla:
`EncuestaTests.ElDesplegableDeLocalidadesSeAlimentaDelAbm` afirma que la primera opción real del
desplegable es `"Corrientes (Corrientes)"` usando `opciones.Nth(1)`. Invertido el orden, la primera
pasa a ser `"Resistencia (Chaco)"` y esa prueba —y solo esa— falla. **[E]**

```bash
git checkout main
git pull --ff-only
git checkout -b feature/151-listado-mas-recientes-primero
# src/MovilidadUrbana.Web/Infraestructura/Persistencia/RepositorioDeLocalidades.cs
#   .OrderBy(l => l.Id)  →  .OrderByDescending(l => l.Id)
git push -u origin feature/151-listado-mas-recientes-primero
```

### 2. Abrir el pull request y esperar

Sin tocar nada más.

### 3. Leer la evidencia antes que el código (I3)

La evidencia de la corrida es el **TRX** de cada configuración, que el workflow sube como artefacto,
más la tabla de contadores del resumen. El TRX trae, por cada caso fallido, el mensaje de la
aserción y su pila: dice qué texto esperaba y cuál encontró. No hay reporte HTML ni traza navegable
—eso es del runner de JavaScript, y esta suite es el proyecto .NET
`tests/MovilidadUrbana.E2ETests`—. **[E]**

### 4. Decidir qué está mal (los tres)

No tiene respuesta única, y esa es la discusión formativa:

- Si el orden nuevo es el correcto, la prueba de la encuesta afirmaba por posición algo que nunca
  fue regla de negocio. Se corrige la prueba —que ubique la opción por texto, como las del ABM— y se
  documenta que el orden del listado no es contractual.
- Si el desplegable sí depende del orden del ABM, el cambio está incompleto: se ordena el
  desplegable por su cuenta.

### 5. El merge, bloqueado mientras tanto

Con la protección del escenario 00, el botón no está disponible. Comprobar que **nadie** puede
saltearlo, administradores incluidos.

## Qué observar

- **Que el rojo aparece antes del merge, no después.** En este modelo no hay una segunda red: si el
  pipeline no lo hubiera detenido, el cambio estaría desplegado.
- **Cuánto tardó el pipeline en dar el rojo.** Ese tiempo es lo que un desarrollador espera para
  saber si rompió algo; si es demasiado, la gente empieza a mergear sin mirar.
- **Que falló una prueba de otra pantalla.** Es el argumento entero a favor de la regresión
  automatizada: nadie de los tres habría probado la encuesta al revisar un cambio en el ABM.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La corrida queda en rojo por intermitencia y no por el cambio | Espera fija o dependencia de orden entre pruebas | Corregir la prueba: una regresión intermitente termina ignorada, y ahí se pierde el control entero |
| Se «arregla» borrando la prueba que molesta | Confundir el síntoma con la causa | Decidir explícitamente cuál de las dos cosas estaba mal, y dejarlo escrito en el pull request |
| El check no bloquea el merge | El nombre del check obligatorio no coincide con el `name:` del job | Corregir la regla de protección |

## Verificación

1. Quedó registro de una corrida en rojo con el TRX descargable.
2. El merge estuvo bloqueado mientras el pipeline estuvo en rojo.
3. La decisión —corregir el cambio o corregir la prueba— está escrita en el pull request, con su
   motivo.

---

Sigue: [04 — Cambio grande con feature flag](04-Cambio-Grande-Con-Feature-Flag.md).
