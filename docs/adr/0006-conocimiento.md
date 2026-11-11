---
adr: 0006
alias: ADR-CONOCIMIENTO
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0006 · Contexto completo de los fragmentos elegidos por una compuerta léxica

## Decisión

La ayuda es un corpus chico (`Ayuda/*.md` más los 12 requisitos generados desde `*Policy` y el glosario generado desde `Glosario`). La recuperación es léxica, con raíz de seis letras, pesos 4/3/2/1 y una compuerta de cobertura (0,6): debajo, la respuesta es «No está en la ayuda» sin llamar al modelo. Los embeddings quedan opcionales y, en esta versión, sin implementar: el panel y `/health/asistente` lo dicen (`soloLexico`).

## Alternativas descartadas

RAG vectorial puro: con 12 requisitos cortos sube de más (Guía §15.3) y sin compuerta se pierde la abstención F5.

## Deja de valer si

El corpus pasa de unas 40 páginas o el recall@3 léxico cae debajo de 0,9.

## Pruebas que la sostienen

`RecallLexico_SobreUmbral`, `CoberturaDelCorpus_SobreUmbral`, `Borrador_NoEsCandidato`, `SinFragmento_NoEstaEnLaAyuda`.
