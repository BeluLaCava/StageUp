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
        if (!panel || !backdrop || !activeRegion || !activeList) return;

        const fields = [
            ["filter-type", "tipo", "Tipo"], ["filter-location", "ubicacion", "Ubicación"],
            ["filter-province", "provincia", "Provincia"], ["filter-city", "ciudad", "Ciudad"],
            ["filter-zone", "zona", "Zona"], ["filter-capacity", "capacidadMin", "Capacidad mínima"],
            ["filter-price-min", "precioMin", "Precio mínimo"], ["filter-price", "precioMax", "Precio máximo"],
            ["filter-availability", "fecha", "Disponibilidad"], ["filter-time-from", "horaDesde", "Desde"],
            ["filter-time-to", "horaHasta", "Hasta"], ["filter-rating", "calificacionMin", "Calificación mínima"],
            ["filter-floor", "piso", "Tipo de piso"], ["filter-features", "caracteristicas", "Características"]
        ];
        const equipment = Array.from(panel.querySelectorAll("[data-equip-code]"));
        const quickFilters = Array.from(document.querySelectorAll("[data-filter-target]"));
        const applied = new URLSearchParams(window.location.search);
        let lastTrigger = null;
        let closeTimer = null;

        function restoreFields() {
            fields.forEach(function (field) {
                const element = document.getElementById(field[0]);
                if (element) element.value = applied.get(field[1]) || "";
            });
            const codes = (applied.get("equip") || "").split(",");
            equipment.forEach(function (checkbox) {
                checkbox.checked = codes.indexOf(checkbox.getAttribute("data-equip-code")) >= 0;
            });
        }

        function navigate(params) {
            const query = params.toString();
            window.location.href = window.location.pathname + (query ? "?" + query : "");
        }

        function removeFilter(param, code) {
            const next = new URLSearchParams(applied.toString());
            if (code) {
                const remaining = (next.get(param) || "").split(",").filter(function (item) { return item !== code; });
                if (remaining.length) next.set(param, remaining.join(",")); else next.delete(param);
            } else {
                next.delete(param);
            }
            navigate(next);
        }

        function addChip(label, param, code) {
            const chip = document.createElement("button");
            chip.type = "button";
            chip.className = "active-filter-chip";
            chip.textContent = label + " ×";
            chip.setAttribute("aria-label", "Quitar filtro " + label);
            chip.addEventListener("click", function () { removeFilter(param, code); });
            activeList.appendChild(chip);
        }

        function renderApplied() {
            activeList.textContent = "";
            fields.forEach(function (field) {
                const value = applied.get(field[1]);
                if (value) addChip(field[2] + ": " + value, field[1]);
            });
            (applied.get("equip") || "").split(",").filter(Boolean).forEach(function (code) {
                const checkbox = equipment.find(function (item) { return item.getAttribute("data-equip-code") === code; });
                addChip(checkbox ? checkbox.value : code, "equip", code);
            });
            activeRegion.hidden = activeList.children.length === 0;
            quickFilters.forEach(function (button) {
                const keys = button.getAttribute("data-filter-params").split(",");
                const selected = keys.some(function (key) { return !!applied.get(key); });
                button.classList.toggle("has-value", selected);
                button.setAttribute("aria-label", button.textContent.trim() + (selected ? ": modificar filtros aplicados" : ": elegir filtro"));
            });
        }

        function openPanel(event) {
            window.clearTimeout(closeTimer);
            lastTrigger = event.currentTarget;
            restoreFields();
            panel.hidden = false;
            backdrop.hidden = false;
            document.body.classList.add("overlay-open");
            window.StageUpModalFocus.activate(panel, lastTrigger);
            document.querySelectorAll("[data-filter-open],[data-filter-target]").forEach(function (button) {
                button.setAttribute("aria-expanded", String(button === lastTrigger));
            });
            window.requestAnimationFrame(function () {
                if (panel.hidden) return;
                panel.classList.add("is-open");
                backdrop.classList.add("is-open");
                const target = document.getElementById(lastTrigger.getAttribute("data-filter-target")) || panel.querySelector("[data-filter-close]");
                if (target) { target.focus(); target.scrollIntoView({ block: "nearest" }); }
            });
        }

        function closePanel() {
            if (panel.hidden) return;
            panel.classList.remove("is-open");
            backdrop.classList.remove("is-open");
            document.querySelectorAll("[data-filter-open],[data-filter-target]").forEach(function (button) { button.setAttribute("aria-expanded", "false"); });
            closeTimer = window.setTimeout(function () {
                panel.hidden = true;
                backdrop.hidden = true;
                window.StageUpModalFocus.deactivate(panel);
            }, 240);
        }

        document.querySelectorAll("[data-filter-open],[data-filter-target]").forEach(function (button) { button.addEventListener("click", openPanel); });
        panel.querySelectorAll("[data-filter-close]").forEach(function (button) { button.addEventListener("click", closePanel); });
        document.querySelectorAll("[data-clear-filters]").forEach(function (button) {
            button.addEventListener("click", function () {
                const next = new URLSearchParams(applied.toString());
                fields.forEach(function (field) { next.delete(field[1]); });
                next.delete("equip");
                navigate(next);
            });
        });
        panel.querySelector("[data-reset-filter-fields]").addEventListener("click", function () {
            fields.forEach(function (field) { const element = document.getElementById(field[0]); if (element) element.value = ""; });
            equipment.forEach(function (checkbox) { checkbox.checked = false; });
        });
        panel.querySelector("[data-apply-filters]").addEventListener("click", function () {
            const invalid = Array.from(panel.querySelectorAll("input,select")).find(function (field) { return !field.checkValidity(); });
            if (invalid) { invalid.reportValidity(); return; }
            const next = new URLSearchParams(applied.toString());
            fields.forEach(function (field) {
                const element = document.getElementById(field[0]);
                const value = element ? element.value.trim() : "";
                if (value) next.set(field[1], value); else next.delete(field[1]);
            });
            const codes = equipment.filter(function (checkbox) { return checkbox.checked; }).map(function (checkbox) { return checkbox.getAttribute("data-equip-code"); });
            if (codes.length) next.set("equip", codes.join(",")); else next.delete("equip");
            const search = document.querySelector("[data-results-search-input]");
            if (search) { if (search.value.trim()) next.set("q", search.value.trim()); else next.delete("q"); }
            navigate(next);
        });
        backdrop.addEventListener("click", closePanel);
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && window.StageUpModalFocus.isTop(panel)) { event.preventDefault(); closePanel(); }
        });
        restoreFields();
        renderApplied();
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
        let closeTimer = null;

        if (!modal || !backdrop) {
            return;
        }

        function openAssistant(event) {
            window.clearTimeout(closeTimer);
            lastTrigger = event.currentTarget;
            modal.hidden = false;
            window.StageUpModalFocus.activate(modal, lastTrigger);
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
            if (modal.hidden) return;
            modal.dispatchEvent(new Event("stageup:assistant-closing"));
            modal.classList.remove("is-open");
            backdrop.classList.remove("is-open");
            closeTimer = window.setTimeout(function () {
                modal.hidden = true;
                backdrop.hidden = true;
                window.StageUpModalFocus.deactivate(modal);
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
            if (event.key === "Escape" && window.StageUpModalFocus.isTop(modal)) {
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

    function setupAccountNavigation() {
        const sidebar = document.querySelector(".app-sidebar");
        const toggle = document.querySelector("[data-sidebar-toggle]");
        const navigation = document.querySelector("[data-sidebar-navigation]");
        if (sidebar && toggle && navigation) {
            sidebar.classList.add("is-collapsible");
            function setOpen(open) {
                sidebar.classList.toggle("is-open", open);
                toggle.setAttribute("aria-expanded", String(open));
                toggle.textContent = open ? "Ocultar menú" : "Mostrar menú";
            }
            toggle.addEventListener("click", function () { setOpen(toggle.getAttribute("aria-expanded") !== "true"); });
            sidebar.addEventListener("keydown", function (event) {
                if (event.key === "Escape" && toggle.getAttribute("aria-expanded") === "true") {
                    setOpen(false);
                    toggle.focus();
                }
            });
        }

        function file(url) { return new URL(url, window.location.href).pathname.split("/").pop().toLowerCase(); }
        const current = file(window.location.href);
        const parents = {
            "solicitudesrecibidas.aspx": "misespacios.aspx", "disponibilidadespacio.aspx": "misespacios.aspx",
            "pagar.aspx": "misreservas.aspx", "detallereserva.aspx": "misreservas.aspx", "detallepago.aspx": "misreservas.aspx",
            "detalleespacio.aspx": "resultadosbusqueda.aspx", "compararespacios.aspx": "resultadosbusqueda.aspx",
            "compararservicios.aspx": "resultadosbusqueda.aspx"
        };

        if (current === "detallereserva.aspx" || current === "detallepago.aspx") {
            const back = document.querySelector("a[id$='lnkVolverListado'],a[id$='lnkVolver']");
            if (back) {
                const target = file(back.href);
                parents[current] = parents[target] || target;
            }
        }
        document.querySelectorAll(".sidebar-navigation a,.primary-navigation a,.user-profile-link").forEach(function (link) {
            const target = file(link.href);
            const exact = target === current;
            if (exact || target === parents[current]) {
                link.classList.add("is-active");
                link.setAttribute("aria-current", exact ? "page" : "location");
            }
        });
    }

    setupNavigation();
    setupAccountNavigation();
    setupFilters();
    setupResultsSearchState();
    setupSearchRedirects();
    setupComparisonBar();
    setupServiceComparison();
    setupAssistantModal();
    setupNotificationsMenu();
    setupAccordions();
}());
