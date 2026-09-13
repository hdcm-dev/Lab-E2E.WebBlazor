# Evidencia — MovilidadUrbana.MAUI.UITests en el teléfono (2026-09-13)

Suite Appium (`Appium.WebDriver` 8.0.0, servidor Appium 3.7.0, driver `uiautomator2@8.6.4`) corrida con
`.devcontainer/dev.sh uitests` contra la app Debug instalada en el Motorola moto e6 play
(Android 9, API 28, armeabi-v7a, 720×1440) conectado por USB.

| Archivo | Qué muestra |
| --- | --- |
| `corrida-a-5-de-5.log` | Primera corrida tras activar la app al abrir la sesión: 5/5 en verde, 3 min 21 s |
| `corrida-b-5-de-5.log`, `corrida-b-5-de-5.trx` | Segunda corrida seguida, sin tocar el teléfono: 5/5 en verde, 3 min 5 s |
| `falsificacion.log` | `NoAvanzaConElPaso1Vacio` modificada para esperar «Paso 2 de 3»: esa prueba en rojo (`Expected: "Paso 2 de 3" But was: "Paso 1 de 3"`), las otras 4 en verde. La modificación se revirtió |
| `*.png` | Las capturas que cada prueba adjunta en su punto de afirmación |

Corridas anteriores en rojo, y por qué (no se guardan: la causa era de la suite, no de la app):

1. El `[SetUpFixture]` estaba en un namespace hijo y no alcanzaba a las pruebas → «La sesión de Appium no está abierta».
2. Esperas de 30 s y el botón «Eliminar» debajo del pliegue → `UiScrollable` y esperas de 60 s.
3. Con `NoReset`, si el proceso quedaba vivo en segundo plano el driver no traía la app al frente
   («already running and noReset is enabled») → `ActivateApp` y espera de la primera pantalla.
