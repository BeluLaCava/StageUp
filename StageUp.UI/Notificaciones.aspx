<%@ Page Title="Notificaciones | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Notificaciones.aspx.cs" Inherits="StageUp.UI.Notificaciones" %>

<asp:Content ID="NotificacionesContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .nc-barra { display: flex; flex-wrap: wrap; gap: 10px 16px; align-items: center; justify-content: space-between; margin: 0 0 16px; }
        .nc-filtros { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
        .nc-filtros label { font-weight: 600; color: var(--color-text-muted); font-size: 0.9em; }
        .nc-resumen { color: var(--color-text-muted); }
        .nc-lista { list-style: none; margin: 0; padding: 0; }
        .nc-item { display: grid; grid-template-columns: auto minmax(0, 1fr) auto; gap: 12px 14px; align-items: start; padding: 14px 16px; margin: 0 0 10px;
            border: 1px solid var(--color-border); border-radius: 14px; background: var(--color-surface); }
        .nc-item.is-unread { border-left: 4px solid var(--color-primary); background: #fffaf6; }
        .nc-punto { width: 10px; height: 10px; margin-top: 6px; border-radius: 50%; border: 2px solid var(--color-primary); }
        .nc-item.is-unread .nc-punto { background: var(--color-primary); }
        .nc-texto { min-width: 0; }
        .nc-texto h2 { margin: 0; font-size: 1rem; font-family: var(--font-interface); color: var(--color-primary-dark); }
        .nc-item.is-read .nc-texto h2 { font-weight: 600; }
        .nc-texto p { margin: 4px 0 6px; color: var(--color-text); overflow-wrap: anywhere; }
        .nc-meta { display: flex; flex-wrap: wrap; gap: 6px 10px; font-size: 0.82em; color: var(--color-text-muted); }
        .nc-tipo { padding: 1px 8px; border-radius: 999px; background: rgba(243,223,209,.6); color: var(--color-brown-dark); font-weight: 700; }
        .nc-estado-nueva { color: var(--color-primary); font-weight: 700; }
        .nc-acciones { display: flex; flex-direction: column; gap: 6px; align-items: flex-end; }
        .nc-vacio { padding: 36px 20px; text-align: center; color: var(--color-text-muted); border: 1px dashed var(--color-border); border-radius: 16px; }
        .nc-nota { color: var(--color-text-muted); font-size: 0.85em; margin-top: 12px; }
        @media (max-width: 600px) {
            .nc-item { grid-template-columns: auto minmax(0, 1fr); }
            .nc-acciones { grid-column: 2; flex-direction: row; flex-wrap: wrap; align-items: center; }
        }
    </style>

    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Tu actividad</span>
            <h1>Notificaciones</h1>
            <p>Avisos sobre tus reservas, pagos, solicitudes de soporte y tu cuenta. Tocá una para ir al detalle.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="nc-barra">
                <div class="nc-filtros">
                    <label for="<%= ddlEstado.ClientID %>">Mostrar</label>
                    <asp:DropDownList ID="ddlEstado" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlEstado_SelectedIndexChanged">
                        <asp:ListItem Value="Todas" Selected="True">Todas</asp:ListItem>
                        <asp:ListItem Value="NoLeidas">No leídas</asp:ListItem>
                        <asp:ListItem Value="Leidas">Leídas</asp:ListItem>
                    </asp:DropDownList>
                    <span class="nc-resumen"><asp:Literal ID="litResumen" runat="server" Mode="Encode" /></span>
                </div>
                <asp:Button ID="btnMarcarTodas" runat="server" CssClass="button button-secondary button-small" Text="Marcar todas como leídas"
                    CausesValidation="false" OnClick="btnMarcarTodas_Click" />
            </div>

            <%-- A1: sin notificaciones. --%>
            <asp:Panel ID="pnlVacio" runat="server" Visible="false" CssClass="nc-vacio">
                <asp:Literal ID="litVacio" runat="server" Mode="Encode" />
            </asp:Panel>

            <ul class="nc-lista">
                <asp:Repeater ID="rptNotificaciones" runat="server" OnItemCommand="rptNotificaciones_ItemCommand">
                    <ItemTemplate>
                        <li class='<%# "nc-item " + ((bool)Eval("Leida") ? "is-read" : "is-unread") %>'>
                            <span class="nc-punto" aria-hidden="true"></span>
                            <div class="nc-texto">
                                <h2><%# Server.HtmlEncode(Convert.ToString(Eval("Titulo"))) %></h2>
                                <p><%# Server.HtmlEncode(Convert.ToString(Eval("Mensaje"))) %></p>
                                <div class="nc-meta">
                                    <span class="nc-tipo"><%# Server.HtmlEncode(StageUp.BLL.BLL_Notificacion.CategoriaPorTipo(Convert.ToString(Eval("Tipo")))) %></span>
                                    <span><%# Server.HtmlEncode(FormatearFecha((DateTime)Eval("FechaCreacion"))) %></span>
                                    <span class='<%# (bool)Eval("Leida") ? string.Empty : "nc-estado-nueva" %>'><%# (bool)Eval("Leida") ? "Leída" : "No leída" %></span>
                                </div>
                            </div>
                            <div class="nc-acciones">
                                <asp:LinkButton runat="server" CssClass="button button-primary button-small" CausesValidation="false"
                                    Visible='<%# !string.IsNullOrEmpty(Convert.ToString(Eval("UrlDestino"))) %>'
                                    CommandName="Abrir" CommandArgument='<%# Eval("IdNotificacion") %>' Text="Ver detalle" />
                                <asp:LinkButton runat="server" CssClass="text-link" CausesValidation="false" Visible='<%# !(bool)Eval("Leida") %>'
                                    CommandName="MarcarLeida" CommandArgument='<%# Eval("IdNotificacion") %>' Text="Marcar como leída" />
                            </div>
                        </li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
            <p class="nc-nota"><asp:Literal ID="litNota" runat="server" Mode="Encode" /></p>
        </div>
    </section>
</asp:Content>
