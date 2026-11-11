# Decisiones de arquitectura

Registro de decisiones del laboratorio, con numeración propia desde 0001. Nació con el asistente de
`MovilidadUrbana.Web` ([especificación](../asistente/especificacion.md), §6). Cada ADR dice qué decide, qué
se descartó, cuándo deja de valer y qué prueba la sostiene. Mientras el responsable del producto no firme,
rige la propuesta como default.

| ADR | Decide |
| --- | --- |
| [0001](0001-llamada.md) | Puerto propio de redacción, no una sesión de chat con herramientas |
| [0002](0002-proveedor.md) | Microsoft.Extensions.AI con el adaptador de Ollama, elegido en `AddAI` |
| [0003](0003-capas.md) | Capas como carpetas de un ensamblado, vigiladas por una prueba |
| [0004](0004-herramientas.md) | Funciones de sólo lectura invocadas por el enrutador |
| [0005](0005-agencia.md) | El asistente no escribe: lleva a la acción y la persona la confirma |
| [0006](0006-conocimiento.md) | Contexto completo de los fragmentos elegidos por una compuerta léxica |
| [0007](0007-votos-sin-migracion.md) | La tabla de votos se crea con `EnsureCreated`, sin migraciones |
| [0008](0008-redaccion-apagada-por-omision.md) | La redacción con IA queda apagada por omisión (`Asistente__Redaccion__Habilitada=false`) |
