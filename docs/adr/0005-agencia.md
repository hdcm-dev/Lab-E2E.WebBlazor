---
adr: 0005
alias: ADR-AGENCIA
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0005 · El asistente no escribe: lleva a la acción y la persona la confirma

## Decisión

Nivel de agencia A1 de lectura (el enrutador) más A0 con persona que firma (el extractor de una frase). El alta y la baja se abren en la pantalla de localidades, prellenadas o con la confirmación existente; la persona guarda o confirma con el control de siempre.

## Alternativas descartadas

A1 con escritura y aprobación en el chat, como PD-5 y PD-6 de la demo: diferido (D-1), con su mesa propia.

## Deja de valer si

El responsable del producto pide escribir desde el chat.

## Pruebas que la sostienen

`Funciones_SoloLectura`, `AltaDesdeFrase_PrellenaYNoEscribe`, E2E `ChipDeBaja_AbreLaConfirmacion_YRechazarNoBorra`.
