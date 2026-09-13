#!/usr/bin/env bash
# =============================================================================
# dev.sh — compilar, instalar y correr MovilidadUrbana.MAUI en el teléfono USB desde un contenedor.
#
#   .devcontainer/dev.sh up         levanta el contenedor (toma el adb si otro lo tiene)
#   .devcontainer/dev.sh devices    dispositivos que ve adb
#   .devcontainer/dev.sh build      compila el APK Debug para armeabi-v7a
#   .devcontainer/dev.sh install    compila e instala
#   .devcontainer/dev.sh run        compila, instala y abre la aplicación
#   .devcontainer/dev.sh logs       logcat de la aplicación
#   .devcontainer/dev.sh uitests    corre las pruebas de interfaz (Appium) contra la app instalada
#   .devcontainer/dev.sh down       apaga el contenedor
#   .devcontainer/dev.sh devolver   apaga y le devuelve el adb al contenedor que lo tenía
#
# El teléfono es un recurso único: dos servidores de adb se lo disputan y el síntoma aparece en el
# otro. Por eso `up` apaga el adb de gda-core-app-dev antes de levantar el propio, y `devolver` lo
# restituye. Mientras este contenedor es el dueño, los medios de observación se usan con
# MEDIOS_ADB_OWNER=lab-e2e-maui-dev.
# =============================================================================
set -euo pipefail

IMAGE=lab-e2e-maui-dev:net10
NAME=lab-e2e-maui-dev
OTRO_DUENO=gda-core-app-dev
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(dirname "$HERE")"
PROJ=src/MovilidadUrbana.MAUI/MovilidadUrbana.MAUI.csproj
PKG=ar.lab.movilidadurbana
# armeabi-v7a es la ABI del teléfono de prueba; una sola ABI acorta el ciclo. Se pisa con ABI=android-arm64.
ABI="${ABI:-android-arm}"

in_container() { docker exec -i "$NAME" bash -lc "$*"; }
adb_root() { docker exec -u 0 -e HOME=/home/dev "$NAME" adb "$@"; }

ensure_up() { [ -n "$(docker ps -q -f name="^${NAME}$")" ] || cmd_up; }

cmd_up() {
    docker image inspect "$IMAGE" >/dev/null 2>&1 || docker build -t "$IMAGE" "$HERE"
    if [ -n "$(docker ps -q -f name="^${OTRO_DUENO}$")" ]; then
        echo "=== $OTRO_DUENO tiene el adb: se lo apago (se restituye con 'devolver') ==="
        docker exec "$OTRO_DUENO" adb kill-server >/dev/null 2>&1 || true
    fi
    docker rm -f "$NAME" >/dev/null 2>&1 || true
    docker run -d --name "$NAME" --privileged \
        -v /dev/bus/usb:/dev/bus/usb \
        -v "$REPO":/work \
        -v lab-maui-nuget:/home/dev/.nuget \
        -v gda-app-adbkeys:/home/dev/.android \
        -v lab-maui-keystore:/home/dev/.local/share/Xamarin \
        -w /work "$IMAGE" sleep infinity >/dev/null
    # Los volúmenes nombrados nacen con dueño root; el SDK escribe ahí como dev.
    docker exec -u 0 "$NAME" chown -R dev:dev /home/dev/.nuget /home/dev/.local /home/dev/.android
    adb_root start-server >/dev/null 2>&1
    cmd_devices
}

cmd_devices() { ensure_up; in_container "adb devices -l"; }

cmd_build() {
    ensure_up
    in_container "dotnet build $PROJ -c Debug -f net10.0-android \
        -p:AndroidPackageFormat=apk -p:RuntimeIdentifier=$ABI -p:EmbedAssembliesIntoApk=true"
}

apk_path() { in_container "ls src/MovilidadUrbana.MAUI/bin/Debug/net10.0-android/$ABI/*-Signed.apk 2>/dev/null | head -1"; }

cmd_install() {
    cmd_build
    local apk; apk="$(apk_path)"
    [ -n "$apk" ] || { echo "No se encontró el APK firmado" >&2; exit 2; }
    in_container "adb install -r '$apk'"
}

cmd_run() {
    cmd_install
    in_container "adb shell monkey -p $PKG -c android.intent.category.LAUNCHER 1 >/dev/null"
    echo "Abierta: $PKG"
}

cmd_logs() { ensure_up; in_container "adb logcat -d --pid=\$(adb shell pidof -s $PKG) | tail -200"; }

# Levanta el servidor de Appium dentro del contenedor (si no está), corre las pruebas de interfaz y
# deja los resultados y las capturas en tests/MovilidadUrbana.MAUI.UITests/resultados/.
cmd_uitests() {
    ensure_up
    if ! in_container "curl -fsS http://127.0.0.1:4723/status >/dev/null 2>&1"; then
        # Desprendido del exec (-d): si se lanzara con & dentro del mismo exec, moriría al terminar este.
        docker exec -d "$NAME" bash -lc "appium --log-level warn > /tmp/appium.log 2>&1"
        for i in $(seq 1 30); do in_container "curl -fsS http://127.0.0.1:4723/status >/dev/null 2>&1" && break; sleep 1; done
        in_container "curl -fsS http://127.0.0.1:4723/status >/dev/null" || { echo "Appium no arrancó; ver /tmp/appium.log en el contenedor" >&2; exit 3; }
    fi
    in_container "dotnet test tests/MovilidadUrbana.MAUI.UITests -c Release \
        --logger 'trx;LogFileName=uitests.trx' --results-directory tests/MovilidadUrbana.MAUI.UITests/resultados"
}

cmd_down() {
    docker exec -u 0 "$NAME" adb kill-server >/dev/null 2>&1 || true
    docker rm -f "$NAME" >/dev/null 2>&1 || true
}

cmd_devolver() {
    cmd_down
    if [ -n "$(docker ps -q -f name="^${OTRO_DUENO}$")" ]; then
        docker exec -u 0 -e HOME=/home/dev "$OTRO_DUENO" adb start-server >/dev/null 2>&1
        echo "adb devuelto a $OTRO_DUENO"
    fi
}

case "${1:-}" in
    up|devices|build|install|run|logs|uitests|down|devolver) "cmd_$1" ;;
    *) sed -n '2,20p' "$0"; exit 1 ;;
esac
