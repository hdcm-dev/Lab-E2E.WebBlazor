#!/usr/bin/env bash
# Ejecuta el SDK de .NET dentro del contenedor oficial, para máquinas sin SDK instalado.
#
#   scripts/dotnet.sh dotnet build
#   scripts/dotnet.sh dotnet publish -c Release
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
IMAGEN="${IMAGEN_SDK:-mcr.microsoft.com/dotnet/sdk:10.0}"

exec docker run --rm -i \
  --user "$(id -u):$(id -g)" \
  --env HOME=/tmp \
  --env DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  --env DOTNET_NOLOGO=1 \
  --env NUGET_PACKAGES=/trabajo/.nuget \
  --volume "$RAIZ:/trabajo" \
  --workdir /trabajo \
  "$IMAGEN" "$@"
