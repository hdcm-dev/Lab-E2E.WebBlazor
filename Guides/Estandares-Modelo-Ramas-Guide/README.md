---
doc_id: GF-00
doc_type: indice
title: Estándares de modelo de ramas — guía de estudio
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po, autoridad-de-cambio]
traces: [GF-01, GF-02, GF-03, GF-04, GF-05, GF-06, GF-07, GF-08, GF-09]
---

# Estándares de modelo de ramas

Cuerpo documental para que un equipo de desarrollo entienda los modelos de ramas, adopte uno con
criterio, y opere el ciclo de vida de sus versiones con pull requests verificados automáticamente.
Se compone de ocho documentos de estudio, cinco anexos más los tres workflows listos para copiar,
y dos guías prácticas de ocho escenarios cada una, ejecutables sobre un repositorio real: la del modelo adoptado y la de GitHub Flow, que sirve de
línea de base para medirlo.

## El problema que le dio origen

El equipo integra pull requests contra una rama estable sin que se dispare ninguna verificación
automatizada, de modo que un cambio puede romper funcionalidad que ya andaba y nadie se entera hasta
que alguien lo prueba a mano. Alrededor de eso aparecen tres huecos más: no está escrito qué entra a
una versión una vez cortada, no está claro quién cierra un issue ni cuándo, y no hay una definición
compartida de qué significa «estable».

La guía responde a los cuatro, en ese orden de importancia.

## Una aclaración necesaria sobre el nombre

El pedido habla de «GitFlow», y conviene ser preciso con el término desde el principio: **GitFlow es
un modelo concreto** —el de Vincent Driessen, 2010— con `master`, `develop` y tres tipos de rama de
soporte, no un sinónimo de «trabajar con ramas».

El propio autor le agregó en 2020 una nota acotando su alcance: fue concebido para software
explícitamente versionado o con varias versiones corriendo en producción, y para un equipo que hace
entrega continua sugiere un flujo más simple. **[F: NVIE-1]** El equipo de esta guía está en el
segundo caso.

La guía toma entonces dos decisiones que conviene tener presentes al leerla:

1. **GitFlow se documenta en serio**, en [04](04-GitFlow.md), porque es el vocabulario que el equipo
   va a encontrar en la industria y es el modelo al que habría que migrar si algún día hay que
   soportar dos versiones en paralelo.
2. **El modelo que se adopta es otro** —tronco con ramas de release, [06](06-Modelo-Adoptado.md)—,
   y [05](05-Como-Elegir-El-Modelo.md) explica el criterio con el que se eligió, para que la decisión
   se pueda revisar cuando cambie el contexto.

Presentar el modelo adoptado como «GitFlow» habría sido cómodo y falso. Por eso esta carpeta se
llama `Estandares-Modelo-Ramas-Guide` y no «Procedimiento GitFlow»: lo que documenta es la elección
entre modelos y el que este equipo sostiene. GitFlow es uno de los comparados, y tiene además su
propia guía práctica al lado.

## Contenido

| # | Documento | De qué trata |
|---|---|---|
| 01 | [Marco de referencia](01-Marco-De-Referencia.md) | Escenarios, contextos y actores: el vocabulario que usa todo lo demás |
| 02 | [Mapa conceptual](02-Mapa-Conceptual.md) | Entradas por escenario, por rol y por artefacto: «estoy acá → qué aplico» |
| 03 | [Fundamentos de Git](03-Fundamentos-De-Git.md) | Merge, squash, rebase, cherry-pick y tags, para entender por qué un modelo elige uno u otro |
| 04 | [GitFlow](04-GitFlow.md) | El modelo original, sus reglas y la nota de 2020 de su autor |
| 05 | [Cómo elegir el modelo](05-Como-Elegir-El-Modelo.md) | GitHub Flow, GitFlow, GitLab Flow y tronco: comparación y criterio de decisión |
| 06 | [Modelo adoptado](06-Modelo-Adoptado.md) | Las siete reglas, de dónde nace cada rama, guardarraíles y antipatrones |
| 07 | [Integración y versionado](07-Integracion-Y-Versionado.md) | Ambientes, artefactos, promoción, versionado semántico, releases y versiones de demostración |
| 08 | [Pull requests y pruebas](08-Pull-Requests-Y-Pruebas.md) | Ciclo del pull request, tamaño, protección de rama y qué verifica el pipeline en cada disparador |
| 09 | [Guía práctica de GitFlow](../GitFlow-Practice-Guide/README.md) | Ocho escenarios ejecutables del modelo adoptado, para un equipo de tres personas |
| 10 | [Guía práctica de GitHub Flow](../GitHubFlow-Practice-Guide/README.md) | Ocho escenarios del modelo que **no** se adoptó, para medir contra qué se lo comparó |

### Anexos

| Anexo | Contenido |
|---|---|
| [Glosario](Anexos/Glosario.md) | Términos con su definición precisa, y los alias que circulan en el equipo |
| [Plantillas](Anexos/Plantillas.md) | Issue, pull request, mensaje de commit y registro de release, comentadas |
| [Listas de verificación](Anexos/Listas-De-Verificacion.md) | Una por momento del proceso, desde abrir un pull request hasta promocionar a producción |
| [Preguntas que forman criterio](Anexos/Preguntas-Frecuentes.md) | Las quince preguntas que aparecen siempre, con respuesta corta |
| [Workflows](Anexos/workflows/README.md) | `ci.yml`, `release.yml` y `auditoria-convergencia.yml`, listos para copiar |
| [Fuentes](Anexos/Fuentes.md) | Tabla de fuentes y una discusión honesta sobre la fuerza de cada una |

## Ruta de lectura

Toda ruta empieza por **01**: es el único documento que define los códigos `E-nn` (escenarios),
`C-n` (contextos) y `A-XXX` (actores) que 03, 06, 07 y 08 usan sin volver a explicarlos. Saltearlo
deja tablas enteras escritas en un código irresoluble.

**Quien recién entra al equipo:** 01 → 03 → 06 → 08, y después practicar los escenarios **00, 01 y
03** —en ese orden: el 02 exige una release abierta que solo el 03 crea—. Los documentos 04 y 05 se
pueden dejar para más adelante.

**Quien va a operar releases:** 01 → 07 y los anexos de listas de verificación y workflows, y después
los escenarios 03, 05 y 07.

**Quien tiene que decidir el modelo:** 04 → 05, y la sección de fuerza de la evidencia del anexo de
fuentes.

**Como capacitación completa:** 01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → guía práctica en su orden de
ejecución (00 → 01 → 03 → 02 → 04 → 05 → 06 → 07). Los escenarios 00 a 05 llevan una jornada si se
hacen con las esperas reales de revisión.

## Convención de marcas

Cada afirmación de la guía lleva una de estas dos marcas, siguiendo la convención del documento de
insumo del equipo:

| Marca | Significado |
|---|---|
| **[F]** | Fundamentada en una fuente externa verificable, listada en [Anexos/Fuentes.md](Anexos/Fuentes.md) |
| **[C]** | Convención de este equipo. No está respaldada por ningún estándar: es una elección deliberada, discutible y cambiable |

Un documento de proceso pierde autoridad cuando presenta preferencias del autor como si fueran
estándares de la industria. Por eso la separación es explícita en todos los documentos.

## Estado de verificación

| Elemento | Estado |
|---|---|
| Contenido conceptual | Fundado en las fuentes del anexo. Las de acceso pago se citan a través del documento de insumo del equipo, sin lectura directa |
| Modelo adoptado | Toma el flujo propuesto por el equipo en `Flujo-De-Trabajo-Ramas.md` |
| Guía práctica | **No ejecutada.** Los ocho escenarios están escritos para correrse sobre `Lab-GitFlow` con la aplicación de `Lab-E2E.WebBlazor`, pero no se corrieron en esta ejecución |
| Workflows del anexo | **Validados solo como YAML.** Su comportamiento en GitHub Actions requiere el runner `i7infra-dev` y no se comprobó |

Lo que está verificado se afirma; lo que no, está marcado como tal. Antes de usar la guía como
capacitación conviene ejecutar el escenario 00 completo y confirmar los cuatro puntos de su sección
de verificación.
