---
titulo: La API de Movilidad Urbana
contexto:
anillo: 3
estado: vigente
preguntas: ¿Cómo cargo una localidad por la API? | ¿Hay una API? | ¿Dónde está la documentación de la API?
destinos: localidades.lista
---
## Otra aplicación
La API REST es otra aplicación del laboratorio, MovilidadUrbana.ApiWeb, independiente de esta web: tiene sus propios datos y su propia sesión, que se manda en el encabezado X-Sesion-Id. Para cargar una localidad se hace un POST a la ruta de localidades de la API, con nombre, provincia, código postal y habitantes; el contrato y la prueba desde el navegador están en su documentación navegable, que se publica en Development. Lo que cargues por la API no aparece en esta web.
