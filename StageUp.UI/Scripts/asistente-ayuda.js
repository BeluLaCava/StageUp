// CU-003-001 A4 a A7: asistente guiado del centro de ayuda (sin IA), con
// formato de chat (como los asistentes de WhatsApp que no usan IA).
//   - Saluda por el nombre y pregunta qué querés hacer.
//   - Cada paso con muchas opciones muestra un botón ("Menú principal",
//     "Opciones de reservas"...) que abre un menú con título, descripción e
//     ícono de cada opción; con pocas opciones se muestran botones de
//     respuesta rápida dentro del mensaje.
//   - Lo que elegís aparece como tu mensaje, citando la pregunta.
//   - Cada respuesta ofrece acciones (ir a una pantalla, ver otra respuesta
//     o registrar una solicitud de soporte, con un resumen para confirmar).
//   - Se puede escribir el número de la opción, una consulta, "menú" para
//     volver al inicio o "salir" para terminar.
// Pide los pasos a Ayuda/AsistenteAyuda.ashx y muestra todo con textContent
// (nunca HTML recibido).
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
    const nombreUsuario = (modal.getAttribute("data-usuario-nombre") || "").trim();
    const conSesion = urlSoporte.indexOf("Soporte.aspx") >= 0;
    const demoraEscritura = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 120 : 450;

    // Íconos de las opciones (clave guardada en NodoAsistente.icono).
    const ICONOS = {
        reservas: "\u{1F4C5}", reservar: "\u{1F4DD}", cancelar: "❌", estado: "\u{1F50E}",
        espera: "⏳", politica: "\u{1F4C4}", pagos: "\u{1F4B3}", pagar: "\u{1F4B5}",
        saldo: "\u{1F4B0}", movimiento: "\u{1F4CA}", tarjeta: "⚠️", espacios: "\u{1F3AD}",
        ofrecer: "\u{1F3E0}", editar: "✏️", calendario: "\u{1F5D3}️", solicitudes: "\u{1F4E5}",
        tecnico: "\u{1F6E0}️", llave: "\u{1F511}", cuenta: "\u{1F464}", correo: "✉️",
        error: "\u{1F5A5}️", soporte: "\u{1F4AC}", menu: "↩️", salir: "\u{1F44B}"
    };
    const MANO_ABAJO = "\u{1F447}";
    const SALUDO = "\u{1F44B}";

    let iniciado = false;
    let disponible = true;
    let opcionesActuales = [];
    let recorridoActual = "";
    let ultimaPregunta = "";
    let cola = Promise.resolve();

    // Cada acción del usuario (escribir, elegir una opción, "menú") abre un
    // turno nuevo. Las respuestas y los mensajes en cola de un turno
    // anterior se descartan, y el pedido anterior se cancela: así una
    // consulta lenta no puede reemplazar las opciones del recorrido actual.
    let turno = 0;
    let pedidoEnCurso = null;
    let esperandoConsulta = null;

    function nuevoTurno() {
        turno++;
        if (pedidoEnCurso) {
            pedidoEnCurso.abort();
            pedidoEnCurso = null;
        }
        if (esperandoConsulta) {
            esperandoConsulta.remove();
            esperandoConsulta = null;
        }
        return turno;
    }

    function vigente(numeroTurno) {
        return numeroTurno === turno;
    }

    // ---------------------------------------------------------------- util

    function hora() {
        const ahora = new Date();
        return String(ahora.getHours()).padStart(2, "0") + ":" + String(ahora.getMinutes()).padStart(2, "0");
    }

    function bajar() {
        chat.scrollTop = chat.scrollHeight;
    }

    function elemento(etiqueta, clase, texto) {
        const el = document.createElement(etiqueta);
        if (clase) {
            el.className = clase;
        }
        if (texto !== undefined && texto !== null) {
            el.textContent = texto;
        }
        return el;
    }

    function icono(clave) {
        return clave && ICONOS[clave] ? ICONOS[clave] : "";
    }

    // Los mensajes del asistente se muestran en orden, con un "escribiendo…"
    // breve antes de cada uno.
    function enCola(tarea) {
        const turnoDelMensaje = turno;
        cola = cola.then(function () {
            if (!vigente(turnoDelMensaje)) {
                return;
            }
            const escribiendo = elemento("div", "asistente-escribiendo");
            escribiendo.setAttribute("aria-hidden", "true");
            escribiendo.appendChild(elemento("span"));
            escribiendo.appendChild(elemento("span"));
            escribiendo.appendChild(elemento("span"));
            chat.appendChild(escribiendo);
            bajar();
            return new Promise(function (resolver) {
                setTimeout(function () {
                    escribiendo.remove();
                    if (vigente(turnoDelMensaje)) {
                        tarea();
                    }
                    bajar();
                    resolver();
                }, demoraEscritura);
            });
        });
        return cola;
    }

    // ------------------------------------------------------------ mensajes

    function pie(burbuja, deUsuario) {
        const meta = elemento("span", "asistente-hora", hora());
        if (deUsuario) {
            const leido = elemento("span", "asistente-leido", "✓✓");
            leido.setAttribute("aria-label", "enviado");
            meta.appendChild(leido);
        }
        burbuja.appendChild(meta);
    }

    // Mensaje del asistente: { titulo, texto, nota }.
    function crearMensajeBot(partes) {
        const burbuja = elemento("div", "asistente-burbuja asistente-burbuja-bot");
        if (partes.titulo) {
            burbuja.appendChild(elemento("strong", "asistente-titulo", partes.titulo));
        }
        if (partes.texto) {
            burbuja.appendChild(elemento("span", "asistente-texto", partes.texto));
        }
        if (partes.nota) {
            burbuja.appendChild(elemento("em", "asistente-nota", partes.nota));
        }
        pie(burbuja, false);
        chat.appendChild(burbuja);
        return burbuja;
    }

    function mensajeBot(partes, opciones, modo, textoBoton) {
        return enCola(function () {
            const burbuja = crearMensajeBot(partes);
            if (opciones && opciones.length > 0) {
                if (modo === "lista") {
                    agregarBotonLista(burbuja, textoBoton || "Ver opciones", opciones);
                } else {
                    agregarRespuestasRapidas(burbuja, opciones);
                }
            }
            if (partes.texto && opciones && opciones.length > 0) {
                ultimaPregunta = partes.texto;
            }
        });
    }

    // Mensaje del usuario; si eligió una opción, cita la pregunta del asistente.
    function mensajeUsuario(texto, descripcion, cita) {
        const burbuja = elemento("div", "asistente-burbuja asistente-burbuja-usuario");
        if (cita) {
            const bloque = elemento("div", "asistente-cita");
            bloque.appendChild(elemento("strong", null, "Asistente StageUp"));
            bloque.appendChild(elemento("span", null, cita));
            burbuja.appendChild(bloque);
        }
        burbuja.appendChild(elemento("span", "asistente-texto", texto));
        if (descripcion) {
            burbuja.appendChild(elemento("span", "asistente-descripcion", descripcion));
        }
        pie(burbuja, true);
        chat.appendChild(burbuja);
        bajar();
    }

    // ------------------------------------------------------------ opciones

    function desactivarOpcionesAnteriores() {
        chat.querySelectorAll(".asistente-respuestas button, .asistente-boton-lista").forEach(function (boton) {
            boton.disabled = true;
        });
        chat.querySelectorAll(".asistente-respuestas a").forEach(function (enlace) {
            enlace.classList.add("is-disabled");
            enlace.setAttribute("tabindex", "-1");
            enlace.setAttribute("aria-disabled", "true");
        });
        opcionesActuales = [];
    }

    function registrarOpciones(lista) {
        opcionesActuales = lista.slice();
    }

    function elegir(opcion) {
        if (opcion.url) {
            window.location.href = opcion.url;
            return;
        }

        cerrarHoja();
        nuevoTurno();
        desactivarOpcionesAnteriores();
        mensajeUsuario(opcion.texto, opcion.descripcion, ultimaPregunta);
        opcion.accion();
    }

    // Botones de respuesta rápida dentro del mensaje (pocas opciones).
    function agregarRespuestasRapidas(burbuja, lista) {
        const contenedor = elemento("div", "asistente-respuestas");
        lista.forEach(function (opcion, indice) {
            let control;
            if (opcion.url) {
                control = elemento("a", "asistente-respuesta asistente-respuesta-enlace");
                control.href = opcion.url;
            } else {
                control = elemento("button", "asistente-respuesta");
                control.type = "button";
                control.addEventListener("click", function () {
                    elegir(opcion);
                });
            }
            if (opcion.destacada) {
                control.classList.add("asistente-respuesta-destacada");
            }
            control.appendChild(elemento("span", "asistente-numero", String(indice + 1)));
            control.appendChild(elemento("span", null, opcion.texto));
            contenedor.appendChild(control);
        });
        burbuja.appendChild(contenedor);
        burbuja.classList.add("asistente-burbuja-con-botones");
        registrarOpciones(lista);
    }

    // Botón que abre el menú de opciones (muchas opciones).
    function agregarBotonLista(burbuja, texto, lista) {
        const boton = elemento("button", "asistente-boton-lista");
        boton.type = "button";
        boton.appendChild(elemento("span", "asistente-icono-lista"));
        boton.appendChild(elemento("span", null, texto));
        boton.addEventListener("click", function () {
            abrirHoja(texto, lista, boton);
        });
        burbuja.appendChild(boton);
        burbuja.classList.add("asistente-burbuja-con-botones");
        registrarOpciones(lista);
    }

    // --------------------------------------------------- menú de opciones

    const hoja = elemento("div", "asistente-hoja");
    hoja.hidden = true;
    hoja.setAttribute("role", "dialog");
    hoja.setAttribute("aria-modal", "true");
    hoja.setAttribute("aria-labelledby", "asistente-hoja-titulo");
    const fondoHoja = elemento("div", "asistente-hoja-fondo");
    fondoHoja.hidden = true;
    const cabeceraHoja = elemento("div", "asistente-hoja-cabecera");
    const tituloHoja = elemento("strong", null, "");
    tituloHoja.id = "asistente-hoja-titulo";
    const cerrarHojaBoton = elemento("button", "asistente-hoja-cerrar", "×");
    cerrarHojaBoton.type = "button";
    cerrarHojaBoton.setAttribute("aria-label", "Cerrar menú");
    cabeceraHoja.appendChild(tituloHoja);
    cabeceraHoja.appendChild(cerrarHojaBoton);
    const listaHoja = elemento("div", "asistente-hoja-lista");
    listaHoja.setAttribute("role", "list");
    hoja.appendChild(cabeceraHoja);
    hoja.appendChild(listaHoja);
    hoja.appendChild(elemento("p", "asistente-hoja-pie", "Tocá una opción para elegirla o escribí su número."));
    modal.appendChild(fondoHoja);
    modal.appendChild(hoja);
    let botonQueAbrio = null;

    function abrirHoja(titulo, lista, origen) {
        botonQueAbrio = origen || null;
        tituloHoja.textContent = titulo;
        listaHoja.textContent = "";
        lista.forEach(function (opcion, indice) {
            const fila = elemento(opcion.url ? "a" : "button", "asistente-hoja-opcion");
            if (opcion.url) {
                fila.href = opcion.url;
            } else {
                fila.type = "button";
                fila.addEventListener("click", function () {
                    elegir(opcion);
                });
            }
            fila.setAttribute("role", "listitem");
            fila.appendChild(elemento("span", "asistente-numero", String(indice + 1)));
            const cuerpo = elemento("span", "asistente-hoja-texto");
            const tituloOpcion = elemento("strong", null, opcion.texto);
            const emoji = icono(opcion.icono);
            if (emoji) {
                tituloOpcion.appendChild(elemento("span", "asistente-emoji", " " + emoji));
            }
            cuerpo.appendChild(tituloOpcion);
            if (opcion.descripcion) {
                cuerpo.appendChild(elemento("span", null, opcion.descripcion));
            }
            fila.appendChild(cuerpo);
            listaHoja.appendChild(fila);
        });
        fondoHoja.hidden = false;
        hoja.hidden = false;
        requestAnimationFrame(function () {
            hoja.classList.add("is-open");
            fondoHoja.classList.add("is-open");
            const primera = listaHoja.querySelector(".asistente-hoja-opcion");
            if (primera) {
                primera.focus();
            }
        });
    }

    function cerrarHoja() {
        if (hoja.hidden) {
            return;
        }
        hoja.classList.remove("is-open");
        fondoHoja.classList.remove("is-open");
        hoja.hidden = true;
        fondoHoja.hidden = true;
        if (botonQueAbrio && !botonQueAbrio.disabled) {
            botonQueAbrio.focus();
        } else {
            entrada.focus();
        }
        botonQueAbrio = null;
    }

    cerrarHojaBoton.addEventListener("click", cerrarHoja);
    fondoHoja.addEventListener("click", cerrarHoja);
    hoja.addEventListener("keydown", function (evento) {
        if (evento.key === "Escape") {
            evento.preventDefault();
            evento.stopPropagation();
            cerrarHoja();
        }
    });

    // ------------------------------------------------------------- pasos

    // Devuelve una promesa que solo se cumple si el pedido sigue siendo del
    // turno actual; si el usuario hizo otra cosa mientras tanto, la
    // respuesta se descarta (la promesa queda pendiente y no muestra nada).
    function pedir(parametros, metodo) {
        const numeroTurno = turno;
        const controlador = typeof AbortController === "function" ? new AbortController() : null;
        pedidoEnCurso = controlador;
        const cuerpo = new URLSearchParams(parametros);
        const opcionesFetch = metodo === "POST"
            ? { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: cuerpo.toString(), credentials: "same-origin" }
            : { method: "GET", credentials: "same-origin" };
        if (controlador) {
            opcionesFetch.signal = controlador.signal;
        }
        const url = metodo === "POST" ? urlAsistente : urlAsistente + "?" + cuerpo.toString();
        return new Promise(function (resolver, rechazar) {
            fetch(url, opcionesFetch).then(function (respuesta) {
                return respuesta.json();
            }).then(function (datos) {
                if (vigente(numeroTurno)) {
                    if (pedidoEnCurso === controlador) {
                        pedidoEnCurso = null;
                    }
                    resolver(datos);
                }
            }).catch(function (error) {
                if (vigente(numeroTurno) && !(error && error.name === "AbortError")) {
                    rechazar(error);
                }
            });
        });
    }

    // CU-003-002 A1: el formulario de soporte se abre con el recorrido o la
    // consulta como motivo (solo con sesión iniciada; si no, va al login).
    function enlaceSoporte(motivo) {
        if (!motivo || !conSesion) {
            return urlSoporte;
        }

        return urlSoporte + (urlSoporte.indexOf("?") < 0 ? "?" : "&") + "consulta=" + encodeURIComponent(motivo.slice(0, 150));
    }

    const opcionMenuPrincipal = { texto: "Menú principal", descripcion: "Volver al inicio", icono: "menu", accion: function () { irANodo(""); } };
    const opcionSalir = { texto: "Salir", descripcion: "Finalizar esta conversación", icono: "salir", accion: salir };

    // A7: se avisa y se ofrece soporte; las preguntas frecuentes siguen visibles.
    function noDisponible(mensaje) {
        disponible = false;
        entrada.disabled = true;
        enviar.disabled = true;
        mensajeBot({ texto: mensaje || "El asistente no está disponible en este momento. Podés revisar las preguntas frecuentes o registrar una solicitud de soporte." },
            [{ texto: "Registrar solicitud de soporte", url: enlaceSoporte(""), destacada: true }], "botones");
    }

    function irANodo(codigo) {
        pedir({ accion: "nodo", codigo: codigo || "" }, "GET").then(function (datos) {
            if (!datos.ok) {
                if (datos.codigo === "A7") {
                    noDisponible(datos.mensaje);
                } else {
                    mensajeBot({ texto: datos.mensaje }, [opcionMenuPrincipal], "botones");
                }
                return;
            }

            mostrarNodo(datos.nodo, []);
        }).catch(function () {
            noDisponible();
        });
    }

    function convertirSugerencias(sugerencias) {
        return (sugerencias || []).map(function (sugerencia) {
            return { texto: sugerencia.texto, accion: function () { irANodo(sugerencia.codigo); } };
        });
    }

    function mostrarNodo(nodo, sugerencias) {
        recorridoActual = nodo.recorrido || "";

        if (!nodo.esRespuesta) {
            // Paso con pregunta: las opciones llevan al siguiente nivel.
            const lista = nodo.opciones.map(function (opcion) {
                return {
                    texto: opcion.texto,
                    descripcion: opcion.descripcion,
                    icono: opcion.icono,
                    accion: function () { irANodo(opcion.codigo); }
                };
            }).concat(convertirSugerencias(sugerencias));
            lista.push(nodo.esInicio ? opcionSalir : opcionMenuPrincipal);

            if (nodo.opciones.length <= 3 && !nodo.esInicio) {
                mensajeBot({ texto: nodo.mensaje }, lista, "botones");
            } else {
                mensajeBot({ texto: nodo.mensaje, nota: "Elegí una opción desde el menú " + MANO_ABAJO }, lista, "lista", nodo.textoBoton);
            }
            return;
        }

        // Respuesta: acciones definidas para este paso.
        const acciones = [];
        let tieneSoporte = false;
        nodo.acciones.forEach(function (accion) {
            if (accion.tipo === "Enlace") {
                acciones.push({ texto: accion.texto, url: accion.url });
            } else if (accion.tipo === "Nodo") {
                acciones.push({ texto: accion.texto, accion: function () { irANodo(accion.codigo); } });
            } else if (accion.tipo === "Soporte") {
                tieneSoporte = true;
                acciones.push({ texto: accion.texto, destacada: true, accion: function () { confirmarSoporte(recorridoActual); } });
            }
        });

        // A6: si la respuesta no alcanza.
        if (!tieneSoporte) {
            acciones.push({ texto: "Necesito más ayuda", accion: necesitoMasAyuda });
        }

        const final = acciones.concat(convertirSugerencias(sugerencias));
        final.push(opcionMenuPrincipal);
        mensajeBot({ texto: nodo.mensaje }, final, "botones");
    }

    function necesitoMasAyuda() {
        mensajeBot({ texto: "Podés registrar una solicitud de soporte y el equipo de StageUp la va a revisar. Te respondemos desde \"Mis solicitudes de soporte\"." },
            [
                { texto: "Registrar solicitud de soporte", destacada: true, accion: function () { confirmarSoporte(recorridoActual); } },
                opcionMenuPrincipal
            ], "botones");
    }

    // Antes de ir al formulario de soporte se muestra un resumen para
    // confirmar (el formulario se abre con ese motivo precargado).
    function confirmarSoporte(motivo) {
        const lineas = ["Tema: " + (motivo || "Consulta general"), "Origen: Asistente de StageUp"];
        if (nombreUsuario) {
            lineas.push("A nombre de: " + nombreUsuario);
        }
        mensajeBot({ titulo: "\u{1F4CB} Resumen de tu consulta", texto: lineas.join("\n") });
        mensajeBot({
            titulo: "\u{1F50D} Revisá los datos antes de confirmar",
            texto: conSesion
                ? "Si es correcto, te llevo al formulario de soporte con este tema cargado. Si querés cambiar algo, volvé al menú principal."
                : "Para registrar la solicitud tenés que iniciar sesión. Si querés cambiar algo, volvé al menú principal."
        }, [
            { texto: conSesion ? "Sí, es correcto" : "Iniciar sesión", url: enlaceSoporte(motivo), destacada: true },
            { texto: "No, volver al menú principal", accion: function () { irANodo(""); } }
        ], "botones");
    }

    function saludar() {
        const saludo = nombreUsuario
            ? "¡Hola, " + nombreUsuario + "! " + SALUDO + " Soy el asistente virtual de StageUp."
            : "¡Hola! " + SALUDO + " Soy el asistente virtual de StageUp.";
        mensajeBot({ texto: saludo });
        irANodo("");
    }

    function salir() {
        mensajeBot({ texto: "¡Gracias por usar el asistente de StageUp! " + SALUDO + " Si necesitás algo más, tocá \"Empezar de nuevo\" o escribí \"menú\"." },
            [{ texto: "Empezar de nuevo", accion: function () { irANodo(""); } }], "botones");
    }

    // ------------------------------------------------- consulta escrita

    // El campo está dentro del formulario de la página: Enter no tiene que
    // enviarlo, sino mandar la consulta al asistente.
    entrada.addEventListener("keydown", function (evento) {
        if (evento.key === "Enter") {
            evento.preventDefault();
            consultar();
        }
    });
    enviar.addEventListener("click", consultar);

    function normalizar(texto) {
        return texto.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[^a-z0-9 ]/g, "").trim();
    }

    function consultar() {
        const texto = entrada.value.trim();
        if (!disponible || texto.length === 0) {
            return;
        }

        entrada.value = "";
        cerrarHoja();

        // Un número elige la opción correspondiente de la última lista
        // (antes de abrir el turno nuevo, que la vacía).
        const opcionesDelTurno = opcionesActuales.slice();
        nuevoTurno();
        if (/^\d{1,2}$/.test(texto)) {
            const elegida = opcionesDelTurno[parseInt(texto, 10) - 1];
            if (elegida) {
                elegir(elegida);
            } else {
                mensajeUsuario(texto);
                mensajeBot({ texto: "Elegí uno de los números de las opciones, o escribí tu consulta." });
            }
            return;
        }

        const palabra = normalizar(texto);
        if (palabra === "menu" || palabra === "inicio" || palabra === "hola" || palabra === "volver") {
            desactivarOpcionesAnteriores();
            mensajeUsuario(texto);
            irANodo("");
            return;
        }
        if (palabra === "salir" || palabra === "chau" || palabra === "gracias") {
            desactivarOpcionesAnteriores();
            mensajeUsuario(texto);
            salir();
            return;
        }

        desactivarOpcionesAnteriores();
        mensajeUsuario(texto);

        // Mientras se busca la respuesta (puede consultar al asistente
        // automático por API) se muestra "escribiendo…".
        const esperando = elemento("div", "asistente-escribiendo");
        esperando.setAttribute("aria-hidden", "true");
        esperando.appendChild(elemento("span"));
        esperando.appendChild(elemento("span"));
        esperando.appendChild(elemento("span"));
        chat.appendChild(esperando);
        esperandoConsulta = esperando;
        bajar();

        pedir({ accion: "consultar", texto: texto }, "POST").then(function (datos) {
            esperando.remove();
            esperandoConsulta = null;
            if (!datos.ok) {
                if (datos.codigo === "A7") {
                    noDisponible(datos.mensaje);
                } else {
                    mensajeBot({ texto: datos.mensaje }, [opcionMenuPrincipal], "botones");
                }
                return;
            }

            // La consulta traía datos privados: no se envió al asistente
            // automático ni se guardó su texto. Se avisa y, si no hubo
            // respuesta propia, se ofrece soporte sin copiar la consulta.
            if (datos.conDatosPrivados) {
                mensajeBot({
                    titulo: "\u{1F512} Cuidá tus datos",
                    texto: "Tu consulta parece incluir datos privados (una contraseña, un código de seguridad, un número de tarjeta o una clave). No la enviamos al asistente automático ni guardamos su texto. Nunca compartas esos datos: StageUp no te los va a pedir."
                });
                if (!(datos.reconocida && datos.nodo)) {
                    recorridoActual = "";
                    mensajeBot({ texto: "Si necesitás ayuda, registrá una solicitud de soporte describiendo el problema sin incluir esos datos." },
                        [{ texto: "Registrar solicitud de soporte", destacada: true, accion: function () { confirmarSoporte(""); } }, opcionMenuPrincipal], "botones");
                    return;
                }
            }

            if (datos.reconocida && datos.nodo) {
                mostrarNodo(datos.nodo, datos.sugerencias);
                return;
            }

            // Respuesta del asistente automático por API (sin contenido propio).
            if (datos.respuestaAutomatica) {
                recorridoActual = texto;
                mensajeBot({
                    titulo: "Respuesta automática",
                    texto: datos.respuestaAutomatica,
                    nota: "La generó un asistente automático y puede no ser exacta. Si no te sirve, registrá una solicitud de soporte."
                }, [{ texto: "Necesito más ayuda", accion: necesitoMasAyuda }]
                    .concat(convertirSugerencias(datos.sugerencias))
                    .concat([opcionMenuPrincipal]), "botones");
                return;
            }

            // A5: consulta no reconocida (o el asistente automático no
            // respondió a tiempo): se deriva a soporte.
            recorridoActual = texto;
            const lista = [{ texto: "Registrar solicitud de soporte", destacada: true, accion: function () { confirmarSoporte(texto); } }]
                .concat(convertirSugerencias(datos.sugerencias));
            lista.push(opcionMenuPrincipal);
            mensajeBot({
                texto: (datos.asistenteAutomaticoNoDisponible
                    ? "El asistente automático no está respondiendo en este momento. Podés registrar una solicitud de soporte y el equipo de StageUp la va a revisar"
                    : "No pude encontrar una respuesta para tu consulta. Podés registrar una solicitud de soporte") +
                    (datos.sugerencias.length > 0 ? " o revisar estas opciones:" : " o volver al menú principal.")
            }, lista, "botones");
        }).catch(function () {
            esperando.remove();
            esperandoConsulta = null;
            noDisponible();
        });
    }

    // ------------------------------------------------------------ inicio

    document.querySelectorAll("[data-assistant-open]").forEach(function (boton) {
        boton.addEventListener("click", function () {
            if (iniciado) {
                return;
            }

            iniciado = true;
            nuevoTurno();
            chat.appendChild(elemento("div", "asistente-dia", "Hoy"));
            saludar();
        });
    });
}());
