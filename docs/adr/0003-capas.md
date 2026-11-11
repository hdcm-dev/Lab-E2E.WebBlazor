---
adr: 0003
alias: ADR-CAPAS
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0003 · Capas como carpetas de un ensamblado, vigiladas por una prueba

## Decisión

El asistente vive entero en `src/MovilidadUrbana.Web`, con la forma que ya tiene el repositorio: `Domain/`, `Application/`, `Infrastructure/` (con `Infrastructure/AI/` nuevo) y `Components/`. Lo que el compilador no impide lo impide `ArquitecturaTests`, que nombra el SDK de IA.

## Alternativas descartadas

Proyectos separados por capa: rompería la regla del repositorio (un proyecto por aplicación, tres aplicaciones independientes).

## Deja de valer si

Se decide compartir la capa entre aplicaciones (hoy prohibido).

## Pruebas que la sostienen

`Domain_SinDependenciasDeIa`, `SdkDeIa_SoloEnInfrastructureAIyRaiz`, `Application_SinInfrastructure`.
