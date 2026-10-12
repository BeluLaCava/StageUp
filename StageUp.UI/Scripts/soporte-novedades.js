// Ítem 12: actualización periódica de las pantallas de soporte. Cada
// tantos segundos pregunta a SoporteNovedades.ashx si cambió lo que se está
// viendo. Si cambió y no hay nada escrito sin enviar, recarga la página;
// si hay un borrador, solo muestra el aviso para no perderlo.
(function () {
    "use strict";

    const contenedor = document.querySelector("[data-soporte-novedades]");
    if (!contenedor || !window.fetch) {
        return;
    }

    const url = contenedor.getAttribute("data-soporte-novedades");
    const marcaInicial = contenedor.getAttribute("data-marca");
    const intervalo = Math.max(10000, parseInt(contenedor.getAttribute("data-intervalo") || "20000", 10));
    const aviso = document.querySelector("[data-soporte-aviso]");
    const recargar = document.querySelector("[data-soporte-recargar]");
    let detenido = false;

    function hayBorrador() {
        return Array.prototype.some.call(document.querySelectorAll("[data-soporte-borrador]"), function (campo) {
            return campo.value.trim().length > 0;
        });
    }

    function volverACargar() {
        window.location.href = window.location.pathname + window.location.search;
    }

    function consultar() {
        if (detenido || document.hidden) {
            return;
        }

        fetch(url, { credentials: "same-origin", cache: "no-store" })
            .then(function (respuesta) {
                return respuesta.ok ? respuesta.json() : null;
            })
            .then(function (datos) {
                if (!datos || !datos.ok || datos.marca === marcaInicial) {
                    return;
                }

                detenido = true;
                if (!hayBorrador()) {
                    volverACargar();
                    return;
                }

                if (aviso) {
                    aviso.hidden = false;
                }
            })
            .catch(function () {
                // Sin conexión: se vuelve a intentar en el próximo ciclo.
            });
    }

    if (recargar) {
        recargar.addEventListener("click", volverACargar);
    }

    window.setInterval(consultar, intervalo);
    document.addEventListener("visibilitychange", function () {
        if (!document.hidden) {
            consultar();
        }
    });
}());
