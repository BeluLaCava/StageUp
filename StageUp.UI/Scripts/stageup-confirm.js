(function () {
    "use strict";

    var dialog;
    var titleElement;
    var messageElement;
    var confirmButton;
    var cancelButton;
    var lastFocused;
    var pendingResolve;

    function translate(text) {
        if (window.StageUpI18n && typeof window.StageUpI18n.traducir === "function") {
            return window.StageUpI18n.traducir(text);
        }

        return text;
    }

    function ensureDialog() {
        if (dialog) {
            return;
        }

        var layer = document.createElement("div");
        layer.className = "stageup-confirm-layer";
        layer.hidden = true;
        layer.innerHTML = [
            "<div class=\"stageup-confirm-backdrop\" data-stageup-confirm-cancel></div>",
            "<section class=\"stageup-confirm-dialog\" role=\"alertdialog\" aria-modal=\"true\" aria-labelledby=\"stageup-confirm-title\" aria-describedby=\"stageup-confirm-message\" tabindex=\"-1\">",
            "  <div class=\"stageup-confirm-icon\" aria-hidden=\"true\">!</div>",
            "  <div class=\"stageup-confirm-copy\">",
            "    <h2 id=\"stageup-confirm-title\"></h2>",
            "    <p id=\"stageup-confirm-message\"></p>",
            "  </div>",
            "  <div class=\"stageup-confirm-actions\">",
            "    <button type=\"button\" class=\"button button-secondary stageup-confirm-cancel\" data-stageup-confirm-cancel></button>",
            "    <button type=\"button\" class=\"button button-primary stageup-confirm-accept\" data-stageup-confirm-accept></button>",
            "  </div>",
            "</section>"
        ].join("");

        document.body.appendChild(layer);
        dialog = layer;
        titleElement = document.getElementById("stageup-confirm-title");
        messageElement = document.getElementById("stageup-confirm-message");
        confirmButton = dialog.querySelector("[data-stageup-confirm-accept]");
        cancelButton = dialog.querySelector(".stageup-confirm-cancel");

        dialog.querySelectorAll("[data-stageup-confirm-cancel]").forEach(function (element) {
            element.addEventListener("click", function () {
                close(false);
            });
        });

        confirmButton.addEventListener("click", function () {
            close(true);
        });

        document.addEventListener("keydown", function (event) {
            if (dialog && !dialog.hidden && event.key === "Escape") {
                close(false);
            }
        });
    }

    function close(result) {
        if (!dialog || dialog.hidden) {
            return;
        }

        dialog.classList.remove("is-open");
        document.body.classList.remove("overlay-open");

        window.setTimeout(function () {
            dialog.hidden = true;
            if (lastFocused && typeof lastFocused.focus === "function") {
                lastFocused.focus();
            }
        }, 180);

        if (pendingResolve) {
            pendingResolve(result);
            pendingResolve = null;
        }
    }

    function show(options) {
        ensureDialog();

        var config = options || {};
        lastFocused = document.activeElement;
        titleElement.textContent = translate(config.title || "Confirmar acción");
        messageElement.textContent = translate(config.message || "¿Querés continuar?");
        confirmButton.textContent = translate(config.confirmText || "Confirmar");
        cancelButton.textContent = translate(config.cancelText || "Cancelar");

        dialog.hidden = false;
        window.requestAnimationFrame(function () {
            dialog.classList.add("is-open");
            confirmButton.focus();
        });

        return new Promise(function (resolve) {
            pendingResolve = resolve;
        });
    }

    function confirmPostback(element, event, message, options) {
        if (!element) {
            return true;
        }

        var config = Object.assign({
            message: message,
            title: "Confirmar acción",
            confirmText: "Sí, continuar",
            cancelText: "Cancelar"
        }, options || {});

        if (element.getAttribute("data-stageup-confirmed") === "true") {
            element.removeAttribute("data-stageup-confirmed");
            if (config.confirmedText) {
                if ("value" in element) {
                    element.value = translate(config.confirmedText);
                } else {
                    element.textContent = translate(config.confirmedText);
                }
            }
            if (config.disableOnConfirm) {
                element.disabled = true;
            }
            return true;
        }

        if (config.validationGroup && typeof window.Page_ClientValidate === "function" && !window.Page_ClientValidate(config.validationGroup)) {
            return false;
        }

        if (event) {
            event.preventDefault();
        }

        show(config).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            element.setAttribute("data-stageup-confirmed", "true");
            element.click();
        });

        return false;
    }

    window.StageUpConfirm = {
        show: show,
        confirmPostback: confirmPostback
    };
}());
