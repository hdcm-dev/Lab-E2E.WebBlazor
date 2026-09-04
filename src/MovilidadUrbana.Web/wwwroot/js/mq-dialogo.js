// Lo mínimo que no se puede escribir en C#: `showModal()` no tiene equivalente administrado.
// Lo que se gana a cambio es el confinamiento de foco y el cierre por Escape nativos del
// elemento <dialog>, que escritos a mano son justamente donde se pierde la accesibilidad.
window.mqDialogo = {
    abrir: function (dlg) {
        if (!dlg) { return; }
        dlg.__mqDisparador = document.activeElement;
        if (dlg.showModal) { dlg.showModal(); } else { dlg.setAttribute('open', ''); }
        var primero = dlg.querySelector('input, button, [href]');
        if (primero) { primero.focus(); }
    },
    cerrar: function (dlg) {
        if (!dlg) { return; }
        if (dlg.close) { dlg.close(); } else { dlg.removeAttribute('open'); }
        // El foco vuelve al control que lo abrió.
        if (dlg.__mqDisparador && dlg.__mqDisparador.focus) { dlg.__mqDisparador.focus(); }
    }
};
