---
doc_id: GHF-06
doc_type: escenario-practico
title: 06 — Vista previa para demostración
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [devops, po, desarrollo]
traces: [GHF-IDX, GF-07]
---

# 06 — Vista previa para demostración (E-06)

## Objetivo

Mostrarle a alguien de afuera del equipo un trabajo que todavía no está integrado, sin tocar lo que
está en producción ni inventar una rama que sobreviva a la demostración.

**Roles:** I1 es A-OPS, I2 es A-PO y pide la demo, I3 es A-DEV y expone el trabajo.

## Precondición

Un pull request abierto con trabajo mostrable. Sirve el tercero del escenario 04, antes de encender
el interruptor.

## Pasos

### 1. El pedido (I2)

«Necesito mostrar la exportación a CSV en la reunión del jueves.» En un modelo con ramas de release
la respuesta sería un tag de demostración sobre un commit elegido. Acá no hay versionado que
aprovechar —el modelo no lo define **[F: GH-1]**—, así que la unidad de demostración es **el pull
request**.

### 2. Levantar la aplicación desde la referencia del pull request (I1)

La aplicación se publica y se corre desde el commit de la rama del pull request, no desde `main`:

```bash
git fetch origin
git checkout -b demo/160 origin/feature/160-boton-exportar-csv
scripts/publicar.sh
```

Y se prueba contra ese binario, o se lo despliega en una máquina aparte. El workflow reutilizable de
la aplicación ya acepta una referencia arbitraria: `e2e.yml` declara la entrada `referencia` —«Rama,
tag o SHA a probar»—, de modo que la corrida se puede pedir sobre el commit del pull request. Y para
verificar un ambiente ya levantado está `verificacion-entorno.yml`, que invoca al mismo `e2e.yml`
pasándole `url-base`. **[E]** Con eso, la demostración se puede verificar antes de
mostrarla, que es lo que evita el papelón.

### 3. Encuadrar la demostración (I3)

Decir en voz alta tres cosas antes de empezar, y no es formalidad: es lo que evita que una demo se
convierta en un compromiso.

- Qué se está mostrando: el pull request número tanto, no la aplicación en producción.
- Que no está verificado por A-QA ni autorizado por nadie.
- Que puede cambiar o no llegar a integrarse.

### 4. Desarmar (I1)

El ambiente de demostración se destruye y la rama local se borra. Lo que **no** hay que hacer es
dejar una rama `demo/…` viva en el remoto: una rama de demostración que sobrevive a la demostración
se convierte, en dos semanas, en la rama larga que este modelo dice no tener.

## Qué observar

- **Que nada de esto tocó `main`.** La demostración no es un despliegue.
- **Qué se pierde sin versionado.** Nadie puede volver a levantar exactamente lo que se mostró el
  jueves, salvo que se anote el SHA. Anotarlo en el issue es la compensación barata. **[C]**
- **Cuánto costó levantar el ambiente.** Si cuesta media jornada, la próxima demo se va a hacer
  sobre producción, y ahí empiezan los problemas.

## Errores frecuentes

| Síntoma | Causa | Corrección |
|---|---|---|
| La demo se hace mergeando a `main` «para que se vea» | No había ambiente de vista previa | Es exactamente lo que este escenario evita: levantar desde la referencia del pull request |
| Queda una rama `demo/…` en el remoto | Nadie la borró al terminar | Borrarla; revisarlo en el escenario 07 |
| Lo mostrado no se puede reproducir después | No se registró el SHA | Anotarlo en el issue junto con la fecha |

## Verificación

1. La demostración corrió sobre el commit del pull request, no sobre `main`.
2. `git ls-remote --heads origin` no muestra ninguna rama `demo/…`.
3. El SHA mostrado quedó anotado en el issue.
4. Producción no cambió durante todo el escenario.

---

Sigue: [07 — Cierre y auditoría](07-Cierre-Y-Auditoria.md).
