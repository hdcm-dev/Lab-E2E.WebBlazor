// Traduce los estados de reconexión que publica el framework al único atributo que el CSS del
// aviso mira. El texto y los colores no se tocan desde acá.
const aviso = document.getElementById("components-reconnect-modal");

const ESTADOS = {
    show: "reconectando",
    retrying: "reintentando",
    failed: "fallo",
    paused: "pausada",
    hide: "oculto"
};

aviso.addEventListener("components-reconnect-state-changed", alCambiarElEstado);
document.getElementById("components-reconnect-button").addEventListener("click", reintentar);
document.getElementById("components-resume-button").addEventListener("click", retomar);

function mostrar(estado) {
    aviso.dataset.estado = estado;
}

function alCambiarElEstado(evento) {
    const estado = evento.detail.state;

    if (estado === "rejected") {
        // El circuito ya no está del otro lado: recargar es lo más rápido para seguir trabajando.
        location.reload();
        return;
    }

    mostrar(ESTADOS[estado] ?? "reconectando");

    if (estado === "failed") {
        document.addEventListener("visibilitychange", reintentarAlVolverALaPestania);
    }
}

async function reintentar() {
    document.removeEventListener("visibilitychange", reintentarAlVolverALaPestania);

    try {
        const reconectado = await Blazor.reconnect();
        if (!reconectado) {
            const retomado = await Blazor.resumeCircuit();
            if (!retomado) {
                location.reload();
            } else {
                mostrar("oculto");
            }
        }
    } catch {
        // No se llegó al servidor: se vuelve a intentar cuando la pestaña esté a la vista.
        document.addEventListener("visibilitychange", reintentarAlVolverALaPestania);
    }
}

async function retomar() {
    try {
        const retomado = await Blazor.resumeCircuit();
        if (!retomado) {
            location.reload();
        }
    } catch {
        mostrar("fallo-al-retomar");
    }
}

async function reintentarAlVolverALaPestania() {
    if (document.visibilityState === "visible") {
        await reintentar();
    }
}
