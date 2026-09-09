(function () {
    "use strict";
    var hidden = document.getElementById("hdnDisponibilidad");
    if (!hidden) return;
    var rows = [];
    var list = document.getElementById("schedule-list");
    var error = document.getElementById("schedule-error");
    var mode = document.getElementById("schedule-mode");
    var days = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
    try { rows = JSON.parse(hidden.value || "[]") || []; } catch (e) { error.textContent = "No se pudieron leer los horarios."; }
    function time(minutes) {
        return String(Math.floor(minutes / 60)).padStart(2, "0") + ":" + String(minutes % 60).padStart(2, "0");
    }
    function render() {
        hidden.value = JSON.stringify(rows);
        list.textContent = "";
        rows.forEach(function (row, index) {
            var item = document.createElement("li");
            var label = document.createElement("span");
            var day = row.Fecha ? row.Fecha.split("-").reverse().join("/") : days[row.DiaSemana];
            label.textContent = day + (row.Bloqueado ? " · Cerrado" : " · " + time(row.MinutoDesde) + " a " + time(row.MinutoHasta));
            var remove = document.createElement("button");
            remove.type = "button";
            remove.textContent = "Quitar";
            remove.setAttribute("aria-label", "Quitar horario: " + label.textContent);
            remove.addEventListener("click", function () { rows.splice(index, 1); render(); });
            item.appendChild(label);
            item.appendChild(remove);
            list.appendChild(item);
        });
    }
    mode.addEventListener("change", function () {
        document.getElementById("schedule-days").hidden = mode.value !== "weekly";
        document.getElementById("schedule-date-wrap").hidden = mode.value === "weekly";
        document.getElementById("schedule-times").hidden = mode.value === "closed";
    });
    function minutes(value) {
        if (!/^\d{2}:\d{2}$/.test(value)) return NaN;
        var parts = value.split(":").map(Number);
        return parts[0] * 60 + parts[1];
    }
    document.getElementById("schedule-add").addEventListener("click", function () {
        error.textContent = "";
        var closed = mode.value === "closed";
        var from = closed ? 0 : minutes(document.getElementById("schedule-from").value);
        var to = closed ? 1440 : minutes(document.getElementById("schedule-to").value);
        if (to === 0) to = 1440;
        if (!Number.isFinite(from) || !Number.isFinite(to) || from >= to || from % 30 || to % 30) {
            error.textContent = "Indicá un horario válido, en intervalos de 30 minutos y con fin posterior al inicio.";
            return;
        }
        var selected = mode.value === "weekly" ?
            Array.from(document.querySelectorAll("#schedule-days input:checked")).map(function (input) { return Number(input.value); }) : [null];
        var date = mode.value === "weekly" ? null : document.getElementById("schedule-date").value;
        if (!selected.length || (mode.value !== "weekly" && !date)) {
            error.textContent = "Seleccioná los días o la fecha correspondiente.";
            return;
        }
        if (rows.length + selected.length > 100) {
            error.textContent = "Podés cargar hasta 100 franjas.";
            return;
        }
        var additions = selected.map(function (day) {
            return { DiaSemana: day, Fecha: date, MinutoDesde: from, MinutoHasta: to, Bloqueado: closed };
        });
        var overlaps = additions.some(function (next) {
            return rows.some(function (row) {
                var sameDay = next.Fecha ? next.Fecha === row.Fecha : !row.Fecha && next.DiaSemana === row.DiaSemana;
                return sameDay && next.MinutoDesde < row.MinutoHasta && row.MinutoDesde < next.MinutoHasta;
            });
        });
        if (overlaps) {
            error.textContent = "Ese horario se superpone con otro. Quitá la franja anterior si querés reemplazarla.";
            return;
        }
        rows = rows.concat(additions);
        render();
    });
    var price = document.getElementById("txtPrecioHora");
    var currency = document.getElementById("ddlMoneda");
    function showPrice() {
        var value = Number(price.value.replace(",", "."));
        var output = document.getElementById("price-example");
        if (!Number.isFinite(value) || value <= 0) {
            output.textContent = "El importe se calcula según la duración: precio por hora × minutos ÷ 60.";
            return;
        }
        var format = new Intl.NumberFormat("es-AR", { style: "currency", currency: currency.value });
        output.textContent = "30 min: " + format.format(value / 2) + " · 1 hora: " + format.format(value) + " · 1 h 30 min: " + format.format(value * 1.5) + " (importe base)";
    }
    price.addEventListener("input", showPrice);
    currency.addEventListener("change", showPrice);
    var upload = document.getElementById("archivoFoto");
    var preview = document.getElementById("imgFotoActual");
    var originalSource = preview.getAttribute("src") || "";
    var objectUrl;
    upload.addEventListener("change", function () {
        document.getElementById("photo-error").textContent = "";
        if (objectUrl) URL.revokeObjectURL(objectUrl);
        var file = upload.files[0];
        if (!file) { preview.src = originalSource; return; }
        if (file.size > 3 * 1024 * 1024 || !/\.(jpe?g|png)$/i.test(file.name)) {
            upload.value = "";
            preview.src = originalSource;
            document.getElementById("photo-error").textContent = "Elegí una imagen JPG o PNG de hasta 3 MB.";
            return;
        }
        objectUrl = URL.createObjectURL(file);
        preview.src = objectUrl;
    });
    render();
    showPrice();
}());
