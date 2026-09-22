<%@ Page Title="Gestión de FAQ | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionFaq.aspx.cs" Inherits="StageUp.UI.Interno.GestionFaq" %>

<asp:Content ID="GestionFaqContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page faq-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Centro de ayuda</span>
            <h1>Gestión de preguntas frecuentes</h1>
            <p>Administrá las consultas que se muestran en la sección de ayuda pública de StageUp.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <div class="faq-admin-summary" aria-label="Resumen de preguntas frecuentes">
                <article>
                    <span>Activas</span>
                    <strong>5</strong>
                </article>
                <article>
                    <span>En borrador</span>
                    <strong>2</strong>
                </article>
                <article>
                    <span>Última edición</span>
                    <strong>Hoy</strong>
                </article>
            </div>

            <div class="faq-admin-layout">
                <asp:Panel ID="pnlFormularioFaq" runat="server" CssClass="auth-card admin-editor-card faq-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Contenido</span>
                        <h2>Nueva pregunta</h2>
                        <p>Formulario preparado para conectar con el ABM real de FAQ.</p>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtPregunta.ClientID %>">Pregunta *</label>
                        <asp:TextBox ID="txtPregunta" runat="server" TextMode="SingleLine" MaxLength="300"
                            Text="¿Cómo puedo reservar un espacio en StageUp?" />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtRespuesta.ClientID %>">Respuesta *</label>
                        <asp:TextBox ID="txtRespuesta" runat="server" TextMode="MultiLine" Rows="7"
                            Text="Primero explorá el catálogo, elegí el espacio que mejor se adapte a tu actividad y seleccioná un horario disponible. Para confirmar la solicitud vas a necesitar iniciar sesión o crear una cuenta." />
                    </div>

                    <div class="faq-form-grid">
                        <div class="form-field">
                            <label for="<%= ddlTema.ClientID %>">Tema</label>
                            <asp:DropDownList ID="ddlTema" runat="server" CssClass="form-select">
                                <asp:ListItem Text="General" Value="General" />
                                <asp:ListItem Text="Reservas" Value="Reservas" Selected="True" />
                                <asp:ListItem Text="Gestores" Value="Gestores" />
                                <asp:ListItem Text="Cuenta y seguridad" Value="Cuenta" />
                            </asp:DropDownList>
                        </div>

                        <div class="form-field">
                            <label for="<%= txtOrden.ClientID %>">Orden</label>
                            <asp:TextBox ID="txtOrden" runat="server" TextMode="Number" Text="6" />
                        </div>
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
                        <asp:Button ID="btnGuardarFaq" runat="server" CssClass="button button-primary" Text="Guardar pregunta" CausesValidation="false" />
                        <asp:Button ID="btnGuardarBorradorFaq" runat="server" CssClass="button button-secondary" Text="Guardar como borrador" CausesValidation="false" />
                        <asp:LinkButton ID="lnkCancelarFaq" runat="server" CssClass="text-link" Text="Cancelar edición" CausesValidation="false" />
                    </div>
                </asp:Panel>

                <div class="faq-admin-side">
                    <div class="auth-card admin-workspace-card faq-preview-card">
                        <div class="auth-card-header admin-workspace-header">
                            <div>
                                <span class="admin-card-eyebrow">Vista previa</span>
                                <h2>Centro de ayuda</h2>
                            </div>
                            <span class="admin-workspace-hint">Así se verá publicada</span>
                        </div>

                        <div class="faq-help-preview">
                            <article class="faq-preview-item is-open">
                                <span class="faq-preview-number">01</span>
                                <div>
                                    <h3>¿Cómo puedo reservar un espacio en StageUp?</h3>
                                    <p>Primero explorá el catálogo, elegí el espacio que mejor se adapte a tu actividad y seleccioná un horario disponible.</p>
                                </div>
                            </article>
                            <article class="faq-preview-item">
                                <span class="faq-preview-number">02</span>
                                <div>
                                    <h3>¿Qué significa ser gestor?</h3>
                                    <p>Un gestor publica espacios artísticos, configura disponibilidad y administra solicitudes recibidas.</p>
                                </div>
                            </article>
                        </div>
                    </div>

                    <div class="auth-card admin-list-card faq-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Directorio</span>
                            <h2>Preguntas cargadas</h2>
                        </div>

                        <div class="faq-admin-list">
                            <article class="faq-admin-row">
                                <div class="faq-admin-row-main">
                                    <span class="faq-order-badge">01</span>
                                    <div>
                                        <h3>¿Qué es StageUp?</h3>
                                        <p>StageUp conecta personas que necesitan espacios artísticos con gestores que desean ofrecerlos temporalmente.</p>
                                        <span class="admin-status-badge admin-status-badge-active">Activa</span>
                                    </div>
                                </div>
                                <div class="space-row-actions">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" Text="Editar" CausesValidation="false" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" Text="Dar de baja" CausesValidation="false" />
                                </div>
                            </article>

                            <article class="faq-admin-row">
                                <div class="faq-admin-row-main">
                                    <span class="faq-order-badge">02</span>
                                    <div>
                                        <h3>¿Cómo puedo buscar un espacio?</h3>
                                        <p>La exploración pública permite buscar por palabras clave y combinar filtros generales con características artísticas.</p>
                                        <span class="admin-status-badge admin-status-badge-active">Activa</span>
                                    </div>
                                </div>
                                <div class="space-row-actions">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" Text="Editar" CausesValidation="false" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" Text="Dar de baja" CausesValidation="false" />
                                </div>
                            </article>

                            <article class="faq-admin-row">
                                <div class="faq-admin-row-main">
                                    <span class="faq-order-badge">03</span>
                                    <div>
                                        <h3>¿Cómo funcionan las reservas?</h3>
                                        <p>Un usuario autenticado podrá elegir día y horario disponible para enviar una solicitud al gestor.</p>
                                        <span class="admin-status-badge admin-status-badge-active">Activa</span>
                                    </div>
                                </div>
                                <div class="space-row-actions">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" Text="Editar" CausesValidation="false" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" Text="Dar de baja" CausesValidation="false" />
                                </div>
                            </article>

                            <article class="faq-admin-row faq-admin-row-muted">
                                <div class="faq-admin-row-main">
                                    <span class="faq-order-badge">04</span>
                                    <div>
                                        <h3>¿Cómo recibo novedades por correo?</h3>
                                        <p>Pregunta preparada para completar cuando se conecte el módulo de newsletter.</p>
                                        <span class="admin-status-badge admin-status-badge-inactive">Borrador</span>
                                    </div>
                                </div>
                                <div class="space-row-actions">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" Text="Editar" CausesValidation="false" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" Text="Activar" CausesValidation="false" />
                                </div>
                            </article>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
