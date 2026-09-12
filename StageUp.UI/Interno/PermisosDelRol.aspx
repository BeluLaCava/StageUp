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

                <p class="admin-workspace-hint">Tildá un grupo entero para habilitar todo lo que tiene adentro (incluido lo que se agregue después) o tildá permisos sueltos uno por uno.</p>

                <div class="permission-tree-wrapper">
                    <asp:TreeView ID="tvPermisos" runat="server" ShowCheckBoxes="All" ShowLines="true" CssClass="permission-tree">
                        <NodeStyle Font-Size="0.9rem" ForeColor="#3E2925" NodeSpacing="4px" VerticalPadding="4px" />
                        <ParentNodeStyle Font-Bold="true" ForeColor="#4D0B17" />
                        <SelectedNodeStyle BackColor="#FCF7F3" />
                        <HoverNodeStyle ForeColor="#6D1021" />
                    </asp:TreeView>
                </div>

                <div class="form-actions permission-actions">
                    <asp:Button ID="btnGuardarPermisos" runat="server" CssClass="button button-primary" Text="Guardar permisos"
                        CausesValidation="false" OnClick="btnGuardarPermisos_Click" />
                    <a class="text-link" href="~/Interno/GestionRoles.aspx" runat="server">Volver a roles</a>
                    <a class="text-link" href="~/Interno/GestionGruposPermisos.aspx" runat="server">Organizar grupos de permisos</a>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
