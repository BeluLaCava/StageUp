<%@ Page Title="Permisos del rol | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="PermisosDelRol.aspx.cs" Inherits="StageUp.UI.Interno.PermisosDelRol" %>

<asp:Content ID="PermisosDelRolContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Administración</span>
            <h1>Permisos de <asp:Literal ID="litNombreRol" runat="server" /></h1>
            <p>Marcá los permisos que va a tener este rol. Los usuarios internos con este rol van a ver solo las secciones habilitadas acá.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlSinRol" runat="server" Visible="false" CssClass="empty-state">
                <h3>No se encontró el rol</h3>
                <p>Volvé al listado de roles e intentá de nuevo.</p>
            </asp:Panel>

            <asp:Panel ID="pnlFormularioPermisos" runat="server" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>Permisos disponibles</h2>
                </div>

                <asp:Repeater ID="rptPermisos" runat="server">
                    <ItemTemplate>
                        <label class="checkbox-field">
                            <asp:CheckBox ID="chkPermiso" runat="server" Checked='<%# Eval("Asignado") %>' />
                            <span>
                                <strong><%# Eval("Nombre") %></strong>
                                <span class="permission-modulo">(<%# Eval("NombreGrupo") %>)</span>
                                <asp:HiddenField ID="hdnIdComponente" runat="server" Value='<%# Eval("IdComponentePermiso") %>' />
                                <br /><%# Eval("Descripcion") %>
                            </span>
                        </label>
                    </ItemTemplate>
                </asp:Repeater>

                <div class="form-actions">
                    <asp:Button ID="btnGuardarPermisos" runat="server" CssClass="button button-primary" Text="Guardar permisos"
                        CausesValidation="false" OnClick="btnGuardarPermisos_Click" />
                    <a class="text-link" href="~/Interno/GestionRoles.aspx" runat="server">Volver a roles</a>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
