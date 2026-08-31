---
doc_id: GF-05
doc_type: documento-tematico
title: Cómo elegir el modelo de ramas
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, devops, po]
traces: [GF-04, GF-06]
---

# Cómo elegir el modelo de ramas

La pregunta «¿cuál es el mejor modelo de ramas?» no tiene respuesta, y quien la contesta sin
preguntar por el contexto está vendiendo algo. La pregunta con respuesta es otra: **cuántas versiones
del producto tienen que estar vivas al mismo tiempo, y con qué frecuencia se libera**. De esas dos
variables sale casi todo lo demás.

## Los cuatro modelos que conviene conocer

### GitHub Flow

El más simple de los modelos vigentes. Una sola rama de larga vida —la rama por defecto— y ramas
cortas que entran por pull request. **[F: GH-1]** El ciclo documentado tiene seis pasos: crear una
rama con nombre corto y descriptivo, hacer los cambios y commitearlos, abrir un pull request —que
puede marcarse como borrador si se busca opinión temprana—, atender los comentarios de la revisión,
mergear una vez aprobado, y borrar la rama. La documentación señala además que la configuración de
protección de rama puede impedir el merge si no se cumplen los requisitos definidos, por ejemplo una
cantidad mínima de aprobaciones.

No define nada sobre versiones ni ambientes: asume que lo mergeado se despliega.

### GitFlow

Dos ramas infinitas y tres de soporte, tratado en [04](04-GitFlow.md). Su contexto de aplicación,
según su autor, es el software explícitamente versionado o con varias versiones en producción.
**[F: NVIE-1]**

### GitLab Flow

Agrega a GitHub Flow lo que le falta para operar versiones: ramas de ambiente o de release aguas
abajo de la principal. Su regla de propagación es la relevante acá. **[F: GL-1]** GitLab documenta
arreglar **hacia adelante**, empujando el cambio a la rama principal y después llevándolo por
cherry-pick a la rama de patch release, y explica el motivo: el problema clásico es arreglar el bug
en la versión recién liberada y olvidarse de arreglarlo en la rama principal.

### Desarrollo basado en tronco con rama de release

Una sola línea principal a la que todo el mundo integra al menos una vez por día, y ramas de release
creadas *just in time* para estabilizar. **[F: TBD-1]** La rama de release se crea justo antes de
necesitarla —unos días antes de liberar— para que se convierta en un lugar estable mientras el resto
sigue integrando al tronco a máxima velocidad; se puede además **cortar retroactivamente** desde un
commit anterior conocido como bueno; y conviene tener a lo sumo un par de releases vivas a la vez
para que nadie lleve una corrección a la rama equivocada.

## Comparación

| | GitHub Flow | GitFlow | GitLab Flow | Tronco + release |
|---|---|---|---|---|
| Ramas de vida larga | 1 | 2 | 1 + ambientes | 1 |
| Versiones vivas que soporta | 1 | varias | 1–2 | 1–2 |
| Dónde se corrige un defecto de producción | rama principal | rama de release / hotfix | rama principal, luego cherry-pick | rama principal, luego cherry-pick |
| Costo de coordinación | bajo | alto | medio | medio |
| Necesita feature flags | sí | no | sí | sí |
| Necesita automatización de pruebas fuerte | sí | menos | sí | sí |

## Aplicación por contexto

| Contexto | Modelo que encaja | Por qué |
|---|---|---|
| **C-1** Sin release abierta, despliegue continuo | GitHub Flow | No hay nada que estabilizar en paralelo |
| **C-2** Con release abierta, una versión viva | Tronco + release, o GitLab Flow | Hace falta una ventana de estabilización sin frenar la integración |
| **C-3** Producción comprometida | El que ya esté en uso, con su vía de excepción | La emergencia no es el momento de cambiar de modelo |
| **C-4** Varias versiones vivas | GitFlow | Es el contexto para el que fue diseñado **[F: NVIE-1]** |

## El criterio de decisión, en tres preguntas

1. **¿Cuántas versiones hay que soportar en simultáneo?** Más de dos empuja a GitFlow o a un modelo
   con ramas de mantenimiento por versión. Una sola vuelve innecesaria la rama `develop`.
2. **¿Qué tan seguido se libera?** Con liberaciones diarias, una rama de integración intermedia
   agrega latencia sin agregar seguridad. Con liberaciones mensuales sujetas a autorización, la
   ventana de estabilización se paga sola.
3. **¿Qué tan buena es la regresión automatizada?** Los modelos de tronco descansan en que la
   verificación automática detecte lo que la rama larga ocultaba. Sin esa red, mover el equipo a
   tronco expone el problema en lugar de resolverlo.

## Qué dice la evidencia, y qué no dice

> **[F: DORA-1]** El análisis de datos de DORA de 2016 y 2017 asocia mejor desempeño de entrega con
> mantener tres o menos ramas activas en el repositorio, integrar al tronco al menos una vez por día,
> y no tener *code freezes* ni fases de integración separadas.

Esa asociación es real y conviene tomarla en serio, pero tiene un límite metodológico que no hay que
esconder: los estudios de DORA son transversales y basados en encuesta autorreportada, de modo que
muestran correlación, no causalidad. Es igualmente plausible que los equipos de alto desempeño puedan
trabajar así **porque** ya tienen buena automatización de pruebas, y no al revés.

La conclusión honesta es más modesta que el eslogan: pocas ramas activas e integración frecuente son
un buen objetivo, y la automatización de pruebas es la condición que lo hace posible. Mover un equipo
a tronco sin esa condición no produce el desempeño de los equipos medidos.

## Ejemplo concreto

El equipo de esta guía trabaja sobre una aplicación web con un ambiente de homologación formal, una
sola versión viva en producción y liberaciones sujetas a autorización de cambio. Aplicando las tres
preguntas: una versión viva descarta C-4 y con ello GitFlow como modelo cotidiano; las liberaciones
con autorización justifican una ventana de estabilización, lo que descarta GitHub Flow puro; y la
regresión automatizada existe y corre en cada PR, que es la condición del modelo de tronco.

El resultado es el modelo que documenta [06 — Modelo adoptado](06-Modelo-Adoptado.md): tronco con
ramas de release cortadas *just in time*. GitFlow queda documentado en esta guía por dos motivos —es
el vocabulario que el equipo va a encontrar en la industria, y es el modelo al que habría que migrar
si alguna vez hay que soportar dos versiones en paralelo—.

## Preguntas guía

1. ¿Cuántas versiones del producto están vivas hoy? ¿Y en un año?

   Contá las que tienen usuarios encima, no las que el equipo dice sostener: la respuesta es una
   lista de tags desplegados, no una impresión. Una sola habilita el modelo de tronco con release
   cortada *just in time*; con **tres o más** ya estás en **C-4**, el contexto donde GitFlow sigue
   siendo razonable. Para el año que viene, la evidencia es el compromiso de soporte asumido, no el
   optimismo del roadmap.

2. Si mañana hay que corregir la versión liberada hace tres meses, ¿desde dónde se rama?

   Desde el tag que marca esa versión en producción, nunca desde la punta de una rama, porque la
   rama se mueve y el tag no. Si ese tag sigue siendo lo último liberado, la corrección entra por
   pull request a `release/x.y` y vuelve a `main` el mismo día. Si además hay una versión más nueva
   viva, la pregunta 1 ya quedó contestada: son dos, y el modelo cambia.

3. ¿La regresión automatizada actual alcanza para confiar en un merge sin verificación manual?

   El pipeline corre hoy compilación con advertencias como errores, descubrimiento de pruebas y
   regresión de extremo a extremo; análisis estático, escaneo de dependencias y pruebas unitarias o
   de integración no. **[C]** La comprobación no admite opinión: señalá job por job en el workflow y
   preguntá qué defectos recientes de producción habría atrapado esa suite. Lo que quede afuera es
   trabajo de homologación, y hay que decirlo en voz alta.

4. ¿Qué de lo que hoy resuelve una rama larga podría resolver un feature flag?

   Un flag separa desplegar de liberar, así que reemplaza a la rama larga justo en lo que ésta
   oculta: trabajo incompleto que todavía no se quiere exponer. Mirá qué guarda cada rama de vida
   larga; si es código a medio hacer, va integrado y apagado. No resuelve, en cambio, un cambio de
   esquema ya aplicado ni la coexistencia de dos versiones con usuarios.

## Criterios de calidad

Una decisión de modelo bien tomada se puede explicar en dos frases, nombra el contexto que la
justifica, y define de antemano qué cambio de contexto la invalidaría. Una decisión mal tomada se
justifica por lo que hace otra empresa.

---

Sigue: [06 — Modelo adoptado](06-Modelo-Adoptado.md).
