<%@ Page Title="Usuarios internos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionUsuariosInternos.aspx.cs" Inherits="StageUp.UI.Interno.GestionUsuariosInternos" %>

<asp:Content ID="GestionUsuariosInternosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label" data-i18n="AdminUsuarios_Seccion">Administración</span>
            <h1 data-i18n="AdminUsuarios_Titulo">Usuarios internos</h1>
            <p data-i18n="AdminUsuarios_Descripcion">Creá las cuentas del equipo de Artera y asignales un área, un rol y el estado de acceso correspondiente.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="admin-two-column-layout internal-users-layout">
                <asp:Panel ID="pnlFormularioUsuario" runat="server" CssClass="auth-card admin-editor-card internal-user-editor">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow" data-i18n="AdminUI_Configuracion">Configuración</span>
                        <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo usuario interno" /></h2>
                        <p><asp:Literal ID="litAyudaFormulario" runat="server" Text="Todos los campos identificados con un asterisco son obligatorios." /></p>
                    </div>

                    <div class="internal-user-form-grid">
                        <div class="form-field">
                            <label for="<%= txtNombre.ClientID %>" data-i18n="AdminUsuarios_Nombre">Nombre *</label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="200" autocomplete="given-name"
                                placeholder="Nombre" data-i18n-placeholder="AdminUsuarios_PlaceholderNombre" />
                            <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                                Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre." ValidationGroup="UsuarioInterno" />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtApellido.ClientID %>" data-i18n="AdminUsuarios_Apellido">Apellido *</label>
                            <asp:TextBox ID="txtApellido" runat="server" MaxLength="200" autocomplete="family-name"
                                placeholder="Apellido" data-i18n-placeholder="AdminUsuarios_PlaceholderApellido" />
                            <asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido"
                                Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el apellido." ValidationGroup="UsuarioInterno" />
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtCorreoElectronico.ClientID %>" data-i18n="AdminUsuarios_Correo">Correo electrónico *</label>
                        <asp:TextBox ID="txtCorreoElectronico" runat="server" TextMode="Email" MaxLength="300" autocomplete="email"
                            placeholder="persona@stageup.com" data-i18n-placeholder="AdminUsuarios_PlaceholderCorreo" />
                        <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreoElectronico"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el correo electrónico." ValidationGroup="UsuarioInterno" />
                        <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreoElectronico"
                            Display="Dynamic" CssClass="field-error-text" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
                            ErrorMessage="El correo electrónico no tiene un formato válido." ValidationGroup="UsuarioInterno" />
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlAreaInterna.ClientID %>" data-i18n="AdminUsuarios_Area">Área interna *</label>
                        <asp:DropDownList ID="ddlAreaInterna" runat="server" CssClass="form-select" DataTextField="NombreArea" DataValueField="IdAreaInterna" />
                        <asp:RequiredFieldValidator ID="rfvArea" runat="server" ControlToValidate="ddlAreaInterna" InitialValue=""
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Seleccioná un área interna." ValidationGroup="UsuarioInterno" />
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlRolInterno.ClientID %>" data-i18n="AdminUsuarios_Rol">Rol asignado *</label>
                        <asp:DropDownList ID="ddlRolInterno" runat="server" CssClass="form-select" DataTextField="NombreRol" DataValueField="IdRolInterno" />
                        <asp:RequiredFieldValidator ID="rfvRol" runat="server" ControlToValidate="ddlRolInterno" InitialValue=""
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Seleccioná un rol interno." ValidationGroup="UsuarioInterno" />
                        <span class="field-help" data-i18n="AdminUsuarios_AyudaRol">El usuario heredará todos los permisos configurados para este rol.</span>
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlEstadoCuenta.ClientID %>" data-i18n="AdminUsuarios_Estado">Estado de la cuenta *</label>
                        <asp:DropDownList ID="ddlEstadoCuenta" runat="server" CssClass="form-select" />
                    </div>

                    <div class="internal-password-block">
                        <div class="internal-password-heading">
                            <strong><asp:Literal ID="litTituloPassword" runat="server" Text="Contraseña inicial" /></strong>
                            <span><asp:Literal ID="litAyudaPassword" runat="server" Text="Debe tener al menos 8 caracteres, letras y números." /></span>
                        </div>
                        <div class="internal-user-form-grid">
                            <div class="form-field">
                                <label for="<%= txtPassword.ClientID %>" data-i18n="AdminUsuarios_Password">Contraseña *</label>
                                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password"
                                    placeholder="Mínimo 8 caracteres" data-i18n-placeholder="AdminUsuarios_PlaceholderPassword" />
                            </div>
                            <div class="form-field">
                                <label for="<%= txtConfirmacionPassword.ClientID %>" data-i18n="AdminUsuarios_ConfirmarPassword">Confirmar contraseña *</label>
                                <asp:TextBox ID="txtConfirmacionPassword" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password"
                                    placeholder="Repetí la contraseña" data-i18n-placeholder="AdminUsuarios_PlaceholderConfirmarPassword" />
                                <asp:CompareValidator ID="cvPassword" runat="server" ControlToValidate="txtConfirmacionPassword" ControlToCompare="txtPassword"
                                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="La contraseña y su confirmación no coinciden."
                                    ValidationGroup="UsuarioInterno" />
                            </div>
                        </div>
                    </div>

                    <asp:Panel ID="pnlPermisosRol" runat="server" Visible="false" CssClass="internal-role-permissions">
                        <strong data-i18n="AdminUsuarios_PermisosRol">Permisos del rol actual</strong>
                        <div class="internal-permission-tags">
                            <asp:Repeater ID="rptPermisosRol" runat="server">
                                <ItemTemplate><span><%#: Container.DataItem %></span></ItemTemplate>
                            </asp:Repeater>
                            <asp:Literal ID="litSinPermisosRol" runat="server" Visible="false" Text="Este rol todavía no tiene permisos configurados." />
                        </div>
                    </asp:Panel>

                    <div class="form-actions">
                        <asp:Button ID="btnGuardarUsuario" runat="server" CssClass="button button-primary" Text="Guardar usuario"
                            ValidationGroup="UsuarioInterno" OnClick="btnGuardarUsuario_Click" />
                        <asp:LinkButton ID="lnkCancelarEdicion" runat="server" CssClass="text-link" CausesValidation="false"
                            Text="Cancelar edición" Visible="false" OnClick="lnkCancelarEdicion_Click" data-i18n="AdminUsuarios_Cancelar" />
                    </div>
                </asp:Panel>

                <div class="auth-card admin-list-card internal-users-list-card">
                    <div class="auth-card-header internal-users-list-heading">
                        <div>
                            <span class="admin-card-eyebrow" data-i18n="AdminUI_Directorio">Directorio</span>
                            <h2 data-i18n="AdminUsuarios_Listado">Equipo interno</h2>
                        </div>
                        <a id="lnkGestionarRoles" runat="server" class="admin-action-link admin-action-link-primary" href="~/Interno/GestionRoles.aspx" data-i18n="AdminUsuarios_GestionarRoles">Gestionar roles</a>
                    </div>

                    <asp:Panel ID="pnlSinUsuarios" runat="server" CssClass="empty-state compact-empty-state" Visible="false">
                        <h3 data-i18n="AdminUsuarios_SinUsuarios">Todavía no hay usuarios internos para mostrar</h3>
                        <p data-i18n="AdminUsuarios_SinUsuariosAyuda">Completá el formulario para crear la primera cuenta del equipo.</p>
                    </asp:Panel>

                    <div class="internal-users-list">
                        <asp:Repeater ID="rptUsuarios" runat="server" OnItemCommand="rptUsuarios_ItemCommand">
                            <ItemTemplate>
                                <article class="space-row admin-list-row internal-user-row <%# ObtenerClaseEstado(Eval("Activo")) %>">
                                    <div class="space-row-info">
                                        <span class="admin-row-icon admin-row-icon-person" aria-hidden="true"></span>
                                        <div class="internal-user-main">
                                            <div class="internal-user-name-line">
                                                <h3><%#: Eval("Nombre") %> <%#: Eval("Apellido") %></h3>
                                                <span class="admin-status-badge <%# ObtenerClaseInsignia(Eval("Activo")) %>"><%#: Eval("EstadoCuenta") %></span>
                                            </div>
                                            <p class="internal-user-email"><%#: Eval("CorreoElectronico") %></p>
                                            <div class="internal-user-meta">
                                                <span><strong data-i18n="AdminUsuarios_AreaCorta">Área:</strong> <%#: Eval("NombreArea") %></span>
                                                <span><strong data-i18n="AdminUsuarios_RolCorto">Rol:</strong> <%#: Eval("NombreRol") %></span>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="space-row-actions">
                                        <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                            CommandName="Editar" CommandArgument='<%# Eval("IdUsuarioInterno") %>' Text="Ver / editar" data-i18n="AdminUsuarios_Editar" />
                                        <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                            CommandName="Baja" CommandArgument='<%# Eval("IdUsuarioInterno") %>' Text="Dar de baja"
                                            Visible='<%# PuedeDarDeBaja(Eval("IdUsuarioInterno"), Eval("Activo")) %>'
                                            OnClientClick="return confirm('¿Seguro que querés dar de baja este usuario interno? Se conservará su historial.');" data-i18n="AdminUsuarios_Baja" />
                                    </div>
                                </article>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
