<%@ Page Title="Gestión de roles | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionRoles.aspx.cs" Inherits="StageUp.UI.Interno.GestionRoles" %>

<asp:Content ID="GestionRolesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Gestión de roles y permisos</h1>
            <p>Los roles agrupan los permisos que puede tener un usuario interno. Los permisos disponibles son fijos; desde acá se decide cuáles tiene cada rol.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="admin-two-column-layout">
            <asp:Panel ID="pnlFormularioRol" runat="server" CssClass="auth-card admin-editor-card">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow" data-i18n="AdminUI_Configuracion">Configuración</span>
                    <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo rol" /></h2>
                    <p>El nombre del rol es obligatorio.</p>
                </div>

                <div class="form-field">
                    <label for="<%= txtNombreRol.ClientID %>">Nombre del rol *</label>
                    <asp:TextBox ID="txtNombreRol" runat="server" TextMode="SingleLine" MaxLength="200" placeholder="Ej: Administrador, Moderador de contenido..." />
                    <asp:RequiredFieldValidator ID="rfvNombreRol" runat="server" ControlToValidate="txtNombreRol"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre del rol." ValidationGroup="Rol" />
                </div>

                <div class="form-field">
                    <label for="<%= txtDescripcionRol.ClientID %>">Descripción</label>
                    <asp:TextBox ID="txtDescripcionRol" runat="server" TextMode="MultiLine" Rows="3" MaxLength="510" placeholder="Para qué se usa este rol." />
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnGuardarRol" runat="server" CssClass="button button-primary" Text="Guardar rol"
                        ValidationGroup="Rol" OnClick="btnGuardarRol_Click" />
                    <asp:LinkButton ID="lnkCancelarEdicionRol" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Cancelar edición" Visible="false" OnClick="lnkCancelarEdicionRol_Click" />
                </div>
            </asp:Panel>

            <div class="auth-card admin-list-card">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow" data-i18n="AdminUI_Directorio">Directorio</span>
                    <h2>Roles existentes</h2>
                </div>

                <asp:Literal ID="litSinRoles" runat="server" Visible="false" Text="Todavía no hay roles cargados." />

                <asp:Repeater ID="rptRoles" runat="server" OnItemCommand="rptRoles_ItemCommand">
                    <ItemTemplate>
                        <div class="space-row admin-list-row role-list-row">
                            <div class="space-row-info">
                                <span class="admin-row-icon admin-row-icon-role" aria-hidden="true"></span>
                                <div>
                                <h3><%# Eval("NombreRol") %></h3>
                                <p><%# Eval("Descripcion") %></p>
                                </div>
                            </div>
                            <div class="space-row-actions">
                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                    CommandName="Editar" CommandArgument='<%# Eval("IdRolInterno") %>' Text="Editar" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                    CommandName="Permisos" CommandArgument='<%# Eval("IdRolInterno") %>' Text="Permisos" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                    CommandName="Baja" CommandArgument='<%# Eval("IdRolInterno") %>' Text="Dar de baja"
                                    OnClientClick="return confirm('¿Seguro que querés dar de baja este rol?');" />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            </div>
        </div>
    </section>
</asp:Content>
