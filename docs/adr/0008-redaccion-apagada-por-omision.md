---
adr: 0008
alias: ADR-REDACCION-APAGADA
estado: aceptada por la mesa (ronda 4); default vigente hasta la firma del responsable del producto
fecha: 2026-11-11
---

# ADR 0008 · La redacción con IA queda apagada por omisión

## Contexto

En la ronda 3 la redacción quedó limitada a medios, motivos y frecuencias, detrás de la compuerta «la
redactada no puede ser peor que la fija». Medido con el modelo del laboratorio, lo que pasó los controles fue
la plantilla con «[1]» agregado: 6, 1 y 1 redactadas en tres corridas, todas idénticas a la fija sin las
citas, a un costo de ~1 s por turno contra 2 ms de la plantilla. La compuerta a ciegas no juzgó ningún par
(todas cayeron en el atajo «idénticas sin citas»). Ver
[`verificacion-2026-11-11.md` §8 y §9](../asistente/verificacion-2026-11-11.md).

## Decisión

`Asistente__Redaccion__Habilitada` vale `false` salvo un `true` explícito (cualquier otro valor la apaga),
como en Geometría. Con la bandera apagada:

- las distribuciones responden con la plantilla, palabra por palabra, y el modelo no se llama;
- las cifras, los destinos, las aclaraciones, el glosario y el alta desde una frase (que usa el extractor, no
  el redactor) siguen igual; no hay aviso de IA apagada, porque no lo está;
- `/health/asistente` informa `redaccion: false`.

El camino del modelo queda detrás de la bandera, con todos sus controles (validador, una cita por fuente,
«sólo suma citas no cuenta como redactada», compuerta a ciegas con pares de control), para un modelo futuro.

## Alternativas descartadas

- **Dejarla prendida** porque «no es peor»: no es mejor, cuesta 500 veces más y con temperatura > 0 el
  validador rechaza entre el 54 % y el 77 % de los intentos (ronda 4).
- **Borrar el camino**: se pierde la compuerta ya armada; se mantiene apagada y probada.

## Deja de valer si

Con otro modelo, `Asistente__Redaccion__Habilitada=true tools/asistente/bateria.sh` da CUMPLE con pares
juzgados > 0 por forma, 0 peores a ciegas con un juez distinto del redactor y una tasa de rechazo que
justifique la latencia (K3 R2.8, K5 CD-8, K7 R9.8). Además:

- **La bandera no se prende mientras el juez por modelo no detecte los pares peores de control** en las tres
  corridas: los 6 de `pares-a-ciegas.v1` (C01–C03, C05, C07, C08) y, desde los pendientes de la ronda 4, los
  8 de `pares-a-ciegas.v2` (suma C10, aclaración perdida, y C12, redundancia). En la ronda 4 acertó 5, 4 y 7
  de 9 pares; un juez que no ve el tuteo ni la nota invertida no habilita nada. Con los pendientes [MEDIDO, 2026-11-11]
  detectó 4, 3 y 4 de los 8 peores; las reglas, 8 de 8 en las tres corridas. Con la bandera en true las reglas
  marcan B11 (corrida 1) y B40 (corrida 2) como peores: k = 45, 45 y 46 de 46 y NO CUMPLE; con la bandera
  apagada, 46/46 y 0 diferencias de redacción sobre 44 respuestas comparadas con la plantilla. `bateria.sh` lo informa
  («juez …: N de 8») y lo deja en `veredicto.json` (`juezDetectaTodos`).
- **El veredicto lo deciden las reglas deterministas declaradas**, no el juez por modelo: R1–R8 de la ronda 4
  (registro ajeno, porcentajes sin porcentajes, nota invertida, número sin origen, cifra cruzada, largo,
  trigrama repetido, eco de instrucción) más R9 (pierde una aclaración del otro texto, como «“Bondi” se
  entiende como colectivo» en B11) y R10 (redundancia, «la frecuencia más frecuente» en B40). El juez por
  modelo se registra al lado de cada par (`peoresSegunElJuez`) y sólo sirve para la condición anterior.

## Pruebas que la sostienen

`Bandera_ApagadaSalvoTrueExplicito`, `BanderaPorOmision_DistribucionConLaPlantilla_SinLlamar`,
`BanderaPorOmision_GlosarioYDestinos_SinModelo`, `BanderaEnTrue_Redacta` (en `RedaccionApagadaTests`) y la
batería ×3 con la bandera apagada: «diferencias de redacción: 0» sobre las respuestas comparadas una por una
con la plantilla. Las reglas R1–R10 y sus pares de control se prueban en `PendientesDeLaRonda4Tests`
(`Reglas_*`), que corren en la CI.
