---
doc_id: E2E-02
doc_type: caso-de-diseño
title: Caso de prueba — la superficie Encuesta (asistente de tres pasos)
status: vigente
origin: agente
confidence: alta
owner: Lab-E2E.WebBlazor.Documentacion
last_review: 2026-09-04
audience: [desarrollo, qa]
traces: [E2E-00, E2E-01]
---

# Caso de prueba: la superficie Encuesta

**Superficie:** [`Components/Pages/Encuesta.razor`](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor)
· [`Encuesta.razor.cs`](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor.cs)
**Componente que la estructura:** [`Componentes/Asistente.razor`](../../src/MovilidadUrbana.Web/Components/Componentes/Asistente.razor)
**Pruebas:** [`tests/MovilidadUrbana.E2ETests/EncuestaTests.cs`](../../tests/MovilidadUrbana.E2ETests/EncuestaTests.cs)
**Qué tipo de superficie es:** interactiva (`InteractiveServer`), con estado direccionable

[`Beginner-Guide.md`](Beginner-Guide.md) explica **cómo se escribe** una prueba con Playwright y
[`Quick-Guide-ABM.md`](Quick-Guide-ABM.md) cómo se monta la de un ABM. Este documento trata otra
cosa: **cómo se decide qué probar** en una superficie que se recorre en tramos, y por qué los nueve
casos quedaron como quedaron.

## Índice

- **[1. Definiciones](#1-definiciones)** — acto divisible, paso, estado direccionable
- **[2. Qué promete esta superficie](#2-que-promete-esta-superficie)**
- **[3. La pregunta que este caso obliga a responder](#3-la-pregunta-que-este-caso-obliga-a-responder)** —
  ¿tres pasos son tres superficies?
- **[4. Los criterios de diseño de estos casos](#4-los-criterios-de-diseno-de-estos-casos)**
- **[5. El mapa de los casos](#5-el-mapa-de-los-casos)**
- **[6. Dos promesas sin caso](#6-dos-promesas-sin-caso)** — hallazgos, no resueltos
- **[7. Lo que estos casos deliberadamente no prueban](#7-lo-que-estos-casos-deliberadamente-no-prueban)**
- **[8. Cómo se corren](#8-como-se-corren)**
- **[9. Los criterios, en una lista](#9-los-criterios-en-una-lista)**

---

## 1. Definiciones

### 1.1 Superficie

**La pantalla como unidad de diseño**: lo que la persona ve, lo que puede hacer y en qué estados
puede quedar. No es «el componente» ni «la página»: un componente es una pieza de implementación, y
una superficie es una promesa hecha a alguien.

**Su criterio de corte:** el conjunto más chico de marcado que tiene **una promesa propia,
verificable de punta a punta**.

#### Dónde está cada parte, en este caso

| La definición dice | En el archivo es | Líneas |
| --- | --- | --- |
| **lo que la persona ve** | El encabezado, con el título y el contador de registradas | [8–16](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor#L8-L16) |
| **lo que puede hacer** | Los tres pasos del formulario, dentro del asistente | [29–155](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor#L29-L155) |
| **en qué estados puede quedar** | El corte entre *cargando la encuesta* y *encuesta registrada* | [27](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor#L27) y [157–185](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor#L157-L185) |

#### Qué **no** es superficie acá

| No es superficie | Qué es | Por qué |
| --- | --- | --- |
| `Asistente` | **Componente** | Estructura pasos, cuenta y dibuja botones. No promete nada por sí solo: no sabe qué se está cargando |
| `PasoDeAsistente` | **Componente** | Muestra su contenido cuando le toca. Es un `@if` con semántica |
| `Campo`, `Banda`, `Insignia` | **Componentes** | Reutilizables en todo el proyecto |
| Cada uno de los tres pasos | **Un estado** de la superficie | §3 |

> **El `Asistente` es el ejemplo más limpio de la distinción.** Es un archivo de 109 líneas, con
> lógica propia, tres estados de botón y navegación entre pasos — y aun así **no es una
> superficie**, porque no hay ninguna frase de la forma «hago X y pasa Y» que se pueda afirmar de
> él sin saber qué encuesta está estructurando. Tamaño y complejidad no hacen una superficie: la
> hace tener una promesa.

### 1.2 Acto divisible

**Una sola operación que se completa en tramos, por comodidad de carga.** No son varias
operaciones encadenadas: es una, partida.

El propio componente declara su ámbito de uso en su primera línea:

> *«Asistente de varios niveles. **Solo para actos divisibles**: el paso es un estado direccionable
> de la superficie.»*
> — [`Asistente.razor` 1–2](../../src/MovilidadUrbana.Web/Components/Componentes/Asistente.razor#L1-L2)

**La prueba para reconocer uno:** ¿el tramo intermedio le entrega algo a la persona? Si al terminar
el paso 2 no pasó nada que ella pueda usar, no era una operación: era un tramo.

### 1.3 Paso

**Uno de los tramos del acto divisible, y a la vez un estado de la superficie.** Acá son tres, y su
cantidad es una constante del dominio:
[`ReglasDeEncuesta.TotalDePasos`](../../src/MovilidadUrbana.Web/Dominio/Reglas/ReglasDeEncuesta.cs#L6).

### 1.4 Estado direccionable

**Un estado que además tiene dirección propia**, para poder llegar a él sin recorrer el camino.

```razor
@page "/encuesta"
@page "/encuesta/{Paso:int}"
```

El motivo está escrito, y es un motivo de verificabilidad:

> *«El acto es divisible y reanudable dentro de la sesión, y el paso vigente está en la dirección
> **para que se pueda verificar de a uno**.»*
> — [`Encuesta.razor` 3–4](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor#L3-L4)

**Y una advertencia que ordena mucho:** que un estado tenga dirección **no lo convierte en
superficie**. La ruta y la superficie coinciden a menudo, y por eso se confunden — pero son cosas
distintas: un modal es superficie sin ruta, y un paso es ruta sin superficie.

---

## 2. Qué promete esta superficie

Antes de escribir una prueba hay que poder decir en una frase qué promete la pantalla. Si no se
puede, no está lista para probarse.

> **Cargo mis datos de viaje en tres tramos, puedo volver atrás sin perder nada, y al terminar la
> encuesta queda registrada.**

Es una promesa **positiva**, como la del Hola Mundo del laboratorio hermano, pero con dos
particularidades que se pagan en casos de prueba:

| Rasgo | Consecuencia |
| --- | --- |
| **«en tres tramos»** | El progreso es parte de la promesa, no un detalle: hay casos sobre el indicador |
| **«sin perder nada»** | Es una promesa sobre la **memoria** de la superficie, y se verifica volviendo |
| **«queda registrada»** | Cruza al servidor: se verifica **recargando**, no mirando la pantalla |

Y una promesa secundaria, que el código enuncia y la frase de arriba no:

> **No se puede saltear un tramo.**

Está escrita textualmente en el `<summary>` de
[`OnParametersSet`](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor.cs#L66-L70) —
*«un paso pedido por dirección no puede saltear los anteriores»*— y **no tiene caso de prueba**
(§6.1).

---

## 3. La pregunta que este caso obliga a responder

### 3.1 ¿Tres pasos son tres superficies?

**Respuesta: no. Son tres estados de una sola superficie, porque el acto es uno solo.**

Es la pregunta central de este caso, y la respuesta no es obvia — de hecho **contradice una regla
demasiado simple** que circula: «un asistente de N pasos son N superficies y un recorrido».

Esa regla vale para un **recorrido de promesas distintas**. No vale para un **acto divisible**. La
diferencia se decide con la prueba de corte de §1.1: *partilo y mirá si alguna mitad sigue
prometiendo algo*.

| Paso | ¿Promete algo por sí solo? |
| --- | --- |
| 1 — Datos de la persona | **No.** Cargar nombre y edad no le entrega nada a nadie |
| 2 — Medios que utiliza | **No.** Tildar casillas no produce ningún desenlace |
| 3 — Distancia recorrida | **No.** Sin los dos anteriores no hay encuesta que registrar |
| Los tres juntos | **Sí:** «queda registrada» |

**Ninguna mitad promete nada. Entonces no eran partes separables: era una sola superficie.**

### 3.2 ¿Cuándo sí serían superficies distintas?

**Respuesta: cuando cada paso le entregue algo a la persona antes del siguiente.**

| | Ejemplo | Qué es |
| --- | --- | --- |
| ✅ una superficie | Esta encuesta: tres tramos, un solo registro al final | Acto divisible |
| ✅ una superficie | Un pago en dos pantallas: datos y confirmación | Acto divisible |
| ❌ varias superficies | «Buscar un vuelo» → «elegir asiento» → «pagar»: entre una y otra **hay un resultado que la persona usa** | Recorrido |
| ❌ varias superficies | Un alta que al terminar el paso 1 **ya crea el registro** en borrador y lo muestra en la lista | Recorrido |

**La señal más confiable:** si al abandonar en el medio queda algo hecho, eran superficies
distintas. Si al abandonar en el medio **no queda nada**, era una sola.

Acá no queda nada: la respuesta se registra recién en
[`FinalizarAsync`](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor.cs#L105-L127).

### 3.3 ¿Y entonces qué gana el diseño de las pruebas?

**Respuesta: que hay una sola promesa central y ocho casos que la rodean, en vez de tres suites.**

| Si fueran tres superficies | Siendo una |
| --- | --- |
| Tres fixtures, con su propio punto de partida | Un `[SetUp]` que abre `/encuesta` |
| «Probar el paso 2» sería un objetivo | El paso 2 **no se prueba solo**: se prueba lo que la superficie promete al pasar por él |
| El recorrido completo sería una prueba de integración aparte | El recorrido completo **es el caso central** |

---

## 4. Los criterios de diseño de estos casos

### 4.1 ¿Qué se mira para saber si la superficie cumplió?

**Respuesta: lo que la persona vería. Y para lo que cruza al servidor, lo que sobrevive a una
recarga.**

| | |
| --- | --- |
| ✅ | `Expect(Page.GetByTestId("resumen-persona")).ToHaveTextAsync("Ana Pérez (34 años)")` |
| ✅ | Recargar y volver a leer el contador — [líneas 173–174](../../tests/MovilidadUrbana.E2ETests/EncuestaTests.cs#L173-L174) |
| ❌ | Consultar la base de datos desde la prueba |
| ❌ | Inspeccionar `_modelo` o `_pasoMaximoAlcanzado` |

**La recarga es el detalle fino.** Que el contador diga «Registradas: 1» podría ser estado en
memoria del circuito. Recargar tira el circuito y lo vuelve a pedir: **eso** demuestra que la
respuesta cruzó al servidor, sin que la prueba tenga que mirar la base.

### 4.2 ¿Cómo se prueba una promesa sobre la memoria?

**Respuesta: yendo y volviendo. Nunca leyendo el modelo.**

«Puedo volver atrás sin perder nada» no se ve en ningún estado: se ve **entre dos visitas al mismo
estado**. El caso lo hace literal — avanza dos pasos, retrocede, y comprueba que lo cargado sigue:

```csharp
await Page.GetByTestId("boton-anterior").ClickAsync();
await Expect(Page.GetByTestId("medio-colectivo")).ToBeCheckedAsync();
await Expect(Page.GetByTestId("campo-frecuencia")).ToHaveValueAsync("diaria");
```

**Es la misma forma que una promesa negativa** —se decide comparando dos observaciones, no
mirando una—, aunque acá lo comparado sea *el antes y el después* en vez de *dos desenlaces
distintos*.

### 4.3 ¿A quién le pertenece cada identificador de prueba?

**Respuesta: al que lo puede garantizar. Y acá hay dos dueños.**

| Identificador | Vive en | Quién lo garantiza |
| --- | --- | --- |
| `boton-siguiente`, `boton-anterior`, `boton-finalizar`, `etiqueta-paso`, `data-paso` | El **componente** `Asistente` | Todo asistente del proyecto, no esta encuesta |
| `campo-nombre`, `medio-colectivo`, `resumen-motivo` | La **superficie** `Encuesta` | Solo esta encuesta |

**Por qué importa:** los del componente son un **contrato reutilizable**. Si mañana otra superficie
usa el `Asistente`, sus pruebas van a poder decir `boton-siguiente` sin coordinar con nadie. Y si
alguien los cambia, rompe **todas** las superficies que lo usan a la vez — que es exactamente lo
que debe pasar.

**El corolario práctico:** un helper como `SiguienteAsync()` en la clase de prueba
([línea 33](../../tests/MovilidadUrbana.E2ETests/EncuestaTests.cs#L33)) pertenece al componente, no
al caso. Cuando aparezca la segunda superficie con asistente, sube a la base.

### 4.4 ¿Cuánto detalle se afirma sobre el progreso?

**Respuesta: los tres estados de paso, porque son la promesa «en tres tramos» hecha visible.**

```csharp
await Expect(pasos.Nth(0)).ToHaveAttributeAsync("aria-current", "step");
await Expect(pasos.Nth(1)).ToHaveClassAsync(new Regex("mq-paso--pendiente"));
// …y tras avanzar:
await Expect(pasos.Nth(0)).ToHaveClassAsync(new Regex("mq-paso--completado"));
```

Esto **parece** violar la regla de no atarse a clases de presentación, y conviene mirarlo de cerca:

| | |
| --- | --- |
| ❌ Atarse a la presentación | Afirmar `mq-btn--primario` porque el botón se ve azul |
| ✅ Afirmar un estado nombrado | `mq-paso--completado` es **el vocabulario de estados del catálogo**, no una decisión visual |

La diferencia es que `completado / actual / pendiente` es una **enumeración de dominio de la
interfaz**: renombrarla es cambiar la promesa, no repintar. Aun así, es la aserción más frágil de
la suite, y si el proyecto quisiera blindarla, el camino es un atributo propio —`data-estado-paso`—
en vez de la clase.

**El `aria-current="step"` es la mitad robusta**: es estándar, y afirmarlo verifica de paso que la
superficie es navegable con lector de pantalla.

### 4.5 ¿Cada paso lleva su propio caso de validación?

**Respuesta: sí, porque cada paso tiene reglas propias y falla por motivos distintos.**

| Caso | Paso | Qué reglas ejercita |
| --- | --- | --- |
| `NoAvanzaDelPaso1ConDatosInvalidos` | 1 | Nombre corto, edad fuera de rango, localidad vacía |
| `NoAvanzaDelPaso2SinMediosNiFrecuencia` | 2 | Conjunto vacío + selección faltante |
| `NoFinalizaConElPaso3Incompleto` | 3 | Distancia fuera de rango + minutos faltantes |

**No es repetición.** Son tres promesas distintas de
[`ServicioDeEncuestas.ValidarPaso`](../../src/MovilidadUrbana.Web/Aplicacion/Encuestas/ServicioDeEncuestas.cs#L16),
y cada una falla por su cuenta. Un solo caso que recorriera los tres con datos malos fallaría por
tres motivos y el reporte no diría cuál.

**Y el error del paso 2 tiene una forma propia** que vale la pena notar: la regla es **del
conjunto** de casillas, así que el error se asocia al `fieldset` y no a una casilla —
[`Encuesta.razor` 75–95](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor#L75-L95).
La prueba lo respeta afirmando `error-medios`, uno solo, y no un error por casilla.

### 4.6 ¿Qué pasa con la hidratación?

**Respuesta: está resuelta en la base, y por eso no aparece en ningún caso.**

Esta superficie es `InteractiveServer`, así que tiene la ventana entre el HTML pintado y el
circuito abierto. Pero
[`PruebaE2E`](../../tests/MovilidadUrbana.E2ETests/Infraestructura/PruebaE2E.cs) ya declara que
aporta *«la espera a que el circuito de Blazor esté conectado antes de tocar nada»*, y lo hace en
`IrAAsync`.

**Es la decisión correcta y conviene nombrarla:** cuando una precondición vale para **todas** las
superficies del proyecto, va en la base y no en cada `[SetUp]`. Lo que no puede pasar es que no
esté en ninguno de los dos lados — y ahí es donde nacen las intermitencias.

---

## 5. El mapa de los casos

| # | Caso | Qué afirma |
| --- | --- | --- |
| 1 | `ArrancaEnElPaso1ConElAnteriorDeshabilitado` | El punto de partida, y que no se puede retroceder desde el principio |
| 2 | `ElDesplegableDeLocalidadesSeAlimentaDelAbm` | Que las dos pantallas comparten almacén |
| 3 | `NoAvanzaDelPaso1ConDatosInvalidos` | Las reglas del tramo 1 |
| 4 | `NoAvanzaDelPaso2SinMediosNiFrecuencia` | Las reglas del tramo 2, incluida la del conjunto |
| 5 | `PermiteVolverAtrasConservandoLoCargado` | **La memoria**: «sin perder nada» |
| 6 | `ElIndicadorDePasosAcompanaElAvance` | «En tres tramos», hecho visible |
| 7 | `NoFinalizaConElPaso3Incompleto` | Las reglas del tramo 3 |
| 8 | `RecorreLosTresPasosMuestraElResumenYRegistraLaRespuesta` | **La promesa central**, y que cruzó al servidor |
| 9 | `NuevaEncuestaDevuelveElAsistenteAlPaso1` | Que reiniciar limpia el acto pero no lo registrado |

### 5.1 ¿Por qué el caso 2 está en esta suite y no en la del ABM?

**Respuesta: porque la promesa que verifica es de esta superficie, aunque el dato venga de otra.**

«El desplegable se alimenta del ABM» es algo que **esta** pantalla promete. Que el dato lo produzca
la pantalla de localidades es implementación. Si mañana las localidades vinieran de un servicio
externo, el caso seguiría siendo válido y seguiría viviendo acá.

**La regla:** un caso vive donde está la promesa que verifica, no donde está el dato que usa.

---

## 6. Dos promesas sin caso

Hallazgos de escribir este documento. **Anotados, no resueltos**: escribir los casos es una
decisión de alcance, y este documento no la toma.

### 6.1 El paso direccionable no se ejercita nunca

La superficie declara dos rutas —`/encuesta` y `/encuesta/{Paso:int}`— y una regla explícita sobre
la segunda:

> *«Un paso pedido por dirección no puede saltear los anteriores: eso es lo que la barra de
> validación de la maqueta permitía y el producto no, porque el paso siguiente depende de que el
> anterior esté válido.»*
> — [`Encuesta.razor.cs` 66–70](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor.cs#L66-L70)

**Verificado el 2026-09-04:** ninguna de las nueve pruebas navega a `/encuesta/2` ni a `/encuesta/3`.
El `[SetUp]` abre siempre `/encuesta`, y todos los cambios de paso ocurren por botón. El
`Math.Min(pedido, _pasoMaximoAlcanzado)` de la
[línea 76](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor.cs#L76) —que es el que
impide el salteo— **no está cubierto**.

Es doblemente llamativo porque el motivo declarado de que el paso sea direccionable es *«para que
se pueda verificar de a uno»*. **La capacidad se construyó para la prueba, y la prueba no la usa.**

Faltarían dos casos: que `/encuesta/3` sin haber pasado por 1 y 2 caiga en el paso 1, y que
`/encuesta/2` sí funcione después de haber completado el paso 1.

### 6.2 El fallo al registrar no se ejercita

[`FinalizarAsync`](../../src/MovilidadUrbana.Web/Components/Pages/Encuesta.razor.cs#L105-L127) tiene
un `catch` que registra el error y muestra un aviso —*«No pudimos registrar la encuesta. Volvé a
intentar en unos segundos.»*—, y el `Asistente` tiene un estado de botón entero para el envío en
curso (`boton-procesando`).

**Verificado el 2026-09-04:** ninguna prueba menciona `boton-procesando` ni ese aviso.

Es una **ausencia con motivo, no un olvido**: provocar el fallo desde el navegador exige un punto
de inyección que la aplicación hoy no tiene. Pero conviene que quede escrito, porque el estado de
error de una operación es de los que más se rompen y menos se miran.

---

## 7. Lo que estos casos deliberadamente no prueban

| No se prueba | Tipo | Por qué |
| --- | --- | --- |
| Las **reglas de rango** una por una | Otra herramienta | Están cubiertas en [`ReglasDeEncuestaTests`](../../tests/MovilidadUrbana.UnitTests/ReglasDeEncuestaTests.cs), donde son baratas. La E2E verifica que la superficie **las use**, no que sean correctas |
| El **paso direccionable** | **Pendiente** | §6.1 |
| El **fallo al registrar** | **Pendiente, con motivo** | §6.2 — falta el punto de inyección |
| El **estado de envío en curso** | Otra clase de verificación | Es un tránsito: afirmarlo obliga a atrapar un instante, y ahí nacen las pruebas intermitentes |
| La **accesibilidad** completa | Otra herramienta | El `aria-current` entra de rebote en el caso 6; el resto tiene su propio cuerpo normativo |
| El **aislamiento entre sesiones** | Ya está | Lo garantiza `PruebaE2E` dándole una sesión propia a cada caso |

La primera fila es la más importante de todas, y es un criterio general: **la E2E no verifica que la
regla de negocio sea correcta; verifica que la superficie la aplique.** Que la edad mínima sea 16 lo
comprueba una prueba unitaria en milisegundos. Que la pantalla no deje avanzar con 12 lo comprueba
esta.

---

## 8. Cómo se corren

```bash
scripts/pruebas.sh                      # chromium
scripts/pruebas.sh firefox
EMULAR_MOVIL=true scripts/pruebas.sh    # chromium emulando un Pixel 7
TRAZAR=false scripts/pruebas.sh         # sin traza de Playwright
```

No hace falta publicar antes: compilar el proyecto de pruebas publica la aplicación bajo prueba. La
traza se graba siempre y se conserva solo en los casos que fallan; se abre con
`playwright show-trace`.

---

## 9. Los criterios, en una lista

1. **Afirmá el efecto, no el mecanismo.** Y si el efecto cruza al servidor, **recargá**: es la forma
   de distinguir lo persistido de lo que vive en el circuito.
2. **Contá las promesas, no los pasos.** Un acto divisible es una superficie; un recorrido de
   promesas distintas son varias.
3. **Si al abandonar en el medio no queda nada, era una sola superficie.**
4. **Una ruta propia no hace una superficie**, igual que un modal sin ruta no deja de serlo.
5. **Una promesa sobre la memoria se prueba yendo y volviendo**, nunca leyendo el modelo.
6. **El identificador pertenece a quien lo puede garantizar**: los del componente son contrato
   reutilizable; los de la superficie, de ella sola.
7. **Un caso, un motivo de falla.** Por eso hay tres casos de validación y no uno.
8. **La E2E no verifica que la regla sea correcta; verifica que la superficie la aplique.**
9. **Una precondición que vale para todas las superficies va en la base**, no en cada `[SetUp]`.
10. **Cada promesa escrita en el `src` es un caso esperando a ser escrito** — §6 son dos de ellas.

---

> **De dónde viene este vocabulario.** Los términos *superficie*, *promesa* y *estado* no se
> inventaron acá; su procedencia —especificación formal, diseño de interacción y prueba
> automatizada— está tratada en el laboratorio hermano, en
> `LAB/Lab-E2E.WebBlazor.Base/Guides/Marco-La-Superficie-Verificable.md`.
