<%@ Page Title="Gestión de soporte | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionSoporte.aspx.cs" Inherits="StageUp.UI.Interno.GestionSoporte" %>

<asp:Content ID="GestionSoporteContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .ticket-badge { display: inline-block; padding: 3px 10px; border-radius: 999px; font-size: 0.78em; font-weight: bold; text-transform: uppercase; letter-spacing: 0.4px; }
        .ticket-badge-abierto { background: #fef3c7; color: #92400e; }
        .ticket-badge-enrevision { background: #dbeafe; color: #1e3a8a; }
        .ticket-badge-respondido { background: #dcfce7; color: #166534; }
        .ticket-badge-cerrado { background: #e5e7eb; color: #374151; }
        .ticket-admin-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; padding: 14px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); flex-wrap: wrap; }
        .ticket-admin-row-meta { color: var(--color-text-muted, #6b7280); font-size: 0.85em; }
        .ticket-admin-filtros { display: flex; gap: 16px; flex-wrap: wrap; align-items: flex-end; margin-bottom: 18px; }
        .ticket-hilo { display: flex; flex-direction: column; gap: 14px; margin: 18px 0; }
        .ticket-mensaje { padding: 12px 16px; border-radius: 14px; max-width: 80%; }
        .ticket-mensaje-usuario { align-self: flex-start; background: #eef2ff; }
        .ticket-mensaje-soporte { align-self: flex-end; background: #fff2ec; }
        .ticket-mensaje-meta { display: flex; justify-content: space-between; gap: 12px; font-size: 0.8em; color: var(--color-text-muted, #6b7280); margin-bottom: 4px; }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Gestión de soporte</h1>
            <p>Revisá, respondé y cerrá los tickets de soporte que abren los usuarios de StageUp.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlListado" runat="server">
                <div class="auth-card admin-list-card">
                    <div class="ticket-admin-filtros">
                        <div class="form-field" style="margin:0;">
                            <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                            <asp:DropDownList ID="ddlFiltroEstado" runat="server" />
                        </div>
                        <div class="form-field" style="margin:0;">
                            <label for="<%= ddlFiltroCategoria.ClientID %>">Categoría</label>
                            <asp:DropDownList ID="ddlFiltroCategoria" runat="server" />
                        </div>
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-secondary" Text="Filtrar"
                            CausesValidation="false" OnClick="btnFiltrar_Click" />
                    </div>

                    <asp:Panel ID="pnlSinTickets" runat="server" CssClass="admin-empty-hint" Visible="false">
                        No hay tickets que coincidan con el filtro seleccionado.
                    </asp:Panel>

                    <asp:Repeater ID="rptTickets" runat="server">
                        <ItemTemplate>
                            <div class="ticket-admin-row">
                                <div>
                                    <strong><%#: Eval("Asunto") %></strong>
                                    <div class="ticket-admin-row-meta">
                                        <%#: Eval("NombreUsuarioExterno") %> (<%#: Eval("CorreoUsuarioExterno") %>) · <%#: Eval("Categoria") %>
                                    </div>
                                    <div class="ticket-admin-row-meta">
                                        Última actividad: <%#: Eval("FechaUltimaActividad", "{0:dd/MM/yyyy HH:mm}") %>
                                        · <%#: ObtenerTextoAsignado(Eval("NombreUsuarioInternoAsignado")) %>
                                    </div>
                                    <asp:PlaceHolder runat="server" Visible='<%# ((StageUp.BE.Entidades.Ticket)Container.DataItem).TieneReservaAsociada %>'>
                                        <div class="ticket-admin-row-meta">
                                            Reserva #<%#: Eval("IdReservaAsociada") %>: <%#: DescribirReserva((StageUp.BE.Entidades.Ticket)Container.DataItem) %>
                                        </div>
                                    </asp:PlaceHolder>
                                </div>
                                <div style="display:flex; align-items:center; gap:12px;">
                                    <span class='<%# "ticket-badge " + ObtenerClaseEstado(Eval("Estado")) %>'><%#: ObtenerTextoEstado(Eval("Estado")) %></span>
                                    <a class="button button-secondary button-small" href='<%#: "GestionSoporte.aspx?ver=" + Eval("IdTicket") %>'>Ver</a>
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
                            <p class="ticket-admin-row-meta">
                                <asp:Literal ID="litUsuarioDetalle" runat="server" /> · <asp:Literal ID="litCategoriaDetalle" runat="server" />
                            </p>
                        </div>
                        <asp:Panel ID="pnlEstadoDetalle" runat="server" CssClass="ticket-badge">
                            <asp:Literal ID="litEstadoDetalle" runat="server" />
                        </asp:Panel>
                    </div>

                    <asp:Panel ID="pnlReservaAsociada" runat="server" CssClass="admin-empty-hint" Visible="false">
                        <strong>Reserva relacionada #<asp:Literal ID="litIdReserva" runat="server" /></strong><br />
                        Espacio: <asp:Literal ID="litEspacioReserva" runat="server" /><br />
                        Fecha y horario: <asp:Literal ID="litFechaReserva" runat="server" /><br />
                        Estado de la reserva: <asp:Literal ID="litEstadoReserva" runat="server" />
                    </asp:Panel>

                    <div class="ticket-hilo">
                        <asp:Repeater ID="rptMensajes" runat="server">
                            <ItemTemplate>
                                <div class='<%# "ticket-mensaje " + (Convert.ToBoolean(Eval("EsInterno")) ? "ticket-mensaje-soporte" : "ticket-mensaje-usuario") %>'>
                                    <div class="ticket-mensaje-meta">
                                        <strong><%#: Eval("NombreAutor") %></strong>
                                        <span><%#: Eval("FechaEnvio", "{0:dd/MM/yyyy HH:mm}") %></span>
                                    </div>
                                    <p style="margin:0;"><%#: Eval("Mensaje") %></p>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnTomarTicket" runat="server" CssClass="button button-secondary" Text="Tomar este ticket"
                            CausesValidation="false" OnClick="btnTomarTicket_Click" />
                        <asp:DropDownList ID="ddlNuevoEstado" runat="server" />
                        <asp:Button ID="btnCambiarEstado" runat="server" CssClass="button button-secondary" Text="Actualizar estado"
                            CausesValidation="false" OnClick="btnCambiarEstado_Click" />
                    </div>

                    <asp:Panel ID="pnlResponder" runat="server">
                        <div class="form-field">
                            <label for="<%= txtRespuestaInterna.ClientID %>">Respuesta para el usuario</label>
                            <asp:TextBox ID="txtRespuestaInterna" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" />
                        </div>
                        <div class="form-actions">
                            <asp:Button ID="btnResponder" runat="server" CssClass="button button-primary" Text="Enviar respuesta"
                                CausesValidation="false" OnClick="btnResponder_Click" />
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlCerrado" runat="server" CssClass="admin-empty-hint" Visible="false">
                        Este ticket está cerrado. Podés reabrirlo cambiando el estado si hace falta.
                    </asp:Panel>

                    <div class="form-actions">
                        <a class="text-link" href="GestionSoporte.aspx">Volver al listado</a>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
