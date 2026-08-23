#!/usr/bin/env bash
# Publica la aplicación como binario autocontenido en `publicacion/`.
#
# Autocontenido a propósito: así el mismo artefacto corre dentro del contenedor de Playwright,
# que trae los navegadores pero no el runtime de .NET. Es lo que hace que las pruebas usen en
# todos lados —máquina de quien desarrolla y CI— exactamente el binario que se va a publicar.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$RAIZ/scripts/dotnet.sh" dotnet publish src/MovilidadUrbana.Web/MovilidadUrbana.Web.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output publicacion \
  "$@"
