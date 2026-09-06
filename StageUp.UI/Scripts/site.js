(function () {
    "use strict";

    // configura el menú hamburguesa del header. 
    function setupNavigation() {
        // Busco el botón que abre el menú y el panel donde están los links.
        var toggle = document.querySelector("[data-nav-toggle]");
        var panel = document.querySelector("[data-nav-panel]");

        // Si esa página no tiene estos elementos, la función termina y no hace nada.
        if (!toggle || !panel) {
            return;
        }

        // Cierra el menú y marca que ya no está desplegado.
        function closeNavigation() {
            panel.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        }

        // Cuando hago clic en el botón alterna entre abrir y cerrar.
        toggle.addEventListener("click", function () {
            var isOpen = panel.classList.toggle("is-open");
            toggle.setAttribute("aria-expanded", String(isOpen));
        });

        // Si elijo un link del menú se cierra.
        panel.addEventListener("click", function (event) {
            if (event.target.closest("a")) {
                closeNavigation();
            }
        });

        // Si la pantalla vuelve a tamaño escritorio cierra el menú mobile.
        window.addEventListener("resize", function () {
            if (window.innerWidth > 992) {
                closeNavigation();
            }
        });

        // Si aprieto Escape también cierra el menú.
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeNavigation();
            }
        });
    }

    // maneja los buscadores visuales. Toma lo que escribo y me manda a la página de resultados con ese texto en la URL.
    
    function setupSearchRedirects() {
        document.querySelectorAll("[data-search-box]").forEach(function (searchBox) {
            // Dentro de cada buscador busco el botón de buscar y el input.
            var action = searchBox.querySelector("[data-search-redirect]");
            var input = searchBox.querySelector("input[type='search']");

            if (!action) {
                return;
            }

            // Arma la dirección de destino. Si escribí algo lo agrega como ?q=texto.
            function redirectToResults() {
                var target = action.getAttribute("data-search-target");
                var term = input ? input.value.trim() : "";

                if (term) {
                    target += (target.indexOf("?") === -1 ? "?" : "&") + "q=" + encodeURIComponent(term);
                }

                window.location.href = target;
            }

            // Buscar con clic en el botón.
            action.addEventListener("click", redirectToResults);

            if (input) {
                // Buscar apretando Enter dentro del campo.
                input.addEventListener("keydown", function (event) {
                    if (event.key === "Enter") {
                        event.preventDefault();
                        redirectToResults();
                    }
                });
            }
        });
    }

    // cuando llego a resultados desde la home o desde "Aplicar filtros", recupera el texto buscado y el tipo de espacio de la URL y los vuelve a mostrar en los controles correspondientes.

    function setupResultsSearchState() {
        var resultsPage = document.querySelector("[data-results-page]");
        var input = document.querySelector("[data-results-search-input]");

        if (!resultsPage || typeof window.URLSearchParams === "undefined") {
            return;
        }

        var params = new window.URLSearchParams(window.location.search);
        var term = params.get("q");
        var tipo = params.get("tipo");

        if (term && input) {
            input.value = term;
            // Esta clase permite que el CSS cambie un poco el diseño si hubo búsqueda.
            resultsPage.classList.add("has-search-query");
        }

        if (tipo) {
            var typeField = document.getElementById("filter-type");
            if (typeField) {
                typeField.value = tipo;
                // Dispara el "change" para que setupFilters() (ya inicializado antes que esta función)
                // muestre el chip de filtro activo correspondiente.
                typeField.dispatchEvent(new Event("change"));
            }
            resultsPage.classList.add("has-search-query");
        }
    }

    // maneja todos los filtros de la página Explorar. 
    function setupFilters() {
        // Elementos principales del panel lateral de filtros.
        var panel = document.querySelector("[data-filter-panel]");
        var backdrop = document.querySelector("[data-filter-backdrop]");
        var activeRegion = document.querySelector("[data-active-filters]");
        var activeList = document.querySelector("[data-active-filter-list]");
        var lastTrigger = null;

        // Si no estoy en una página con filtros, no hace nada.
        if (!panel || !backdrop || !activeRegion || !activeList) {
            return;
        }

        // Filtros rápidos son los chips visibles. Filtros avanzados son los campos del panel.
        var quickFilters = Array.from(document.querySelectorAll("[data-filter-chip]"));
        var advancedFilters = Array.from(document.querySelectorAll("[data-advanced-filter]"));
        var advancedSelect = document.querySelector("[data-advanced-select]");
        var generalFields = [
            { id: "filter-type", label: "Tipo" },
            { id: "filter-location", label: "Ubicación" },
            { id: "filter-capacity", label: "Capacidad mínima" },
            { id: "filter-price", label: "Valor máximo" },
            { id: "filter-availability", label: "Disponibilidad" }
        ];

        // Abre el panel lateral de filtros y muestra el fondo oscuro.
        function openPanel(event) {
            lastTrigger = event ? event.currentTarget : null;
            panel.hidden = false;
            backdrop.hidden = false;
            document.body.classList.add("overlay-open");
            window.requestAnimationFrame(function () {
                panel.classList.add("is-open");
                backdrop.classList.add("is-open");
                var closeButton = panel.querySelector("[data-filter-close]");
                if (closeButton) {
                    closeButton.focus();
                }
            });
        }

        // Cierra el panel de filtros y vuelve el foco al botón que lo abrió.
        function closePanel() {
            panel.classList.remove("is-open");
            backdrop.classList.remove("is-open");
            document.body.classList.remove("overlay-open");
            window.setTimeout(function () {
                panel.hidden = true;
                backdrop.hidden = true;
                if (lastTrigger) {
                    lastTrigger.focus();
                }
            }, 240);
        }

        // Junta en una lista todos los filtros que el usuario tiene seleccionados.
        function getSelections() {
            var selections = [];

            // Revisa los chips rápidos, como Tipo de espacio o Ubicación.
            quickFilters.forEach(function (filter, index) {
                if (filter.getAttribute("aria-pressed") === "true") {
                    selections.push({ label: filter.getAttribute("data-filter-label"), element: filter, type: "quick", index: index });
                }
            });

            // Revisa los campos generales del panel, como capacidad, precio o fecha.
            generalFields.forEach(function (definition) {
                var field = document.getElementById(definition.id);
                if (field && field.value.trim()) {
                    selections.push({ label: definition.label + ": " + field.value.trim(), element: field, type: "field" });
                }
            });

            // Revisa el campo de tipo de piso.
            if (advancedSelect && advancedSelect.value) {
                selections.push({ label: advancedSelect.value, element: advancedSelect, type: "field" });
            }

            // Revisa los checks de características artísticas.
            advancedFilters.forEach(function (filter) {
                if (filter.checked) {
                    selections.push({ label: filter.value, element: filter, type: "checkbox" });
                }
            });

            return selections;
        }

        // Borra un filtro puntual, según si era chip, checkbox o campo de texto.
        function clearSelection(selection) {
            if (selection.type === "quick") {
                selection.element.setAttribute("aria-pressed", "false");
            } else if (selection.type === "checkbox") {
                selection.element.checked = false;
            } else {
                selection.element.value = "";
            }
        }

        // Muestra visualmente los filtros elegidos como chips activos.
        function renderSelections() {
            var selections = getSelections();
            activeList.innerHTML = "";
            activeRegion.hidden = selections.length === 0;

            selections.forEach(function (selection) {
                // Crea un botoncito por cada filtro activo para poder quitarlo.
                var chip = document.createElement("button");
                var removeIcon = document.createElement("span");
                chip.type = "button";
                chip.className = "active-filter-chip";
                chip.appendChild(document.createTextNode(selection.label));
                removeIcon.setAttribute("aria-hidden", "true");
                removeIcon.textContent = "×";
                chip.appendChild(removeIcon);
                chip.setAttribute("aria-label", "Quitar filtro " + selection.label);
                chip.addEventListener("click", function () {
                    clearSelection(selection);
                    renderSelections();
                });
                activeList.appendChild(chip);
            });
        }

        // Limpia todos los filtros seleccionados.
        function clearAllFilters() {
            getSelections().forEach(clearSelection);
            renderSelections();
        }

        // Botones que abren, cierran o limpian filtros.
        document.querySelectorAll("[data-filter-open]").forEach(function (trigger) {
            trigger.addEventListener("click", openPanel);
        });
        document.querySelectorAll("[data-filter-close]").forEach(function (trigger) {
            trigger.addEventListener("click", closePanel);
        });
        document.querySelectorAll("[data-clear-filters]").forEach(function (trigger) {
            trigger.addEventListener("click", clearAllFilters);
        });

        // Cada chip rápido se prende o se apaga al hacer clic.
        quickFilters.forEach(function (filter) {
            filter.addEventListener("click", function () {
                var selected = filter.getAttribute("aria-pressed") === "true";
                filter.setAttribute("aria-pressed", String(!selected));
                renderSelections();
            });
        });

        // Cada vez que cambia un filtro avanzado, actualiza la lista visual.
        advancedFilters.forEach(function (filter) {
            filter.addEventListener("change", renderSelections);
        });

        generalFields.forEach(function (definition) {
            var field = document.getElementById(definition.id);
            if (field) {
                field.addEventListener("change", renderSelections);
            }
        });

        if (advancedSelect) {
            advancedSelect.addEventListener("change", renderSelections);
        }

        var applyButton = document.querySelector("[data-apply-filters]");
        if (applyButton) {
            applyButton.addEventListener("click", function () {
                // El único filtro que hoy se puede aplicar contra datos reales es
                // "Tipo de espacio" (búsqueda avanzada, ítem 15). El resto de los
                // campos de este panel (ubicación, capacidad, precio, disponibilidad,
                // características) siguen siendo una vista previa visual: se
                // muestran como filtro activo pero no recargan la búsqueda.
                var typeField = document.getElementById("filter-type");

                if (typeField && typeof window.URLSearchParams !== "undefined") {
                    var searchInput = document.querySelector("[data-results-search-input]");
                    var currentParams = new window.URLSearchParams(window.location.search);
                    var nextParams = new window.URLSearchParams();

                    var term = searchInput ? searchInput.value.trim() : (currentParams.get("q") || "").trim();
                    if (term) {
                        nextParams.set("q", term);
                    }

                    if (typeField.value) {
                        nextParams.set("tipo", typeField.value);
                    }

                    var queryString = nextParams.toString();
                    window.location.href = window.location.pathname + (queryString ? "?" + queryString : "");
                    return;
                }

                // Si por algún motivo no está el campo de tipo (u otra página reutiliza
                // este mismo panel), se mantiene el comportamiento anterior: solo visual.
                renderSelections();
                closePanel();
            });
        }

        // También se puede cerrar tocando el fondo oscuro o apretando Escape.
        backdrop.addEventListener("click", closePanel);
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !panel.hidden) {
                closePanel();
            }
        });
    }

    // Maneja la selección de espacios para comparar (ítem 14 del checklist)
    // en la página de resultados: cada tarjeta tiene una casilla "Comparar",
    // y una barra flotante permite ir a CompararEspacios.aspx con los ids
    // elegidos (entre 2 y un máximo fijo).
    function setupComparisonBar() {
        var bar = document.querySelector("[data-compare-bar]");
        var checkboxes = Array.from(document.querySelectorAll("[data-compare-checkbox]"));

        if (!bar || checkboxes.length === 0) {
            return;
        }

        var CANTIDAD_MAXIMA = 3;
        var barText = bar.querySelector("[data-compare-bar-text]");
        var goButton = bar.querySelector("[data-compare-go]");
        var clearButton = bar.querySelector("[data-compare-clear]");

        // Devuelve las casillas que están tildadas en este momento.
        function elegidos() {
            return checkboxes.filter(function (checkbox) {
                return checkbox.checked;
            });
        }

        // Actualiza el texto de la barra, habilita/deshabilita "Comparar"
        // y bloquea casillas nuevas una vez llegado al máximo permitido.
        function actualizar() {
            var seleccion = elegidos();
            bar.hidden = seleccion.length === 0;

            if (barText) {
                if (seleccion.length === 1) {
                    barText.textContent = "1 espacio seleccionado (elegí al menos 2 para comparar)";
                } else {
                    barText.textContent = seleccion.length + " espacios seleccionados";
                }
            }

            if (goButton) {
                goButton.disabled = seleccion.length < 2;
            }

            checkboxes.forEach(function (checkbox) {
                if (!checkbox.checked) {
                    checkbox.disabled = seleccion.length >= CANTIDAD_MAXIMA;
                }
            });
        }

        checkboxes.forEach(function (checkbox) {
            checkbox.addEventListener("change", actualizar);
        });

        if (goButton) {
            goButton.addEventListener("click", function () {
                var ids = elegidos().map(function (checkbox) {
                    return checkbox.value;
                });

                if (ids.length >= 2) {
                    window.location.href = "CompararEspacios.aspx?ids=" + ids.join(",");
                }
            });
        }

        if (clearButton) {
            clearButton.addEventListener("click", function () {
                checkboxes.forEach(function (checkbox) {
                    checkbox.checked = false;
                    checkbox.disabled = false;
                });
                actualizar();
            });
        }

        actualizar();
    }

    // Apunte: maneja el modal del Asistente StageUp en la página de ayuda.
    // Es una vista previa visual: se abre, se cierra y permite marcar una opción.
    function setupAssistantModal() {
        var modal = document.querySelector("[data-assistant-modal]");
        var backdrop = document.querySelector("[data-assistant-backdrop]");
        var openButtons = document.querySelectorAll("[data-assistant-open]");
        var closeButtons = document.querySelectorAll("[data-assistant-close]");
        var lastTrigger = null;

        if (!modal || !backdrop) {
            return;
        }

        // Abre el asistente y muestra el fondo oscuro.
        function openAssistant(event) {
            lastTrigger = event.currentTarget;
            modal.hidden = false;
            backdrop.hidden = false;
            document.body.classList.add("overlay-open");
            window.requestAnimationFrame(function () {
                modal.classList.add("is-open");
                backdrop.classList.add("is-open");
                var closeButton = modal.querySelector("[data-assistant-close]");
                if (closeButton) {
                    closeButton.focus();
                }
            });
        }

        // Cierra el asistente y vuelve el foco al botón que lo abrió.
        function closeAssistant() {
            modal.classList.remove("is-open");
            backdrop.classList.remove("is-open");
            document.body.classList.remove("overlay-open");
            window.setTimeout(function () {
                modal.hidden = true;
                backdrop.hidden = true;
                if (lastTrigger) {
                    lastTrigger.focus();
                }
            }, 220);
        }

        openButtons.forEach(function (button) {
            button.addEventListener("click", openAssistant);
        });

        closeButtons.forEach(function (button) {
            button.addEventListener("click", closeAssistant);
        });

        backdrop.addEventListener("click", closeAssistant);

        // Permite seleccionar una consulta sugerida dentro del asistente.
        modal.querySelectorAll("[data-assistant-option]").forEach(function (option) {
            option.addEventListener("click", function () {
                modal.querySelectorAll("[data-assistant-option]").forEach(function (current) {
                    current.setAttribute("aria-pressed", String(current === option));
                });
            });
        });

        // Escape también cierra el asistente.
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !modal.hidden) {
                closeAssistant();
            }
        });
    }

    // Apunte: maneja las preguntas frecuentes de ayuda. Al tocar una pregunta,
    // muestra su respuesta y cierra las demás.
    function setupAccordions() {
        var triggers = document.querySelectorAll("[data-accordion-trigger]");

        triggers.forEach(function (trigger) {
            trigger.addEventListener("click", function () {
                var answer = document.getElementById(trigger.getAttribute("aria-controls"));
                var expanded = trigger.getAttribute("aria-expanded") === "true";
                if (answer) {
                    triggers.forEach(function (current) {
                        var currentAnswer = document.getElementById(current.getAttribute("aria-controls"));
                        if (current !== trigger && currentAnswer) {
                            current.setAttribute("aria-expanded", "false");
                            currentAnswer.hidden = true;
                        }
                    });
                    trigger.setAttribute("aria-expanded", String(!expanded));
                    answer.hidden = expanded;
                }
            });
        });
    }

    // Apunte: al cargar la página, se activan todas las funciones anteriores.
    // Si una página no tiene cierto elemento, esa función simplemente no hace nada.
    setupNavigation();
    setupFilters();
    setupResultsSearchState();
    setupSearchRedirects();
    setupComparisonBar();
    setupAssistantModal();
    setupAccordions();
}());
