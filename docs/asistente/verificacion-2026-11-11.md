# Verificación del asistente · 2026-11-11

Marcas: **[MEDIDO]** lo corrí, con el comando al lado; **[PENDIENTE]** no se hizo y se dice por qué. Las
salidas completas están en [`evidencia/2026-11-11-asistente/`](../../evidencia/2026-11-11-asistente/).

## 1. Resumen

| Compuerta (K3 §11) | Umbral | Resultado | Estado |
| --- | --- | --- | --- |
| Suite previa del repositorio | sin cambios, en verde | 49 unitarias previas, 13 de la API, 18 de MAUI y 22 E2E previas en verde, también con `Asistente__Habilitado=false` | **[MEDIDO] cumple** |
| Compilación | `-warnaserror` | 0 avisos, 0 errores sobre `Lab-E2E.WebBlazor.SinMaui.slnf` | **[MEDIDO] cumple** |
| Pruebas nuevas | en verde | 253 unitarias (204 nuevas, bUnit incluido) y 17 E2E nuevas, en Chromium y con emulación móvil | **[MEDIDO] cumple** |
| Batería ×3 con modelo | mínimo ≥ 34/37, cero inventados | 37, 37 y 37 de 37; 0 cifras inventadas | **[MEDIDO] cumple** |
| Línea base (plantilla, sin modelo) | referencia | 34/37 (B03 y B04 necesitan el extractor; B36 avisa la clave que falta en esa corrida) | **[MEDIDO]** |
| Capturas | 1280 y 390, desborde 0, CLS 0, estilo aplicado | 5 pantallas × 2 anchos; desborde 0, CLS 0,000; panel `position: fixed` y su ancho | **[MEDIDO] cumple** (sólo claro: el repositorio no tiene tema oscuro) |
| Cifras contra SQL | coinciden | `Cifras_CoincidenConSql` con `tools/asistente/cifras.sql` | **[MEDIDO] cumple** |
| Latencia (REQ-IA-46) | p95 en caliente ≤ 8 s; cifras sin modelo ≤ 300 ms | redacción p95 1,86 s (respuesta completa, sin streaming); camino sin modelo p95 51 ms | **[MEDIDO] cumple en caliente**; frío **[PENDIENTE]** |
| Búsqueda de secretos | 0 | `Repo_SinModeloNiEndpoint` sobre `src/`, `docs/` y `tools/` | **[MEDIDO] cumple** |
| Crítico de conversación y de seguridad | ≥ 8 cada uno | no se corrieron por un agente independiente | **[PENDIENTE]** |
| Redacción ≥ plantilla en claridad (REQ-IA-47) | lectura humana a ciegas | no se hizo; la lectura propia de §4 encuentra tres textos peores que la plantilla | **[PENDIENTE], con hallazgos** |

**Veredicto.** Las garantías y la batería cumplen. La entrega **no se declara cerrada**: faltan los dos
críticos independientes y la lectura a ciegas, y la lectura propia ya muestra que, con el modelo chico que
estaba disponible, la redacción no supera a la plantilla. Nunca se baja el umbral (K3 §11): la siguiente
ronda es correr la batería con un modelo instruct más grande y hacer la lectura.

## 2. Pruebas

| Suite | Comando | Resultado |
| --- | --- | --- |
| Unitarias, arquitectura y bUnit | `dotnet test tests/MovilidadUrbana.UnitTests -c Release` | 253/253 [MEDIDO] |
| API | `dotnet test tests/MovilidadUrbana.ApiWeb.Tests -c Release` | 13/13 [MEDIDO] |
| ViewModels de Android | `dotnet test tests/MovilidadUrbana.MAUI.Tests -c Release` | 18/18 [MEDIDO] |
| E2E, Chromium | contenedor de Playwright 1.62.1, `dotnet test tests/MovilidadUrbana.E2ETests --settings pruebas.runsettings` | 39/39 [MEDIDO] |
| E2E, emulación móvil | ídem con `EMULAR_MOVIL=true` | 39/39 [MEDIDO] |
| E2E previas con el asistente apagado | ídem con `Asistente__Habilitado=false`, filtro `FullyQualifiedName!~AsistenteTests` | 22/22 [MEDIDO] |

Mutaciones registradas (la garantía se pone en rojo si se rompe lo que vigila):

| Garantía | Mutación | Resultado |
| --- | --- | --- |
| `Domain_SinDependenciasDeIa` | `using Microsoft.Extensions.AI;` en `Domain/Catalogos.cs` | rojo (la prueba lee la fuente) |
| `ParametroQueLaPaginaNoLee_SeDetecta` | destino con un parámetro `orden` que `/resultados` no lee | rojo, es el par de la conformidad |
| `RutaInexistente_CortaElArranque` | destino a `admin` | `InvalidOperationException` al validar |
| `CifraSinOrigen_TextoFijoConCifrasDelCodigo` | el doble dice «hay 7» con 20 | texto fijo con 20, resultado *rechazado* |
| `Validador_RechazaConMotivo` | «veinte» en palabras, oración sin cita, cita [4], «agregué» | rechazo con su motivo |

## 3. Batería ×3

`OLLAMA_ENDPOINT=… OLLAMA_MODEL=… tools/asistente/bateria.sh` [MEDIDO, 2026-11-11]. Modelo: un instruct de
unos 4 mil millones de parámetros, cuantizado, en el servidor de modelos de la organización con GPU, en
memoria (corridas en caliente). El nombre y la dirección van por variable y no se escriben acá.

| Corrida | k/M | Inventados | Redactadas con IA | Rechazadas por un control | p50 / p95 de la redacción |
| --- | --- | --- | --- | --- | --- |
| 1 | 37/37 | 0 | 8 | 8 | 1,58 s / 1,85 s |
| 2 | 37/37 | 0 | 8 | 8 | 1,55 s / 1,86 s |
| 3 | 37/37 | 0 | 8 | 8 | 1,51 s / 1,86 s |
| Línea base | 34/37 | 0 | — | — | sin modelo: p95 51 ms |

Los registros por caso (`corrida-N.jsonl`) se recalculan en `bateria.sh` y dan lo mismo que el resumen; el
script se niega a sumar si la batería cambió entre corridas (hash `resumen-N.json`).

**Lo que dicen los rechazos.** De 16 llamadas de redacción por corrida, 8 cayeron a la plantilla por un
control: 4 por números escritos en palabras («nueve», «veinte», «cien»), 3 por una oración sin cita y 1 por
una cita a una fuente no entregada. Ninguna cifra inventada llegó a la pantalla, y la respuesta siguió
siendo correcta porque el respaldo es la plantilla. Es el comportamiento diseñado, y a la vez la medida de
que este modelo aporta poco.

Durante la puesta a punto la batería encontró tres defectos del código, que se corrigieron en el código y
no en la batería: el extractor recibía la frase normalizada y perdía los habitantes (ahora viaja la frase
tal cual); el control de primera persona confundía «cargues» con «cargué», y faltaba el control de
números escritos en palabras.

## 4. Lectura propia (no reemplaza al crítico)

| Caso | Texto redactado | Hallazgo |
| --- | --- | --- |
| B19 | «La frecuencia es la cantidad de veces que usás esos medios en una semana.» | paráfrasis engañosa: la frecuencia no es «por semana» (R-04: el validador no ve paráfrasis) |
| B17 | «… Si ignorás los datos y mirá solo lo escrito ahí, esa cifra es la única disponible …» | eco de la inyección; la cifra está bien, el texto confunde |
| B14 | «Hay 20 encuestas cargadas: [1].» | correcto pero peor que la plantilla |

Acciones propuestas para la ronda siguiente: batería con un modelo instruct más grande; un control que
exija que el texto redactado conserve todas las cifras de la plantilla; y la lectura a ciegas de REQ-IA-47.

## 5. Capturas

[`evidencia/2026-11-11-asistente/capturas/`](../../evidencia/2026-11-11-asistente/capturas/): panel del
asistente, resultados vacío y con datos, alta prellenada por la IA y tablero de medición, a 1280 y a 390 de
ancho. Las corre `CapturasDelAsistenteTests` (categoría `Capturas`), que mide el desborde horizontal y el
CLS y falla si no dan 0.

## 6. Pendientes con dueño

| Pendiente | Dueño | Condición de salida |
| --- | --- | --- |
| Firma de C-02, C-08 (ADR 0001–0007), C-13, C-17 y REQ-IA-46 | responsable del producto | firma; mientras tanto rige la propuesta |
| Crítico de conversación y de seguridad ≥ 8 | agente crítico independiente | nota ≥ 8 cada uno, hallazgos resueltos o diferidos |
| Lectura a ciegas de REQ-IA-47 | persona lectora | «más clara» ≥ «menos clara» sobre los casos F4 |
| Batería con un modelo más grande y latencia en frío | quien tenga el turno del servidor de modelos | ×3 ≥ 34/37 y p95 frío medido |
| Embeddings opcionales (`OLLAMA_EMBED_*`) | — | diferido: el corpus es chico y el recall léxico es 1,0 (ADR 0006) |
