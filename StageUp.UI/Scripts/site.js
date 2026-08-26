(function () {
    "use strict";

    var toggle = document.querySelector("[data-nav-toggle]");
    var panel = document.querySelector("[data-nav-panel]");

    if (!toggle || !panel) {
        return;
    }

    function closeNavigation() {
        panel.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
    }

    toggle.addEventListener("click", function () {
        var isOpen = panel.classList.toggle("is-open");
        toggle.setAttribute("aria-expanded", String(isOpen));
    });

    panel.addEventListener("click", function (event) {
        if (event.target.closest("a")) {
            closeNavigation();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeNavigation();
        }
    });

    window.addEventListener("resize", function () {
        if (window.innerWidth > 992) {
            closeNavigation();
        }
    });
}());
