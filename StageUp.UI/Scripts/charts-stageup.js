(function () {
    "use strict";

    const colores = ["#6d1021", "#8f1f37", "#b8465d", "#cb7180", "#9b6555", "#d39a83", "#7f2637", "#e0b8a5"];

    function numero(valor) {
        const convertido = Number.parseFloat(valor);
        return Number.isFinite(convertido) ? Math.max(0, convertido) : 0;
    }

    function crearLeyenda(contenedor, opciones) {
        contenedor.textContent = "";

        opciones.forEach(function (opcion, indice) {
            const item = document.createElement("div");
            const muestra = document.createElement("span");
            const texto = document.createElement("span");
            const valor = document.createElement("strong");
            const color = colores[indice % colores.length];

            item.className = "su-chart-legend-item";
            item.tabIndex = 0;
            item.title = opcion.etiqueta + ": " + opcion.cantidad + " respuestas (" + opcion.porcentaje + " %)";

            muestra.className = "su-chart-legend-swatch";
            muestra.style.backgroundColor = color;

            texto.textContent = opcion.etiqueta;
            valor.textContent = opcion.porcentaje + " %";

            item.appendChild(muestra);
            item.appendChild(texto);
            item.appendChild(valor);
            contenedor.appendChild(item);
        });
    }

    function cambiarModo(grafico, modo) {
        const barras = grafico.querySelector("[data-su-chart-bars]");
        const dona = grafico.querySelector("[data-su-chart-donut-panel]");
        const botones = grafico.querySelectorAll("[data-su-chart-mode]");
        const mostrarDona = modo === "donut";

        barras.hidden = mostrarDona;
        dona.hidden = !mostrarDona;

        botones.forEach(function (boton) {
            const activo = boton.getAttribute("data-su-chart-mode") === modo;
            boton.setAttribute("aria-pressed", activo ? "true" : "false");
        });
    }

    function inicializarGrafico(grafico) {
        const filas = Array.from(grafico.querySelectorAll("[data-su-chart-option]"));
        const dona = grafico.querySelector("[data-su-chart-donut]");
        const leyenda = grafico.querySelector("[data-su-chart-legend]");
        const opciones = [];
        const segmentos = [];
        let acumulado = 0;

        filas.forEach(function (fila, indice) {
            const porcentaje = Math.min(100, numero(fila.getAttribute("data-percentage")));
            const cantidad = Math.round(numero(fila.getAttribute("data-count")));
            const etiqueta = fila.getAttribute("data-label") || "Opción";
            const color = colores[indice % colores.length];
            const siguiente = Math.min(100, acumulado + porcentaje);

            fila.style.setProperty("--su-option-color", color);
            fila.style.setProperty("--su-chart-delay", (indice * 80) + "ms");
            opciones.push({ etiqueta: etiqueta, porcentaje: porcentaje.toLocaleString("es-AR", { maximumFractionDigits: 2 }), cantidad: cantidad });

            if (porcentaje > 0) {
                segmentos.push(color + " " + acumulado + "% " + siguiente + "%");
                acumulado = siguiente;
            }
        });

        if (dona) {
            dona.style.background = segmentos.length > 0
                ? "conic-gradient(" + segmentos.join(", ") + (acumulado < 100 ? ", #f2e6df " + acumulado + "% 100%" : "") + ")"
                : "conic-gradient(#eadbd3 0% 100%)";
        }

        if (leyenda) {
            crearLeyenda(leyenda, opciones);
        }

        grafico.querySelectorAll("[data-su-chart-mode]").forEach(function (boton) {
            boton.addEventListener("click", function () {
                cambiarModo(grafico, boton.getAttribute("data-su-chart-mode"));
            });
        });

        cambiarModo(grafico, "bars");
        grafico.classList.add("su-chart-ready");
    }

    function inicializar() {
        document.querySelectorAll("[data-su-survey-chart]").forEach(inicializarGrafico);
        window.requestAnimationFrame(function () {
            document.documentElement.classList.add("su-report-charts-ready");
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", inicializar);
    } else {
        inicializar();
    }
}());
