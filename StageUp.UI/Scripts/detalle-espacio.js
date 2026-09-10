(function () {
    "use strict";

    var gallery = document.querySelector("[data-space-gallery]");
    if (gallery) {
        var mainImage = gallery.querySelector("[data-gallery-main]");
        var thumbnails = Array.prototype.slice.call(gallery.querySelectorAll("[data-gallery-thumbnail]"));
        var previous = gallery.querySelector("[data-gallery-previous]");
        var next = gallery.querySelector("[data-gallery-next]");
        var counter = gallery.querySelector("[data-gallery-counter]");
        var expand = gallery.querySelector("[data-gallery-expand]");
        var lightbox = document.getElementById("space-gallery-lightbox");
        var lightboxImage = lightbox ? lightbox.querySelector("[data-gallery-lightbox-image]") : null;
        var current = 0;

        function show(index) {
            if (!thumbnails.length) {
                return;
            }

            current = (index + thumbnails.length) % thumbnails.length;
            mainImage.src = thumbnails[current].getAttribute("data-src");
            thumbnails.forEach(function (thumbnail, itemIndex) {
                var active = itemIndex === current;
                thumbnail.classList.toggle("is-active", active);
                thumbnail.setAttribute("aria-current", active ? "true" : "false");
            });
            counter.textContent = (current + 1) + " / " + thumbnails.length;
        }

        thumbnails.forEach(function (thumbnail, index) {
            thumbnail.addEventListener("click", function () {
                show(index);
            });
        });

        previous.addEventListener("click", function () {
            show(current - 1);
        });

        next.addEventListener("click", function () {
            show(current + 1);
        });

        expand.addEventListener("click", function () {
            if (!lightbox || !lightboxImage) {
                return;
            }

            lightboxImage.src = mainImage.src;
            if (typeof lightbox.showModal === "function") {
                lightbox.showModal();
            } else {
                lightbox.setAttribute("open", "open");
            }
        });

        if (lightbox) {
            lightbox.querySelector("[data-gallery-close]").addEventListener("click", function () {
                lightbox.close();
            });
            lightbox.addEventListener("click", function (event) {
                if (event.target === lightbox) {
                    lightbox.close();
                }
            });
        }

        if (thumbnails.length < 2) {
            gallery.classList.add("has-single-image");
        }

        show(0);
    }

    var planner = document.querySelector("[data-reservation-planner]");
    var dateInput = document.getElementById("txtFechaReserva");
    var startSelect = document.getElementById("reservation-start");
    var durationSelect = document.getElementById("reservation-duration");
    var feedback = document.getElementById("reservation-feedback");
    var total = document.getElementById("reservation-total");
    var summary = document.getElementById("reservation-summary");
    var startHidden = document.getElementById("hdnMinutoDesdeReserva");
    var durationHidden = document.getElementById("hdnDuracionReserva");
    var submitButton = document.getElementById("btnSolicitarReserva");

    function plannerIsComplete() {
        return !!(dateInput && dateInput.value && startHidden && startHidden.value &&
            durationHidden && durationHidden.value);
    }

    function validatePlanner() {
        if (plannerIsComplete()) {
            return true;
        }

        if (feedback) {
            feedback.textContent = "Elegí una fecha, un horario de inicio y una duración antes de enviar.";
            feedback.classList.add("is-error");
        }
        return false;
    }

    window.StageUpDetalleReserva = {
        validar: validatePlanner
    };

    if (!planner || !dateInput || !startSelect || !durationSelect) {
        return;
    }

    var price = Number(document.getElementById("hdnPrecioHoraDetalle").value);
    var currency = document.getElementById("hdnMonedaDetalle").value || "ARS";
    var rows = [];
    try {
        rows = JSON.parse(document.getElementById("hdnDisponibilidadDetalle").value || "[]") || [];
    } catch (error) {
        rows = [];
    }

    var savedStart = startHidden.value;
    var savedDuration = durationHidden.value;
    var now = new Date();
    var minimumDate = now.getFullYear() + "-" +
        String(now.getMonth() + 1).padStart(2, "0") + "-" +
        String(now.getDate()).padStart(2, "0");
    dateInput.min = minimumDate;

    function time(minutes) {
        if (minutes === 1440) {
            return "24:00";
        }

        return String(Math.floor(minutes / 60)).padStart(2, "0") + ":" +
            String(minutes % 60).padStart(2, "0");
    }

    function rangesForDate(dateValue) {
        var exact = rows.filter(function (row) {
            return row.Fecha === dateValue;
        });

        if (exact.length) {
            return exact.filter(function (row) {
                return !row.Bloqueado;
            });
        }

        var date = new Date(dateValue + "T12:00:00");
        var weekday = date.getDay() === 0 ? 7 : date.getDay();
        return rows.filter(function (row) {
            return !row.Fecha && row.DiaSemana === weekday && !row.Bloqueado;
        });
    }

    function durationLabel(minutes) {
        var hours = Math.floor(minutes / 60);
        var remainder = minutes % 60;
        if (!hours) {
            return "30 minutos";
        }

        return hours + (hours === 1 ? " hora" : " horas") +
            (remainder ? " 30 minutos" : "");
    }

    function updateEstimate() {
        var complete = !!(dateInput.value && startSelect.value && durationSelect.value);
        startHidden.value = complete ? startSelect.value : "";
        durationHidden.value = complete ? durationSelect.value : "";

        if (submitButton) {
            submitButton.disabled = !complete;
        }

        if (!complete || !Number.isFinite(price) || price <= 0) {
            total.textContent = "—";
            summary.textContent = "Se calcula según la duración elegida.";
            return;
        }

        var duration = Number(durationSelect.value);
        var start = Number(startSelect.value);
        var amount = price * duration / 60;
        var formatter = new Intl.NumberFormat("es-AR", {
            style: "currency",
            currency: currency
        });
        var formattedDate = new Intl.DateTimeFormat("es-AR", {
            weekday: "short",
            day: "2-digit",
            month: "short"
        }).format(new Date(dateInput.value + "T12:00:00"));

        total.textContent = formatter.format(amount);
        summary.textContent = formattedDate + " · " + time(start) + " a " +
            time(start + duration) + " · " + durationLabel(duration);
    }

    function renderDurations() {
        var previousValue = savedDuration || durationSelect.value || "60";
        durationSelect.textContent = "";

        if (!dateInput.value || startSelect.value === "") {
            durationSelect.add(new Option("Elegí un horario", ""));
            updateEstimate();
            return;
        }

        var start = Number(startSelect.value);
        var containingRange = rangesForDate(dateInput.value).filter(function (range) {
            return start >= range.MinutoDesde && start < range.MinutoHasta;
        }).sort(function (left, right) {
            return right.MinutoHasta - left.MinutoHasta;
        })[0];
        var maximum = containingRange ? containingRange.MinutoHasta - start : 0;

        durationSelect.add(new Option("Seleccioná la duración", ""));
        for (var minutes = 30; minutes <= maximum; minutes += 30) {
            durationSelect.add(new Option(durationLabel(minutes), String(minutes)));
        }

        if (Number(previousValue) <= maximum) {
            durationSelect.value = previousValue;
        } else if (maximum >= 60) {
            durationSelect.value = "60";
        } else if (maximum >= 30) {
            durationSelect.value = "30";
        }

        savedDuration = "";
        updateEstimate();
    }

    function renderStarts() {
        var previousValue = savedStart || startSelect.value;
        startSelect.textContent = "";
        feedback.classList.remove("is-error");

        if (!dateInput.value) {
            startSelect.add(new Option("Elegí una fecha", ""));
            feedback.textContent = "Seleccioná una fecha para consultar sus horarios.";
            renderDurations();
            return;
        }

        var ranges = rangesForDate(dateInput.value);
        var values = [];
        ranges.forEach(function (range) {
            for (var minute = range.MinutoDesde; minute + 30 <= range.MinutoHasta; minute += 30) {
                if (values.indexOf(minute) < 0) {
                    values.push(minute);
                }
            }
        });
        values.sort(function (left, right) {
            return left - right;
        });

        if (!values.length) {
            startSelect.add(new Option("Sin horarios disponibles", ""));
            feedback.textContent = "No hay franjas disponibles para esa fecha.";
        } else {
            startSelect.add(new Option("Seleccioná un horario", ""));
            values.forEach(function (minute) {
                startSelect.add(new Option(time(minute), String(minute)));
            });
            if (values.indexOf(Number(previousValue)) >= 0) {
                startSelect.value = previousValue;
            }
            feedback.textContent = values.length +
                (values.length === 1 ? " horario de inicio disponible." : " horarios de inicio disponibles.");
        }

        savedStart = "";
        renderDurations();
    }

    dateInput.addEventListener("change", function () {
        startHidden.value = "";
        durationHidden.value = "";
        renderStarts();
    });
    startSelect.addEventListener("change", renderDurations);
    durationSelect.addEventListener("change", updateEstimate);
    renderStarts();
}());
