(function () {
    "use strict";

    const raiz = document.documentElement;
    const movimientoReducido = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const selectores = [
        ".hero-copy > .section-label",
        ".hero-copy > h1",
        ".hero-copy > p",
        ".hero-copy > .hero-search",
        ".hero-visual",
        ".static-page-header > *",
        ".profile-page-header > *",
        ".explore-heading > *",
        ".auth-page .auth-card",
        ".space-card",
        ".requests-summary-card",
        ".request-card",
        ".internal-panel-card",
        ".profile-summary-card",
        ".profile-card",
        ".institutional-hero-card",
        ".institutional-step-card",
        ".contact-channel-card",
        ".institutional-summary-card",
        ".public-news-featured",
        ".public-news-item",
        ".faq-item",
        ".support-card",
        ".assistant-card",
        ".empty-state",
        ".form-message",
        ".booking-progress",
        ".dr-tracking-card",
        ".admin-editor-card",
        ".admin-list-card",
        ".admin-workspace-card",
        ".admin-filter-card",
        ".permission-card",
        ".language-card",
        ".newsletter-campaign-row",
        ".faq-admin-row",
        ".cc-resumen-card",
        ".cc-movimientos-card",
        ".offer-space-card",
        ".helpdesk-card",
        ".ticket-lista-item",
        ".activities-board",
        ".activity-editor"
    ];
    const selectorGeneral = selectores.join(",");
    const selectorEscala = [
        ".hero-visual",
        ".space-card",
        ".requests-summary-card",
        ".internal-panel-card",
        ".institutional-step-card",
        ".contact-channel-card",
        ".public-news-item",
        ".permission-card"
    ].join(",");

    function estaExcluido(elemento) {
        return elemento.matches("[data-motion='off']") || elemento.closest("[data-motion='off']") !== null;
    }

    function calcularDemora(elemento) {
        const contenedor = elemento.parentElement;
        if (!contenedor) {
            return 0;
        }

        const hermanos = Array.from(contenedor.children).filter(function (hermano) {
            return hermano.matches(selectorGeneral);
        });
        const indice = hermanos.indexOf(elemento);
        return indice < 0 ? 0 : Math.min(indice, 5) * 55;
    }

    function preparar(elemento) {
        if (estaExcluido(elemento) || elemento.classList.contains("ui-reveal")) {
            return;
        }

        elemento.classList.add("ui-reveal");
        if (elemento.matches(selectorEscala)) {
            elemento.classList.add("ui-reveal-scale");
        }
        elemento.style.setProperty("--ui-motion-delay", calcularDemora(elemento) + "ms");
    }

    function iniciar() {
        if (movimientoReducido) {
            raiz.classList.add("ui-motion-reduced");
            return;
        }

        raiz.classList.add("ui-motion-enabled");
        const elementos = Array.from(document.querySelectorAll(selectorGeneral));
        elementos.forEach(preparar);
        document.body.offsetHeight;

        if (!("IntersectionObserver" in window)) {
            elementos.forEach(function (elemento) {
                elemento.classList.add("is-visible");
            });
            raiz.classList.add("ui-motion-ready");
            return;
        }

        const observador = new IntersectionObserver(function (entradas) {
            entradas.forEach(function (entrada) {
                if (entrada.isIntersecting) {
                    entrada.target.classList.add("is-visible");
                    observador.unobserve(entrada.target);
                }
            });
        }, {
            rootMargin: "0px 0px -7% 0px",
            threshold: 0.06
        });

        elementos.forEach(function (elemento) {
            observador.observe(elemento);
        });
        raiz.classList.add("ui-motion-ready");
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciar);
    } else {
        iniciar();
    }
}());
