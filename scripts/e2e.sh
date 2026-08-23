#!/usr/bin/env bash
# Ejecuta npm / Playwright dentro del contenedor oficial, para máquinas sin Node instalado.
#
#   scripts/e2e.sh npm ci
#   scripts/e2e.sh npx playwright test
#   scripts/e2e.sh npx playwright test --project=chromium
#
# La imagen ya trae Node y los navegadores con sus dependencias del sistema. La aplicación bajo
# prueba es el binario autocontenido de `publicacion/`, que no necesita el runtime de .NET.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION_PLAYWRIGHT="${VERSION_PLAYWRIGHT:-1.62.1}"
IMAGEN="${IMAGEN_E2E:-mcr.microsoft.com/playwright:v${VERSION_PLAYWRIGHT}-noble}"

exec docker run --rm -i \
  --user "$(id -u):$(id -g)" \
  --ipc=host \
  --env HOME=/tmp \
  --env CI="${CI:-}" \
  --env URL_BASE="${URL_BASE:-}" \
  --env npm_config_cache=/tmp/.npm \
  --volume "$RAIZ:/trabajo" \
  --workdir /trabajo \
  "$IMAGEN" "$@"
