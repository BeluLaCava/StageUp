<%@ Page Title="Soporte | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Soporte.aspx.cs" Inherits="StageUp.UI.Soporte" %>

<asp:Content ID="SoporteContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page user-module-page helpdesk-page">
        <div class="static-page-header">
            <span class="section-label">Ayuda</span>
            <h1>Soporte</h1>
            <p>Contanos tu consulta y seguí la conversación con nuestro equipo desde acá.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlDashboard" runat="server" Visible="false" CssClass="helpdesk-dashboard">
                <div class="auth-card helpdesk-card helpdesk-compose-card">
                    <div class="auth-card-header">
                        <h2>Nueva consulta</h2>
                        <p>Contanos qué necesitás y te vamos a responder acá mismo.</p>
                    </div>

                    <div class="helpdesk-select-grid">
                        <div class="form-field">
                            <label for="<%= ddlCategoria.ClientID %>">Categoría</label>
                            <asp:DropDownList ID="ddlCategoria" runat="server" />
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlReservaAsociada.ClientID %>">Reserva relacionada (opcional)</label>
                            <asp:DropDownList ID="ddlReservaAsociada" runat="server" />
                            <span class="ticket-lista-item-meta">Si tu consulta es sobre una reserva puntual, elegila para que soporte la vea directamente.</span>
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtAsunto.ClientID %>">Asunto *</label>
                        <asp:TextBox ID="txtAsunto" runat="server" TextMode="SingleLine" MaxLength="200"
                            placeholder="Ej: No puedo cancelar una reserva" />
                        <asp:RequiredFieldValidator ID="rfvAsunto" runat="server" ControlToValidate="txtAsunto"
                            ValidationGroup="Ticket" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá un asunto." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtMensajeInicial.ClientID %>">Mensaje *</label>
                        <asp:TextBox ID="txtMensajeInicial" runat="server" TextMode="MultiLine" Rows="5" MaxLength="2000"
                            CssClass="helpdesk-textarea" placeholder="Contanos con el mayor detalle posible qué te está pasando." />
                        <asp:RequiredFieldValidator ID="rfvMensajeInicial" runat="server" ControlToValidate="txtMensajeInicial"
                            ValidationGroup="Ticket" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Contanos tu consulta." />
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnCrearTicket" runat="server" CssClass="button button-primary" Text="Enviar consulta"
                            ValidationGroup="Ticket" OnClick="btnCrearTicket_Click" />
                    </div>
                </div>

                <div class="auth-card helpdesk-card helpdesk-list-card">
                    <div class="auth-card-header">
                        <h2>Mis consultas</h2>
                    </div>
                    <asp:Panel ID="pnlSinTickets" runat="server" CssClass="admin-empty-hint" Visible="false">
                        Todavía no enviaste ninguna consulta a soporte.
                    </asp:Panel>
                    <asp:Repeater ID="rptMisTickets" runat="server">
                        <ItemTemplate>
                            <div class="ticket-lista-item">
                                <div class="ticket-lista-item-info">
                                    <strong><%#: Eval("Asunto") %></strong>
                                    <span class="ticket-lista-item-meta">
                                        <%#: Eval("Categoria") %> · Última actividad: <%#: Eval("FechaUltimaActividad", "{0:dd/MM/yyyy HH:mm}") %>
                                    </span>
                                    <asp:PlaceHolder runat="server" Visible='<%# ((StageUp.BE.Entidades.Ticket)Container.DataItem).TieneReservaAsociada %>'>
                                        <span class="ticket-lista-item-meta">Reserva: <%#: DescribirReserva((StageUp.BE.Entidades.Ticket)Container.DataItem) %></span>
                                    </asp:PlaceHolder>
                                </div>
                                <div class="ticket-lista-item-actions">
                                    <span class='<%# "ticket-badge " + ObtenerClaseEstado(Eval("Estado")) %>'><%#: ObtenerTextoEstado(Eval("Estado")) %></span>
                                    <a class="button button-secondary button-small" href='<%#: "Soporte.aspx?ver=" + Eval("IdTicket") %>'>Ver conversación</a>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="auth-card helpdesk-card helpdesk-detail-card">
                    <div class="auth-card-header helpdesk-detail-header">
                        <div>
                            <h2><asp:Literal ID="litAsuntoDetalle" runat="server" /></h2>
                            <p class="ticket-lista-item-meta"><asp:Literal ID="litCategoriaDetalle" runat="server" /></p>
                            <asp:Panel ID="pnlReservaDetalle" runat="server" Visible="false">
                                <p class="ticket-lista-item-meta">Reserva relacionada: <asp:Literal ID="litReservaDetalle" runat="server" /></p>
                            </asp:Panel>
                        </div>
                        <asp:Panel ID="pnlEstadoDetalle" runat="server" CssClass="ticket-badge">
                            <asp:Literal ID="litEstadoDetalle" runat="server" />
                        </asp:Panel>
                    </div>

                    <div class="ticket-hilo">
                        <asp:Repeater ID="rptMensajes" runat="server">
                            <ItemTemplate>
                                <div class='<%# "ticket-mensaje " + (Convert.ToBoolean(Eval("EsInterno")) ? "ticket-mensaje-soporte" : "ticket-mensaje-usuario") %>'>
                                    <div class="ticket-mensaje-meta">
                                        <strong><%#: Convert.ToBoolean(Eval("EsInterno")) ? "Soporte StageUp" : "Vos" %></strong>
                                        <span><%#: Eval("FechaEnvio", "{0:dd/MM/yyyy HH:mm}") %></span>
                                    </div>
                                    <p class="ticket-mensaje-texto"><%#: Eval("Mensaje") %></p>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <asp:Panel ID="pnlResponder" runat="server" CssClass="helpdesk-reply">
                        <div class="form-field">
                            <label for="<%= txtNuevoMensaje.ClientID %>">Agregar un mensaje</label>
                            <asp:TextBox ID="txtNuevoMensaje" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000"
                                CssClass="helpdesk-textarea" placeholder="Escribí tu mensaje para el equipo de soporte." />
                        </div>
                        <div class="form-actions">
                            <asp:Button ID="btnEnviarMensaje" runat="server" CssClass="button button-primary" Text="Enviar mensaje"
                                CausesValidation="false" OnClick="btnEnviarMensaje_Click" />
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlCerrado" runat="server" CssClass="admin-empty-hint" Visible="false">
                        Este ticket está cerrado. Si necesitás ayuda adicional, enviá una nueva consulta.
                    </asp:Panel>

                    <div class="form-actions">
                        <a class="text-link" href="Soporte.aspx">Volver a mis consultas</a>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
