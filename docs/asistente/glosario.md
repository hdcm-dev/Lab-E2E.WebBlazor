# Glosario de la capa de IA

Términos que el repositorio no tenía y que trae el asistente. Los del dominio (localidad, medio,
frecuencia…) viven como datos en `Domain/Asistente/Glosario.cs` y el asistente los explica.

| Término | Significado |
| --- | --- |
| Capa de IA | Lo que agrega el modelo: puertos en `Application/Asistente/` y adaptadores en `Infrastructure/AI/` |
| Puerto | Interfaz que declara lo que la aplicación necesita del modelo (`IRedactorDeAyuda`, `IExtractorDeLocalidad`), sin nombrar el SDK |
| Adaptador | Implementación del puerto sobre el SDK (`RedactorConModelo`, `ExtractorDeLocalidad`) |
| Escalón de conocimiento | Cuánto conocimiento se le da al modelo: −1 sin modelo, K0 nada, K1 contexto completo, K2 recuperación |
| Nivel de agencia | Cuánto puede hacer: A0 propone, A1 lee o actúa con aprobación |
| Anillo | Franja de la frontera de competencia: 1 operar, 2 entender el dominio, 3 lo de al lado, afuera |
| Frontera de competencia | Qué contesta el asistente y qué no; la decide el código |
| Destino | Entrada del catálogo tipado de pantallas a las que un chip puede llevar |
| Deeplink | Dirección con consulta que abre una acción (`localidades?alta=1`) |
| Forma de respuesta | Cómo se presenta una respuesta, de F1 a F8 |
| Validador de cifras | Control que rechaza un texto del modelo con un número sin origen |
| Habilitación | Estado de la capa de IA con su motivo exacto (`Habilitada`, `FaltaModelo`…) |
| Huella del turno | Intención, anillo, forma, destino y resultado de una respuesta, sin texto |
| Voto | 👍/👎 sobre una respuesta, guardado con la huella y sin la pregunta |
| Batería | Conjunto congelado de preguntas con oráculos, que se corre tres veces contra el modelo |
| Oráculo | Lo que decide si un caso pasa: forma, destino, textos y cifras esperados |
| Línea base | La batería corrida sin modelo, por el camino de la plantilla |
| Degradado | Respuesta fija que reemplaza a la del modelo cuando no responde, está ocupado o falta configurarlo |
