# Registro de frentes del asistente

Medido el 2026-11-11. Un frente es algo que puede cambiar debajo del trabajo: se mide, no se supone.

| Frente | Estado medido | Comando | Condición de salida |
| --- | --- | --- | --- |
| Base del repositorio | `origin/main` = `c6b60ce` | `git fetch && git rev-parse --short origin/main` | la rama `feat/asistente-integral` sale de ahí |
| IA en la web | implementada en `feat/asistente-integral` | `ls src/MovilidadUrbana.Web/Infrastructure/AI` | fusión |
| Deeplinks por consulta | `Localidades` y `Resultados` leen `[SupplyParameterFromQuery]` | `grep -rln SupplyParameterFromQuery src/MovilidadUrbana.Web` | fusión |
| Votos | tabla `Votos` con `EnsureCreated` (ADR 0007) | `Voto_SePersisteYSeLee` | fusión |
| Reglas de localidad endurecidas | aplicadas; las 49 unitarias previas sin cambios | `dotnet test tests/MovilidadUrbana.UnitTests` | fusión |
| Firma del responsable del producto (C-02, C-08, C-13, C-17, REQ-IA-46) | pendiente; rige el default propuesto | — | firma |
| Servidor de modelos | por variable; el modelo grande no cargó en el turno de la medición | `curl …/api/ps` | batería con un modelo más grande |
| Críticos y lectura a ciegas | pendientes | — | notas ≥ 8 y REQ-IA-47 |
