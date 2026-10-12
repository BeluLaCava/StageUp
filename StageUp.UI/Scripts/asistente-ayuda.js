// CU-003-001 A4 a A7: asistente guiado del centro de ayuda. Pide temas,
// preguntas y respuestas a Ayuda/AsistenteAyuda.ashx y muestra todo con
// textContent (nunca HTML recibido). Si no puede resolver la consulta,
// ofrece registrar una solicitud de soporte.
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
    let ultimaConsulta = "";

    // CU-003-002 A1: el formulario de soporte se abre con la última consulta
    // como motivo (solo si el usuario tiene sesión; si no, va al login).
    function enlaceSoporte() {
        if (!ultimaConsulta || urlSoporte.indexOf("Soporte.aspx") < 0) {
            return urlSoporte;
        }

        return urlSoporte + (urlSoporte.indexOf("?") < 0 ? "?" : "&") + "consulta=" + encodeURIComponent(ultimaConsulta.slice(0, 150));
    }

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

    function opciones(lista) {
        const contenedor = document.createElement("div");
        contenedor.className = "asistente-opciones";
        lista.forEach(function (opcion) {
            let elemento;
            if (opcion.url) {
                elemento = document.createElement("a");
                elemento.href = opcion.url;
            } else {
                elemento = document.createElement("button");
                elemento.type = "button";
                elemento.addEventListener("click", function () {
                    contenedor.querySelectorAll("button").forEach(function (b) { b.disabled = true; });
                    opcion.accion();
                });
            }

            if (opcion.clase) {
                elemento.className = opcion.clase;
            }

            elemento.textContent = opcion.texto;
            contenedor.appendChild(elemento);
        });
        chat.appendChild(contenedor);
        bajar();
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

    // A7 (y errores): se avisa y se ofrece soporte; las FAQ siguen visibles.
    function noDisponible(mensaje) {
        disponible = false;
        entrada.disabled = true;
        enviar.disabled = true;
        burbuja(mensaje || "El asistente no está disponible en este momento.", false);
        ofrecerSoporte();
    }

    function ofrecerSoporte() {
        opciones([
            { texto: "Registrar solicitud de soporte", url: enlaceSoporte(), clase: "asistente-soporte" },
            { texto: "Ver los temas", accion: mostrarTemas }
        ]);
    }

    function mostrarTemas() {
        pedir({ accion: "temas" }, "GET").then(function (datos) {
            if (!datos.ok) {
                noDisponible(datos.mensaje);
                return;
            }

            burbuja("Elegí un tema o escribí tu consulta abajo.", false);
            opciones(datos.temas.map(function (tema) {
                return { texto: tema.nombre, accion: function () { elegirTema(tema); } };
            }));
        }).catch(function () {
            noDisponible();
        });
    }

    function elegirTema(tema) {
        burbuja(tema.nombre, true);
        pedir({ accion: "preguntas", tema: tema.id }, "GET").then(function (datos) {
            if (!datos.ok) {
                noDisponible(datos.mensaje);
                return;
            }

            if (datos.preguntas.length === 0) {
                burbuja("No hay consultas cargadas para ese tema todavía.", false);
                ofrecerSoporte();
                return;
            }

            burbuja("Estas son las consultas más comunes de " + tema.nombre + ":", false);
            const lista = datos.preguntas.map(function (pregunta) {
                return { texto: pregunta.pregunta, accion: function () { elegirPregunta(pregunta); } };
            });
            lista.push({ texto: "Volver a los temas", accion: mostrarTemas });
            opciones(lista);
        }).catch(function () {
            noDisponible();
        });
    }

    function elegirPregunta(pregunta) {
        ultimaConsulta = pregunta.pregunta;
        burbuja(pregunta.pregunta, true);
        pedir({ accion: "respuesta", id: pregunta.id }, "GET").then(function (datos) {
            if (!datos.ok) {
                burbuja(datos.mensaje, false);
                ofrecerSoporte();
                return;
            }

            mostrarRespuesta(datos.respuesta, []);
        }).catch(function () {
            noDisponible();
        });
    }

    // A4 paso 8 y A6.
    function mostrarRespuesta(respuesta, sugerencias) {
        burbuja(respuesta.respuesta, false, respuesta.pregunta);
        const lista = [
            { texto: "Me sirvió, gracias", accion: function () { burbuja("¡Genial! Si necesitás algo más, escribime.", false); } },
            { texto: "Necesito más ayuda", accion: necesitoMasAyuda }
        ];
        sugerencias.forEach(function (sugerencia) {
            lista.push({ texto: sugerencia.pregunta, accion: function () { elegirPregunta(sugerencia); } });
        });
        lista.push({ texto: "Ver los temas", accion: mostrarTemas });
        opciones(lista);
    }

    function necesitoMasAyuda() {
        burbuja("Necesito más ayuda", true);
        burbuja("Podés registrar una solicitud de soporte y el equipo de StageUp la va a revisar.", false);
        ofrecerSoporte();
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
        ultimaConsulta = texto;
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

            if (datos.reconocida) {
                mostrarRespuesta(datos.respuesta, datos.sugerencias);
                return;
            }

            // A5: consulta no reconocida.
            burbuja("No encontré una respuesta para tu consulta. Podés registrar una solicitud de soporte" +
                (datos.sugerencias.length > 0 ? " o revisar estas preguntas parecidas:" : "."), false);
            const lista = datos.sugerencias.map(function (sugerencia) {
                return { texto: sugerencia.pregunta, accion: function () { elegirPregunta(sugerencia); } };
            });
            lista.unshift({ texto: "Registrar solicitud de soporte", url: enlaceSoporte(), clase: "asistente-soporte" });
            opciones(lista);
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
            burbuja("Hola, soy el asistente de StageUp. Te respondo con el contenido de ayuda de la plataforma.", false);
            mostrarTemas();
        });
    });
}());
