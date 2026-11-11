---
adr: 0004
alias: ADR-HERRAMIENTAS
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0004 · Funciones de sólo lectura invocadas por el enrutador

## Decisión

Las cifras salen de `FuncionesDeLectura`: métodos marcados con `[FuncionDeSoloLectura]` que envuelven casos de uso (`LocalidadService`, `ResultadosDeEncuestaService`), creados por circuito y cerrados sobre la sesión. Los invoca el enrutador. `HerramientasDeLectura` los expresa como `AIFunction` para poder comparar los dos caminos más adelante, pero hoy no se ofrecen al modelo.

## Alternativas descartadas

Ofrecerlas al modelo como herramientas: no se descarta para siempre; se mide antes (K6 §9).

## Deja de valer si

—

## Pruebas que la sostienen

`Funciones_SinRepositorios`, `Funciones_SoloLectura`, `DosSesionesEnParalelo_NoSeVen`.
