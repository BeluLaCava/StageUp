(function () {
    "use strict";

    const input = document.querySelector("[data-internal-menu-search]");
    const list = document.querySelector("[data-internal-menu-list]");
    const empty = document.querySelector("[data-internal-menu-empty]");
    if (!input || !list) {
        return;
    }

    const links = Array.prototype.slice.call(list.querySelectorAll(".internal-nav-link"));

    function normalize(value) {
        const text = (value || "").toLocaleLowerCase("es-AR");
        return typeof text.normalize === "function"
            ? text.normalize("NFD").replace(/[\u0300-\u036f]/g, "")
            : text;
    }

    function filterMenu() {
        const query = normalize(input.value.trim());
        let visible = 0;

        links.forEach(function (link) {
            const matches = !query || normalize(link.getAttribute("data-menu-name")).indexOf(query) >= 0;
            link.hidden = !matches;
            if (matches) {
                visible++;
            }
        });

        if (empty) {
            empty.hidden = visible > 0;
        }
    }

    const currentPath = window.location.pathname.toLocaleLowerCase("es-AR").replace(/\/$/, "");
    let active = links.find(function (link) {
        return new URL(link.href, window.location.origin).pathname.toLocaleLowerCase("es-AR").replace(/\/$/, "") === currentPath;
    });

    const exact = !!active;
    if (!active) {
        const parents = {
            "gestiongrupospermisos.aspx": "gestionroles.aspx", "permisosdelrol.aspx": "gestionroles.aspx",
            "configuraridioma.aspx": "gestionidiomas.aspx", "detalleoperacionfinanciera.aspx": "historialfinanciero.aspx",
            "detalleespaciointerno.aspx": "busquedainterna.aspx", "historialcorreos.aspx": "transaccioneseconomicas.aspx",
            "gestionnewsletter.aspx": "gestionnovedades.aspx"
        };
        const parent = parents[currentPath.split("/").pop()];
        active = links.find(function (link) {
            return new URL(link.href, window.location.origin).pathname.split("/").pop().toLowerCase() === parent;
        });
    }

    if (active) {
        active.classList.add("is-active");
        active.setAttribute("aria-current", exact ? "page" : "location");
        window.requestAnimationFrame(function () {
            const destination = active.offsetTop - list.offsetTop - ((list.clientHeight - active.offsetHeight) / 2);
            list.scrollTop = Math.max(0, destination);
        });
    }

    input.addEventListener("input", filterMenu);
    input.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && input.value) {
            input.value = "";
            filterMenu();
        }
    });
}());
