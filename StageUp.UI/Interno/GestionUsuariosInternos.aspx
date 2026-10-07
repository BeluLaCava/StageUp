<%@ Page Title="Usuarios internos y permisos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionUsuariosInternos.aspx.cs" Inherits="StageUp.UI.Interno.GestionUsuariosInternos" %>

<asp:Content ID="GestionUsuariosInternosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label" data-i18n="AdminUsuarios_Seccion">Administración</span>
            <h1>Usuarios internos y permisos</h1>
            <p data-i18n="AdminUsuarios_Descripcion">Creá las cuentas del equipo de Artera y asignales un área, un rol y el estado de acceso correspondiente.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- Pasos 3 a 6 del escenario principal: listado, búsqueda y opciones. --%>
            <div class="auth-card admin-list-card internal-users-list-card">
                <div class="auth-card-header internal-users-list-heading">
                    <div>
                        <span class="admin-card-eyebrow" data-i18n="AdminUI_Directorio">Directorio</span>
                        <h2 data-i18n="AdminUsuarios_Listado">Equipo interno</h2>
                    </div>
                    <div class="internal-heading-actions">
                        <a id="lnkGestionarRoles" runat="server" class="admin-action-link" href="~/Interno/GestionRoles.aspx" data-i18n="AdminUsuarios_GestionarRoles">Gestionar roles</a>
                        <asp:Button ID="btnAgregarUsuario" runat="server" CssClass="button button-primary" Text="＋ Agregar usuario interno"
                            CausesValidation="false" OnClick="btnAgregarUsuario_Click" />
                    </div>
                </div>

                <asp:Panel ID="pnlFiltros" runat="server" CssClass="internal-filter-row" DefaultButton="btnBuscar" role="search">
                    <div class="form-field">
                        <label for="<%= txtBuscar.ClientID %>">Buscar</label>
                        <asp:TextBox ID="txtBuscar" runat="server" MaxLength="100" placeholder="Nombre, apellido o correo" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlFiltroRol.ClientID %>">Rol</label>
                        <asp:DropDownList ID="ddlFiltroRol" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                        <asp:DropDownList ID="ddlFiltroEstado" runat="server">
                            <asp:ListItem Text="Todos" Value="Todos" />
                            <asp:ListItem Text="Activos" Value="Activos" />
                            <asp:ListItem Text="Inactivos" Value="Inactivos" />
                        </asp:DropDownList>
                    </div>
                    <div class="internal-filter-actions">
                        <asp:Button ID="btnBuscar" runat="server" CssClass="button button-secondary" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
                        <asp:LinkButton ID="lnkLimpiarFiltros" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkLimpiarFiltros_Click">Limpiar</asp:LinkButton>
                    </div>
                </asp:Panel>

                <%-- A2: no hay usuarios internos registrados. --%>
                <asp:Panel ID="pnlSinUsuarios" runat="server" CssClass="empty-state compact-empty-state" Visible="false">
                    <h3 data-i18n="AdminUsuarios_SinUsuarios">Todavía no hay usuarios internos para mostrar</h3>
                    <p>No existen usuarios internos disponibles. Usá “Agregar usuario interno” para crear la primera cuenta del equipo.</p>
                </asp:Panel>

                <asp:Panel ID="pnlSinResultados" runat="server" CssClass="empty-state compact-empty-state" Visible="false">
                    <h3>No hay usuarios que coincidan con la búsqueda</h3>
                    <p>Probá con otro texto o cambiá los filtros de rol y estado.</p>
                </asp:Panel>

                <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                <div class="internal-users-list">
                    <asp:Repeater ID="rptUsuarios" runat="server" OnItemCommand="rptUsuarios_ItemCommand">
                        <ItemTemplate>
                            <article class="space-row admin-list-row internal-user-row <%# ObtenerClaseEstado(Eval("Activo")) %>">
                                <div class="space-row-info">
                                    <span class="admin-row-icon admin-row-icon-person" aria-hidden="true"></span>
                                    <div class="internal-user-main">
                                        <div class="internal-user-name-line">
                                            <h3><%# Server.HtmlEncode(Convert.ToString(Eval("Nombre"))) %> <%# Server.HtmlEncode(Convert.ToString(Eval("Apellido"))) %></h3>
                                            <span class="admin-status-badge <%# ObtenerClaseInsignia(Eval("Activo")) %>"><%# Server.HtmlEncode(Convert.ToString(Eval("EstadoCuenta"))) %></span>
                                        </div>
                                        <p class="internal-user-email"><%# Server.HtmlEncode(Convert.ToString(Eval("CorreoElectronico"))) %></p>
                                        <div class="internal-user-meta">
                                            <span><strong data-i18n="AdminUsuarios_AreaCorta">Área:</strong> <%# Server.HtmlEncode(Convert.ToString(Eval("NombreArea"))) %></span>
                                            <span><strong data-i18n="AdminUsuarios_RolCorto">Rol:</strong> <%# Server.HtmlEncode(Convert.ToString(Eval("NombreRol"))) %></span>
                                        </div>
                                    </div>
                                </div>
                                <div class="space-row-actions">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                        CommandName="Ver" CommandArgument='<%# Eval("IdUsuarioInterno") %>' Text="Ver detalle" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                        CommandName="Editar" CommandArgument='<%# Eval("IdUsuarioInterno") %>' Text="Editar" />
                                </div>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
    </section>

    <%-- Pasos 7 a 12 (alta) y A8 (edición). --%>
    <asp:Panel ID="pnlFormularioUsuario" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog internal-dialog internal-user-editor" open aria-labelledby="internal-user-form-title">
            <asp:LinkButton ID="lnkCerrarFormulario" runat="server" CssClass="review-dialog-close" CausesValidation="false" OnClick="lnkCancelarEdicion_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label" data-i18n="AdminUI_Configuracion">Configuración</span>
            <h2 id="internal-user-form-title"><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo usuario interno" /></h2>
            <p><asp:Literal ID="litAyudaFormulario" runat="server" Text="Todos los campos identificados con un asterisco son obligatorios." /></p>

            <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" CssClass="form-message form-message-error" role="alert">
                <asp:Literal ID="litMensajeFormulario" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="internal-user-form-grid">
                <div class="form-field">
                    <label for="<%= txtNombre.ClientID %>" data-i18n="AdminUsuarios_Nombre">Nombre *</label>
                    <asp:TextBox ID="txtNombre" runat="server" MaxLength="200" autocomplete="off"
                        placeholder="Nombre" data-i18n-placeholder="AdminUsuarios_PlaceholderNombre" />
                    <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre." ValidationGroup="UsuarioInterno" />
                </div>

                <div class="form-field">
                    <label for="<%= txtApellido.ClientID %>" data-i18n="AdminUsuarios_Apellido">Apellido *</label>
                    <asp:TextBox ID="txtApellido" runat="server" MaxLength="200" autocomplete="off"
                        placeholder="Apellido" data-i18n-placeholder="AdminUsuarios_PlaceholderApellido" />
                    <asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el apellido." ValidationGroup="UsuarioInterno" />
                </div>
            </div>

            <div class="form-field">
                <label for="<%= txtCorreoElectronico.ClientID %>" data-i18n="AdminUsuarios_Correo">Correo electrónico *</label>
                <asp:TextBox ID="txtCorreoElectronico" runat="server" TextMode="Email" MaxLength="300" autocomplete="off"
                    placeholder="persona@artera.com" />
                <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreoElectronico"
                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el correo electrónico." ValidationGroup="UsuarioInterno" />
                <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreoElectronico"
                    Display="Dynamic" CssClass="field-error-text" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
                    ErrorMessage="El correo electrónico no tiene un formato válido." ValidationGroup="UsuarioInterno" />
            </div>

            <div class="internal-user-form-grid">
                <div class="form-field">
                    <label for="<%= ddlAreaInterna.ClientID %>" data-i18n="AdminUsuarios_Area">Área interna *</label>
                    <asp:DropDownList ID="ddlAreaInterna" runat="server" CssClass="form-select" DataTextField="NombreArea" DataValueField="IdAreaInterna" />
                    <asp:RequiredFieldValidator ID="rfvArea" runat="server" ControlToValidate="ddlAreaInterna" InitialValue=""
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Seleccioná un área interna." ValidationGroup="UsuarioInterno" />
                </div>

                <div class="form-field">
                    <label for="<%= ddlEstadoCuenta.ClientID %>" data-i18n="AdminUsuarios_Estado">Estado de la cuenta *</label>
                    <asp:DropDownList ID="ddlEstadoCuenta" runat="server" CssClass="form-select" />
                </div>
            </div>

            <div class="form-field">
                <label for="<%= ddlRolInterno.ClientID %>" data-i18n="AdminUsuarios_Rol">Rol asignado *</label>
                <asp:DropDownList ID="ddlRolInterno" runat="server" CssClass="form-select" DataTextField="NombreRol" DataValueField="IdRolInterno" />
                <asp:RequiredFieldValidator ID="rfvRol" runat="server" ControlToValidate="ddlRolInterno" InitialValue=""
                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="Seleccioná un rol interno." ValidationGroup="UsuarioInterno" />
                <span class="field-help" data-i18n="AdminUsuarios_AyudaRol">El usuario heredará todos los permisos configurados para este rol.</span>
            </div>

            <%-- Paso 15: permisos que va a tener según el rol elegido. --%>
            <div class="internal-role-permissions">
                <strong>Permisos que le da este rol</strong>
                <div id="internal-role-preview" class="internal-permission-tags" aria-live="polite"></div>
            </div>
            <asp:HiddenField ID="hdnPermisosPorRol" runat="server" />

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

            <div class="review-dialog-actions">
                <asp:LinkButton ID="lnkCancelarEdicion" runat="server" CssClass="text-link" CausesValidation="false"
                    Text="Cancelar" OnClick="lnkCancelarEdicion_Click" />
                <asp:Button ID="btnGuardarUsuario" runat="server" CssClass="button button-primary" Text="Guardar usuario"
                    ValidationGroup="UsuarioInterno" OnClick="btnGuardarUsuario_Click" />
            </div>
        </dialog>
    </asp:Panel>

    <%-- A7: detalle del usuario interno (A8 editar y A10 baja). --%>
    <asp:Panel ID="pnlDetalleUsuario" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog internal-dialog" open aria-labelledby="internal-user-detail-title">
            <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="review-dialog-close" CausesValidation="false" OnClick="lnkCerrarDetalle_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label">Detalle de usuario interno</span>
            <h2 id="internal-user-detail-title"><asp:Literal ID="litDetalleNombreCompleto" runat="server" Mode="Encode" /></h2>
            <p><asp:Label ID="lblDetalleEstado" runat="server" /></p>

            <asp:Panel ID="pnlDetalleMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litDetalleMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <dl class="internal-detail-grid">
                <div><dt>Nombre</dt><dd><asp:Literal ID="litDetalleNombre" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Apellido</dt><dd><asp:Literal ID="litDetalleApellido" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Correo electrónico</dt><dd><asp:Literal ID="litDetalleCorreo" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Área interna</dt><dd><asp:Literal ID="litDetalleArea" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Rol asignado</dt><dd><asp:Literal ID="litDetalleRol" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Estado</dt><dd><asp:Literal ID="litDetalleEstadoDato" runat="server" Mode="Encode" /></dd></div>
                <div class="internal-detail-wide"><dt>Alta, última modificación y baja</dt><dd><asp:Literal ID="litDetalleFechas" runat="server" Mode="Encode" /></dd></div>
            </dl>

            <h3 class="internal-detail-subtitle">Permisos vinculados <asp:Literal ID="litDetalleCantidadPermisos" runat="server" Mode="Encode" /></h3>
            <asp:Literal ID="litDetalleSinPermisos" runat="server" Visible="false" Text="&lt;p class=&quot;internal-detail-empty&quot;&gt;El rol no tiene permisos configurados.&lt;/p&gt;" />
            <div class="internal-permission-groups">
                <asp:Repeater ID="rptDetallePermisos" runat="server">
                    <ItemTemplate>
                        <div class="internal-permission-group">
                            <strong><%# Server.HtmlEncode(Convert.ToString(Eval("Grupo"))) %></strong>
                            <div class="internal-permission-tags"><%# Eval("Permisos") %></div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>

            <div class="review-dialog-actions internal-detail-actions">
                <asp:LinkButton ID="lnkVolverListado" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCerrarDetalle_Click">Volver al listado</asp:LinkButton>
                <asp:Button ID="btnBajaDesdeDetalle" runat="server" CssClass="button button-secondary" Text="Dar de baja usuario" CausesValidation="false" OnClick="btnBajaDesdeDetalle_Click" />
                <asp:Button ID="btnEditarDesdeDetalle" runat="server" CssClass="button button-primary" Text="Editar usuario" CausesValidation="false" OnClick="btnEditarDesdeDetalle_Click" />
            </div>

            <%-- A10 paso 2. --%>
            <asp:Panel ID="pnlConfirmarBaja" runat="server" Visible="false" CssClass="internal-confirm" role="alertdialog">
                <p><strong>¿Dar de baja a <asp:Literal ID="litConfirmarBajaNombre" runat="server" Mode="Encode" />?</strong>
                    Va a dejar de tener acceso al entorno administrativo, pero se conserva la información histórica asociada a sus intervenciones dentro del sistema.</p>
                <asp:Button ID="btnConfirmarBaja" runat="server" CssClass="button button-primary" Text="Sí, dar de baja" CausesValidation="false" OnClick="btnConfirmarBaja_Click" />
                <asp:LinkButton ID="lnkCancelarBaja" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarBaja_Click">Cancelar</asp:LinkButton>
            </asp:Panel>
        </dialog>
    </asp:Panel>

    <style>
        .internal-heading-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 0.6rem 1rem; }
        .internal-filter-row { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1.2fr) minmax(0, 1fr) auto; gap: 0.75rem; align-items: end; margin: 0.5rem 0 0.4rem; }
        .internal-filter-row .form-field { margin-bottom: 0; }
        .internal-filter-row input, .internal-filter-row select { width: 100%; box-sizing: border-box; padding: 0.7rem 0.8rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .internal-filter-actions { display: flex; align-items: center; gap: 0.75rem; }
        .internal-list-count { margin: 0.6rem 0 0; color: var(--color-text-muted); font-size: 0.8rem; }
        .review-dialog.internal-dialog { width: min(46rem, calc(100vw - 2rem)); max-height: calc(100vh - 2rem); }
        .internal-dialog .form-field input:not([type=checkbox]), .internal-dialog .form-field select { width: 100%; box-sizing: border-box; }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 0 0 18px; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .internal-detail-wide { grid-column: 1 / -1; }
        .internal-detail-subtitle { margin: 6px 0 10px; color: var(--color-primary-dark); font-size: 1rem; }
        .internal-detail-empty { color: var(--color-text-muted); }
        .internal-permission-groups { display: grid; gap: 10px; }
        .internal-permission-group { padding: 10px 12px; border: 1px solid var(--color-border); border-radius: 12px; background: var(--color-nude-light); }
        .internal-permission-group > strong { color: var(--color-primary-dark); font-size: 0.83rem; }
        .internal-permission-group .internal-permission-tags { margin-top: 0.4rem; }
        .internal-detail-actions { flex-wrap: wrap; align-items: center; }
        .internal-confirm { margin-top: 14px; padding: 14px 16px; border-radius: 12px; border: 2px solid var(--color-primary); }
        .internal-confirm p { margin: 0 0 12px; }
        .internal-confirm .text-link { margin-left: 12px; }
        body:has(.review-dialog-layer) { overflow: hidden; }
        @media (max-width: 760px) {
            .internal-filter-row { grid-template-columns: 1fr; }
            .internal-detail-grid { grid-template-columns: 1fr; }
            .review-dialog.internal-dialog { padding: 1.5rem 1.1rem; }
        }
    </style>

    <script>
        (function () {
            var oculto = document.getElementById('<%= hdnPermisosPorRol.ClientID %>');
            var combo = document.getElementById('<%= ddlRolInterno.ClientID %>');
            var destino = document.getElementById('internal-role-preview');
            if (!oculto || !combo || !destino) { return; }
            var mapa = {};
            try { mapa = JSON.parse(oculto.value || '{}'); } catch (e) { mapa = {}; }
            function mostrar() {
                while (destino.firstChild) { destino.removeChild(destino.firstChild); }
                var permisos = mapa[combo.value];
                var texto;
                if (!combo.value) { texto = 'Elegí un rol para ver sus permisos.'; }
                else if (!permisos || permisos.length === 0) { texto = 'Este rol todavía no tiene permisos configurados: elegí otro o configuralo en Gestionar roles.'; }
                if (texto) { var p = document.createElement('em'); p.textContent = texto; destino.appendChild(p); return; }
                permisos.forEach(function (nombre) { var s = document.createElement('span'); s.textContent = nombre; destino.appendChild(s); });
            }
            combo.addEventListener('change', mostrar);
            mostrar();
        })();
    </script>
</asp:Content>
