---
doc_id: GHF-04
doc_type: escenario-practico
title: 04 — Cambio grande con feature flag
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, po]
traces: [GHF-IDX, GF-05]
---

# 04 — Cambio grande con feature flag

## Objetivo

Integrar en tres pull requests una funcionalidad que no está terminada, sin que ningún usuario la
vea. Es el ejercicio que reemplaza a la rama larga, y la razón por la que la comparación de la guía
de estudio marca a GitHub Flow como modelo que **necesita** feature flags.

**Roles:** I1 es A-DEV, I2 es A-REV, I3 es A-PO y decide cuándo se enciende.

## Precondición

Escenario 03 resuelto y `main` en verde.

## Pasos

### 1. Partir el trabajo (I1 + I3)

**#160 — Exportar el listado de localidades a CSV**, estimado en cuatro o cinco días. En un modelo
con rama larga, esa rama viviría una semana y llegaría con conflictos. Acá se parte en incrementos
que entren cada uno o dos días:

| Pull request | Qué entra | Visible para el usuario |
|---|---|---|
| 1 | El interruptor, apagado, y su lectura desde configuración | No |
| 2 | La generación del CSV y sus pruebas, alcanzable solo con el interruptor encendido | No |
| 3 | El botón en la pantalla, detrás del mismo interruptor | No, hasta que I3 lo encienda |

La guía de estudio ya enuncia el principio para el modelo adoptado: una funcionalidad que tarda
semanas se parte en incrementos que entren cada uno o dos días, ocultos tras un feature flag; si no
se puede partir, el problema es de diseño de la solución y no del modelo de ramas.

### 2. El interruptor, primero (I1)

El valor sale de configuración por ambiente, nunca de compilación condicional ni del nombre de la
rama. En esta aplicación, la configuración ya viaja por variables de entorno y `appsettings`, que es
el mecanismo que corresponde.

```bash
git checkout main
git pull --ff-only
git checkout -b feature/160-interruptor-exportar-csv
```

Este pull request es chico y aburrido, y esa es la idea: **[F: GOOG-1]** los cambios chicos se
revisan más rápido y más a fondo, tienen menos probabilidad de introducir defectos, desperdician
menos trabajo si son rechazados y son más simples de revertir.

### 3. La funcionalidad, apagada (I1)

Segundo pull request. Las pruebas de la funcionalidad nueva encienden el interruptor por
configuración en su propio contexto; las de regresión siguen corriendo con el interruptor apagado y
tienen que seguir dando exactamente lo mismo que antes. Esa doble corrida es la prueba de que el
cambio está realmente oculto.

### 4. La entrada en la interfaz (I1)

Tercer pull request. Al terminar, la funcionalidad está completa en la rama principal y desplegada,
y ningún usuario la ve.

### 5. Encender (I3)

I3 cambia la configuración del ambiente. **No hay merge, no hay despliegue, no hay pull request**:
es el momento en que desplegar y liberar se separan de verdad, y conviene detenerse a mirarlo,
porque es el concepto que sostiene todo el modelo.

### 6. Retirar el interruptor

Un flag que nadie apaga es deuda. Una vez que la funcionalidad se dio por buena, el interruptor se
saca en un pull request propio. **[C]**

## Qué observar

- **Que `main` estuvo desplegable todo el tiempo**, incluso con la funcionalidad a medio hacer
  adentro. Es lo que permite que el modelo no necesite una rama de integración.
- **Cuántos conflictos hubo.** Con tres pull requests de uno o dos días, ninguno. Comparalo con lo
  que habría pasado con una rama de una semana.
- **Qué pasa si alguien enciende el flag en producción antes de tiempo.** Si la respuesta es «se ve
  a medio hacer», falta un control: quién puede tocar esa configuración es una decisión que hay que
  escribir. **[C]**

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| El flag se lee desde una constante en el código | Se resolvió con compilación en vez de configuración | Moverlo a configuración por ambiente: si no, encender exige desplegar |
| Las pruebas de regresión empiezan a fallar con el flag apagado | El cambio no quedó realmente oculto | Es un defecto, no un ajuste de pruebas |
| El flag sigue ahí seis meses después | Nadie lo agendó | Retirarlo es parte del trabajo, no un extra |

## Verificación

1. Tres pull requests mergeados, ninguno de más de dos días de vida.
2. La suite pasa en verde con el flag apagado y con el flag encendido.
3. Encender la funcionalidad no requirió ningún despliegue.
4. Existe un issue abierto para retirar el interruptor, con responsable.

---

Sigue: [05 — Reversión](05-Reversion.md).
