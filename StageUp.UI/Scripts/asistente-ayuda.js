// CU-003-001 A4 a A7: asistente guiado del centro de ayuda (sin IA).
// Funciona como un árbol de decisión: muestra un mensaje y opciones
// numeradas; cada opción lleva a otro paso hasta llegar a una respuesta con
// acciones (ir a una pantalla, ver otra respuesta o registrar una solicitud
// de soporte). También se puede escribir el número de la opción o una
// consulta. Pide los pasos a Ayuda/AsistenteAyuda.ashx y muestra todo con
// textContent (nunca HTML recibido).
(function () {
    "use strict";

    const modal = document.querySelector("[data-assistant-modal]");
    if (!modal) {
        return;
    }

    const chat = modal.querySelector("[data-asistente-chat]");
    const entrada = modal.querySelector("[data-asistente-texto]");
    const enviar = modal.querySelector("[data-asistente-enviar]");
    const urlAsistente = modal.getAttribute("data-asistente-url");
    const urlSoporte = modal.getAttribute("data-soporte-url");
    let iniciado = false;
    let disponible = true;
    let opcionesActuales = [];
    let recorridoActual = "";

    function bajar() {
        chat.scrollTop = chat.scrollHeight;
    }

    function burbuja(texto, deUsuario, titulo) {
        const div = document.createElement("div");
        div.className = "asistente-burbuja " + (deUsuario ? "asistente-burbuja-usuario" : "asistente-burbuja-bot");
        if (titulo) {
            const fuerte = document.createElement("strong");
            fuerte.textContent = titulo;
            div.appendChild(fuerte);
        }

        const parrafo = document.createElement("span");
        parrafo.textContent = texto;
        div.appendChild(parrafo);
        chat.appendChild(div);
        bajar();
        return div;
    }

    // CU-003-002 A1: el formulario de soporte se abre con el recorrido o la
    // consulta como motivo (solo con sesión iniciada; si no, va al login).
    function enlaceSoporte(motivo) {
        if (!motivo || urlSoporte.indexOf("Soporte.aspx") < 0) {
            return urlSoporte;
        }

        return urlSoporte + (urlSoporte.indexOf("?") < 0 ? "?" : "&") + "consulta=" + encodeURIComponent(motivo.slice(0, 150));
    }

    function desactivarOpcionesAnteriores() {
        chat.querySelectorAll(".asistente-opciones button").forEach(function (boton) {
            boton.disabled = true;
        });
        opcionesActuales = [];
    }

    // Lista numerada de opciones. Cada opción: { texto, accion } o { texto, url }.
    function opciones(lista, numeradas) {
        const contenedor = document.createElement("div");
        contenedor.className = "asistente-opciones" + (numeradas ? " asistente-opciones-numeradas" : "");
        opcionesActuales = [];

        lista.forEach(function (opcion, indice) {
            let elemento;
            if (opcion.url) {
                elemento = document.createElement("a");
                elemento.href = opcion.url;
            } else {
                elemento = document.createElement("button");
                elemento.type = "button";
                elemento.addEventListener("click", function () {
                    elegir(opcion);
                });
            }

            if (opcion.clase) {
                elemento.className = opcion.clase;
            }

            elemento.textContent = (numeradas ? (indice + 1) + ". " : "") + opcion.texto;
            contenedor.appendChild(elemento);
            opcionesActuales.push({ opcion: opcion, elemento: elemento });
        });

        chat.appendChild(contenedor);
        bajar();
    }

    function elegir(opcion) {
        if (opcion.url) {
            window.location.href = opcion.url;
            return;
        }

        desactivarOpcionesAnteriores();
        burbuja(opcion.texto, true);
        opcion.accion();
    }

    function pedir(parametros, metodo) {
        const cuerpo = new URLSearchParams(parametros);
        const opcionesFetch = metodo === "POST"
            ? { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: cuerpo.toString(), credentials: "same-origin" }
            : { method: "GET", credentials: "same-origin" };
        const url = metodo === "POST" ? urlAsistente : urlAsistente + "?" + cuerpo.toString();
        return fetch(url, opcionesFetch).then(function (respuesta) {
            return respuesta.json();
        });
    }

    // A7: se avisa y se ofrece soporte; las preguntas frecuentes siguen visibles.
    function noDisponible(mensaje) {
        disponible = false;
        entrada.disabled = true;
        enviar.disabled = true;
        burbuja(mensaje || "El asistente no está disponible en este momento.", false);
        opciones([{ texto: "Registrar solicitud de soporte", url: enlaceSoporte(""), clase: "asistente-soporte" }], false);
    }

    function irANodo(codigo) {
        pedir({ accion: "nodo", codigo: codigo || "" }, "GET").then(function (datos) {
            if (!datos.ok) {
                if (datos.codigo === "A7") {
                    noDisponible(datos.mensaje);
                } else {
                    burbuja(datos.mensaje, false);
                    opciones([{ texto: "Volver al inicio", accion: function () { irANodo(""); } }], false);
                }
                return;
            }

            mostrarNodo(datos.nodo, []);
        }).catch(function () {
            noDisponible();
        });
    }

    function mostrarNodo(nodo, sugerencias) {
        recorridoActual = nodo.recorrido || "";
        burbuja(nodo.mensaje, false);

        const lista = [];
        if (!nodo.esRespuesta) {
            // Paso con pregunta: las opciones llevan al siguiente nivel.
            nodo.opciones.forEach(function (opcion) {
                lista.push({ texto: opcion.texto, accion: function () { irANodo(opcion.codigo); } });
            });
        } else {
            // Respuesta: acciones definidas para este paso.
            let tieneSoporte = false;
            nodo.acciones.forEach(function (accion) {
                if (accion.tipo === "Enlace") {
                    lista.push({ texto: accion.texto, url: accion.url });
                } else if (accion.tipo === "Nodo") {
                    lista.push({ texto: accion.texto, accion: function () { irANodo(accion.codigo); } });
                } else if (accion.tipo === "Soporte") {
                    tieneSoporte = true;
                    lista.push({ texto: accion.texto, url: enlaceSoporte(recorridoActual), clase: "asistente-soporte" });
                }
            });

            // A6: si la respuesta no alcanza.
            if (!tieneSoporte) {
                lista.push({ texto: "Necesito más ayuda", accion: necesitoMasAyuda });
            }
        }

        (sugerencias || []).forEach(function (sugerencia) {
            lista.push({ texto: sugerencia.texto, accion: function () { irANodo(sugerencia.codigo); } });
        });

        if (!nodo.esInicio) {
            lista.push({ texto: "Volver al inicio", accion: function () { irANodo(""); } });
        }

        opciones(lista, true);
    }

    function necesitoMasAyuda() {
        burbuja("Podés registrar una solicitud de soporte y el equipo de StageUp la va a revisar. Te respondemos desde \"Mis solicitudes de soporte\".", false);
        opciones([
            { texto: "Registrar solicitud de soporte", url: enlaceSoporte(recorridoActual), clase: "asistente-soporte" },
            { texto: "Volver al inicio", accion: function () { irANodo(""); } }
        ], true);
    }

    // El campo está dentro del formulario de la página: Enter no tiene que
    // enviarlo, sino mandar la consulta al asistente.
    entrada.addEventListener("keydown", function (evento) {
        if (evento.key === "Enter") {
            evento.preventDefault();
            consultar();
        }
    });
    enviar.addEventListener("click", consultar);

    function consultar() {
        const texto = entrada.value.trim();
        if (!disponible || texto.length === 0) {
            return;
        }

        entrada.value = "";

        // Un número elige la opción correspondiente de la última lista.
        if (/^\d{1,2}$/.test(texto)) {
            const elegida = opcionesActuales[parseInt(texto, 10) - 1];
            if (elegida) {
                elegir(elegida.opcion);
            } else {
                burbuja(texto, true);
                burbuja("Elegí uno de los números de las opciones, o escribí tu consulta.", false);
            }
            return;
        }

        desactivarOpcionesAnteriores();
        burbuja(texto, true);
        pedir({ accion: "consultar", texto: texto }, "POST").then(function (datos) {
            if (!datos.ok) {
                if (datos.codigo === "A7") {
                    noDisponible(datos.mensaje);
                } else {
                    burbuja(datos.mensaje, false);
                }
                return;
            }

            if (datos.reconocida && datos.nodo) {
                mostrarNodo(datos.nodo, datos.sugerencias);
                return;
            }

            if (datos.reconocida && datos.preguntaFrecuente) {
                recorridoActual = texto;
                burbuja(datos.preguntaFrecuente.respuesta, false, datos.preguntaFrecuente.pregunta);
                opciones([
                    { texto: "Necesito más ayuda", accion: necesitoMasAyuda },
                    { texto: "Volver al inicio", accion: function () { irANodo(""); } }
                ], true);
                return;
            }

            // A5: consulta no reconocida.
            burbuja("No pude encontrar una respuesta para tu consulta. Podés registrar una solicitud de soporte" +
                (datos.sugerencias.length > 0 ? " o revisar estas opciones:" : "."), false);
            const lista = [{ texto: "Registrar solicitud de soporte", url: enlaceSoporte(texto), clase: "asistente-soporte" }];
            datos.sugerencias.forEach(function (sugerencia) {
                lista.push({ texto: sugerencia.texto, accion: function () { irANodo(sugerencia.codigo); } });
            });
            lista.push({ texto: "Volver al inicio", accion: function () { irANodo(""); } });
            opciones(lista, true);
        }).catch(function () {
            noDisponible();
        });
    }

    document.querySelectorAll("[data-assistant-open]").forEach(function (boton) {
        boton.addEventListener("click", function () {
            if (iniciado) {
                return;
            }

            iniciado = true;
            irANodo("");
        });
    });
}());
