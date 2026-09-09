(function () {
    "use strict";

    var gallery = document.querySelector("[data-space-gallery]");
    if (gallery) {
        var mainImage = gallery.querySelector("[data-gallery-main]");
        var thumbnails = Array.from(gallery.querySelectorAll("[data-gallery-thumbnail]"));
        var previous = gallery.querySelector("[data-gallery-previous]");
        var next = gallery.querySelector("[data-gallery-next]");
        var counter = gallery.querySelector("[data-gallery-counter]");
        var expand = gallery.querySelector("[data-gallery-expand]");
        var lightbox = document.getElementById("space-gallery-lightbox");
        var lightboxImage = lightbox.querySelector("[data-gallery-lightbox-image]");
        var current = 0;

        function show(index) {
            if (!thumbnails.length) return;
            current = (index + thumbnails.length) % thumbnails.length;
            mainImage.src = thumbnails[current].getAttribute("data-src");
            thumbnails.forEach(function (thumbnail, itemIndex) {
                thumbnail.classList.toggle("is-active", itemIndex === current);
                thumbnail.setAttribute("aria-current", itemIndex === current ? "true" : "false");
            });
            counter.textContent = (current + 1) + " / " + thumbnails.length;
        }

        thumbnails.forEach(function (thumbnail, index) {
            thumbnail.addEventListener("click", function () { show(index); });
        });
        previous.addEventListener("click", function () { show(current - 1); });
        next.addEventListener("click", function () { show(current + 1); });
        expand.addEventListener("click", function () {
            lightboxImage.src = mainImage.src;
            lightbox.showModal();
        });
        lightbox.querySelector("[data-gallery-close]").addEventListener("click", function () { lightbox.close(); });
        lightbox.addEventListener("click", function (event) { if (event.target === lightbox) lightbox.close(); });
        if (thumbnails.length < 2) gallery.classList.add("has-single-image");
        show(0);
    }

    var planner = document.querySelector("[data-reservation-planner]");
    if (!planner) return;

    var dateInput = document.getElementById("reservation-date");
    var startSelect = document.getElementById("reservation-start");
    var durationSelect = document.getElementById("reservation-duration");
    var feedback = document.getElementById("reservation-feedback");
    var total = document.getElementById("reservation-total");
    var summary = document.getElementById("reservation-summary");
    var price = Number(document.getElementById("hdnPrecioHoraDetalle").value);
    var currency = document.getElementById("hdnMonedaDetalle").value || "ARS";
    var rows = [];
    try { rows = JSON.parse(document.getElementById("hdnDisponibilidadDetalle").value || "[]") || []; } catch (error) { rows = []; }

    var now = new Date();
    var minimumDate = now.getFullYear() + "-" + String(now.getMonth() + 1).padStart(2, "0") + "-" + String(now.getDate()).padStart(2, "0");
    dateInput.min = minimumDate;

    function time(minutes) {
        var normalized = minutes === 1440 ? 0 : minutes;
        return String(Math.floor(normalized / 60)).padStart(2, "0") + ":" + String(normalized % 60).padStart(2, "0");
    }

    function rangesForDate(dateValue) {
        var exact = rows.filter(function (row) { return row.Fecha === dateValue; });
        if (exact.length) return exact.filter(function (row) { return !row.Bloqueado; });
        var date = new Date(dateValue + "T12:00:00");
        var weekday = date.getDay() === 0 ? 7 : date.getDay();
        return rows.filter(function (row) { return !row.Fecha && row.DiaSemana === weekday && !row.Bloqueado; });
    }

    function renderStarts() {
        var previousValue = startSelect.value;
        startSelect.textContent = "";
        if (!dateInput.value) {
            startSelect.add(new Option("Elegí una fecha", ""));
            updateEstimate();
            return;
        }
        var ranges = rangesForDate(dateInput.value);
        var values = [];
        ranges.forEach(function (range) {
            for (var minute = range.MinutoDesde; minute + 30 <= range.MinutoHasta; minute += 30)
                if (values.indexOf(minute) < 0) values.push(minute);
        });
        values.sort(function (a, b) { return a - b; });
        if (!values.length) {
            startSelect.add(new Option("Sin horarios para esta duración", ""));
            feedback.textContent = "No hay franjas disponibles para esa fecha.";
        } else {
            startSelect.add(new Option("Seleccioná un horario", ""));
            values.forEach(function (minute) { startSelect.add(new Option(time(minute), String(minute))); });
            if (values.indexOf(Number(previousValue)) >= 0) startSelect.value = previousValue;
            feedback.textContent = values.length + (values.length === 1 ? " horario disponible." : " horarios de inicio disponibles.");
        }
        renderDurations();
    }

    function durationLabel(minutes) {
        var hours = Math.floor(minutes / 60);
        var remainder = minutes % 60;
        if (!hours) return "30 minutos";
        return hours + (hours === 1 ? " hora" : " horas") + (remainder ? " 30 minutos" : "");
    }

    function renderDurations() {
        var previousValue = durationSelect.value || "60";
        durationSelect.textContent = "";
        if (!dateInput.value || startSelect.value === "") {
            durationSelect.add(new Option("Elegí un horario", ""));
            updateEstimate();
            return;
        }
        var start = Number(startSelect.value);
        var containingRange = rangesForDate(dateInput.value).filter(function (range) {
            return start >= range.MinutoDesde && start < range.MinutoHasta;
        }).sort(function (a, b) { return b.MinutoHasta - a.MinutoHasta; })[0];
        var maximum = containingRange ? containingRange.MinutoHasta - start : 0;
        for (var minutes = 30; minutes <= maximum; minutes += 30)
            durationSelect.add(new Option(durationLabel(minutes), String(minutes)));
        durationSelect.value = Number(previousValue) <= maximum ? previousValue : String(Math.min(60, maximum));
        updateEstimate();
    }

    function updateEstimate() {
        if (!dateInput.value || startSelect.value === "" || !Number.isFinite(price) || price <= 0) {
            total.textContent = "—";
            summary.textContent = "Se calcula según la duración elegida.";
            return;
        }
        var duration = Number(durationSelect.value);
        var start = Number(startSelect.value);
        var amount = price * duration / 60;
        var formatter = new Intl.NumberFormat("es-AR", { style: "currency", currency: currency });
        var formattedDate = new Intl.DateTimeFormat("es-AR", { weekday: "short", day: "2-digit", month: "short" }).format(new Date(dateInput.value + "T12:00:00"));
        total.textContent = formatter.format(amount);
        summary.textContent = formattedDate + " · " + time(start) + " a " + time(start + duration) + " · " + duration + " minutos";
    }

    dateInput.addEventListener("change", renderStarts);
    durationSelect.addEventListener("change", updateEstimate);
    startSelect.addEventListener("change", renderDurations);
}());
