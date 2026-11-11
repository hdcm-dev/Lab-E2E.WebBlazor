---
adr: 0001
alias: ADR-LLAMADA
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0001 · Puerto propio de redacción, no una sesión de chat con herramientas

## Decisión

El modelo sólo redacta. El asistente llama a `IRedactorDeAyuda` y a `IExtractorDeLocalidad`, dos puertos propios de `Application/Asistente/`, con un pedido que arma el código. No hay `UseFunctionInvocation` ni herramientas ofrecidas al modelo.

## Alternativas descartadas

Una sesión de chat con herramientas, como la página de chat de la demo de la guía (cap. 15). Se descartó por ARQ-1 (el código elige la intención y la función) y por CD-6 (el asistente no actúa ni dice que actúa).

## Deja de valer si

Una intención pasa a ser elegida por el modelo porque la batería ×3, corrida por los dos caminos, lo justifica (K6 §9; diferido D-4).

## Pruebas que la sostienen

`Redaccion_SinHerramientas`, `Ruteo_TablaDeReferencia`.
