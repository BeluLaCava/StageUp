<%@ Page Title="Permisos del rol | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="PermisosDelRol.aspx.cs" Inherits="StageUp.UI.Interno.PermisosDelRol" %>

<asp:Content ID="PermisosDelRolContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Permisos de <asp:Literal ID="litNombreRol" runat="server" /></h1>
            <p>Marcá los permisos que va a tener este rol. Los usuarios internos con este rol van a ver solo las secciones habilitadas acá.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlSinRol" runat="server" Visible="false" CssClass="empty-state">
                <h3>No se encontró el rol</h3>
                <p>Volvé al listado de roles e intentá de nuevo.</p>
            </asp:Panel>

            <asp:Panel ID="pnlFormularioPermisos" runat="server" CssClass="auth-card admin-workspace-card permission-workspace">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                    <span class="admin-card-eyebrow" data-i18n="AdminUI_AccesosDelRol">Accesos del rol</span>
                    <h2>Permisos disponibles</h2>
                    </div>
                    <span class="admin-workspace-hint" data-i18n="AdminUI_ActivarDesactivar">Podés activar o desactivar cada permiso.</span>
                </div>

                <div class="permission-grid">
                <asp:Repeater ID="rptPermisos" runat="server">
                    <ItemTemplate>
                        <label class="checkbox-field permission-card">
                            <asp:CheckBox ID="chkPermiso" runat="server" Checked='<%# Eval("Asignado") %>' />
                            <span class="permission-card-copy">
                                <strong><%# Eval("Nombre") %></strong>
                                <span class="permission-modulo"><%# Eval("NombreGrupo") %></span>
                                <asp:HiddenField ID="hdnIdComponente" runat="server" Value='<%# Eval("IdComponentePermiso") %>' />
                                <span class="permission-description"><%# Eval("Descripcion") %></span>
                            </span>
                        </label>
                    </ItemTemplate>
                </asp:Repeater>
                </div>

                <div class="form-actions permission-actions">
                    <asp:Button ID="btnGuardarPermisos" runat="server" CssClass="button button-primary" Text="Guardar permisos"
                        CausesValidation="false" OnClick="btnGuardarPermisos_Click" />
                    <a class="text-link" href="~/Interno/GestionRoles.aspx" runat="server">Volver a roles</a>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
