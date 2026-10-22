(function () {
    "use strict";

    const gallery = document.querySelector("[data-space-gallery]");
    if (gallery) {
        const mainImage = gallery.querySelector("[data-gallery-main]");
        const thumbnails = Array.prototype.slice.call(gallery.querySelectorAll("[data-gallery-thumbnail]"));
        const previous = gallery.querySelector("[data-gallery-previous]");
        const next = gallery.querySelector("[data-gallery-next]");
        const counter = gallery.querySelector("[data-gallery-counter]");
        const expand = gallery.querySelector("[data-gallery-expand]");
        const expandImage = gallery.querySelector("[data-gallery-expand-image]");
        const lightbox = document.getElementById("space-gallery-lightbox");
        const lightboxImage = lightbox ? lightbox.querySelector("[data-gallery-lightbox-image]") : null;
        const lightboxPrevious = lightbox ? lightbox.querySelector("[data-gallery-lightbox-previous]") : null;
        const lightboxNext = lightbox ? lightbox.querySelector("[data-gallery-lightbox-next]") : null;
        const lightboxCounter = lightbox ? lightbox.querySelector("[data-gallery-lightbox-counter]") : null;
        let current = 0;

        function lightboxIsOpen() {
            return !!(lightbox && lightbox.hasAttribute("open"));
        }

        function updateLightbox() {
            if (!lightboxImage || !thumbnails.length) {
                return;
            }

            lightboxImage.src = thumbnails[current].getAttribute("data-src");
            lightboxImage.alt = "Fotografía " + (current + 1) + " de " + thumbnails.length + " del espacio";
            if (lightboxCounter) {
                lightboxCounter.textContent = (current + 1) + " de " + thumbnails.length;
            }
        }

        function show(index) {
            if (!thumbnails.length) {
                return;
            }

            current = (index + thumbnails.length) % thumbnails.length;
            mainImage.src = thumbnails[current].getAttribute("data-src");
            thumbnails.forEach(function (thumbnail, itemIndex) {
                const active = itemIndex === current;
                thumbnail.classList.toggle("is-active", active);
                thumbnail.setAttribute("aria-current", active ? "true" : "false");
            });
            counter.textContent = (current + 1) + " de " + thumbnails.length;
            if (lightboxIsOpen()) {
                updateLightbox();
            }
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

        function openLightbox() {
            if (!lightbox || !lightboxImage) {
                return;
            }

            updateLightbox();
            if (typeof lightbox.showModal === "function") {
                lightbox.showModal();
            } else {
                lightbox.setAttribute("open", "open");
            }

            lightbox.querySelector("[data-gallery-close]").focus();
        }

        function closeLightbox() {
            if (!lightbox) {
                return;
            }

            if (typeof lightbox.close === "function" && lightbox.open) {
                lightbox.close();
            } else {
                lightbox.removeAttribute("open");
            }
        }

        expand.addEventListener("click", openLightbox);
        if (expandImage) {
            expandImage.addEventListener("click", openLightbox);
        }

        if (lightbox) {
            lightbox.querySelector("[data-gallery-close]").addEventListener("click", function () {
                closeLightbox();
            });
            lightbox.addEventListener("click", function (event) {
                if (event.target === lightbox) {
                    closeLightbox();
                }
            });

            if (lightboxPrevious) {
                lightboxPrevious.addEventListener("click", function () {
                    show(current - 1);
                });
            }

            if (lightboxNext) {
                lightboxNext.addEventListener("click", function () {
                    show(current + 1);
                });
            }

            document.addEventListener("keydown", function (event) {
                if (!lightboxIsOpen()) {
                    return;
                }

                if (event.key === "Escape") {
                    event.preventDefault();
                    closeLightbox();
                } else if (event.key === "ArrowLeft") {
                    event.preventDefault();
                    show(current - 1);
                } else if (event.key === "ArrowRight") {
                    event.preventDefault();
                    show(current + 1);
                }
            });
        }

        if (thumbnails.length < 2) {
            gallery.classList.add("has-single-image");
            if (lightbox) {
                lightbox.classList.add("has-single-image");
            }
        }

        show(0);
    }

    const planner = document.querySelector("[data-reservation-planner]");
    const dateInput = document.getElementById("txtFechaReserva");
    const startSelect = document.getElementById("reservation-start");
    const durationSelect = document.getElementById("reservation-duration");
    const feedback = document.getElementById("reservation-feedback");
    const total = document.getElementById("reservation-total");
    const summary = document.getElementById("reservation-summary");
    const startHidden = document.getElementById("hdnMinutoDesdeReserva");
    const durationHidden = document.getElementById("hdnDuracionReserva");
    const submitButton = document.getElementById("btnSolicitarReserva");

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

    const price = Number(document.getElementById("hdnPrecioHoraDetalle").value);
    const currency = document.getElementById("hdnMonedaDetalle").value || "ARS";
    let rows = [];
    try {
        rows = JSON.parse(document.getElementById("hdnDisponibilidadDetalle").value || "[]") || [];
    } catch (error) {
        rows = [];
    }





    let occupiedRows = [];
    const occupiedField = document.getElementById("hdnReservasOcupadasDetalle");
    if (occupiedField) {
        try {
            occupiedRows = JSON.parse(occupiedField.value || "[]") || [];
        } catch (error) {
            occupiedRows = [];
        }
    }

    function restarSegmentos(ranges, bloqueadores) {
        if (!bloqueadores.length) {
            return ranges;
        }

        const libres = [];
        ranges.forEach(function (range) {
            let segmentos = [{ MinutoDesde: range.MinutoDesde, MinutoHasta: range.MinutoHasta }];
            bloqueadores.forEach(function (bloqueador) {
                const siguientes = [];
                segmentos.forEach(function (segmento) {
                    if (bloqueador.MinutoHasta <= segmento.MinutoDesde || bloqueador.MinutoDesde >= segmento.MinutoHasta) {
                        siguientes.push(segmento);
                        return;
                    }

                    if (bloqueador.MinutoDesde > segmento.MinutoDesde) {
                        siguientes.push({ MinutoDesde: segmento.MinutoDesde, MinutoHasta: bloqueador.MinutoDesde });
                    }

                    if (bloqueador.MinutoHasta < segmento.MinutoHasta) {
                        siguientes.push({ MinutoDesde: bloqueador.MinutoHasta, MinutoHasta: segmento.MinutoHasta });
                    }
                });
                segmentos = siguientes;
            });

            segmentos.forEach(function (segmento) {
                if (segmento.MinutoHasta > segmento.MinutoDesde) {
                    libres.push(segmento);
                }
            });
        });

        return libres;
    }











    function franjasParaFecha(candidatas, dateValue, weekday) {
        const exactas = candidatas.filter(function (row) {
            return row.Fecha === dateValue;
        });

        if (exactas.length) {
            return exactas;
        }

        return candidatas.filter(function (row) {
            return !row.Fecha && row.DiaSemana === weekday;
        });
    }

    let savedStart = startHidden.value;
    let savedDuration = durationHidden.value;
    const now = new Date();
    const minimumDate = now.getFullYear() + "-" +
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
        const date = new Date(dateValue + "T12:00:00");
        const weekday = date.getDay() === 0 ? 7 : date.getDay();






        const disponibilidadRows = rows.filter(function (row) {
            return !row.Bloqueado;
        });
        const bloqueadoRows = rows.filter(function (row) {
            return row.Bloqueado;
        });

        const disponibles = franjasParaFecha(disponibilidadRows, dateValue, weekday);



        const bloqueadosDelDia = bloqueadoRows.filter(function (row) {
            return row.Fecha === dateValue || (!row.Fecha && row.DiaSemana === weekday);
        });
        const ocupadosDelDia = occupiedRows.filter(function (occupied) {
            return occupied.Fecha === dateValue;
        });

        const sinBloqueos = restarSegmentos(disponibles, bloqueadosDelDia);
        return restarSegmentos(sinBloqueos, ocupadosDelDia);
    }

    function durationLabel(minutes) {
        const hours = Math.floor(minutes / 60);
        const remainder = minutes % 60;
        if (!hours) {
            return "30 minutos";
        }

        return hours + (hours === 1 ? " hora" : " horas") +
            (remainder ? " 30 minutos" : "");
    }

    function updateEstimate() {
        const complete = !!(dateInput.value && startSelect.value && durationSelect.value);
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

        const duration = Number(durationSelect.value);
        const start = Number(startSelect.value);
        const amount = price * duration / 60;
        const formatter = new Intl.NumberFormat("es-AR", {
            style: "currency",
            currency: currency
        });
        const formattedDate = new Intl.DateTimeFormat("es-AR", {
            weekday: "short",
            day: "2-digit",
            month: "short"
        }).format(new Date(dateInput.value + "T12:00:00"));

        total.textContent = formatter.format(amount);
        summary.textContent = formattedDate + " · " + time(start) + " a " +
            time(start + duration) + " · " + durationLabel(duration);
    }

    function renderDurations() {
        const previousValue = savedDuration || durationSelect.value || "60";
        durationSelect.textContent = "";

        if (!dateInput.value || startSelect.value === "") {
            durationSelect.add(new Option("Elegí un horario", ""));
            updateEstimate();
            return;
        }

        const start = Number(startSelect.value);
        const containingRange = rangesForDate(dateInput.value).filter(function (range) {
            return start >= range.MinutoDesde && start < range.MinutoHasta;
        }).sort(function (left, right) {
            return right.MinutoHasta - left.MinutoHasta;
        })[0];
        const maximum = containingRange ? containingRange.MinutoHasta - start : 0;

        durationSelect.add(new Option("Seleccioná la duración", ""));
        for (let minutes = 30; minutes <= maximum; minutes += 30) {
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
        const previousValue = savedStart || startSelect.value;
        startSelect.textContent = "";
        feedback.classList.remove("is-error");

        if (!dateInput.value) {
            startSelect.add(new Option("Elegí una fecha", ""));
            feedback.textContent = "Seleccioná una fecha para consultar sus horarios.";
            renderDurations();
            return;
        }

        const ranges = rangesForDate(dateInput.value);
        const values = [];
        ranges.forEach(function (range) {
            for (let minute = range.MinutoDesde; minute + 30 <= range.MinutoHasta; minute += 30) {
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
