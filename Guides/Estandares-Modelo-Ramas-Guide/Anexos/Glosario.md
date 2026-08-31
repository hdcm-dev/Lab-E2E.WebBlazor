---
doc_id: GF-AX-GL
doc_type: anexo
title: Anexo — glosario
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po, autoridad-de-cambio]
traces: [GF-03, GF-06, GF-07]
---

# Glosario

Términos que la guía usa con un significado preciso. Cuando dos personas discuten un modelo de ramas
sin haber acordado estas definiciones, la discusión no es sobre ramas.

## Vocabulario codificado

Los códigos que las tablas de la guía usan sin volver a explicarlos. Las listas completas están en
[01 — Marco de referencia](../01-Marco-De-Referencia.md); acá está lo mínimo para resolver una tabla
sin salir del recorrido.

| Prefijo | Qué nombra | Valores |
|---|---|---|
| **E-nn** | Escenario: situación de trabajo con disparador reconocible y final verificable | E-01 funcionalidad nueva · E-02 defecto antes de liberar · E-03 corte de versión · E-04 estabilización de la candidata · E-05 emergencia en producción · E-06 versión de demostración · E-07 mantenimiento sin efecto funcional · E-08 rechazo de un cambio |
| **C-n** | Contexto: lo que cambia la respuesta correcta dentro de un mismo escenario | C-1 sin release abierta · C-2 con release abierta · C-3 producción comprometida · C-4 varias versiones soportadas en paralelo |
| **A-XXX** | Actor: se define por lo que decide, no por el cargo | A-PO product owner · A-DEV desarrollo · A-REV revisión de código · A-QA prueba y verificación · A-OPS devops e ingeniería de releases · A-SEC seguridad · A-AUT autoridad de cambio |
| **I1, I2, I3** | Los tres integrantes del equipo de la guía práctica, que rotan por los actores | Ver la tabla de rotación de la [guía práctica](../../GitFlow-Practice-Guide/README.md) |

## Objetos del control de versiones

| Término | Definición | Dónde se trata |
|---|---|---|
| **Tronco** (`main`) | Rama única de larga vida donde converge todo el trabajo. Garantiza *integrable*: compila y la verificación está en verde. No garantiza *probado por QA* | [06](../06-Modelo-Adoptado.md) |
| **Rama corta** | Rama de vida breve para un cambio autocontenido. Nace del tronco y muere al mergear | [06](../06-Modelo-Adoptado.md) |
| **Rama de release** | Rama creada desde un punto elegido del tronco para estabilizar y liberar una versión | [07](../07-Integracion-Y-Versionado.md) |
| **Tag** | Puntero inmutable a un commit. Una rama se mueve; un tag no | [03](../03-Fundamentos-De-Git.md) |
| **Línea base** | Configuración formalmente revisada y aprobada que sirve de referencia para el desarrollo posterior; su modificación pasa por control de cambios formal **[F: ISO-12207]** | [07](../07-Integracion-Y-Versionado.md) |

## Objetos del despliegue

| Término | Definición |
|---|---|
| **Artefacto** | Resultado compilado y versionado del build. Es lo que se despliega |
| **Ambiente** | Infraestructura donde corre un artefacto. **Un ambiente no es una rama** |
| **Promoción** | Mover el *mismo artefacto ya construido* de un ambiente al siguiente. No implica recompilar |
| **Build hermético** | Build insensible a las bibliotecas y herramientas de la máquina que lo ejecuta: dos personas que construyen la misma revisión obtienen resultados idénticos **[F: SRE-1]** |
| **Candidata (RC)** | Artefacto propuesto para liberación, todavía no aprobado. `v1.4.0-rc2` |
| **Versión de demostración** | Artefacto etiquetado para mostrar trabajo no liberado. No soportado, no promocionable **[C]** |

## Operaciones

| Término | Definición |
|---|---|
| **Merge** | Une dos historias creando un commit con dos padres |
| **Squash merge** | Aplana todos los commits de una rama en uno solo sobre el destino |
| **Rebase** | Reescribe commits como si hubieran nacido de otro punto; cambia sus identificadores |
| **Cherry-pick** | Aplica un commit específico sobre otra rama, salteando los que ocurrieron antes de él pero después del corte |
| **Fix forward** | Política de corregir siempre primero en el tronco y propagar hacia las releases, nunca al revés |
| **Retorno** (*backport*) | Traer a la línea principal un cambio hecho primero en una rama de release. En este modelo es la excepción |
| **Feature flag** | Interruptor de configuración que permite integrar código incompleto al tronco manteniéndolo inactivo |

## Proceso

| Término | Definición |
|---|---|
| **Desplegar** | Operación técnica: poner un artefacto a correr en un ambiente |
| **Liberar** | Decisión de negocio: exponer una funcionalidad a los usuarios |
| **Criterio de aceptación** | Condición verificable, escrita antes del desarrollo, que define cuándo un issue está cumplido |
| **Criterios de admisión** | Reglas escritas sobre qué cambios entran a una release ya cortada |
| **Fidelidad del ambiente** | En qué medida un ambiente de prueba se parece o se desvía del de producción **[F: ISO-29119]** |
| **Autoridad de cambio** | Rol que autoriza un cambio a producción según su riesgo **[F: ITIL-1]** |
| **Auditoría de convergencia** | Control que verifica que todo cambio de una rama de release tenga equivalente en la línea principal |

## Alias

Sinónimos que circulan en el equipo y su término canónico en esta guía:

| Se dice | Acá se llama |
|---|---|
| «rama estable» | rama de release, o mejor: el tag |
| «subir a homologación» | promocionar el artefacto al ambiente de homologación |
| «backport» | retorno |
| «la rama de producción» | no existe: producción es un ambiente |
| «pasar a QA» | promocionar la candidata |
