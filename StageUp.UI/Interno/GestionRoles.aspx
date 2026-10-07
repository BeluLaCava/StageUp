<%@ Page Title="Roles y permisos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionRoles.aspx.cs" Inherits="StageUp.UI.Interno.GestionRoles" %>

<asp:Content ID="GestionRolesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Roles y permisos</h1>
            <p>Los roles agrupan los permisos según las funciones de cada persona en Artera. Los usuarios internos acceden solo a las secciones que habilitan los permisos de su rol.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- A11 pasos 2 a 5. --%>
            <div class="auth-card admin-list-card">
                <div class="auth-card-header internal-users-list-heading">
                    <div>
                        <span class="admin-card-eyebrow" data-i18n="AdminUI_Directorio">Directorio</span>
                        <h2>Roles internos</h2>
                    </div>
                    <div class="internal-heading-actions">
                        <a id="lnkUsuariosInternos" runat="server" class="admin-action-link" href="~/Interno/GestionUsuariosInternos.aspx">Usuarios internos</a>
                        <a class="admin-action-link" href="~/Interno/GestionGruposPermisos.aspx" runat="server">Organizar grupos de permisos</a>
                        <asp:Button ID="btnAgregarRol" runat="server" CssClass="button button-primary" Text="＋ Agregar rol"
                            CausesValidation="false" OnClick="btnAgregarRol_Click" />
                    </div>
                </div>

                <div class="internal-filter-row internal-filter-row-compact">
                    <div class="form-field">
                        <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                        <asp:DropDownList ID="ddlFiltroEstado" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlFiltroEstado_SelectedIndexChanged">
                            <asp:ListItem Text="Activos" Value="Activos" />
                            <asp:ListItem Text="Dados de baja" Value="Inactivos" />
                            <asp:ListItem Text="Todos" Value="Todos" />
                        </asp:DropDownList>
                    </div>
                    <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>
                </div>

                <asp:Panel ID="pnlSinRoles" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No hay roles para mostrar</h3>
                    <p><asp:Literal ID="litSinRoles" runat="server" Mode="Encode" /></p>
                </asp:Panel>

                <asp:Repeater ID="rptRoles" runat="server" OnItemCommand="rptRoles_ItemCommand">
                    <ItemTemplate>
                        <article class="space-row admin-list-row role-list-row internal-user-row <%# Convert.ToBoolean(Eval("Activo")) ? string.Empty : "internal-user-row-inactive" %>">
                            <div class="space-row-info">
                                <span class="admin-row-icon admin-row-icon-role" aria-hidden="true"></span>
                                <div class="internal-user-main">
                                    <div class="internal-user-name-line">
                                        <h3><%# Server.HtmlEncode(Convert.ToString(Eval("NombreRol"))) %></h3>
                                        <span class="admin-status-badge <%# Convert.ToBoolean(Eval("Activo")) ? "admin-status-badge-active" : "admin-status-badge-inactive" %>"><%# Server.HtmlEncode(Convert.ToString(Eval("EstadoRol"))) %></span>
                                    </div>
                                    <p class="internal-user-email"><%# Server.HtmlEncode(Convert.ToString(Eval("Descripcion") ?? "Sin descripción")) %></p>
                                    <div class="internal-user-meta">
                                        <span><strong>Área:</strong> <%# Server.HtmlEncode(Convert.ToString(Eval("NombreArea") ?? "Sin área específica")) %></span>
                                        <span><strong>Permisos asignados:</strong> <%# Eval("CantidadComponentes") %></span>
                                        <span><strong>Usuarios activos:</strong> <%# Eval("CantidadUsuariosActivos") %></span>
                                    </div>
                                </div>
                            </div>
                            <div class="space-row-actions">
                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                    CommandName="Ver" CommandArgument='<%# Eval("IdRolInterno") %>' Text="Ver detalle" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false" Visible='<%# Convert.ToBoolean(Eval("Activo")) %>'
                                    CommandName="Editar" CommandArgument='<%# Eval("IdRolInterno") %>' Text="Editar" />
                            </div>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <%-- A11 pasos 6 a 9 (alta) y A12 pasos 3 a 6 (edición). --%>
    <asp:Panel ID="pnlFormularioRol" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog internal-dialog" open aria-labelledby="internal-role-form-title">
            <asp:LinkButton ID="lnkCerrarFormulario" runat="server" CssClass="review-dialog-close" CausesValidation="false" OnClick="lnkCancelarEdicionRol_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label" data-i18n="AdminUI_Configuracion">Configuración</span>
            <h2 id="internal-role-form-title"><asp:Literal ID="litTituloFormulario" runat="server" Mode="Encode" Text="Nuevo rol" /></h2>
            <p>El nombre del rol es obligatorio y tiene que tener al menos un permiso asignado.</p>

            <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" CssClass="form-message form-message-error" role="alert">
                <asp:Literal ID="litMensajeFormulario" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="internal-user-form-grid">
                <div class="form-field">
                    <label for="<%= txtNombreRol.ClientID %>">Nombre del rol *</label>
                    <asp:TextBox ID="txtNombreRol" runat="server" MaxLength="200" placeholder="Ej: Soporte, Moderación de contenido" />
                    <asp:RequiredFieldValidator ID="rfvNombreRol" runat="server" ControlToValidate="txtNombreRol"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre del rol." ValidationGroup="Rol" />
                </div>

                <div class="form-field">
                    <label for="<%= ddlAreaRol.ClientID %>">Área asociada</label>
                    <asp:DropDownList ID="ddlAreaRol" runat="server" DataTextField="NombreArea" DataValueField="IdAreaInterna" />
                </div>
            </div>

            <div class="form-field">
                <label for="<%= txtDescripcionRol.ClientID %>">Descripción</label>
                <asp:TextBox ID="txtDescripcionRol" runat="server" TextMode="MultiLine" Rows="2" MaxLength="510" placeholder="Para qué funciones se usa este rol." />
            </div>

            <div class="internal-role-permissions">
                <strong>Permisos disponibles *</strong>
                <p class="field-help">Tildá un grupo entero para habilitar todo lo que tiene adentro (incluido lo que se agregue después) o tildá permisos sueltos.</p>
                <div class="permission-tree-wrapper internal-permission-tree">
                    <asp:TreeView ID="tvPermisos" runat="server" ShowCheckBoxes="All" ShowLines="true" CssClass="permission-tree">
                        <NodeStyle Font-Size="14px" ForeColor="#3E2925" NodeSpacing="4px" VerticalPadding="4px" />
                        <ParentNodeStyle Font-Bold="true" ForeColor="#4D0B17" />
                        <HoverNodeStyle ForeColor="#6D1021" />
                    </asp:TreeView>
                </div>
            </div>

            <div class="review-dialog-actions">
                <asp:LinkButton ID="lnkCancelarEdicionRol" runat="server" CssClass="text-link" CausesValidation="false"
                    Text="Cancelar" OnClick="lnkCancelarEdicionRol_Click" />
                <asp:Button ID="btnGuardarRol" runat="server" CssClass="button button-primary" Text="Guardar rol"
                    ValidationGroup="Rol" OnClick="btnGuardarRol_Click" />
            </div>
        </dialog>
    </asp:Panel>

    <%-- A12 paso 2: detalle del rol. --%>
    <asp:Panel ID="pnlDetalleRol" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog internal-dialog" open aria-labelledby="internal-role-detail-title">
            <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="review-dialog-close" CausesValidation="false" OnClick="lnkCerrarDetalle_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label">Detalle del rol</span>
            <h2 id="internal-role-detail-title"><asp:Literal ID="litDetalleNombre" runat="server" Mode="Encode" /></h2>
            <p><asp:Label ID="lblDetalleEstado" runat="server" /></p>

            <asp:Panel ID="pnlDetalleMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litDetalleMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <dl class="internal-detail-grid">
                <div class="internal-detail-wide"><dt>Descripción</dt><dd><asp:Literal ID="litDetalleDescripcion" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Área asociada</dt><dd><asp:Literal ID="litDetalleArea" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Estado</dt><dd><asp:Literal ID="litDetalleEstadoDato" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Alta y última modificación</dt><dd><asp:Literal ID="litDetalleFechas" runat="server" Mode="Encode" /></dd></div>
                <div class="internal-detail-wide"><dt>Asignado en el rol</dt><dd><asp:Literal ID="litDetalleAsignados" runat="server" Mode="Encode" /></dd></div>
            </dl>

            <h3 class="internal-detail-subtitle">Permisos que da el rol <asp:Literal ID="litDetalleCantidadPermisos" runat="server" Mode="Encode" /></h3>
            <asp:Literal ID="litDetalleSinPermisos" runat="server" Visible="false" Text="&lt;p class=&quot;internal-detail-empty&quot;&gt;El rol no tiene permisos asignados.&lt;/p&gt;" />
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

            <h3 class="internal-detail-subtitle">Usuarios internos con este rol <asp:Literal ID="litDetalleCantidadUsuarios" runat="server" Mode="Encode" /></h3>
            <asp:Literal ID="litDetalleSinUsuarios" runat="server" Visible="false" Text="&lt;p class=&quot;internal-detail-empty&quot;&gt;Ningún usuario interno tiene este rol.&lt;/p&gt;" />
            <ul class="internal-detail-people">
                <asp:Repeater ID="rptDetalleUsuarios" runat="server">
                    <ItemTemplate>
                        <li>
                            <a href='<%# ResolveUrl("~/Interno/GestionUsuariosInternos.aspx?ver=" + Eval("IdUsuarioInterno")) %>'><%# Server.HtmlEncode(Eval("Nombre") + " " + Eval("Apellido")) %></a>
                            <span><%# Server.HtmlEncode(Convert.ToString(Eval("CorreoElectronico"))) %> · <%# Server.HtmlEncode(Convert.ToString(Eval("EstadoCuenta"))) %></span>
                        </li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>

            <div class="review-dialog-actions internal-detail-actions">
                <asp:LinkButton ID="lnkVolverListado" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCerrarDetalle_Click">Volver al listado</asp:LinkButton>
                <asp:Button ID="btnBajaDesdeDetalle" runat="server" CssClass="button button-secondary" Text="Dar de baja rol" CausesValidation="false" OnClick="btnBajaDesdeDetalle_Click" />
                <asp:Button ID="btnEditarDesdeDetalle" runat="server" CssClass="button button-primary" Text="Editar rol" CausesValidation="false" OnClick="btnEditarDesdeDetalle_Click" />
            </div>

            <asp:Panel ID="pnlConfirmarBaja" runat="server" Visible="false" CssClass="internal-confirm" role="alertdialog">
                <p><strong>¿Dar de baja el rol <asp:Literal ID="litConfirmarBajaNombre" runat="server" Mode="Encode" />?</strong>
                    Ya no se va a poder asignar a usuarios internos. Solo se puede dar de baja si ningún usuario activo lo tiene; se conserva su historial.</p>
                <asp:Button ID="btnConfirmarBaja" runat="server" CssClass="button button-primary" Text="Sí, dar de baja" CausesValidation="false" OnClick="btnConfirmarBaja_Click" />
                <asp:LinkButton ID="lnkCancelarBaja" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarBaja_Click">Cancelar</asp:LinkButton>
            </asp:Panel>
        </dialog>
    </asp:Panel>

    <style>
        .internal-heading-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 0.6rem 1rem; }
        .internal-filter-row { display: flex; flex-wrap: wrap; gap: 0.75rem 1.25rem; align-items: end; margin: 0.5rem 0 0.8rem; }
        .internal-filter-row .form-field { margin-bottom: 0; min-width: 12rem; }
        .internal-filter-row select, .internal-dialog .form-field input, .internal-dialog .form-field select, .internal-dialog .form-field textarea { width: 100%; box-sizing: border-box; padding: 0.7rem 0.8rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .internal-list-count { margin: 0 0 0.6rem; color: var(--color-text-muted); font-size: 0.8rem; }
        .review-dialog.internal-dialog { width: min(46rem, calc(100vw - 2rem)); max-height: calc(100vh - 2rem); }
        .internal-permission-tree { max-height: 18rem; overflow: auto; margin-top: 0.5rem; padding: 0.5rem 0.75rem; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 0.75rem; }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 0 0 18px; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .internal-detail-wide { grid-column: 1 / -1; }
        .internal-detail-subtitle { margin: 6px 0 10px; color: var(--color-primary-dark); font-size: 1rem; }
        .internal-detail-empty { color: var(--color-text-muted); }
        .internal-permission-groups { display: grid; gap: 10px; margin-bottom: 14px; }
        .internal-permission-group { padding: 10px 12px; border: 1px solid var(--color-border); border-radius: 12px; background: var(--color-nude-light); }
        .internal-permission-group > strong { color: var(--color-primary-dark); font-size: 0.83rem; }
        .internal-permission-group .internal-permission-tags { margin-top: 0.4rem; }
        .internal-detail-people { list-style: none; margin: 0; padding: 0; display: grid; gap: 6px; }
        .internal-detail-people li { display: flex; flex-wrap: wrap; gap: 4px 10px; padding: 8px 12px; border: 1px solid var(--color-border); border-radius: 10px; }
        .internal-detail-people span { color: var(--color-text-muted); font-size: 0.85rem; }
        .internal-detail-actions { flex-wrap: wrap; align-items: center; }
        .internal-confirm { margin-top: 14px; padding: 14px 16px; border-radius: 12px; border: 2px solid var(--color-primary); }
        .internal-confirm p { margin: 0 0 12px; }
        .internal-confirm .text-link { margin-left: 12px; }
        body:has(.review-dialog-layer) { overflow: hidden; }
        @media (max-width: 760px) {
            .internal-detail-grid { grid-template-columns: 1fr; }
            .review-dialog.internal-dialog { padding: 1.5rem 1.1rem; }
        }
    </style>
</asp:Content>
