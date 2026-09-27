<%@ Page Title="Soporte | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Soporte.aspx.cs" Inherits="StageUp.UI.Soporte" %>

<asp:Content ID="SoporteContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .ticket-lista-item { display: flex; justify-content: space-between; align-items: center; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); flex-wrap: wrap; }
        .ticket-lista-item-info { display: flex; flex-direction: column; gap: 2px; }
        .ticket-lista-item-meta { color: var(--color-text-muted, #6b7280); font-size: 0.85em; }
        .ticket-badge { display: inline-block; padding: 3px 10px; border-radius: 999px; font-size: 0.78em; font-weight: bold; text-transform: uppercase; letter-spacing: 0.4px; }
        .ticket-badge-abierto { background: #fef3c7; color: #92400e; }
        .ticket-badge-enrevision { background: #dbeafe; color: #1e3a8a; }
        .ticket-badge-respondido { background: #dcfce7; color: #166534; }
        .ticket-badge-cerrado { background: #e5e7eb; color: #374151; }
        .ticket-hilo { display: flex; flex-direction: column; gap: 14px; margin: 18px 0; }
        .ticket-mensaje { padding: 12px 16px; border-radius: 14px; max-width: 80%; }
        .ticket-mensaje-usuario { align-self: flex-end; background: #eef2ff; }
        .ticket-mensaje-soporte { align-self: flex-start; background: #fff2ec; }
        .ticket-mensaje-meta { display: flex; justify-content: space-between; gap: 12px; font-size: 0.8em; color: var(--color-text-muted, #6b7280); margin-bottom: 4px; }
    </style>

    <section class="static-page public-news-page">
        <header class="public-news-hero container-wide">
            <div>
                <span class="section-label">Ayuda</span>
                <h1>Soporte</h1>
                <p>Contanos tu consulta y seguí la conversación con nuestro equipo desde acá.</p>
            </div>
        </header>

        <div class="public-news-layout container-wide">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlDashboard" runat="server" Visible="false">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <h2>Nueva consulta</h2>
                        <p>Contanos qué necesitás y te vamos a responder acá mismo.</p>
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlCategoria.ClientID %>">Categoría</label>
                        <asp:DropDownList ID="ddlCategoria" runat="server" />
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
                            placeholder="Contanos con el mayor detalle posible qué te está pasando." />
                        <asp:RequiredFieldValidator ID="rfvMensajeInicial" runat="server" ControlToValidate="txtMensajeInicial"
                            ValidationGroup="Ticket" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Contanos tu consulta." />
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnCrearTicket" runat="server" CssClass="button button-primary" Text="Enviar consulta"
                            ValidationGroup="Ticket" OnClick="btnCrearTicket_Click" />
                    </div>
                </div>

                <div class="auth-card" style="margin-top: 20px;">
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
                                </div>
                                <div style="display:flex; align-items:center; gap:12px;">
                                    <span class='<%# "ticket-badge " + ObtenerClaseEstado(Eval("Estado")) %>'><%#: ObtenerTextoEstado(Eval("Estado")) %></span>
                                    <a class="button button-secondary button-small" href='<%#: "Soporte.aspx?ver=" + Eval("IdTicket") %>'>Ver conversación</a>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="auth-card">
                    <div class="auth-card-header" style="display:flex; justify-content:space-between; align-items:flex-start; gap:12px; flex-wrap:wrap;">
                        <div>
                            <h2><asp:Literal ID="litAsuntoDetalle" runat="server" /></h2>
                            <p class="ticket-lista-item-meta"><asp:Literal ID="litCategoriaDetalle" runat="server" /></p>
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
                                    <p style="margin:0;"><%#: Eval("Mensaje") %></p>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <asp:Panel ID="pnlResponder" runat="server">
                        <div class="form-field">
                            <label for="<%= txtNuevoMensaje.ClientID %>">Agregar un mensaje</label>
                            <asp:TextBox ID="txtNuevoMensaje" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
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
