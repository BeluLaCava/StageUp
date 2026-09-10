<%@ Page Title="Aprobación de gestores | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="AprobacionGestores.aspx.cs" Inherits="StageUp.UI.Interno.AprobacionGestores" %>

<asp:Content ID="AprobacionGestoresContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Aprobación de gestores</span>
            <h1>Solicitudes de habilitación como gestor</h1>
            <p>Usuarios externos que pidieron poder publicar y administrar sus propios espacios artísticos.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="auth-card admin-workspace-card manager-requests-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                    <span class="admin-card-eyebrow" data-i18n="AdminUI_Revision">Revisión</span>
                    <h2>Solicitudes pendientes</h2>
                    </div>
                    <span class="admin-workspace-hint" data-i18n="AdminUI_RevisarCuenta">Revisá cada cuenta antes de tomar una decisión.</span>
                </div>

                <asp:Repeater ID="rptSolicitudes" runat="server" OnItemCommand="rptSolicitudes_ItemCommand">
                    <ItemTemplate>
                        <div class="space-row admin-list-row manager-request-row">
                            <div class="space-row-info">
                                <span class="admin-row-icon admin-row-icon-person" aria-hidden="true"></span>
                                <div>
                                <h3><%# Eval("Nombre") %> <%# Eval("Apellido") %></h3>
                                <p><%# Eval("CorreoElectronico") %></p>
                                <span class="admin-status-badge" data-i18n="AdminUI_PendienteRevision">Pendiente de revisión</span>
                                </div>
                            </div>
                            <div class="space-row-actions">
                                <asp:Button runat="server" CssClass="button button-primary button-small"
                                    Text="Aprobar" CommandName="Aprobar" CommandArgument='<%# Eval("IdUsuarioExterno") %>' CausesValidation="false" />
                                <asp:Button runat="server" CssClass="button button-ghost button-small"
                                    Text="Rechazar" CommandName="Rechazar" CommandArgument='<%# Eval("IdUsuarioExterno") %>' CausesValidation="false" />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlSinSolicitudes" runat="server" CssClass="empty-state" Visible="false">
                    <h3>No hay solicitudes pendientes</h3>
                    <p>A medida que los usuarios externos pidan habilitarse como gestores, van a aparecer acá.</p>
                </asp:Panel>
            </div>
        </div>
    </section>
</asp:Content>
