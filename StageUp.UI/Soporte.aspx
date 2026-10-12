<%@ Page Title="Soporte | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Soporte.aspx.cs" Inherits="StageUp.UI.Soporte" %>

<asp:Content ID="SoporteContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page user-module-page helpdesk-page">
        <div class="static-page-header">
            <span class="section-label">Ayuda</span>
            <h1><asp:Literal ID="litTitulo" runat="server" Text="Mis solicitudes de soporte" /></h1>
            <p><asp:Literal ID="litSubtitulo" runat="server" Text="Registrá una consulta o un inconveniente y seguí su avance desde acá." /></p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- Ítem 12: aviso de novedades cuando hay un borrador sin enviar. --%>
            <div class="ticket-novedades-aviso" data-soporte-aviso hidden>
                <span>Hay novedades en tus solicitudes de soporte.</span>
                <button type="button" class="button button-secondary button-small" data-soporte-recargar>Ver novedades</button>
            </div>

            <%-- CU-003-002 A5 y A6: "Mis solicitudes de soporte". --%>
            <asp:Panel ID="pnlListado" runat="server" Visible="false">
                <div class="auth-card helpdesk-card helpdesk-list-card">
                    <div class="auth-card-header helpdesk-detail-header">
                        <div>
                            <h2>Mis solicitudes de soporte</h2>
                            <p>Cada solicitud tiene un número de caso. Entrá al detalle para ver las respuestas del equipo y las actualizaciones.</p>
                        </div>
                        <a class="button button-primary" href="Soporte.aspx?nueva=1">Registrar solicitud de soporte</a>
                    </div>

                    <p class="ticket-lista-item-meta ticket-estados-leyenda">
                        Estados:
                        <span class="ticket-badge ticket-badge-pendiente">Pendiente</span> a la espera de revisión ·
                        <span class="ticket-badge ticket-badge-enproceso">En proceso</span> el equipo la está atendiendo ·
                        <span class="ticket-badge ticket-badge-resuelta">Resuelta</span> tiene una respuesta de resolución ·
                        <span class="ticket-badge ticket-badge-cerrada">Cerrada</span> solo para consulta
                    </p>

                    <asp:Panel ID="pnlSinSolicitudes" runat="server" CssClass="empty-state compact-empty-state" Visible="false">
                        <h3>No tenés solicitudes de soporte para consultar</h3>
                        <p>Si tenés una consulta o un inconveniente con StageUp, registrá una solicitud y te respondemos acá.</p>
                        <a class="button button-secondary" href="Soporte.aspx?nueva=1">Registrar solicitud de soporte</a>
                    </asp:Panel>

                    <asp:Repeater ID="rptSolicitudes" runat="server">
                        <ItemTemplate>
                            <div class='<%# "ticket-lista-item" + (Convert.ToInt32(Eval("NovedadesUsuario")) > 0 ? " ticket-lista-item-novedad" : "") %>'>
                                <div class="ticket-lista-item-info">
                                    <span class="ticket-codigo"><%#: Eval("Codigo") %></span>
                                    <strong><%#: Eval("Motivo") %></strong>
                                    <span class="ticket-lista-item-meta">
                                        Registrada el <%#: Eval("FechaCreacion", "{0:dd/MM/yyyy HH:mm}") %> ·
                                        <%#: StageUp.BLL.BLL_Ticket.TextoFuncionalidad(Convert.ToString(Eval("FuncionalidadVinculada"))) %> ·
                                        Última actualización: <%#: Eval("FechaUltimaActividad", "{0:dd/MM/yyyy HH:mm}") %>
                                    </span>
                                </div>
                                <div class="ticket-lista-item-actions">
                                    <asp:PlaceHolder runat="server" Visible='<%# Convert.ToInt32(Eval("NovedadesUsuario")) > 0 %>'>
                                        <span class="ticket-nuevo"><%#: TextoNovedades(Eval("NovedadesUsuario")) %></span>
                                    </asp:PlaceHolder>
                                    <span class='<%# "ticket-badge " + StageUp.BLL.BLL_Ticket.ClaseEstado(Convert.ToString(Eval("Estado"))) %>'><%#: StageUp.BLL.BLL_Ticket.TextoEstado(Convert.ToString(Eval("Estado"))) %></span>
                                    <a class="button button-secondary button-small" href='<%#: "Soporte.aspx?ver=" + Eval("IdTicket") %>'>Ver detalle</a>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>

            <%-- CU-003-002 pasos 2 a 16, A1 a A4: "Nueva solicitud de soporte". --%>
            <asp:Panel ID="pnlNueva" runat="server" Visible="false" DefaultButton="btnEnviar">
                <div class="auth-card helpdesk-card helpdesk-compose-card">
                    <div class="auth-card-header">
                        <h2>Nueva solicitud de soporte</h2>
                        <p>Contanos qué necesitás. Con estos datos el equipo de soporte puede analizar el caso y responderte acá mismo.</p>
                    </div>

                    <asp:Panel ID="pnlOrigenAsistente" runat="server" Visible="false" CssClass="ticket-estado-aviso">
                        Venís del asistente de ayuda: usamos tu consulta como motivo. Revisala y completá el resto de los datos.
                    </asp:Panel>

                    <%-- A9: si ya hay casos en curso, se pueden seguir sin registrar otro. --%>
                    <asp:Panel ID="pnlEnCurso" runat="server" Visible="false" CssClass="ticket-estado-aviso">
                        <strong>Ya tenés solicitudes en curso.</strong> Si es por el mismo inconveniente, podés seguirla y agregar información sin registrar una nueva:
                        <asp:Repeater ID="rptEnCurso" runat="server">
                            <ItemTemplate>
                                <br /><a class="text-link" href='<%#: "Soporte.aspx?ver=" + Eval("IdTicket") %>'><%#: Eval("Codigo") %> · <%#: Eval("Motivo") %></a>
                                (<%#: StageUp.BLL.BLL_Ticket.TextoEstado(Convert.ToString(Eval("Estado"))) %>)
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>

                    <div class="form-field">
                        <label for="<%= txtMotivo.ClientID %>">Motivo de la consulta *</label>
                        <asp:TextBox ID="txtMotivo" runat="server" MaxLength="150" placeholder="Ej: No puedo cancelar una reserva" />
                    </div>

                    <div class="helpdesk-select-grid">
                        <div class="form-field">
                            <label for="<%= ddlFuncionalidad.ClientID %>">Funcionalidad vinculada *</label>
                            <asp:DropDownList ID="ddlFuncionalidad" runat="server" />
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlReservaAsociada.ClientID %>">Reserva relacionada (opcional)</label>
                            <asp:DropDownList ID="ddlReservaAsociada" runat="server" />
                            <span class="ticket-lista-item-meta">Si es sobre una reserva o su pago, elegila para que soporte la vea directamente.</span>
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtDescripcion.ClientID %>">Descripción del inconveniente *</label>
                        <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="5" MaxLength="2000"
                            CssClass="helpdesk-textarea" placeholder="Qué intentabas hacer, qué pasó y, si apareció, qué mensaje viste." data-soporte-borrador="1" />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtInformacionAdicional.ClientID %>">Información adicional</label>
                        <asp:TextBox ID="txtInformacionAdicional" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000"
                            CssClass="helpdesk-textarea" placeholder="Número de reserva o de pago, fecha y hora en que ocurrió, dispositivo o navegador, etc." data-soporte-borrador="1" />
                        <span class="ticket-lista-item-meta">Necesaria si elegís "Otra funcionalidad", o si es sobre una reserva o un pago y no la elegiste arriba.</span>
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnEnviar" runat="server" CssClass="button button-primary" Text="Enviar solicitud"
                            CausesValidation="false" OnClick="btnEnviar_Click" />
                        <asp:HyperLink ID="lnkCancelar" runat="server" CssClass="button button-secondary" NavigateUrl="~/Soporte.aspx" Text="Cancelar" />
                    </div>
                </div>
            </asp:Panel>

            <%-- CU-003-002 A5 pasos 8 a 10, A7, A8 y A9: "Detalle de solicitud de soporte". --%>
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false" DefaultButton="btnEnviarMensaje">
                <div class="auth-card helpdesk-card helpdesk-detail-card">
                    <div class="auth-card-header helpdesk-detail-header">
                        <div>
                            <span class="ticket-codigo"><asp:Literal ID="litCodigo" runat="server" Mode="Encode" /></span>
                            <h2><asp:Literal ID="litMotivo" runat="server" Mode="Encode" /></h2>
                        </div>
                        <asp:Label ID="lblEstado" runat="server" CssClass="ticket-badge" />
                    </div>

                    <%-- A8: actualizaciones desde la última visita. --%>
                    <asp:Panel ID="pnlNovedades" runat="server" Visible="false" CssClass="ticket-novedades-aviso">
                        <asp:Literal ID="litNovedades" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <%-- A7: mensaje según el estado. --%>
                    <asp:Panel ID="pnlEstadoAviso" runat="server" CssClass="ticket-estado-aviso">
                        <asp:Literal ID="litEstadoAviso" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <dl class="ticket-datos">
                        <div><dt>Fecha de registro</dt><dd><asp:Literal ID="litFechaRegistro" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Funcionalidad vinculada</dt><dd><asp:Literal ID="litFuncionalidad" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Última actualización</dt><dd><asp:Literal ID="litUltimaActividad" runat="server" Mode="Encode" /></dd></div>
                        <asp:PlaceHolder ID="phResolucion" runat="server" Visible="false">
                            <div><dt>Resuelta el</dt><dd><asp:Literal ID="litFechaResolucion" runat="server" Mode="Encode" /></dd></div>
                        </asp:PlaceHolder>
                        <asp:PlaceHolder ID="phCierre" runat="server" Visible="false">
                            <div><dt>Cerrada el</dt><dd><asp:Literal ID="litFechaCierre" runat="server" Mode="Encode" /></dd></div>
                        </asp:PlaceHolder>
                        <asp:PlaceHolder ID="phReserva" runat="server" Visible="false">
                            <div><dt>Reserva relacionada</dt><dd><asp:Literal ID="litReserva" runat="server" Mode="Encode" /></dd></div>
                        </asp:PlaceHolder>
                    </dl>

                    <div class="ticket-descripcion">
                        <h3>Descripción del inconveniente</h3>
                        <p><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></p>
                        <asp:PlaceHolder ID="phInformacion" runat="server" Visible="false">
                            <h3>Información adicional</h3>
                            <p><asp:Literal ID="litInformacion" runat="server" Mode="Encode" /></p>
                        </asp:PlaceHolder>
                    </div>

                    <h3 class="ticket-seccion-titulo">Respuestas y mensajes</h3>
                    <div class="ticket-hilo">
                        <asp:Panel ID="pnlSinMensajes" runat="server" Visible="false">
                            <p class="ticket-hilo-vacio">Todavía no hay respuestas del equipo de soporte.</p>
                        </asp:Panel>
                        <asp:Repeater ID="rptMensajes" runat="server">
                            <ItemTemplate>
                                <div class='<%# "ticket-mensaje " + (Convert.ToBoolean(Eval("EsInterno")) ? "ticket-mensaje-soporte" : "ticket-mensaje-usuario") + (Convert.ToBoolean(Eval("EsNuevo")) ? " ticket-mensaje-nuevo" : "") %>'>
                                    <div class="ticket-mensaje-meta">
                                        <strong><%#: Convert.ToBoolean(Eval("EsInterno")) ? "Soporte StageUp" : "Vos" %></strong>
                                        <span>
                                            <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("EsNuevo")) %>'><span class="ticket-nuevo">Nuevo</span></asp:PlaceHolder>
                                            <%#: Eval("FechaEnvio", "{0:dd/MM/yyyy HH:mm}") %>
                                        </span>
                                    </div>
                                    <p class="ticket-mensaje-texto"><%#: Eval("Mensaje") %></p>
                                    <asp:PlaceHolder runat="server" Visible='<%# !Convert.ToBoolean(Eval("EsInterno")) %>'>
                                        <span class="ticket-mensaje-leido"><%#: Convert.ToBoolean(Eval("LeidoPorLaOtraParte")) ? "Visto por soporte" : "Enviado" %></span>
                                    </asp:PlaceHolder>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <h3 class="ticket-seccion-titulo">Actualizaciones del caso</h3>
                    <ul class="ticket-historial">
                        <asp:Repeater ID="rptActualizaciones" runat="server">
                            <ItemTemplate>
                                <li>
                                    <span class="ticket-historial-fecha"><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %> hs
                                        <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("EsNuevo")) %>'> · <span class="ticket-nuevo">Nuevo</span></asp:PlaceHolder>
                                    </span>
                                    <%#: DescribirActualizacion((StageUp.BE.Entidades.TicketActualizacion)Container.DataItem) %>
                                </li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>

                    <%-- A8/A9: se sigue el mismo caso sin registrar otro. --%>
                    <asp:Panel ID="pnlResponder" runat="server" CssClass="helpdesk-reply">
                        <div class="form-field">
                            <label for="<%= txtNuevoMensaje.ClientID %>">Agregar información o responder</label>
                            <asp:TextBox ID="txtNuevoMensaje" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000"
                                CssClass="helpdesk-textarea" placeholder="Escribí tu mensaje para el equipo de soporte." data-soporte-borrador="1" />
                            <span class="ticket-lista-item-meta">No hace falta registrar otra solicitud por el mismo inconveniente: lo que escribas acá le llega al equipo en este mismo caso.</span>
                        </div>
                        <div class="form-actions">
                            <asp:Button ID="btnEnviarMensaje" runat="server" CssClass="button button-primary" Text="Enviar mensaje"
                                CausesValidation="false" OnClick="btnEnviarMensaje_Click" />
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlCerrada" runat="server" CssClass="empty-state compact-empty-state" Visible="false">
                        <p>Esta solicitud está cerrada y ya no admite mensajes. Si tenés un inconveniente nuevo, registrá otra solicitud.</p>
                        <a class="button button-secondary" href="Soporte.aspx?nueva=1">Registrar solicitud de soporte</a>
                    </asp:Panel>

                    <div class="form-actions">
                        <a class="text-link" href="Soporte.aspx">Volver a mis solicitudes</a>
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlNovedadesPeriodicas" runat="server" Visible="false">
                <div data-soporte-novedades="<%: UrlNovedades %>" data-marca="<%: MarcaNovedades %>" data-intervalo="20000"></div>
            </asp:Panel>
        </div>
    </section>

    <style>
        .ticket-estados-leyenda { margin: -0.4rem 0 0.6rem; line-height: 2.1; }
        .ticket-estados-leyenda .ticket-badge { min-height: 1.4rem; padding: 0.05rem 0.5rem; font-size: 0.62rem; }
        .helpdesk-compose-card .form-actions { display: flex; gap: 0.75rem; flex-wrap: wrap; }
    </style>
    <script src="<%= ResolveUrl("~/Scripts/soporte-novedades.js") %>?v=20261012" defer></script>
</asp:Content>
