(function () {
    "use strict";

    function setupNavigation() {
        const toggle = document.querySelector("[data-nav-toggle]");
        const panel = document.querySelector("[data-nav-panel]");

        if (!toggle || !panel) {
            return;
        }

        function closeNavigation() {
            panel.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        }

        toggle.addEventListener("click", function () {
            const isOpen = panel.classList.toggle("is-open");
            toggle.setAttribute("aria-expanded", String(isOpen));
        });

        panel.addEventListener("click", function (event) {
            if (event.target.closest("a")) {
                closeNavigation();
            }
        });

        window.addEventListener("resize", function () {
            if (window.innerWidth > 992) {
                closeNavigation();
            }
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeNavigation();
            }
        });
    }

    function setupSearchRedirects() {
        document.querySelectorAll("[data-search-box]").forEach(function (searchBox) {
            const action = searchBox.querySelector("[data-search-redirect]");
            const input = searchBox.querySelector("input[type='search']");

            if (!action) {
                return;
            }

            function redirectToResults() {
                let target = action.getAttribute("data-search-target");
                const term = input ? input.value.trim() : "";

                if (term) {
                    target += (target.indexOf("?") === -1 ? "?" : "&") + "q=" + encodeURIComponent(term);
                }

                window.location.href = target;
            }

            action.addEventListener("click", redirectToResults);

            if (input) {
                input.addEventListener("keydown", function (event) {
                    if (event.key === "Enter") {
                        event.preventDefault();
                        redirectToResults();
                    }
                });
            }
        });
    }

    function setupResultsSearchState() {
        const resultsPage = document.querySelector("[data-results-page]");
        const input = document.querySelector("[data-results-search-input]");

        if (!resultsPage || typeof window.URLSearchParams === "undefined") {
            return;
        }

        const params = new window.URLSearchParams(window.location.search);
        const term = params.get("q");
        const tipo = params.get("tipo");

        if (term && input) {
            input.value = term;
            resultsPage.classList.add("has-search-query");
        }

        if (tipo) {
            const typeField = document.getElementById("filter-type");
            if (typeField) {
                typeField.value = tipo;
                typeField.dispatchEvent(new Event("change"));
            }
            resultsPage.classList.add("has-search-query");
        }

        const camposTexto = [
            { param: "ubicacion", id: "filter-location" },
            { param: "provincia", id: "filter-province" },
            { param: "ciudad", id: "filter-city" },
            { param: "zona", id: "filter-zone" },
            { param: "capacidadMin", id: "filter-capacity" },
            { param: "precioMin", id: "filter-price-min" },
            { param: "precioMax", id: "filter-price" },
            { param: "fecha", id: "filter-availability" },
            { param: "horaDesde", id: "filter-time-from" },
            { param: "horaHasta", id: "filter-time-to" },
            { param: "calificacionMin", id: "filter-rating" },
            { param: "piso", id: "filter-floor" },
            { param: "caracteristicas", id: "filter-features" }
        ];

        camposTexto.forEach(function (campo) {
            const valor = params.get(campo.param);
            const elemento = document.getElementById(campo.id);
            if (valor && elemento) {
                elemento.value = valor;
                elemento.dispatchEvent(new Event("change"));
                resultsPage.classList.add("has-search-query");
            }
        });

        const equipamiento = params.get("equip");
        if (equipamiento) {
            const codigos = equipamiento.split(",");
            document.querySelectorAll("[data-equip-code]").forEach(function (checkbox) {
                if (codigos.indexOf(checkbox.getAttribute("data-equip-code")) !== -1) {
                    checkbox.checked = true;
                    checkbox.dispatchEvent(new Event("change"));
                }
            });
            resultsPage.classList.add("has-search-query");
        }
    }

    function setupFilters() {
        const panel = document.querySelector("[data-filter-panel]");
        const backdrop = document.querySelector("[data-filter-backdrop]");
        const activeRegion = document.querySelector("[data-active-filters]");
        const activeList = document.querySelector("[data-active-filter-list]");
        let lastTrigger = null;

        if (!panel || !backdrop || !activeRegion || !activeList) {
            return;
        }

        const quickFilters = Array.from(document.querySelectorAll("[data-filter-chip]"));
        const advancedFilters = Array.from(document.querySelectorAll("[data-advanced-filter]"));
        const advancedSelect = document.querySelector("[data-advanced-select]");
        const generalFields = [
            { id: "filter-type", label: "Tipo" },
            { id: "filter-location", label: "Ubicación" },
            { id: "filter-province", label: "Provincia" },
            { id: "filter-city", label: "Ciudad" },
            { id: "filter-zone", label: "Zona" },
            { id: "filter-capacity", label: "Capacidad mínima" },
            { id: "filter-price-min", label: "Valor mínimo" },
            { id: "filter-price", label: "Valor máximo" },
            { id: "filter-availability", label: "Disponibilidad" },
            { id: "filter-time-from", label: "Desde" },
            { id: "filter-time-to", label: "Hasta" },
            { id: "filter-rating", label: "Calificación mínima" },
            { id: "filter-features", label: "Características" }
        ];

        function openPanel(event) {
            lastTrigger = event ? event.currentTarget : null;
            panel.hidden = false;
            backdrop.hidden = false;
            document.body.classList.add("overlay-open");
            window.requestAnimationFrame(function () {
                panel.classList.add("is-open");
                backdrop.classList.add("is-open");
                const closeButton = panel.querySelector("[data-filter-close]");
                if (closeButton) {
                    closeButton.focus();
                }
            });
        }

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

        function getSelections() {
            const selections = [];

            quickFilters.forEach(function (filter, index) {
                if (filter.getAttribute("aria-pressed") === "true") {
                    selections.push({ label: filter.getAttribute("data-filter-label"), element: filter, type: "quick", index: index });
                }
            });

            generalFields.forEach(function (definition) {
                const field = document.getElementById(definition.id);
                if (field && field.value.trim()) {
                    selections.push({ label: definition.label + ": " + field.value.trim(), element: field, type: "field" });
                }
            });

            if (advancedSelect && advancedSelect.value) {
                selections.push({ label: advancedSelect.value, element: advancedSelect, type: "field" });
            }

            advancedFilters.forEach(function (filter) {
                if (filter.checked) {
                    selections.push({ label: filter.value, element: filter, type: "checkbox" });
                }
            });

            return selections;
        }

        function clearSelection(selection) {
            if (selection.type === "quick") {
                selection.element.setAttribute("aria-pressed", "false");
            } else if (selection.type === "checkbox") {
                selection.element.checked = false;
            } else {
                selection.element.value = "";
            }
        }

        function renderSelections() {
            const selections = getSelections();
            activeList.innerHTML = "";
            activeRegion.hidden = selections.length === 0;

            selections.forEach(function (selection) {
                const chip = document.createElement("button");
                const removeIcon = document.createElement("span");
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

        function clearAllFilters() {
            getSelections().forEach(clearSelection);
            renderSelections();
        }

        document.querySelectorAll("[data-filter-open]").forEach(function (trigger) {
            trigger.addEventListener("click", openPanel);
        });
        document.querySelectorAll("[data-filter-close]").forEach(function (trigger) {
            trigger.addEventListener("click", closePanel);
        });
        document.querySelectorAll("[data-clear-filters]").forEach(function (trigger) {
            trigger.addEventListener("click", clearAllFilters);
        });

        quickFilters.forEach(function (filter) {
            filter.addEventListener("click", function () {
                const selected = filter.getAttribute("aria-pressed") === "true";
                filter.setAttribute("aria-pressed", String(!selected));
                renderSelections();
            });
        });

        advancedFilters.forEach(function (filter) {
            filter.addEventListener("change", renderSelections);
        });

        generalFields.forEach(function (definition) {
            const field = document.getElementById(definition.id);
            if (field) {
                field.addEventListener("change", renderSelections);
            }
        });

        if (advancedSelect) {
            advancedSelect.addEventListener("change", renderSelections);
        }

        const applyButton = document.querySelector("[data-apply-filters]");
        if (applyButton) {
            applyButton.addEventListener("click", function () {
                const typeField = document.getElementById("filter-type");

                if (typeField && typeof window.URLSearchParams !== "undefined") {
                    const searchInput = document.querySelector("[data-results-search-input]");
                    const currentParams = new window.URLSearchParams(window.location.search);
                    const nextParams = new window.URLSearchParams();

                    const term = searchInput ? searchInput.value.trim() : (currentParams.get("q") || "").trim();
                    if (term) {
                        nextParams.set("q", term);
                    }

                    if (typeField.value) {
                        nextParams.set("tipo", typeField.value);
                    }

                    const camposTexto = [
                        { id: "filter-location", param: "ubicacion" },
                        { id: "filter-province", param: "provincia" },
                        { id: "filter-city", param: "ciudad" },
                        { id: "filter-zone", param: "zona" },
                        { id: "filter-capacity", param: "capacidadMin" },
                        { id: "filter-price-min", param: "precioMin" },
                        { id: "filter-price", param: "precioMax" },
                        { id: "filter-availability", param: "fecha" },
                        { id: "filter-time-from", param: "horaDesde" },
                        { id: "filter-time-to", param: "horaHasta" },
                        { id: "filter-rating", param: "calificacionMin" },
                        { id: "filter-floor", param: "piso" },
                        { id: "filter-features", param: "caracteristicas" }
                    ];

                    camposTexto.forEach(function (campo) {
                        const elemento = document.getElementById(campo.id);
                        const valor = elemento ? elemento.value.trim() : "";
                        if (valor) {
                            nextParams.set(campo.param, valor);
                        }
                    });

                    const codigosEquipamiento = Array.from(document.querySelectorAll("[data-equip-code]"))
                        .filter(function (checkbox) { return checkbox.checked; })
                        .map(function (checkbox) { return checkbox.getAttribute("data-equip-code"); });

                    if (codigosEquipamiento.length > 0) {
                        nextParams.set("equip", codigosEquipamiento.join(","));
                    }


                    if (currentParams.get("orden")) {
                        nextParams.set("orden", currentParams.get("orden"));
                    }

                    const queryString = nextParams.toString();
                    window.location.href = window.location.pathname + (queryString ? "?" + queryString : "");
                    return;
                }

                renderSelections();
                closePanel();
            });
        }

        backdrop.addEventListener("click", closePanel);
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !panel.hidden) {
                closePanel();
            }
        });
    }

    function setupComparisonBar() {
        const bar = document.querySelector("[data-compare-bar]");
        const checkboxes = Array.from(document.querySelectorAll("[data-compare-checkbox]"));

        if (!bar || checkboxes.length === 0) {
            return;
        }

        const CANTIDAD_MAXIMA = 3;
        const barText = bar.querySelector("[data-compare-bar-text]");
        const goButton = bar.querySelector("[data-compare-go]");
        const clearButton = bar.querySelector("[data-compare-clear]");

        function elegidos() {
            return checkboxes.filter(function (checkbox) {
                return checkbox.checked;
            });
        }

        function actualizar() {
            const seleccion = elegidos();
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
                const ids = elegidos().map(function (checkbox) {
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

    function setupServiceComparison() {
        const checkboxes = Array.from(document.querySelectorAll("[data-service-compare-checkbox]"));
        const goButton = document.querySelector("[data-service-compare-go]");
        const status = document.querySelector("[data-service-compare-status]");

        if (checkboxes.length === 0 || !goButton) {
            return;
        }

        const CANTIDAD_MAXIMA = 3;

        function elegidos() {
            return checkboxes.filter(function (checkbox) {
                return checkbox.checked;
            });
        }

        function actualizar() {
            const seleccion = elegidos();

            if (goButton) {
                goButton.disabled = seleccion.length < 2;
            }

            if (status) {
                if (seleccion.length === 0) {
                    status.textContent = "Elegí al menos 2 servicios para comparar";
                } else if (seleccion.length === 1) {
                    status.textContent = "1 servicio seleccionado (elegí uno más)";
                } else {
                    status.textContent = seleccion.length + " servicios seleccionados";
                }
            }

            checkboxes.forEach(function (checkbox) {
                const option = checkbox.closest(".service-type-option");
                if (!checkbox.checked) {
                    checkbox.disabled = seleccion.length >= CANTIDAD_MAXIMA;
                }
                if (option) {
                    option.classList.toggle("is-selected", checkbox.checked);
                    option.classList.toggle("is-disabled", checkbox.disabled && !checkbox.checked);
                }
            });
        }

        checkboxes.forEach(function (checkbox) {
            checkbox.addEventListener("change", actualizar);
        });

        goButton.addEventListener("click", function () {
            const tipos = elegidos().map(function (checkbox) {
                return encodeURIComponent(checkbox.value);
            });

            if (tipos.length >= 2) {
                window.location.href = "CompararServicios.aspx?tipos=" + tipos.join(",");
            }
        });

        actualizar();
    }

    function setupAssistantModal() {
        const modal = document.querySelector("[data-assistant-modal]");
        const backdrop = document.querySelector("[data-assistant-backdrop]");
        const openButtons = document.querySelectorAll("[data-assistant-open]");
        const closeButtons = document.querySelectorAll("[data-assistant-close]");
        let lastTrigger = null;

        if (!modal || !backdrop) {
            return;
        }

        function openAssistant(event) {
            lastTrigger = event.currentTarget;
            modal.hidden = false;
            backdrop.hidden = false;
            document.body.classList.add("overlay-open");
            window.requestAnimationFrame(function () {
                modal.classList.add("is-open");
                backdrop.classList.add("is-open");
                const closeButton = modal.querySelector("[data-assistant-close]");
                if (closeButton) {
                    closeButton.focus();
                }
            });
        }

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

        modal.querySelectorAll("[data-assistant-option]").forEach(function (option) {
            option.addEventListener("click", function () {
                modal.querySelectorAll("[data-assistant-option]").forEach(function (current) {
                    current.setAttribute("aria-pressed", String(current === option));
                });
            });
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !modal.hidden) {
                closeAssistant();
            }
        });
    }

    function setupNotificationsMenu() {
        const menu = document.querySelector("[data-notifications-menu]");
        if (!menu) {
            return;
        }

        const trigger = menu.querySelector("[data-notifications-trigger]");
        const panel = menu.querySelector("[data-notifications-panel]");
        const markRead = menu.querySelector("[data-notifications-read]");
        const count = menu.querySelector(".notifications-count");
        const storageKey = "stageup.notifications.read";

        if (!trigger || !panel) {
            return;
        }

        function loadReadIds() {
            try {
                return JSON.parse(window.localStorage.getItem(storageKey) || "[]");
            } catch (error) {
                return [];
            }
        }

        function saveReadIds(ids) {
            try {
                window.localStorage.setItem(storageKey, JSON.stringify(ids));
            } catch (error) {
                return;
            }
        }

        function markItemRead(item) {
            if (!item) {
                return;
            }

            item.classList.remove("is-unread");
            item.classList.add("is-read");

            const state = item.querySelector(".notification-state");
            if (state) {
                state.setAttribute("aria-label", "Leída");
            }
        }

        function updateUnreadCount() {
            const unreadItems = menu.querySelectorAll(".notification-item.is-unread");
            if (!count) {
                return;
            }

            if (!unreadItems.length) {
                count.hidden = true;
                return;
            }

            count.hidden = false;
            count.textContent = String(unreadItems.length);
            count.setAttribute("aria-label", unreadItems.length === 1 ? "1 notificación sin leer" : unreadItems.length + " notificaciones sin leer");
        }

        function persistItemRead(item) {
            const id = item.getAttribute("data-notification-id");
            if (!id) {
                return;
            }

            const readIds = loadReadIds();
            if (readIds.indexOf(id) === -1) {
                readIds.push(id);
                saveReadIds(readIds);
            }
        }

        const readIds = loadReadIds();
        menu.querySelectorAll(".notification-item[data-notification-id]").forEach(function (item) {
            if (readIds.indexOf(item.getAttribute("data-notification-id")) !== -1) {
                markItemRead(item);
            }
        });
        updateUnreadCount();

        function openMenu() {
            panel.hidden = false;
            trigger.setAttribute("aria-expanded", "true");
        }

        function closeMenu() {
            panel.hidden = true;
            trigger.setAttribute("aria-expanded", "false");
        }

        trigger.addEventListener("click", function (event) {
            event.stopPropagation();
            if (panel.hidden) {
                openMenu();
            } else {
                closeMenu();
            }
        });

        panel.addEventListener("click", function (event) {
            event.stopPropagation();
        });

        if (markRead) {
            markRead.addEventListener("click", function () {
                menu.querySelectorAll(".notification-item.is-unread").forEach(function (item) {
                    markItemRead(item);
                    persistItemRead(item);
                });
                updateUnreadCount();
            });
        }

        menu.querySelectorAll(".notification-item[data-notification-id]").forEach(function (item) {
            item.addEventListener("click", function () {
                markItemRead(item);
                persistItemRead(item);
                updateUnreadCount();
            });
        });

        document.addEventListener("click", closeMenu);
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeMenu();
            }
        });
    }

    function setupAccordions() {
        const triggers = document.querySelectorAll("[data-accordion-trigger]");

        triggers.forEach(function (trigger) {
            trigger.addEventListener("click", function () {
                const answer = document.getElementById(trigger.getAttribute("aria-controls"));
                const expanded = trigger.getAttribute("aria-expanded") === "true";
                if (answer) {
                    triggers.forEach(function (current) {
                        const currentAnswer = document.getElementById(current.getAttribute("aria-controls"));
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

    setupNavigation();
    setupFilters();
    setupResultsSearchState();
    setupSearchRedirects();
    setupComparisonBar();
    setupServiceComparison();
    setupAssistantModal();
    setupNotificationsMenu();
    setupAccordions();
}());
