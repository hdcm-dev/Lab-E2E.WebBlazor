# Evidencia — MovilidadUrbana.MAUI en el teléfono (2026-09-12)

Capturas tomadas por `adb screencap` sobre el dispositivo de `dispositivo.txt` (Motorola moto e6 play,
armeabi-v7a, 720×1440), con el APK Debug compilado en el devcontainer (`.devcontainer/dev.sh build`) e
instalado por USB. Se tomaron a lo largo de varias iteraciones de mejora; cada archivo muestra el
estado **final** de lo que nombra, salvo que se indique lo contrario.

| Archivo | Qué muestra |
| --- | --- |
| `01-localidades-inicio.png` | La lista con las dos localidades sembradas, búsqueda, filtro por provincia, resumen «2 localidades» y el botón flotante «Agregar» |
| `02-filtrado-sin-resultados.png` | Estado «Ninguna localidad coincide» con «0 de 2 localidades» y la salida «Limpiar filtro» |
| `03-filtro-limpio.png` | Después de «Limpiar filtro»: la lista completa otra vez |
| `05-editor-errores.png` | Alta con todos los campos vacíos: banda de aviso, un error por campo y el requisito debajo (iteración previa, sin borde rojo) |
| `06-selector-provincia.png` | El selector nativo de provincia, con las siete del catálogo |
| `07-editor-completo.png` | El editor con los cuatro campos cargados (iteración previa: los errores todavía no se borraban al corregir) |
| `09-lista-con-goya.png` | La lista después del alta, con Goya ordenada entre Corrientes y Resistencia |
| `10-editor-edicion.png`, `11-editor-edicion-abajo.png` | Edición: título «Editar localidad», datos cargados y la acción «Eliminar localidad» al pie del formulario |
| `12-confirmar-baja.png` | El diálogo de confirmación de la baja |
| `13-baja-cancelada.png` | «Cancelar» deja el editor como estaba |
| `15-lista-sin-goya.png` | Confirmada la baja, la lista vuelve a dos localidades |
| `16-encuesta-paso1.png` | Paso 1 de 3: etiqueta del paso, contador, título, barra de progreso, campos con requisito |
| `17-encuesta-paso1-errores.png` | «Siguiente» con el paso incompleto: aviso, campos con borde rojo y su error |
| `18-encuesta-nombre-corregido.png` | Al corregir el nombre, su error y su borde desaparecen; el de edad sigue |
| `19-selector-localidad.png` | El selector de localidad, alimentado por el ABM |
| `21-encuesta-paso2-errores.png` | Paso 2 incompleto: el aviso y los errores de medios y frecuencia (iteración previa: el error debajo de la tarjeta) |
| `32-paso2-errores-junto-al-rotulo.png` | Paso 2 incompleto, versión final: el error junto al rótulo, antes de las opciones |
| `22-encuesta-paso2-completo.png` | Paso 2 con medios marcados y frecuencia elegida |
| `23-encuesta-paso3.png`, `24-encuesta-paso3-completo.png` | Paso 3: distancia y minutos lado a lado, motivo; «Registrar» reemplaza a «Siguiente». En 24 la distancia quedó «125» porque el teclado descartó la coma: el defecto que corrige `EntradaDecimal` |
| `33-paso3-coma-decimal-con-teclado.png` | Versión final: «12,5» con el teclado numérico abierto y la barra de navegación del asistente encima del teclado |
| `34-paso3-completo.png` | Paso 3 completo con «12,5» y «30» |
| `25-encuesta-resumen.png`, `26-encuesta-resumen-abajo.png` | Estado de éxito: «Encuesta completada», contador actualizado, resumen con los siete campos y «Cargar otra encuesta» |
| `35-resumen-coma-decimal.png` | El resumen con «12,5 km»: la coma decimal llegó entera al registro |
| `27-encuesta-nueva.png` | Después de «Cargar otra encuesta»: paso 1 vacío y el contador conservado |
| `29-editor-errores-bordes.png` | Versión final de los errores del editor: borde rojo en el campo, pestañas ocultas |
| `31-lista-con-mercedes.png` | La lista con una localidad más, dada de alta desde el teléfono |
| `36-editor-teclado-con-guardar.png` | Editor apilado con el teclado abierto: «Guardar» queda encima del teclado |
| `37-editor-sin-teclado.png` | Al cerrar el teclado, la barra vuelve al pie sin dejar hueco |
| `38-editor-teclado-campo-bajo.png` | Con el foco en el último campo: el contenido se desplaza, el encabezado no se corta y «Guardar» sigue visible |
| `toast-logcat.txt` | El aviso breve (Toast) del alta, que dura menos que un ciclo de captura: la prueba es la línea de `logcat` con `Toast#0 … producer=(…:ar.lab.movilidadurbana)` |

Lo que se corrigió a partir de estas capturas, en orden: el indicador de despliegue en los selectores y el
subrayado nativo dentro de las cajas (visible en `01`), los errores que no se borraban al corregir el
campo (`07` → `18`), el borde rojo del campo con error (`05` → `29`), el error de los grupos de opciones
debajo de la tarjeta (`21` → `32`), la coma decimal descartada por el teclado (`24` → `35`) y la barra de
acciones tapada por el teclado en la página apilada (`36`–`38`).
