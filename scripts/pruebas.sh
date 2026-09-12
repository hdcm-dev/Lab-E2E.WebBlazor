#!/usr/bin/env bash
# Corre las pruebas E2E en máquinas sin SDK ni navegadores instalados.
#
#   scripts/pruebas.sh                      # Movilidad Urbana, chromium
#   scripts/pruebas.sh firefox
#   NAVEGADOR=webkit scripts/pruebas.sh
#   EMULAR_MOVIL=true scripts/pruebas.sh    # chromium con emulación de Pixel 7
#
#   PROYECTO=holamundo scripts/pruebas.sh   # la superficie Hola Mundo
#   PROYECTO=login scripts/pruebas.sh       # la superficie de acceso
#   REPETIR=8 PROYECTO=login scripts/pruebas.sh   # la batería ocho veces seguidas
#
# Usa la imagen oficial de Playwright, que trae las librerías de sistema que los navegadores
# necesitan, y le agrega el SDK de .NET en `.dotnet/` (ignorado por git). Los navegadores quedan
# en `.navegadores/`, así que solo se descargan la primera vez.
#
# Los tres proyectos no se corren igual, y a propósito: tienen grados de complejidad distintos.
#   - Movilidad Urbana levanta la aplicación desde su propio fixture, en un puerto libre, y no
#     hace falta nada de este lado.
#   - Hola Mundo y Login no tienen fixture: la prueba apunta a una URL fija, así que este script
#     levanta la aplicación en esa URL antes de probar y la apaga al terminar. La URL no se elige
#     acá: se copia de lo que la prueba espera, y cambiarla de un lado obliga a cambiarla del otro.
#
# REPETIR sirve para buscar intermitencias: una prueba que pasó una vez no probó nada.
#
# En CI no hace falta nada de esto: cada proyecto tiene su propio workflow.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION_PLAYWRIGHT="${VERSION_PLAYWRIGHT:-1.62.1}"
IMAGEN="${IMAGEN_E2E:-mcr.microsoft.com/playwright:v${VERSION_PLAYWRIGHT}-noble}"
NAVEGADOR="${NAVEGADOR:-${1:-chromium}}"
PROYECTO="${PROYECTO:-movilidad}"
REPETIR="${REPETIR:-1}"

case "$PROYECTO" in
  movilidad)
    PRUEBAS=tests/MovilidadUrbana.E2ETests
    APP=
    URL_APP=
    ;;
  holamundo)
    PRUEBAS=tests/WebBlazor.E2E.Base.HolaMundo.E2ETests
    APP=src/WebBlazor.E2E.Base.HolaMundo
    URL_APP=http://localhost:5027      # HolaMundoE2ETests.cs
    ;;
  login)
    PRUEBAS=tests/WebBlazor.E2E.Base.Login.E2ETests
    APP=src/WebBlazor.E2E.Base.Login
    URL_APP=http://localhost:5181      # PruebaDeSuperficie.UrlBase
    ;;
  *)
    echo "Proyecto desconocido: $PROYECTO (esperaba movilidad, holamundo o login)" >&2
    exit 2
    ;;
esac

exec docker run --rm -i \
  --user "$(id -u):$(id -g)" \
  --ipc=host \
  --env HOME=/tmp \
  --env DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  --env DOTNET_NOLOGO=1 \
  --env DOTNET_USE_POLLING_FILE_WATCHER="${DOTNET_USE_POLLING_FILE_WATCHER:-}" \
  --env NUGET_PACKAGES=/trabajo/.nuget \
  --env PLAYWRIGHT_BROWSERS_PATH=/trabajo/.navegadores \
  --env EMULAR_MOVIL="${EMULAR_MOVIL:-false}" \
  --env URL_BASE="${URL_BASE:-}" \
  --env CARPETA_APLICACION="${CARPETA_APLICACION:-}" \
  --env PUBLICAR_ANTES_DE_PROBAR="${PUBLICAR_ANTES_DE_PROBAR:-}" \
  --env NAVEGADOR="$NAVEGADOR" \
  --env PRUEBAS="$PRUEBAS" \
  --env APP="$APP" \
  --env URL_APP="$URL_APP" \
  --env REPETIR="$REPETIR" \
  --volume "$RAIZ:/trabajo" \
  --workdir /trabajo \
  "$IMAGEN" bash -lc '
set -euo pipefail
export PATH="/trabajo/.dotnet:$PATH"
export DOTNET_ROOT=/trabajo/.dotnet

if [ ! -x /trabajo/.dotnet/dotnet ]; then
  echo "== Instalando el SDK de .NET en .dotnet/ (solo la primera vez) =="
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /trabajo/.dotnet --no-path
fi

dotnet build "$PRUEBAS" --configuration Debug

if [ -n "$APP" ]; then
  # Sin fixture, nadie descarga el navegador por la prueba: se lo pide al CLI del propio paquete,
  # que baja la build que corresponde a su versión.
  cli="$PRUEBAS/bin/Debug/net10.0/.playwright"
  "$cli/node/linux-x64/node" "$cli/package/cli.js" install "$NAVEGADOR"

  dotnet build "$APP" --configuration Debug
  echo "== Levantando $APP en $URL_APP =="
  ASPNETCORE_URLS="$URL_APP" ASPNETCORE_ENVIRONMENT=Development \
    dotnet run --project "$APP" --no-build --no-launch-profile > /tmp/aplicacion.log 2>&1 &
  PID_APP=$!
  trap "kill $PID_APP 2>/dev/null || true" EXIT

  for _ in $(seq 1 60); do curl -fs -o /dev/null "$URL_APP/" && break || sleep 1; done
  if ! curl -fs -o /dev/null "$URL_APP/"; then
    echo "La aplicación no respondió en $URL_APP. Log:" >&2
    tail -20 /tmp/aplicacion.log >&2
    exit 1
  fi
fi

FALLAS=0
for N in $(seq 1 "$REPETIR"); do
  [ "$REPETIR" -gt 1 ] && echo "== Corrida $N de $REPETIR =="
  dotnet test "$PRUEBAS" \
    --no-build \
    --settings pruebas.runsettings \
    -- Playwright.BrowserName="$NAVEGADOR" || FALLAS=$((FALLAS + 1))
done

[ "$REPETIR" -gt 1 ] && echo "== Resumen: $FALLAS de $REPETIR corridas en rojo =="
exit $([ "$FALLAS" -eq 0 ] && echo 0 || echo 1)
'
