<%@ Page Title="Detalle de reserva | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetalleReserva.aspx.cs" Inherits="StageUp.UI.DetalleReserva" %>

<asp:Content ID="DetalleReservaContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .dr-encabezado { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; margin-top: 6px; }
        .dr-estado { display: inline-block; padding: 3px 12px; border-radius: 999px; font-size: 0.85em; font-weight: 700; border: 1px solid var(--color-border, #e2e2e2); }
        .dr-estado-pendiente { background: #fff4e5; border-color: #f5c27a; color: #8a4b08; }
        .dr-estado-aceptada { background: #e8f5ec; border-color: #9fd3ae; color: #1e6b37; }
        .dr-estado-finalizada { background: #eef2f7; border-color: #c4cfdd; color: #33475b; }
        .dr-estado-rechazada, .dr-estado-cancelada { background: #fdecea; border-color: #f0b4ae; color: #8c1d18; }
        .dr-grid { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); gap: 16px; align-items: start; }
        .dr-grid > * { min-width: 0; }
        .dr-grid .auth-card { margin: 0 0 16px; }
        .dr-datos { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 20px; margin: 0; }
        .dr-datos div { min-width: 0; }
        .dr-datos dt { color: var(--color-text-muted, #765f55); font-size: 0.82em; }
        .dr-datos dd { margin: 2px 0 0; font-weight: 600; overflow-wrap: anywhere; }
        .dr-persona + .dr-persona { margin-top: 14px; padding-top: 14px; border-top: 1px solid var(--color-border, #e2e2e2); }
        .dr-persona small { display: block; color: var(--color-text-muted, #765f55); font-size: 0.82em; }
        .dr-persona strong { display: block; }
        .dr-persona span { display: block; color: var(--color-text-muted, #765f55); font-size: 0.9em; overflow-wrap: anywhere; }
        .dr-texto { margin: 0 0 10px; }
        .dr-nota { color: var(--color-text-muted, #765f55); font-size: 0.9em; margin: 0; }
        .dr-politica-ahora { margin-top: 10px; padding: 10px 14px; border-radius: 10px; background: var(--color-nude-light, #f6eee8); }
        .dr-acciones { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; }
        .dr-confirmacion { margin-top: 14px; padding: 14px 16px; border-radius: 12px; border: 2px solid var(--color-primary, #6d1021); }
        .dr-confirmacion p { margin: 0 0 12px; }
        .dr-comentario textarea { width: 100%; box-sizing: border-box; }
        .button-danger { background: #b3261e; border-color: #b3261e; color: #fff; }
        .button-danger:hover, .button-danger:focus-visible { background: #8c1d18; border-color: #8c1d18; color: #fff; }
        .dr-mensajes-card { scroll-margin-top: 90px; }
        .dr-chat { display: flex; flex-direction: column; gap: 10px; max-height: 26rem; overflow-y: auto; margin: 10px 0 12px; padding: 12px; border-radius: 12px; background: var(--color-nude-light, #f6eee8); }
        .dr-chat-mensaje { display: flex; flex-direction: column; gap: 3px; max-width: 85%; padding: 8px 12px; border-radius: 12px; background: #fff; box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06); }
        .dr-chat-ajeno { align-self: flex-start; border-top-left-radius: 3px; }
        .dr-chat-propio { align-self: flex-end; border-top-right-radius: 3px; background: #f3dfe3; }
        .dr-chat-nuevo { outline: 2px solid var(--color-primary, #6d1021); }
        .dr-chat-autor { font-size: 0.8em; font-weight: 700; color: var(--color-primary, #6d1021); }
        .dr-chat-texto { margin: 0; white-space: pre-line; overflow-wrap: anywhere; }
        .dr-chat-meta { align-self: flex-end; font-size: 0.75em; color: var(--color-text-muted, #765f55); }
        .dr-chat-vacio { color: var(--color-text-muted, #765f55); text-align: center; padding: 14px 6px; }
        .dr-chat-vacio p { margin: 0; }
        .dr-chat-form textarea { width: 100%; box-sizing: border-box; resize: vertical; }
        .dr-chat-form-pie { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-top: 8px; }
        @media (max-width: 800px) { .dr-grid { grid-template-columns: 1fr; } }
        @media (max-width: 520px) {
            .dr-datos { grid-template-columns: 1fr; }
            .dr-acciones { flex-direction: column; align-items: stretch; }
        }
    </style>

    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Reservas</span>
            <h1>Detalle de reserva</h1>
            <asp:Panel ID="pnlEncabezado" runat="server" CssClass="dr-encabezado">
                <span>Reserva N° <asp:Literal ID="litNumero" runat="server" Mode="Encode" /></span>
                <asp:Label ID="lblEstado" runat="server" CssClass="dr-estado" />
            </asp:Panel>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <asp:Panel ID="pnlNoEncontrada" runat="server" Visible="false" CssClass="auth-card">
                <p class="dr-texto"><asp:Literal ID="litNoEncontrada" runat="server" Mode="Encode" /></p>
                <a class="button button-secondary" href="MisReservas.aspx">Ir a Mis reservas</a>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="dr-grid">
                    <div>
                        <div class="auth-card">
                            <div class="auth-card-header"><h2>La reserva</h2></div>
                            <dl class="dr-datos">
                                <div>
                                    <dt>Espacio</dt>
                                    <dd><asp:HyperLink ID="lnkEspacio" runat="server" /></dd>
                                </div>
                                <div>
                                    <dt>Tipo de espacio</dt>
                                    <dd><asp:Literal ID="litTipoEspacio" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Ubicación</dt>
                                    <dd><asp:Literal ID="litUbicacion" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Fecha</dt>
                                    <dd><asp:Literal ID="litFecha" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Franja horaria</dt>
                                    <dd><asp:Literal ID="litHorario" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Valor de la operación</dt>
                                    <dd><asp:Literal ID="litValor" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Pago</dt>
                                    <dd><asp:Literal ID="litPago" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Solicitada el</dt>
                                    <dd><asp:Literal ID="litFechaSolicitud" runat="server" Mode="Encode" /></dd>
                                </div>
                                <asp:PlaceHolder ID="phResolucion" runat="server">
                                    <div>
                                        <dt>Resuelta el</dt>
                                        <dd><asp:Literal ID="litFechaResolucion" runat="server" Mode="Encode" /></dd>
                                    </div>
                                </asp:PlaceHolder>
                                <asp:PlaceHolder ID="phCancelacion" runat="server">
                                    <div>
                                        <dt>Cancelada el</dt>
                                        <dd><asp:Literal ID="litFechaCancelacion" runat="server" Mode="Encode" /></dd>
                                    </div>
                                    <div>
                                        <dt>Cargo por cancelación</dt>
                                        <dd><asp:Literal ID="litCargoCancelacion" runat="server" Mode="Encode" /></dd>
                                    </div>
                                </asp:PlaceHolder>
                            </dl>
                        </div>

                        <asp:Panel ID="pnlComentarios" runat="server" CssClass="auth-card">
                            <div class="auth-card-header"><h2>Comentarios</h2></div>
                            <asp:Panel ID="pnlComentarioSolicitante" runat="server">
                                <p class="dr-nota">Del solicitante</p>
                                <p class="dr-texto"><asp:Literal ID="litComentarioSolicitante" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                            <asp:Panel ID="pnlComentarioResolucion" runat="server">
                                <p class="dr-nota">Del gestor</p>
                                <p class="dr-texto"><asp:Literal ID="litComentarioResolucion" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                        </asp:Panel>

                        <div id="mensajes" class="auth-card dr-mensajes-card">
                            <div class="auth-card-header"><h2>Mensajes</h2></div>
                            <p class="dr-nota"><asp:Literal ID="litMensajesAyuda" runat="server" Mode="Encode" /></p>
                            <asp:Panel ID="pnlMensajeChat" runat="server" Visible="false" CssClass="form-message" role="status">
                                <asp:Literal ID="litMensajeChat" runat="server" Mode="Encode" />
                            </asp:Panel>
                            <div id="dr-chat" class="dr-chat" tabindex="0" role="log" aria-label="Conversación de la reserva">
                                <asp:Repeater ID="rptMensajes" runat="server">
                                    <ItemTemplate>
                                        <div class='<%# ClaseMensaje(Container.DataItem) %>'>
                                            <span class="dr-chat-autor"><%#: AutorMensaje(Container.DataItem) %></span>
                                            <p class="dr-chat-texto"><%#: Eval("Texto") %></p>
                                            <span class="dr-chat-meta"><%#: Eval("FechaEnvio", "{0:dd/MM/yyyy HH:mm}") %><%#: EstadoLectura(Container.DataItem) %></span>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <asp:Panel ID="pnlSinMensajes" runat="server" CssClass="dr-chat-vacio">
                                    <p>Todavía no hay mensajes. Podés escribir para coordinar detalles de la reserva, como el horario de llegada, el equipamiento o el acceso al espacio.</p>
                                </asp:Panel>
                            </div>
                            <asp:Panel ID="pnlEscribirMensaje" runat="server" CssClass="dr-chat-form">
                                <label class="sr-only" for="<%= txtMensaje.ClientID %>">Mensaje</label>
                                <asp:TextBox ID="txtMensaje" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000" data-contador-mensaje="dr-contador"
                                    placeholder="Escribí tu mensaje" />
                                <div class="dr-chat-form-pie">
                                    <span id="dr-contador" class="dr-nota" aria-live="polite">Hasta 1000 caracteres</span>
                                    <asp:Button ID="btnEnviarMensaje" runat="server" CssClass="button button-primary button-small" Text="Enviar mensaje"
                                        CausesValidation="false" UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Enviando...';"
                                        OnClick="btnEnviarMensaje_Click" />
                                </div>
                            </asp:Panel>
                            <asp:Panel ID="pnlMensajesCerrados" runat="server" Visible="false">
                                <p class="dr-nota"><asp:Literal ID="litMensajesCerrados" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                        </div>

                        <div class="auth-card dr-tracking-card">
                            <div class="auth-card-header"><h2>Seguimiento</h2></div>
                            <ol class="booking-progress-detail-list booking-progress-detail-list-standalone">
                                <asp:Repeater ID="rptSeguimiento" runat="server">
                                    <ItemTemplate>
                                        <li class='<%# "booking-progress-detail booking-progress-detail-" + Eval("Estado").ToString().ToLowerInvariant() %>'>
                                            <time><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %></time>
                                            <div>
                                                <strong><%#: Eval("Titulo") %></strong>
                                                <span><%#: Eval("Detalle") %></span>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ol>
                        </div>
                    </div>

                    <div>
                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Participantes</h2></div>
                            <div class="dr-persona">
                                <small>Solicitante</small>
                                <strong><asp:Literal ID="litSolicitante" runat="server" Mode="Encode" /></strong>
                                <asp:Label ID="lblCorreoSolicitante" runat="server" />
                            </div>
                            <div class="dr-persona">
                                <small>Gestor del espacio</small>
                                <strong><asp:Literal ID="litGestor" runat="server" Mode="Encode" /></strong>
                            </div>
                        </div>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Condiciones de uso</h2></div>
                            <dl class="dr-datos" style="grid-template-columns: 1fr;">
                                <div>
                                    <dt>Capacidad máxima</dt>
                                    <dd><asp:Literal ID="litCapacidad" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Tipo de piso</dt>
                                    <dd><asp:Literal ID="litTipoPiso" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Equipamiento y condiciones</dt>
                                    <dd><asp:Literal ID="litEquipamiento" runat="server" Mode="Encode" /></dd>
                                </div>
                            </dl>
                        </div>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Política de cancelación</h2></div>
                            <p class="dr-texto"><asp:Literal ID="litPolitica" runat="server" Mode="Encode" /></p>
                            <asp:Panel ID="pnlPoliticaAhora" runat="server" CssClass="dr-politica-ahora">
                                <asp:Literal ID="litPoliticaAhora" runat="server" Mode="Encode" />
                            </asp:Panel>
                        </div>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Acciones</h2></div>
                            <asp:Panel ID="pnlAcciones" runat="server" CssClass="dr-acciones">
                                <asp:HyperLink ID="lnkPagar" runat="server" CssClass="button button-primary" Text="Pagar reserva" Visible="false" />
                                <asp:HyperLink ID="lnkVerPago" runat="server" CssClass="button button-secondary" Text="Ver pago y factura" Visible="false" />
                                <asp:Button ID="btnCancelar" runat="server" CssClass="button button-secondary" CausesValidation="false" Visible="false" OnClick="btnCancelar_Click" />
                                <asp:Button ID="btnAceptar" runat="server" CssClass="button button-primary" Text="Aceptar solicitud" CausesValidation="false" Visible="false" OnClick="btnAceptar_Click" />
                                <asp:Button ID="btnRechazar" runat="server" CssClass="button button-secondary" Text="Rechazar solicitud" CausesValidation="false" Visible="false" OnClick="btnRechazar_Click" />
                                <asp:HyperLink ID="lnkCalificar" runat="server" CssClass="button button-secondary" Visible="false" />
                                <asp:HyperLink ID="lnkSoporte" runat="server" CssClass="text-link" Text="Contactar soporte" Visible="false" />
                            </asp:Panel>
                            <asp:Panel ID="pnlSinAcciones" runat="server" Visible="false">
                                <p class="dr-nota">No hay acciones disponibles para esta reserva en su estado actual.</p>
                            </asp:Panel>

                            <asp:Panel ID="pnlConfirmacion" runat="server" CssClass="dr-confirmacion" Visible="false" role="alertdialog" aria-labelledby="dr-confirmacion-texto">
                                <asp:HiddenField ID="hfAccion" runat="server" />
                                <p id="dr-confirmacion-texto"><strong><asp:Literal ID="litConfirmacion" runat="server" Mode="Encode" /></strong></p>
                                <asp:Panel ID="pnlComentarioGestor" runat="server" CssClass="form-field dr-comentario" Visible="false">
                                    <label for="<%= txtComentarioGestor.ClientID %>">Comentario para el solicitante (opcional)</label>
                                    <asp:TextBox ID="txtComentarioGestor" runat="server" TextMode="MultiLine" Rows="3" MaxLength="750" />
                                </asp:Panel>
                                <div class="dr-acciones">
                                    <asp:Button ID="btnConfirmar" runat="server" CssClass="button button-danger" CausesValidation="false"
                                        UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Procesando...';" OnClick="btnConfirmar_Click" />
                                    <asp:LinkButton ID="lnkVolver" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkVolver_Click">No, volver</asp:LinkButton>
                                </div>
                            </asp:Panel>
                        </div>

                        <p><asp:HyperLink ID="lnkVolverListado" runat="server" CssClass="text-link" /></p>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
    <script>
        (function () {
            var chat = document.getElementById("dr-chat");
            if (chat) {
                chat.scrollTop = chat.scrollHeight;
            }
            if (/[?&](ver=mensajes|enviado=1)/.test(window.location.search)) {
                var tarjeta = document.getElementById("mensajes");
                if (tarjeta) {
                    tarjeta.scrollIntoView();
                }
            }
            var campo = document.querySelector("[data-contador-mensaje]");
            var contador = campo ? document.getElementById(campo.getAttribute("data-contador-mensaje")) : null;
            if (campo && contador) {
                var actualizar = function () {
                    var largo = campo.value.trim().length;
                    contador.textContent = largo > 0 ? largo + " de 1000 caracteres" : "Hasta 1000 caracteres";
                };
                campo.addEventListener("input", actualizar);
                actualizar();
            }
        }());
    </script>
</asp:Content>
