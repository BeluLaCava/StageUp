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
    var plantillas = [];
    var textosBase = configuracion.porTexto || {};

    Object.keys(textosBase).forEach(function (texto) {
        porTextoNormalizado[normalizar(texto)] = textosBase[texto];
        if (/\{\d+\}/.test(texto)) {
            plantillas.push(crearPlantilla(normalizar(texto), textosBase[texto]));
        }
    });

    function normalizar(texto) {
        return String(texto || "").replace(/\s+/g, " ").trim();
    }

    function traducirValor(valor) {
        var normalizado = normalizar(valor);
        if (!normalizado) {
            return null;
        }

        if (Object.prototype.hasOwnProperty.call(porTextoNormalizado, normalizado)) {
            return porTextoNormalizado[normalizado];
        }

        for (var indice = 0; indice < plantillas.length; indice++) {
            var plantilla = plantillas[indice];
            var coincidencia = normalizado.match(plantilla.patron);
            if (coincidencia) {
                return completarPlantilla(plantilla, coincidencia);
            }
        }

        return null;
    }

    function escaparExpresionRegular(valor) {
        return valor.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    }

    function crearPlantilla(textoBase, traduccion) {
        var indices = [];
        var partes = [];
        var expresion = /\{(\d+)\}/g;
        var posicion = 0;
        var coincidencia;

        while ((coincidencia = expresion.exec(textoBase))) {
            partes.push(escaparExpresionRegular(textoBase.substring(posicion, coincidencia.index)));
            partes.push("([\\s\\S]+?)");
            indices.push(parseInt(coincidencia[1], 10));
            posicion = coincidencia.index + coincidencia[0].length;
        }

        partes.push(escaparExpresionRegular(textoBase.substring(posicion)));
        return {
            patron: new RegExp("^" + partes.join("") + "$"),
            indices: indices,
            traduccion: traduccion
        };
    }

    function completarPlantilla(plantilla, coincidencia) {
        var valores = {};
        plantilla.indices.forEach(function (indice, posicion) {
            var valor = coincidencia[posicion + 1];
            var valorNormalizado = normalizar(valor);
            valores[indice] = Object.prototype.hasOwnProperty.call(porTextoNormalizado, valorNormalizado)
                ? porTextoNormalizado[valorNormalizado]
                : valor;
        });

        return plantilla.traduccion.replace(/\{(\d+)\}/g, function (marcador, indice) {
            return Object.prototype.hasOwnProperty.call(valores, indice) ? valores[indice] : marcador;
        });
    }

    function traducirPorClave(elemento, atributoClave, atributoDestino) {
        var clave = elemento.getAttribute(atributoClave);
        if (!clave || !Object.prototype.hasOwnProperty.call(porClave, clave)) {
            return;
        }

        var traduccion = porClave[clave];
        if (atributoDestino) {
            elemento.setAttribute(atributoDestino, traduccion);
        } else if (elemento.tagName === "INPUT" && /^(button|submit)$/i.test(elemento.type)) {
            elemento.value = traduccion;
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

        return /^(SCRIPT|STYLE|TEXTAREA|CODE|PRE)$/.test(padre.tagName) ||
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

    window.StageUpI18n = {
        traducir: function (texto) {
            return traducirValor(texto) || texto;
        },
        traducirClave: function (clave) {
            return Object.prototype.hasOwnProperty.call(porClave, clave) ? porClave[clave] : clave;
        }
    };

    if (!window.__stageUpI18nDialogos) {
        var alertaOriginal = window.alert;
        var confirmacionOriginal = window.confirm;

        window.alert = function (mensaje) {
            return alertaOriginal.call(window, traducirValor(mensaje) || mensaje);
        };

        window.confirm = function (mensaje) {
            return confirmacionOriginal.call(window, traducirValor(mensaje) || mensaje);
        };

        window.__stageUpI18nDialogos = true;
    }

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
