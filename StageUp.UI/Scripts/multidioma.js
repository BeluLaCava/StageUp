(function () {
    "use strict";

    var campo = document.getElementById("hdnDiccionarioIdioma");
    if (!campo || !campo.value) {
        return;
    }

    var configuracion;
    try {
        configuracion = JSON.parse(campo.value);
    } catch (error) {
        return;
    }

    var porClave = configuracion.porClave || {};
    var porTextoNormalizado = {};
    var textosBase = configuracion.porTexto || {};

    Object.keys(textosBase).forEach(function (texto) {
        porTextoNormalizado[normalizar(texto)] = textosBase[texto];
    });

    function normalizar(texto) {
        return String(texto || "").replace(/\s+/g, " ").trim();
    }

    function traducirValor(valor) {
        var normalizado = normalizar(valor);
        return normalizado && Object.prototype.hasOwnProperty.call(porTextoNormalizado, normalizado)
            ? porTextoNormalizado[normalizado]
            : null;
    }

    function traducirPorClave(elemento, atributoClave, atributoDestino) {
        var clave = elemento.getAttribute(atributoClave);
        if (!clave || !Object.prototype.hasOwnProperty.call(porClave, clave)) {
            return;
        }

        var traduccion = porClave[clave];
        if (atributoDestino) {
            elemento.setAttribute(atributoDestino, traduccion);
        } else {
            elemento.textContent = traduccion;
        }
    }

    function traducirElementosExplicitos(raiz) {
        if (raiz.nodeType !== 1 && raiz.nodeType !== 9) {
            return;
        }

        var elementos = [];
        if (raiz.nodeType === 1) {
            elementos.push(raiz);
        }

        elementos = elementos.concat(Array.prototype.slice.call(raiz.querySelectorAll(
            "[data-i18n], [data-i18n-placeholder], [data-i18n-title], [data-i18n-aria-label]"
        )));

        elementos.forEach(function (elemento) {
            if (elemento.hasAttribute("data-i18n")) {
                traducirPorClave(elemento, "data-i18n", null);
            }
            if (elemento.hasAttribute("data-i18n-placeholder")) {
                traducirPorClave(elemento, "data-i18n-placeholder", "placeholder");
            }
            if (elemento.hasAttribute("data-i18n-title")) {
                traducirPorClave(elemento, "data-i18n-title", "title");
            }
            if (elemento.hasAttribute("data-i18n-aria-label")) {
                traducirPorClave(elemento, "data-i18n-aria-label", "aria-label");
            }
        });
    }

    function debeIgnorarse(nodo) {
        var padre = nodo.parentElement;
        if (!padre) {
            return true;
        }

        return /^(SCRIPT|STYLE|TEXTAREA|SELECT|OPTION|CODE|PRE)$/.test(padre.tagName) ||
            padre.closest("[data-i18n-skip]") !== null;
    }

    function traducirNodoTexto(nodo) {
        if (!nodo || nodo.nodeType !== 3 || debeIgnorarse(nodo)) {
            return;
        }

        var traduccion = traducirValor(nodo.nodeValue);
        if (!traduccion) {
            return;
        }

        var coincidencia = nodo.nodeValue.match(/^(\s*)([\s\S]*?)(\s*)$/);
        var nuevoValor = coincidencia[1] + traduccion + coincidencia[3];
        if (nuevoValor !== nodo.nodeValue) {
            nodo.nodeValue = nuevoValor;
        }
    }

    function traducirTextos(raiz) {
        if (raiz.nodeType === 3) {
            traducirNodoTexto(raiz);
            return;
        }

        if (raiz.nodeType !== 1 && raiz.nodeType !== 9) {
            return;
        }

        var recorrido = document.createTreeWalker(raiz, NodeFilter.SHOW_TEXT, null, false);
        var nodos = [];
        var actual;
        while ((actual = recorrido.nextNode())) {
            nodos.push(actual);
        }

        nodos.forEach(traducirNodoTexto);
    }

    function traducirAtributos(raiz) {
        if (raiz.nodeType !== 1 && raiz.nodeType !== 9) {
            return;
        }

        var elementos = raiz.nodeType === 1 ? [raiz] : [];
        elementos = elementos.concat(Array.prototype.slice.call(raiz.querySelectorAll(
            "input[placeholder], textarea[placeholder], [title], [aria-label], input[type='button'], input[type='submit']"
        )));

        elementos.forEach(function (elemento) {
            ["placeholder", "title", "aria-label"].forEach(function (atributo) {
                if (!elemento.hasAttribute(atributo)) {
                    return;
                }
                var traduccion = traducirValor(elemento.getAttribute(atributo));
                if (traduccion) {
                    elemento.setAttribute(atributo, traduccion);
                }
            });

            if (elemento.tagName === "INPUT" && /^(button|submit)$/i.test(elemento.type)) {
                var traduccionValor = traducirValor(elemento.value);
                if (traduccionValor) {
                    elemento.value = traduccionValor;
                }
            }
        });
    }

    function aplicarTraducciones(raiz) {
        traducirElementosExplicitos(raiz);
        traducirTextos(raiz);
        traducirAtributos(raiz);
    }

    aplicarTraducciones(document);

    if (window.MutationObserver) {
        var observador = new MutationObserver(function (cambios) {
            cambios.forEach(function (cambio) {
                if (cambio.type === "characterData") {
                    traducirNodoTexto(cambio.target);
                    return;
                }

                Array.prototype.forEach.call(cambio.addedNodes, aplicarTraducciones);
            });
        });

        observador.observe(document.body, {
            childList: true,
            subtree: true,
            characterData: true
        });
    }
}());
