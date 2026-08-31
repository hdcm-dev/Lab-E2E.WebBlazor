---
doc_id: GF-01
doc_type: marco-de-referencia
title: Marco de referencia — escenarios, contextos y actores
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po, autoridad-de-cambio]
traces: [GF-00, GF-02]
---

# Marco de referencia

Todo el resto de la guía se apoya en tres listas cerradas: los **escenarios** en los que un equipo
toca el control de versiones, los **contextos** que cambian la respuesta correcta dentro de un mismo
escenario, y los **actores** que intervienen. Cuando un documento posterior dice «en el escenario
E-03, contexto C-2, el actor A-QA hace tal cosa», se refiere a estas tablas y a ninguna otra.

Fijar este vocabulario primero no es una formalidad. La mayoría de las discusiones sobre ramas se
traban porque dos personas usan la misma palabra para cosas distintas: uno llama «release» a una
rama y el otro a un tag, uno llama «estable» a lo que compila y el otro a lo que QA aprobó.

## Escenarios

Un escenario es una situación de trabajo que arranca con un disparador reconocible y termina con un
cambio incorporado —o descartado— de manera verificable.

| ID | Escenario | Disparador | Termina cuando |
|---|---|---|---|
| **E-01** | Funcionalidad nueva | Un issue con criterio de aceptación escrito | QA valida la funcionalidad en el ambiente correspondiente |
| **E-02** | Corrección de defecto detectado antes de liberar | Reporte de QA sobre la candidata o sobre integración | El caso que fallaba pasa, y la corrección viaja a la versión que corresponde |
| **E-03** | Corte de una versión | Decisión de liberar el alcance acumulado | Existe una rama de release y una candidata numerada |
| **E-04** | Estabilización de la candidata | La candidata está en el ambiente de prueba | La versión se libera con su tag, o se descarta |
| **E-05** | Emergencia en producción | Servicio caído, degradado o vulnerabilidad explotada | La versión de parche está desplegada **y** la corrección volvió a la línea principal |
| **E-06** | Versión de demostración | Pedido de mostrar trabajo todavía no liberado | Existe un artefacto identificable y desechable, sin comprometer el calendario |
| **E-07** | Mantenimiento sin efecto funcional | Dependencia vencida, cambio de build o de configuración | Integrado sin alterar el comportamiento observable |
| **E-08** | Rechazo de un cambio | La verificación automática o la revisión encuentran un problema | El cambio se corrige y vuelve a la cola, o se descarta con su motivo registrado |

Un escenario que no aparece acá no está previsto por el procedimiento; ante uno nuevo, la guía
propone razonar con las preguntas de [las preguntas frecuentes del anexo](Anexos/Preguntas-Frecuentes.md)
antes de inventar una regla.

## Contextos

El contexto es lo que hace que la respuesta cambie dentro de un mismo escenario. Son cuatro y
conviene tenerlos presentes porque son la fuente habitual del «depende» que frustra a quien recién
entra al equipo.

| ID | Contexto | Pregunta que lo determina | Qué cambia |
|---|---|---|---|
| **C-1** | Sin release abierta | ¿Existe hoy una `release/x.y` viva? | El cambio viaja solo por la línea principal; no hay cherry-pick que decidir |
| **C-2** | Con release abierta | Ídem | Cada cambio requiere una decisión explícita de admisión a esa release |
| **C-3** | Producción comprometida | ¿Hay usuarios afectados ahora? | Se habilita la vía de excepción: ramar desde el tag, aprobación de emergencia |
| **C-4** | Producto con varias versiones vivas | ¿Se **soportan** hoy **tres o más** versiones en paralelo, es decir, versiones que reciben parches? | Cambia el modelo de ramas completo: es el contexto donde GitFlow sigue siendo razonable |

Las dos magnitudes que conviene no confundir: **releases vivas simultáneas** —la que está en
producción y la candidata: dos como máximo, y es la operación normal de C-2, ver
[07](07-Integracion-Y-Versionado.md)— y **versiones soportadas en paralelo**, las que siguen
recibiendo parches. El disparador de cambio de modelo es esta segunda, con el mismo operador en todo
el cuerpo documental: **tres o más**. Dos versiones soportadas siguen dentro de C-2.

La distinción entre **C-2** y **C-4** es la que decide qué modelo de ramas conviene, y está tratada
en [05 — Cómo elegir el modelo](05-Como-Elegir-El-Modelo.md).

## Actores

Cada actor se define por lo que **decide**, no por su cargo. Una misma persona puede cubrir dos
funciones en un equipo chico; lo que no puede es cubrir las dos en el mismo cambio.

| ID | Actor | Decide | No decide |
|---|---|---|---|
| **A-PO** | Product owner / analista | Qué se construye, con qué criterio de aceptación, y la prioridad | Cómo se implementa; cuándo está probado |
| **A-DEV** | Desarrollo | Cómo se implementa; qué pruebas automatizadas acompañan al cambio | Si el cambio está verificado; si entra a una release |
| **A-REV** | Revisión de código | Si el cambio es comprensible, revertible y de tamaño razonable | Si cumple el criterio funcional |
| **A-QA** | Prueba y verificación | Si lo construido cumple el criterio; cuándo se cierra el issue | Qué se construye; cuándo se despliega |
| **A-OPS** | Devops / ingeniería de releases | Cómo se construye, versiona, promociona y despliega el artefacto | Si el contenido funcional es correcto |
| **A-SEC** | Seguridad | Qué controles corren en el pipeline y qué hallazgo bloquea | La prioridad funcional |
| **A-AUT** | Autoridad de cambio | Si un cambio se autoriza a producción según su riesgo | El detalle técnico de la implementación |

La tabla de actores es una convención de este equipo **[C]**: los nombres, los códigos y el reparto
de decisiones no provienen de ningún estándar.

Sobre esa tabla, dos separaciones que la guía sostiene en todos los escenarios, ambas también
convención de este equipo **[C]** —son segregación de funciones, y renunciar a ellas no rompe ningún
estándar citado en esta guía, pero sí el control interno que la guía se propone instalar—:

**Quien escribe el cambio no declara que está verificado. [C]** El desarrollador cierra su parte con
el merge; el issue lo cierra A-QA cuando lo valida. Mergeado no es verificado.

**Quien construye el artefacto no autoriza su liberación. [C]** A-OPS produce y promociona; A-AUT
autoriza. En un equipo de tres personas estas funciones se reparten entre esas tres personas, pero
no se colapsan en una sola para el mismo cambio.

Ninguna de las dos está respaldada por las citas que siguen: ISTQB distingue funciones dentro de QA
e ITIL asigna autoridad según riesgo, pero ninguno prescribe estas dos separaciones.

> **[F: ISTQB-1]** El esquema de certificación de pruebas distingue funciones diferenciadas dentro
> de lo que un organigrama suele llamar «QA» —gestión de pruebas, análisis de pruebas y análisis
> técnico orientado a riesgo, técnicas de caja blanca y automatización—, además de una certificación
> específica de pruebas de aceptación centrada en la colaboración entre product owners y testers.

De ahí este equipo deriva que **A-QA nombra la función, no un puesto [C]**: es una decisión de
composición propia, no parte de lo que dice la fuente.

> **[F: ITIL-1]** La autoridad de aprobación se asigna en función del riesgo del cambio, no
> enviando todo cambio a un comité central: los cambios estándar son de bajo riesgo y están
> preaprobados.

Que rutear todo cambio por el comité sea un «antipatrón» es la etiqueta de este equipo **[C]**, no
una cita del literal de la fuente: el texto completo de ITIL 4 requiere licencia y en esta ejecución
no se abrió, de modo que ninguna afirmación sobre lo que dice *explícitamente* puede sostenerse.
Ver [Anexos/Fuentes.md](Anexos/Fuentes.md).

## Preguntas guía

Antes de seguir, conviene poder responder estas cuatro sobre el propio equipo:

1. ¿Cuál de los cuatro contextos describe la situación de hoy? ¿Cuántas versiones se sostienen vivas?

   Son dos preguntas distintas y se contestan con datos, no con impresiones: contá las versiones
   que hoy **reciben parches** —no las que el equipo dice sostener— y fijate si existe una
   `release/x.y` viva. Tres o más soportadas es **C-4**; con dos o menos estás en **C-1** o
   **C-2**, y **C-3** se superpone a cualquiera de ellos cuando hay usuarios afectados ahora.

2. ¿Qué persona cubre cada actor, y qué pares de funciones quedan en la misma persona?

   Escribí los siete IDs y al lado el nombre propio; las casillas que quedan vacías informan tanto
   como las llenas. Que una persona cubra dos funciones es normal en un equipo chico. Los dos
   pares que sí importan son **A-DEV** con **A-QA** y **A-OPS** con **A-AUT** sobre el mismo
   cambio: ahí se pierde la segregación de funciones que la guía sostiene **[C]**.

3. ¿Quién cierra hoy los issues, y en qué momento?

   El dato está en el historial del gestor de issues, no en el procedimiento escrito. Compará la
   marca de cierre con la del merge: si coinciden, y las hace la misma persona, el equipo está
   declarando verificado lo que apenas está integrado. La respuesta buena nombra persona y
   momento; la vaga dice «lo cierra quien lo tomó».

4. ¿Qué escenario de la tabla ocurrió la última vez que algo salió mal, y qué faltó?

   Un episodio se clasifica con evidencia —tags, ramas, pull requests, fechas—, no de memoria;
   recién con eso a la vista se le pone el ID: **E-02**, **E-05**, **E-08**. Lo que faltó suele
   ser un paso del propio escenario: la corrección que nunca volvió a la línea principal, el
   motivo del rechazo que nadie registró. Si el episodio no se puede reconstruir, ese es el
   hallazgo.

---

Sigue: [02 — Mapa conceptual](02-Mapa-Conceptual.md).
