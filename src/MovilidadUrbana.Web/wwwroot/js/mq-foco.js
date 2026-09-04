// Mover el foco al contenido principal no tiene equivalente administrado cuando el elemento no
// es del componente que lo pide: `#mq-main` vive en el layout y el paso del asistente, en la
// página. Es lo mínimo, y nada más que eso, lo que se resuelve por interoperabilidad.
window.mqFoco = {
    alContenidoPrincipal: function () {
        var principal = document.getElementById('mq-main');
        if (principal) { principal.focus(); }
    }
};
