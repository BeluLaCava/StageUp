<%@ Page Title="Gestión de FAQ | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionFaq.aspx.cs" Inherits="StageUp.UI.Interno.GestionFaq" %>

<asp:Content ID="GestionFaqContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page faq-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Centro de ayuda</span>
            <h1>Gestión de preguntas frecuentes</h1>
            <p>Administrá las consultas que se muestran en la sección de ayuda pública de StageUp.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <div class="faq-admin-summary" aria-label="Resumen de preguntas frecuentes">
                <article>
                    <span>Activas</span>
                    <strong><asp:Literal ID="litCantidadActivas" runat="server" /></strong>
                </article>
                <article>
                    <span>En borrador</span>
                    <strong><asp:Literal ID="litCantidadBorrador" runat="server" /></strong>
                </article>
                <article>
                    <span>Última edición</span>
                    <strong><asp:Literal ID="litUltimaEdicion" runat="server" /></strong>
                </article>
            </div>

            <div class="faq-admin-layout">
                <asp:Panel ID="pnlFormularioFaq" runat="server" CssClass="auth-card admin-editor-card faq-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Contenido</span>
                        <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva pregunta" /></h2>
                        <p>Los cambios se reflejan al instante en el centro de ayuda público.</p>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtPregunta.ClientID %>">Pregunta *</label>
                        <asp:TextBox ID="txtPregunta" runat="server" TextMode="SingleLine" MaxLength="300"
                            placeholder="Ej: ¿Cómo puedo reservar un espacio en StageUp?" />
                        <asp:RequiredFieldValidator ID="rfvPregunta" runat="server" ControlToValidate="txtPregunta"
                            ValidationGroup="Faq" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la pregunta." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtRespuesta.ClientID %>">Respuesta *</label>
                        <asp:TextBox ID="txtRespuesta" runat="server" TextMode="MultiLine" Rows="7"
                            placeholder="Escribí la respuesta que va a ver el público." />
                        <asp:RequiredFieldValidator ID="rfvRespuesta" runat="server" ControlToValidate="txtRespuesta"
                            ValidationGroup="Faq" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la respuesta." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtOrden.ClientID %>">Orden</label>
                        <asp:TextBox ID="txtOrden" runat="server" TextMode="Number" placeholder="Ej: 6" />
                        <small class="field-help">Las preguntas se muestran en el centro de ayuda ordenadas de menor a mayor.</small>
                        <asp:RequiredFieldValidator ID="rfvOrden" runat="server" ControlToValidate="txtOrden"
                            ValidationGroup="Faq" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el orden." />
                    </div>

                    <div class="faq-options">
                        <label class="newsletter-check-option">
                            <asp:CheckBox ID="chkActiva" runat="server" Checked="true" />
                            <span>
                                <strong>Mostrar en el centro de ayuda</strong>
                                <small>Si está activa, la pregunta queda visible para usuarios y visitantes.</small>
                            </span>
                        </label>
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnGuardarFaq" runat="server" CssClass="button button-primary" Text="Crear pregunta"
                            ValidationGroup="Faq" OnClick="btnGuardarFaq_Click" />
                        <asp:Button ID="btnGuardarBorradorFaq" runat="server" CssClass="button button-secondary" Text="Guardar como borrador"
                            ValidationGroup="Faq" OnClick="btnGuardarBorradorFaq_Click" />
                        <asp:LinkButton ID="lnkCancelarFaq" runat="server" CssClass="text-link" Text="Cancelar edición"
                            CausesValidation="false" Visible="false" OnClick="lnkCancelarFaq_Click" />
                    </div>
                </asp:Panel>

                <div class="faq-admin-side">
                    <div class="auth-card admin-workspace-card faq-preview-card">
                        <div class="auth-card-header admin-workspace-header">
                            <div>
                                <span class="admin-card-eyebrow">Vista previa</span>
                                <h2>Centro de ayuda</h2>
                            </div>
                            <span class="admin-workspace-hint">Así se ve publicada</span>
                        </div>

                        <asp:Panel ID="pnlSinPreview" runat="server" CssClass="admin-empty-hint" Visible="false">
                            Todavía no hay preguntas activas para mostrar en el centro de ayuda.
                        </asp:Panel>

                        <div class="faq-help-preview">
                            <asp:Repeater ID="rptFaqPreview" runat="server">
                                <ItemTemplate>
                                    <article class="faq-preview-item">
                                        <span class="faq-preview-number"><%#: Eval("Orden", "{0:00}") %></span>
                                        <div>
                                            <h3><%#: Eval("Pregunta") %></h3>
                                            <p><%#: Eval("Respuesta") %></p>
                                        </div>
                                    </article>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>

                    <div class="auth-card admin-list-card faq-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Directorio</span>
                            <h2>Preguntas cargadas</h2>
                        </div>

                        <asp:Panel ID="pnlSinFaqs" runat="server" CssClass="admin-empty-hint" Visible="false">
                            Todavía no cargaste ninguna pregunta frecuente. Creá la primera desde el formulario.
                        </asp:Panel>

                        <div class="faq-admin-list">
                            <asp:Repeater ID="rptFaq" runat="server" OnItemCommand="rptFaq_ItemCommand">
                                <ItemTemplate>
                                    <article class='<%# ObtenerClaseFilaFaq((StageUp.BE.Entidades.Faq)Container.DataItem) %>'>
                                        <div class="faq-admin-row-main">
                                            <span class="faq-order-badge"><%#: Eval("Orden", "{0:00}") %></span>
                                            <div>
                                                <h3><%#: Eval("Pregunta") %></h3>
                                                <p><%#: Eval("Respuesta") %></p>
                                                <span class='<%# ObtenerClaseEstadoFaq((StageUp.BE.Entidades.Faq)Container.DataItem) %>'>
                                                    <%#: ObtenerTextoEstadoFaq((StageUp.BE.Entidades.Faq)Container.DataItem) %>
                                                </span>
                                            </div>
                                        </div>
                                        <div class="space-row-actions">
                                            <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                                CommandName="Editar" CommandArgument='<%# Eval("IdFaq") %>' Text="Editar" />
                                            <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                                CommandName="Baja" CommandArgument='<%# Eval("IdFaq") %>' Text="Dar de baja"
                                                Visible='<%# Convert.ToBoolean(Eval("Activo")) %>'
                                                OnClientClick="return confirm('¿Seguro que querés dar de baja esta pregunta? Deja de mostrarse en el centro de ayuda.');" />
                                            <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                CommandName="Activar" CommandArgument='<%# Eval("IdFaq") %>' Text="Activar"
                                                Visible='<%# !Convert.ToBoolean(Eval("Activo")) %>' />
                                        </div>
                                    </article>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
