---
adr: 0002
alias: ADR-PROVEEDOR
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0002 · Microsoft.Extensions.AI con el adaptador de Ollama, elegido en AddAI

## Decisión

`IChatClient` de Microsoft.Extensions.AI; el adaptador del servidor de modelos local se compone en `Infrastructure/AI/DependencyInjection.cs` (`AddAI`) y en ningún otro lugar. Endpoint, modelo y contexto llegan por variables de entorno (`OLLAMA_ENDPOINT`, `OLLAMA_MODEL`, `OLLAMA_NUM_CTX`), sin valores por defecto en el código.

## Alternativas descartadas

Un proveedor en la nube: descartado por CTX-SENS (la encuesta guarda nombre y edad, aunque sean sintéticos) y por el costo en un laboratorio.

## Deja de valer si

El laboratorio pasa a un contexto de nube (CTX-NUBE).

## Pruebas que la sostienen

`SdkDeIa_SoloEnInfrastructureAIyRaiz`, `Repo_SinModeloNiEndpoint`, `HabilitacionTests`.
