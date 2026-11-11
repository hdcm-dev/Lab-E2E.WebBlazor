---
titulo: Expediente 0001 — Asistente integral de MovilidadUrbana.Web
tipo: expediente
estado: implementado, sin cerrar (faltan críticos y lectura a ciegas)
fecha: 2026-11-11
base: main en c6b60ce
rama: feat/asistente-integral
toca-codigo: sí (sólo src/MovilidadUrbana.Web y sus pruebas)
---

# Expediente 0001 — Asistente integral de MovilidadUrbana.Web

## 1. Pedido

El responsable del producto pidió usar Movilidad Urbana, por simple, para construir una idea **completa e
integral**: el asistente entero, como ejemplo trabajado de la guía de la capa de IA en .NET y de sus
conocimientos para agentes. No una rebanada: los cuatro anillos, glosario y sinónimos, catálogo de destinos
con deeplink a la acción, cifras por código con salidas adaptadas, aclaraciones y opciones, rechazo amable,
votos sin texto y medición, redacción anclada, degradación sin modelo, pruebas y batería real.

## 2. Folios

| Folio | Contenido |
| --- | --- |
| [especificacion.md](../asistente/especificacion.md) | arc42 reducido: frontera, requisitos `REQ-IA-nn`, riesgos y checklist |
| [ADR 0001–0007](../adr/README.md) | decisiones de arquitectura |
| [glosario.md](../asistente/glosario.md) | términos nuevos |
| [registro-de-frentes.md](../asistente/registro-de-frentes.md) | frentes medidos |
| [verificacion-2026-11-11.md](../asistente/verificacion-2026-11-11.md) | evidencia |
| [README del asistente](../asistente/README.md) | cómo correrlo con y sin modelo, y qué mirar |

## 3. Entregas

| # | Entrega | Estado |
| --- | --- | --- |
| E0 | Especificación, ADR y pruebas de arquitectura | hecha |
| E1 | Cifras por código y `/resultados` | hecha |
| E2 | Panel sin modelo: enrutador, anillos, glosario, sinónimos, aclaración, rechazo, F8 | hecha |
| E3 | Catálogo de destinos y deeplinks que abren la acción | hecha |
| E4 | Redacción anclada con modelo | hecha; REQ-IA-47 pendiente |
| E5 | Votos sin texto y tablero de medición | hecha |
| E6 | Alta desde una frase y reglas endurecidas | hecha |
| E7 | Batería ×3, críticos y cierre | batería hecha (37/37 ×3); críticos y lectura pendientes |

Las entregas se hicieron en una sola rama y no en un PR por entrega, como planteaba el plan: el pedido fue
una rama `feat/asistente-integral` con push y sin PR todavía. Cada entrega deja la web funcionando sola
(por ejemplo, sin modelo el asistente responde con plantillas, y con `Asistente__Habilitado=false` no hay
panel).

## 4. Decisiones elevadas al responsable del producto

Se elevan en un solo mensaje, con su default (mientras no firme, rige el default):

1. **C-02 y C-08 (ADR 0001–0007).** Default: el código elige, el modelo redacta; el asistente no escribe.
2. **C-13 (datos que salen).** Default: al modelo viajan la pregunta normalizada, las cifras ya calculadas y
   los fragmentos; nunca nombre, edad, sesión ni texto de terceros sin marca.
3. **C-17 (umbral).** Default: batería ×3 con mínimo ≥ 34/37, cero inventados, críticos ≥ 8.
4. **REQ-IA-46 (latencia).** Default: p95 en caliente ≤ 8 s; cifras sin modelo ≤ 300 ms.

## 5. Diferidos

D-1 escritura con aprobación en el chat · D-2 el asistente en MAUI y en ApiWeb (una copia por aplicación) ·
D-3 migraciones · D-4 intenciones elegidas por el modelo · D-5 la velocidad implícita como función · D-6
MCP · embeddings opcionales (ADR 0006).
