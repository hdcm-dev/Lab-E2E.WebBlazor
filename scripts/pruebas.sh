#!/usr/bin/env bash
# Corre las pruebas E2E en máquinas sin SDK ni navegadores instalados.
#
#   scripts/pruebas.sh                      # chromium
#   scripts/pruebas.sh firefox
#   NAVEGADOR=webkit scripts/pruebas.sh
#   EMULAR_MOVIL=true scripts/pruebas.sh    # chromium con emulación de Pixel 7
#
# Usa la imagen oficial de Playwright, que trae las librerías de sistema que los navegadores
# necesitan, y le agrega el SDK de .NET en `.dotnet/` (ignorado por git). Los navegadores quedan
# en `.navegadores/`, así que solo se descargan la primera vez.
#
# No hace falta publicar antes: compilar el proyecto de pruebas publica la aplicación bajo prueba.
#
# En CI no hace falta nada de esto: el runner ya tiene el SDK y Playwright instala los navegadores.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION_PLAYWRIGHT="${VERSION_PLAYWRIGHT:-1.62.1}"
IMAGEN="${IMAGEN_E2E:-mcr.microsoft.com/playwright:v${VERSION_PLAYWRIGHT}-noble}"
NAVEGADOR="${NAVEGADOR:-${1:-chromium}}"

exec docker run --rm -i \
  --user "$(id -u):$(id -g)" \
  --ipc=host \
  --env HOME=/tmp \
  --env DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  --env DOTNET_NOLOGO=1 \
  --env NUGET_PACKAGES=/trabajo/.nuget \
  --env PLAYWRIGHT_BROWSERS_PATH=/trabajo/.navegadores \
  --env EMULAR_MOVIL="${EMULAR_MOVIL:-false}" \
  --env URL_BASE="${URL_BASE:-}" \
  --env CARPETA_APLICACION="${CARPETA_APLICACION:-}" \
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

PROYECTO=tests/MovilidadUrbana.E2ETests
dotnet build "$PROYECTO" --configuration Debug

CLI="$PROYECTO/bin/Debug/net10.0/.playwright"
echo "== Asegurando el navegador '"$NAVEGADOR"' =="
"$CLI/node/linux-x64/node" "$CLI/package/cli.js" install '"$NAVEGADOR"'

dotnet test "$PROYECTO" \
  --no-build \
  --settings pruebas.runsettings \
  -- Playwright.BrowserName='"$NAVEGADOR"'
'
