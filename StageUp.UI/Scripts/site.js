(function () {
    "use strict";

    function setupNavigation() {
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
            var action = searchBox.querySelector("[data-search-redirect]");
            var input = searchBox.querySelector("input[type='search']");

            if (!action) {
                return;
            }

            function redirectToResults() {
                var target = action.getAttribute("data-search-target");
                var term = input ? input.value.trim() : "";

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
            resultsPage.classList.add("has-search-query");
        }

        if (tipo) {
            var typeField = document.getElementById("filter-type");
            if (typeField) {
                typeField.value = tipo;
                typeField.dispatchEvent(new Event("change"));
            }
            resultsPage.classList.add("has-search-query");
        }
    }

    function setupFilters() {
        var panel = document.querySelector("[data-filter-panel]");
        var backdrop = document.querySelector("[data-filter-backdrop]");
        var activeRegion = document.querySelector("[data-active-filters]");
        var activeList = document.querySelector("[data-active-filter-list]");
        var lastTrigger = null;

        if (!panel || !backdrop || !activeRegion || !activeList) {
            return;
        }

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
            var selections = [];

            quickFilters.forEach(function (filter, index) {
                if (filter.getAttribute("aria-pressed") === "true") {
                    selections.push({ label: filter.getAttribute("data-filter-label"), element: filter, type: "quick", index: index });
                }
            });

            generalFields.forEach(function (definition) {
                var field = document.getElementById(definition.id);
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
            var selections = getSelections();
            activeList.innerHTML = "";
            activeRegion.hidden = selections.length === 0;

            selections.forEach(function (selection) {
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
                var selected = filter.getAttribute("aria-pressed") === "true";
                filter.setAttribute("aria-pressed", String(!selected));
                renderSelections();
            });
        });

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
        var bar = document.querySelector("[data-compare-bar]");
        var checkboxes = Array.from(document.querySelectorAll("[data-compare-checkbox]"));

        if (!bar || checkboxes.length === 0) {
            return;
        }

        var CANTIDAD_MAXIMA = 3;
        var barText = bar.querySelector("[data-compare-bar-text]");
        var goButton = bar.querySelector("[data-compare-go]");
        var clearButton = bar.querySelector("[data-compare-clear]");

        function elegidos() {
            return checkboxes.filter(function (checkbox) {
                return checkbox.checked;
            });
        }

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

    function setupAssistantModal() {
        var modal = document.querySelector("[data-assistant-modal]");
        var backdrop = document.querySelector("[data-assistant-backdrop]");
        var openButtons = document.querySelectorAll("[data-assistant-open]");
        var closeButtons = document.querySelectorAll("[data-assistant-close]");
        var lastTrigger = null;

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
                var closeButton = modal.querySelector("[data-assistant-close]");
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

    setupNavigation();
    setupFilters();
    setupResultsSearchState();
    setupSearchRedirects();
    setupComparisonBar();
    setupAssistantModal();
    setupAccordions();
}());
