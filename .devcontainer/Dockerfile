# =============================================================================
# Entorno de build/depuracion de GDA.Core.APP (net10.0-android) sobre Linux.
#
# Trae lo que la maquina anfitriona no tiene: el SDK de .NET 10 con el workload
# maui-android, un JDK 17 y el SDK de Android (incluido adb). El celular entra
# por USB: el contenedor se corre con /dev/bus/usb montado, asi que el adb de
# adentro habla directo con el dispositivo, sin adb en el host.
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG USERNAME=dev
ARG USER_UID=1000
ARG USER_GID=1000

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && apt-get install -y --no-install-recommends \
        openjdk-17-jdk-headless \
        unzip zip curl git ca-certificates \
        usbutils udev sudo \
        libicu-dev locales \
    && rm -rf /var/lib/apt/lists/*

# El .csproj pone InvariantGlobalization=false: sin locales reales el runtime
# de build y la app se quedan sin ICU util.
RUN sed -i 's/^# es_AR.UTF-8/es_AR.UTF-8/; s/^# en_US.UTF-8/en_US.UTF-8/' /etc/locale.gen \
    && locale-gen

ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=/opt/android-sdk

# --- SDK de Android ---------------------------------------------------------
# API 36 y build-tools 36.0.0: es lo que espera net10.0-android. Se instala
# tambien android-35 porque algunas dependencias AndroidX todavia compilan
# contra ella.
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools \
    && curl -fsSL -o /tmp/cmdline.zip \
        https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip \
    && unzip -q /tmp/cmdline.zip -d ${ANDROID_HOME}/cmdline-tools \
    && mv ${ANDROID_HOME}/cmdline-tools/cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest \
    && rm /tmp/cmdline.zip

ENV PATH=${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/platform-tools:${PATH}

RUN yes | sdkmanager --licenses > /dev/null \
    && sdkmanager --install \
        "platform-tools" \
        "platforms;android-36" \
        "platforms;android-35" \
        "build-tools;36.0.0" \
        "build-tools;35.0.0" \
    && rm -rf ${ANDROID_HOME}/.temp

# --- Workload MAUI ----------------------------------------------------------
# Solo android: el host es Linux, net10.0-ios no se puede construir aca de
# todos modos (el .csproj ya lo condiciona a macOS).
RUN dotnet workload install maui-android --skip-sign-check \
    && dotnet nuget locals http-cache --clear

# --- Usuario ----------------------------------------------------------------
# Mismo UID/GID que el usuario del host: los bin/ y obj/ que quedan en el
# volumen montado siguen siendo editables desde afuera.
RUN if getent group ${USER_GID} >/dev/null; then \
        groupmod -n ${USERNAME} "$(getent group ${USER_GID} | cut -d: -f1)"; \
    else groupadd -g ${USER_GID} ${USERNAME}; fi \
    && if getent passwd ${USER_UID} >/dev/null; then \
        usermod -l ${USERNAME} -d /home/${USERNAME} -m "$(getent passwd ${USER_UID} | cut -d: -f1)"; \
    else useradd -u ${USER_UID} -g ${USER_GID} -m -s /bin/bash ${USERNAME}; fi \
    && mkdir -p /home/${USERNAME}/.dotnet /home/${USERNAME}/.nuget /home/${USERNAME}/.android \
    && chown -R ${USER_UID}:${USER_GID} /home/${USERNAME} \
    && chown -R ${USER_UID}:${USER_GID} ${ANDROID_HOME}

# El servidor de adb necesita root: el nodo USB del telefono queda en solo
# lectura para cualquier otro usuario (ver el comentario en dev.sh). El
# contenedor es privilegiado y de un solo desarrollador, asi que el sudo sin
# password es el camino corto para que el "adb start-server" del arranque de
# VS Code funcione sin cambiar de usuario a mano.
RUN echo "${USERNAME} ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/${USERNAME} \
    && chmod 0440 /etc/sudoers.d/${USERNAME}

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    LANG=es_AR.UTF-8

USER ${USERNAME}
WORKDIR /workspace
