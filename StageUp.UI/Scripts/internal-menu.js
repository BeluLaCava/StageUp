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
    const active = links.find(function (link) {
        return new URL(link.href, window.location.origin).pathname.toLocaleLowerCase("es-AR").replace(/\/$/, "") === currentPath;
    });

    if (active) {
        active.classList.add("is-active");
        active.setAttribute("aria-current", "page");
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
