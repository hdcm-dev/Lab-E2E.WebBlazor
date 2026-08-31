---
doc_id: GF-AX-PF
doc_type: anexo
title: Anexo — preguntas que forman criterio
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po]
traces: [GF-06, GF-07]
---

# Preguntas que forman criterio

Este anexo existe para que, ante una situación no prevista, el equipo pueda razonar en lugar de
buscar una regla. Las respuestas son cortas a propósito: si hace falta más, el enlace lleva al
documento que lo desarrolla.

**¿Por qué no puedo ramar desde la rama en la que ya estaba parado?**
Porque arrastrás cambios ajenos sin querer: el pull request va a mostrar archivos que no tocaste y
quien revisa no va a poder distinguir tu trabajo del que viene de atrás. Además el cherry-pick
posterior deja de ser de un solo commit.

**Si la corrección nace de `main`, ¿no arrastro funcionalidades nuevas a la release?**
No, porque no se mergea `main` a la release: se cherry-pickea solo el commit de la corrección. El
cherry-pick saltea justamente los commits anteriores a él y posteriores al corte. La objeción es
válida contra el *merge* de rama a rama, no contra el cherry-pick. Ver
[03](../03-Fundamentos-De-Git.md).

**¿Y si el cherry-pick no aplica limpio?**
Es una señal, no un accidente: el tronco divergió mucho de la release, o sea que la release lleva
demasiado tiempo abierta. Se resuelve puntualmente, pero el aprendizaje es acortar la ventana de
estabilización.

**¿Por qué no corregir directamente en la rama de release, que es más rápido?**
Porque la corrección queda solo ahí y el defecto reaparece en la próxima versión. **[F: GL-1, TBD-2]**
El ahorro de cinco minutos se paga con un defecto que vuelve dentro de tres meses sin que nadie
entienda por qué.

**¿`main` está siempre lista para producción?**
Está siempre lista para *desplegar*, que no es lo mismo que *aprobada para liberar*. La diferencia la
marcan la validación de A-QA y la autorización de cambio, no el estado del pipeline.

**¿Necesito una rama estable?**
Ya la tenés: es `release/x.y`. Y lo verdaderamente estable no es ni siquiera esa rama —que se mueve
al recibir cherry-picks— sino el tag y su artefacto, que son inmutables.

**¿Qué hago con una funcionalidad que tarda tres semanas?**
Se parte en incrementos que entren al tronco cada uno o dos días, ocultos tras un feature flag. Si no
se puede partir, el problema es de diseño de la solución, no del modelo de ramas.

**¿Cuándo corto la rama de release?**
Lo más tarde posible, unos días antes de liberar. **[F: TBD-1]** Y si te olvidaste de cortarla en el
momento justo, podés cortarla retroactivamente desde el commit que corresponda: no hace falta que
nadie congele nada.

**¿Qué pasa si entra al tronco un commit que no quiero en la release?**
Nada: el corte retroactivo y el cherry-pick selectivo existen para eso. Lo que queda afuera puede
venir después por el mismo mecanismo. **[F: TBD-1]**

**¿A-QA mira la versión lanzada o la candidata?**
La candidata, salvo cuando hay un hotfix de la versión lanzada que validar. Ver
[07](../07-Integracion-Y-Versionado.md).

**¿Quién cierra el issue?**
Quien lo validó, cuando lo validó. Nunca quien lo programó, al mergear.

**¿Puedo saltear la revisión si el cambio es de una línea?**
No, pero la revisión de una línea toma un minuto. El problema real no es la revisión: es que se
acumulen cambios grandes que la vuelven costosa. **[F: GOOG-2]**

**¿Cuántas ramas de release puedo tener abiertas?**
Dos. Con más, aumenta el riesgo de cherry-pickear a la equivocada. **[F: TBD-1]**

**¿Este modelo sirve para cualquier equipo?**
No. Está pensado para una aplicación con despliegue frecuente y un ambiente de homologación formal.
Un producto instalable con cinco versiones soportadas en paralelo necesita ramas de larga vida, y ahí
GitFlow sigue siendo razonable. **[F: NVIE-1]** Ver [05](../05-Como-Elegir-El-Modelo.md).

**Entonces, ¿esta guía está a favor o en contra de GitFlow?**
Ninguna de las dos. GitFlow resuelve un problema —varias versiones soportadas en paralelo— que este
equipo hoy no tiene. Si algún día lo tiene, [04](../04-GitFlow.md) es el documento al que hay que
volver.
