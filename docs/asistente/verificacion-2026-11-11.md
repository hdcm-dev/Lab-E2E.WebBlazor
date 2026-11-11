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

## 7. Segunda ronda: defectos de la verificación y de la lectura propia

Evidencia: [`evidencia/2026-11-11-asistente-ronda2/`](../../evidencia/2026-11-11-asistente-ronda2/). Todas las
pruebas nuevas se corrieron contra el código anterior: **34 en rojo** (validador, redondeo y panel), en verde
con la corrección.

| Defecto | Corrección | Prueba que lo fija |
| --- | --- | --- |
| 1. «Trabajo y Estudio, cada una con 4» con Trabajo en 5: el validador miraba números sueltos | `VinculoDeCifras`: cada número se ata a su categoría (la que lo precede, la pegada que lo sigue o la primera de su cláusula; «cada una con N» se aplica a todas), promedio y mediana a su unidad y estadístico, y los ordinales («la segunda», «el más usado», «el menos elegido») al orden de las cifras, con empates como rango | `AtribucionTests` (prueba de intercambio sobre todos los pares, ordinales, promedio↔mediana); `FixtureDeEncuestas.EmpatesCercanos()` (20 encuestas: trabajo 6, estudio 5, salud 5, otros 4); oráculo independiente `CifrasCruzadas` en la batería |
| 2. `Math.Round` al par contra `ROUND` de SQLite | `MidpointRounding.AwayFromZero` en promedios y mediana | `Cifras_CoincidenConSql` sin tolerancia, con promedios en la mitad (41,25 → 41,3; 15,25 → 15,3) y todos los medios; `PromedioEnLaMitad_SeRedondeaComoSql`; `Mediana([1,5; 3]) = 2,3` |
| 3. `**…**` crudo en el panel | `TextoConMarcas`: un par cerrado se pinta como `<strong>`, un `**` suelto se quita; nunca HTML del modelo | `Ayuda_SinMarcasCrudas` (bUnit), `TextoConMarcasTests`, E2E `AyudaSinMarcasCrudas` a 1280 y 390: sin «**» en el texto visible, `<strong>` con peso ≥ 600, desborde 0, CLS 0 |
| B19 «veces por semana» | control «período sin origen»: «veces», «por/cada/al semana·día·mes·año», «en una semana» sólo si están en las fuentes | `B19_PeriodoSinOrigen_SeRechaza`; en la batería, B40 cayó por este control («8 veces por semana») |
| B17 eco de la inyección | control «eco de una instrucción» (ignor…, olvid…, instruccion…) salvo que esté en la ayuda; el 80 % atado a «auto» se rechaza por atribución | `B17_EcoDeLaInyeccion_SeRechaza` |
| B14 peor que la fija | controles «cita colgante» («…: [1]») y «cifra sin la nota de la sesión» | `B14_PeorQueLaPlantilla_SeRechaza` |

**Tasa de redacción.** Se clasificaron los rechazos guardando, sólo en la batería, el texto del modelo antes del
validador (campo `crudo` de `corrida-N.jsonl`).

| Rechazo de la ronda 1 | Clase | Qué se hizo |
| --- | --- | --- |
| 4 por número en palabras («cien», «nueve», «veinte») | evitable: la cifra era correcta | la palabra se convierte a su valor y pasa por el mismo control de origen (y de atribución) |
| 3 por oración sin cita, 1 por cita no entregada | evitable en parte: la referencia citaba sólo al final y el modelo la copiaba | la referencia que ve el modelo cita cada oración (`CitarCadaOracion`); prompt `redactor.v2` con la regla explícita |
| B02 «8 caracteres» (ronda 2) | evitable: la ayuda dice «ocho caracteres» | los números en palabras de las fuentes también anclan |

Con el modelo chico, por corrida: **13 de 43 redactadas** (antes 8 de 37) y 10 rechazos, de los que 9 son
legítimos: oraciones agregadas sin fuente («Queda solo en tu sesión», «Vé a la sección **Medios**», «pero son
menos frecuentes»), textos sin ninguna cita y B40 con «8 veces por semana». Ningún control se aflojó.

| Suite / corrida | Resultado |
| --- | --- |
| Compilación `-warnaserror` | 0 avisos, 0 errores |
| Unitarias, bUnit y arquitectura | 300/300 |
| API / ViewModels de Android | 13/13 · 18/18 |
| E2E Chromium / emulación móvil | 41/41 · 41/41 (22/22 previas con el asistente apagado) |
| Batería `bateria.v2` (43 casos, umbral 40) ×3 con modelo | 43, 43, 43; 0 inventados ni cruzados; p95 1,8 s |
| Línea base `bateria.v2` sin modelo | 40/43 (B03, B04, B36, como en v1) |

Queda a la vista: B25 redactado usa «la vuestra» (no es voseo); ningún control lo mira. Siguen pendientes los
críticos independientes y la lectura a ciegas (§6).

## 8. Tercera ronda: la redactada no puede ser peor que la fija

La ronda 2 no pasó: crítico de conversación 6, seguridad 8 y lectura a ciegas 5,5 (la redactada fue mejor en 2
de 13 y peor en 6: B09, B11, B14, B24, B25, B26). Las tres corridas eran además idénticas palabra por palabra,
así que «×3» no medía estabilidad.

| Hallazgo | Cambio | Prueba que falla con el código de la ronda 2 |
| --- | --- | --- |
| La IA empeora el glosario, el «cómo», los promedios y el conteo | `Intenciones.ConRedaccion`: sólo medios, motivos y frecuencias pasan por el modelo; el resto es la plantilla fija y el modelo no se llama | `FueraDeLasDistribuciones_PlantillaFija_SinLlamada` (B09, B14, B19, B24, B25, B26), `ConRedaccion_SonLasTresDistribuciones` |
| Registro «vuestra», «tú» (B25) | control «registro ajeno» (vosotros, vuestro/a, tú, tienes, puedes…) | `Forma_Rechazada` |
| Más larga y repetida (B09, B26) | tope de largo 1,5 × plantilla + 40 (mínimo 80) y rechazo de trigramas repetidos | `MasLargaQueLaPlantilla_Rechazada`, `Forma_Rechazada` |
| B11 «no llegan al cien» (falso) | la nota de multirrespuesta tiene que decir «no suman 100 %»; «no llega…», «menos de cien» se rechazan | `Forma_Rechazada` (B11 tal cual y B10 de esta ronda) |
| B17, B42 «los porcentajes no suman» sin porcentajes | control «nota de porcentaje sin porcentajes»; al modelo la nota le llega una vez y sin «porcentajes» | `NotaDePorcentaje_SinPorcentajes_Rechazada`, `Mensaje_NotaUnaVezSinPorcentajes` |
| B24 «una encuesta en Corrientes» | control «entidad sin origen»: provincias, localidades de la siembra, medios, frecuencias y motivos sólo si están en las fuentes (la pregunta no cuenta) | `EntidadQueSoloEstaEnLaPregunta_Rechazada`, `MedioQueNoEstaEnLasCifras_Rechazado` |
| B13 pasó una cláusula sin cita | el validador exige lo que `CitarCadaOracion` le muestra al modelo: la cita cierra la oración | `ClausulaDespuesDeLaCita_Rechazada` |
| Corridas idénticas | `Asistente:Semilla` y `Asistente:Temperatura` (producción 7 y 0); `bateria.sh` corre 7/0, 11/0,5 y 23/0,5 y registra la variación y los casos idénticos | `SemillaYTemperatura_PorConfiguracion` |
| Eco de instrucción con variantes | «dejá de lado», «pasá por alto», inglés (ignore, disregard, forget, instructions); el enrutador ya no manda «pasá por alto» a horarios | `EcoDeInstruccion_Variantes_Rechazadas`, ruteo de B46; casos B44–B46 en `bateria.v3` |
| Intercambio con cifras en palabras | (ya cubierto por el vínculo; queda fijado) | `Intercambio_CifrasEnPalabras_Rechazado` |
| `**<script>**` | `TextoMarcado` (componente del panel): el contenido de la negrita se escapa | `TextoMarcadoTests` (bUnit: `<script>`, `<b>`, `<img onerror>`) |

**Compuerta a ciegas en la batería.** Cada redactada se compara con la fija de la misma pregunta sobre los
mismos datos: orden X/Y sorteado por caso y corrida, sin citas ni negritas (la plantilla cita una sola vez y
delataría el origen); el juez es el mismo modelo, con temperatura 0. Un veredicto ilegible cuenta como peor;
dos textos iguales sin citas cuentan como iguales sin preguntar. Cada línea de `corrida-N.jsonl` guarda
`comparacion` (orden, veredicto, ganadora y la fija) y `bateria.sh` exige cero peores.

| Corrida | Semilla / temperatura | k/M | Inventados | Redactadas | Peor a ciegas |
| --- | --- | --- | --- | --- | --- |
| 1 | 7 / 0 | 46/46 | 0 | 6 | 0 |
| 2 | 11 / 0,5 | 46/46 | 0 | 1 | 0 |
| 3 | 23 / 0,5 | 46/46 | 0 | 1 | 0 |
| Línea base | — | 43/46 (B03, B04, B36) | 0 | — | — |

[MEDIDO, 2026-11-11, `bateria.v3`, 46 casos, umbral 43; p95 1,3–1,5 s]. Evidencia en
[`evidencia/2026-11-11-asistente-ronda3/`](../../evidencia/2026-11-11-asistente-ronda3/).

**Lectura honesta.** Con este modelo la redacción casi no aporta: lo que pasa los controles es, salvo las
citas, la plantilla misma, y con temperatura 0,5 cae a 1 redactada por corrida (los rechazos son sobre todo
«oración sin cita al final» y la nota de multirrespuesta). Ninguna redactada es peor que la fija, que es lo
que exige el criterio; que sea *mejor* no se demostró. Ningún caso dio el mismo texto en las tres corridas.

| Suite | Resultado |
| --- | --- |
| Compilación `-warnaserror` (`SinMaui.slnf`) | 0 avisos, 0 errores |
| Unitarias, bUnit y arquitectura | 338/338 (27 de las nuevas fallan con el código de la ronda 2) |
| API / ViewModels de Android | 13/13 · 18/18 |
| E2E Chromium / emulación móvil | 41/41 · 41/41 |

## 9. Cuarta ronda: la redacción con IA apagada por omisión

Decisión de la mesa sobre la ronda 3 medida: con este modelo la redacción sólo le agregaba «[1]» a la plantilla
y costaba ~1 s contra 2 ms. Queda apagada por omisión (`Asistente__Redaccion__Habilitada=false`, como en
Geometría; [ADR 0008](../adr/0008-redaccion-apagada-por-omision.md)); el camino del modelo queda detrás de la
bandera, con su compuerta, para un modelo futuro.

| Hallazgo | Cambio | Prueba que falla con el código de la ronda 3 |
| --- | --- | --- |
| La redacción no aporta y cuesta 500 veces más | bandera `Asistente:Redaccion:Habilitada`, `false` salvo `true` explícito; con la bandera apagada el redactor no se llama y la respuesta es la plantilla, sin aviso (la IA sigue habilitada para el extractor); `/health/asistente` informa `redaccion` | `Bandera_ApagadaSalvoTrueExplicito`, `BanderaPorOmision_DistribucionConLaPlantilla_SinLlamar`, `BanderaEnTrue_Redacta` |
| B17, B42: «no suman 100 %» con conteos y sin porcentajes, también en la plantilla | `Plantillas.NotaDeMultirrespuesta(conPorcentajes)`: con conteos, «los conteos pueden sumar más que el total de encuestas»; «no suman 100 %» queda para la pantalla de resultados, que muestra porcentajes. Al modelo le llega la nota de los conteos; el validador rechaza «%» o «porcentaje» si la plantilla no los tiene y exige la nota de conteos con sus palabras. Prompt `redactor.v4` | `NotaDeMultirrespuesta_ConConteos_SinPorcentajes` (B17, B42), `Mensaje_NotaDeConteos`, `Validador_NotaDeConteos`, `Forma_PorcentajeSinPorcentajes` |
| Una cita por oración con una sola fuente; espacio antes de «[» | `Citas.Normalizar`: un espacio antes de cada cita y, con una fuente, sólo la última | `Citas_Normalizar`, `Redactada_UnaSolaCita` |
| Sumar citas a la plantilla contaba como redactada (K5 CD-8) | `Citas.SoloSumaCitas` (sin citas, negritas, espacios ni punto final): la respuesta es la fija, `RedactadaConIa=false`, motivo «sólo suma citas» | `SoloSumaCitas_NoCuentaComoRedactada`, `Citas_SoloSumaCitas` (incluye B38 de esta ronda, sin el punto) |
| Compuerta a ciegas sin ejercer (K7 R9.8): 0 pares juzgados en la ronda 3 | 9 pares fijos de control (`pares-a-ciegas.v1.json`, 3 por forma, 6 peores y 3 no peores, la redactada siempre distinta) que se juzgan en cada corrida, con o sin modelo; además cada redactada distinta de la fija. Juez distinto del redactor: `BATERIA_JUEZ_MODELO` (otro modelo del servidor) o reglas deterministas declaradas R1–R8, que corren siempre al lado. 0 pares juzgados → NO EJECUTADO | la batería falla si `paresJuzgados == 0` o si las reglas no detectan los 6 peores de control |
| Sin tasa de rechazo del validador (K6 §12.10) | el resumen trae intentos, rechazos, tasa, «sólo citas» y motivos, en total y por forma | `bateria.sh` la imprime |
| Eco: faltaban «omití», «no hagas caso», «salteá», «override», «system prompt» (K7 R9.9) | se suman con voseo, tuteo, mayúsculas y acentos; también «skip», «prompt del sistema». `ValidadorDeCifras.Eco` es público para fijar los falsos positivos (K7 R9.10): «en ese caso», «Salta», «salteños», «ómnibus», «hacé caso», «el sistema registra» | `Eco_Variantes_Marcadas` (13), `Eco_FalsosPositivos_SinMarca` (9), `Eco_PalabraDeLaAyuda_NoEsEco`, `Validador_EcoNuevo_Rechazado` |

Con el código de la ronda 3 (más sólo la superficie de API nueva, con el comportamiento viejo, para que
compile) fallan 39 de las 396 unitarias.

**Batería ×3** (`bateria.v3`, 46 casos, umbral 43; semilla/temperatura 7/0, 11/0,5 y 23/0,5; juez por modelo
distinto del redactor, ambos por variable de entorno y fuera del repositorio):

| Camino | k/M | Inventados | Redactadas | Pares juzgados (control + redactadas) | Peores a ciegas | Validador: rechazos / intentos | p95 | Veredicto |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Plantilla (sin modelo), juez por reglas | 43, 43, 43 (B03, B04, B36) | 0 | 0 | 9, 9, 9 | 0 | — | 32–34 ms | CUMPLE |
| Modelo, **bandera apagada** (producción) | 46, 46, 46 | 0 | 0 | 9, 9, 9 | 0 | — | 73–111 ms | CUMPLE; distintas de la plantilla: ninguna |
| Modelo, bandera en `true` | 46, 45, 46 | 0 | 1, 2, 0 | 10, 11, 9 | 1 (B11, corrida 2) | 7/13, 10/13, 10/13 (54 %, 77 %, 77 %); sólo citas 5, 1, 3 | 1,7–3,9 s | NO CUMPLE |

[MEDIDO, 2026-11-11]. Evidencia en [`evidencia/2026-11-11-asistente-ronda4/`](../../evidencia/2026-11-11-asistente-ronda4/)
(`corrida-N.jsonl`, `control-N.jsonl`, `resumen-N.json`, `veredicto.json` por camino).

- **Bandera apagada = plantilla.** Con el modelo presente y la bandera apagada, cada respuesta (salvo el alta
  desde una frase, que usa el extractor) es palabra por palabra la de la plantilla en las tres corridas. Las
  tres diferencias con la línea base sin modelo (B03 y B04 leen la frase con el extractor; B36 espera el aviso de
  IA apagada) no son de redacción.
- **Bandera en true.** El redactor se llama 13 veces por corrida y el validador rechaza más de la mitad
  (sobre todo «oración sin cita al final» y la nota de multirrespuesta alterada); de lo que pasa, 1 a 5 por corrida
  sólo sumaban citas y ya no cuentan. Quedan 3 redactadas en 3 corridas; en B11 (corrida 2) el juez eligió la
  fija. Confirma la decisión: el camino no está listo para este modelo.
- **El juez.** Las reglas aciertan los 9 pares de control en cada corrida. El juez por modelo acierta 5, 4 y 7
  de 9: no ve el tuteo de C01 ni la nota invertida de C02. Es un juez distinto del redactor, como pide R9.8, pero
  no confiable sólo; por eso las reglas corren al lado y la batería exige que detecten los 6 peores.

| Suite | Resultado |
| --- | --- |
| Compilación `-warnaserror` (`SinMaui.slnf`) | 0 avisos, 0 errores |
| Unitarias, bUnit y arquitectura | 396/396 (39 fallan con el código de la ronda 3) |
| API / ViewModels de Android | 13/13 · 18/18 |
| E2E Chromium / emulación móvil | 41/41 · 41/41 |
