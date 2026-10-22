(function () {
    "use strict";

    const selector = [
        ".internal-content [role='search']:not(.activity-filter-card)",
        ".internal-content [data-admin-filter]"
    ].join(",");
    const panels = Array.prototype.slice.call(document.querySelectorAll(selector));

    panels.forEach(function (panel) {
        if (panel.closest(".admin-filter-popover")) {
            return;
        }

        const parent = panel.parentNode;
        const details = document.createElement("details");
        const summary = document.createElement("summary");
        const icon = document.createElement("span");
        const body = document.createElement("div");

        details.className = "admin-filter-popover";
        summary.className = "admin-filter-trigger";
        summary.setAttribute("aria-label", "Mostrar u ocultar filtros");
        summary.setAttribute("title", "Filtrar resultados");
        icon.className = "activity-filter-icon";
        icon.setAttribute("aria-hidden", "true");
        body.className = "admin-filter-popover-body";

        summary.appendChild(icon);
        parent.insertBefore(details, panel);
        details.appendChild(summary);
        details.appendChild(body);
        body.appendChild(panel);
        panel.classList.add("admin-filter-content");

        if (parent.classList && parent.classList.contains("auth-card") && parent.children.length === 1) {
            parent.classList.add("admin-filter-only-card");
        }

        details.addEventListener("toggle", function () {
            if (parent.classList) {
                parent.classList.toggle("admin-filter-host-open", details.open);
            }

            if (!details.open) {
                return;
            }

            document.querySelectorAll(".admin-filter-popover[open]").forEach(function (other) {
                if (other !== details) {
                    other.removeAttribute("open");
                }
            });
        });
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") {
            return;
        }

        document.querySelectorAll(".admin-filter-popover[open]").forEach(function (details) {
            details.removeAttribute("open");
            const summary = details.querySelector(":scope > summary");
            if (summary) {
                summary.focus();
            }
        });
    });

    document.addEventListener("click", function (event) {
        document.querySelectorAll(".admin-filter-popover[open]").forEach(function (details) {
            if (!details.contains(event.target)) {
                details.removeAttribute("open");
            }
        });
    });
})();
