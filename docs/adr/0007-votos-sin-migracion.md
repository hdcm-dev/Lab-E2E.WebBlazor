---
adr: 0007
alias: ADR-VOTOS-SIN-MIGRACION
estado: propuesta (default vigente hasta la firma del responsable del producto)
fecha: 2026-11-11
---

# ADR 0007 · La tabla de votos se crea con EnsureCreated, sin migraciones

## Decisión

El repositorio crea su esquema con `EnsureCreated`, a propósito. La tabla `Votos` entra en `AppDbContext` y, sobre un archivo de una versión anterior, `DatabaseInitializer` la crea con la sentencia que genera EF, con `IF NOT EXISTS`. Rige la norma del anfitrión sobre la de K2 §8.5.

## Alternativas descartadas

Una migración versionada (K2 §8.5): el repositorio no usa migraciones.

## Deja de valer si

El repositorio adopta migraciones (diferido D-3).

## Pruebas que la sostienen

`Voto_SePersisteYSeLee`, `Voto_SinTextoNiPersona`.
