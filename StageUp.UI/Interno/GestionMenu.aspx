<%@ Page Title="Gestión del menú | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionMenu.aspx.cs" Inherits="StageUp.UI.Interno.GestionMenu" %>

<asp:Content ID="GestionMenuContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .menu-admin-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); flex-wrap: wrap; }
        .menu-admin-row.inactiva { opacity: 0.6; }
        .menu-admin-meta { color: var(--color-text-muted, #6b7280); font-size: 0.85em; }
        .menu-admin-orden { display: inline-block; min-width: 2.4em; padding: 2px 8px; margin-right: 8px; border-radius: 999px; background: #f3dfd4; color: #7a0c20; font-weight: bold; text-align: center; }
        .menu-admin-modulo { display: inline-block; padding: 2px 10px; border-radius: 999px; background: #eef2ff; color: #1e3a8a; font-size: 0.78em; font-weight: bold; }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Gestión del menú</h1>
            <p>Administrá las opciones del menú del panel interno: qué texto muestran, a qué página llevan, en qué módulo y orden aparecen, y qué permiso hace falta para verlas.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="auth-card admin-editor-card">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Opción de menú</span>
                    <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva opción" /></h2>
                    <p>Cada usuario interno ve solo las opciones activas cuyo permiso tiene su rol.</p>
                </div>

                <div class="form-field">
                    <label for="<%= txtTexto.ClientID %>">Texto *</label>
                    <asp:TextBox ID="txtTexto" runat="server" MaxLength="100" placeholder="Ej: Gestión de FAQ" />
                    <asp:RequiredFieldValidator ID="rfvTexto" runat="server" ControlToValidate="txtTexto"
                        ValidationGroup="Menu" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el texto de la opción." />
                </div>

                <div class="form-field">
                    <label for="<%= txtUrl.ClientID %>">URL *</label>
                    <asp:TextBox ID="txtUrl" runat="server" MaxLength="300" placeholder="Ej: ~/Interno/GestionFaq.aspx" />
                    <small class="field-help">Tiene que ser una página de StageUp y empezar con ~/</small>
                    <asp:RequiredFieldValidator ID="rfvUrl" runat="server" ControlToValidate="txtUrl"
                        ValidationGroup="Menu" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la URL." />
                </div>

                <div class="form-field">
                    <label for="<%= txtModulo.ClientID %>">Módulo *</label>
                    <asp:TextBox ID="txtModulo" runat="server" MaxLength="100" placeholder="Ej: Administración" />
                    <small class="field-help">Las opciones consecutivas del mismo módulo se agrupan en el menú.</small>
                    <asp:RequiredFieldValidator ID="rfvModulo" runat="server" ControlToValidate="txtModulo"
                        ValidationGroup="Menu" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el módulo." />
                </div>

                <div class="form-field">
                    <label for="<%= ddlPermiso.ClientID %>">Permiso requerido *</label>
                    <asp:DropDownList ID="ddlPermiso" runat="server" />
                </div>

                <div class="form-field">
                    <label for="<%= txtDescripcion.ClientID %>">Descripción</label>
                    <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="2" MaxLength="300"
                        placeholder="Se muestra en las tarjetas del panel administrativo." />
                </div>

                <label class="newsletter-check-option">
                    <asp:CheckBox ID="chkActiva" runat="server" Checked="true" />
                    <span><strong>Mostrar en el menú</strong></span>
                </label>

                <div class="form-actions">
                    <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Agregar opción"
                        ValidationGroup="Menu" OnClick="btnGuardar_Click" />
                    <asp:LinkButton ID="lnkCancelar" runat="server" CssClass="text-link" Text="Cancelar edición"
                        CausesValidation="false" Visible="false" OnClick="lnkCancelar_Click" />
                </div>
            </asp:Panel>

            <div class="auth-card admin-list-card">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Menú actual</span>
                    <h2>Opciones cargadas</h2>
                    <p>Se muestran en el orden en que aparecen en el menú.</p>
                </div>

                <asp:Panel ID="pnlSinOpciones" runat="server" CssClass="admin-empty-hint" Visible="false">
                    Todavía no hay opciones de menú cargadas.
                </asp:Panel>

                <asp:Repeater ID="rptOpciones" runat="server" OnItemCommand="rptOpciones_ItemCommand">
                    <ItemTemplate>
                        <div class='<%# "menu-admin-row" + (Convert.ToBoolean(Eval("Activo")) ? "" : " inactiva") %>'>
                            <div>
                                <span class="menu-admin-orden"><%# Container.ItemIndex + 1 %></span>
                                <strong><%#: Eval("Texto") %></strong>
                                <span class="menu-admin-modulo"><%#: Eval("Modulo") %></span>
                                <div class="menu-admin-meta">
                                    <%#: Eval("Url") %> · Permiso: <%#: Eval("NombrePermiso") %> (<%#: Eval("CodigoPermiso") %>)
                                    · <%#: Convert.ToBoolean(Eval("Activo")) ? "Activa" : "Dada de baja" %>
                                </div>
                            </div>
                            <div class="space-row-actions">
                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                    CommandName="Subir" CommandArgument='<%# Eval("IdOpcionMenu") %>' Text="↑ Subir" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                    CommandName="Bajar" CommandArgument='<%# Eval("IdOpcionMenu") %>' Text="↓ Bajar" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                    CommandName="Editar" CommandArgument='<%# Eval("IdOpcionMenu") %>' Text="Editar" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                    CommandName="Baja" CommandArgument='<%# Eval("IdOpcionMenu") %>' Text="Dar de baja"
                                    Visible='<%# Convert.ToBoolean(Eval("Activo")) %>'
                                    OnClientClick="return confirm('¿Seguro que querés dar de baja esta opción? Deja de mostrarse en el menú.');" />
                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                    CommandName="Activar" CommandArgument='<%# Eval("IdOpcionMenu") %>' Text="Reactivar"
                                    Visible='<%# !Convert.ToBoolean(Eval("Activo")) %>' />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>
</asp:Content>
