<%@ Page Title="Novedades y newsletter | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionNovedades.aspx.cs" Inherits="StageUp.UI.Interno.GestionNovedades" %>

<asp:Content ID="GestionNovedadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page newsletter-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comunicación</span>
            <h1>Novedades y newsletter</h1>
            <p>Administrá noticias públicas de StageUp y prepará envíos por correo para usuarios activos.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <div class="newsletter-dashboard-strip" aria-label="Resumen de newsletters">
                <article>
                    <span>Borradores</span>
                    <strong>2</strong>
                </article>
                <article>
                    <span>Publicadas</span>
                    <strong>4</strong>
                </article>
                <article>
                    <span>Envíos programados</span>
                    <strong>1</strong>
                </article>
                <article>
                    <span>Último envío</span>
                    <strong>18/09</strong>
                </article>
            </div>

            <div class="newsletter-admin-layout">
                <asp:Panel ID="pnlFormularioNewsletter" runat="server" CssClass="auth-card admin-editor-card newsletter-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Contenido</span>
                        <h2>Nueva novedad</h2>
                        <p>Vista preparada para conectar con el ABM real de novedades y newsletter.</p>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtTitulo.ClientID %>">Título *</label>
                        <asp:TextBox ID="txtTitulo" runat="server" TextMode="SingleLine" MaxLength="180"
                            Text="Nuevos espacios para crear esta temporada" />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtResumen.ClientID %>">Resumen para listado y correo *</label>
                        <asp:TextBox ID="txtResumen" runat="server" TextMode="MultiLine" Rows="3" MaxLength="300"
                            Text="Sumamos salas, estudios y teatros con disponibilidad actualizada para que cada artista encuentre un lugar acorde a su actividad." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtContenido.ClientID %>">Contenido de la novedad *</label>
                        <asp:TextBox ID="txtContenido" runat="server" TextMode="MultiLine" Rows="8"
                            Text="Durante septiembre incorporamos nuevos espacios publicados por gestores verificados. La actualización incluye salas para ensayos, estudios fotográficos y teatros de distintos formatos." />
                    </div>

                    <div class="newsletter-form-grid">
                        <div class="form-field">
                            <label for="<%= ddlCategoria.ClientID %>">Categoría</label>
                            <asp:DropDownList ID="ddlCategoria" runat="server" CssClass="form-select">
                                <asp:ListItem Text="Novedad institucional" Value="Institucional" />
                                <asp:ListItem Text="Nuevos espacios" Value="Catalogo" Selected="True" />
                                <asp:ListItem Text="Consejos para usuarios" Value="Consejos" />
                                <asp:ListItem Text="Comunidad artística" Value="Comunidad" />
                            </asp:DropDownList>
                        </div>

                        <div class="form-field">
                            <label for="<%= txtFechaPublicacion.ClientID %>">Fecha de publicación</label>
                            <asp:TextBox ID="txtFechaPublicacion" runat="server" TextMode="Date" />
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtUrlImagen.ClientID %>">Imagen destacada</label>
                        <asp:TextBox ID="txtUrlImagen" runat="server" TextMode="SingleLine" MaxLength="500"
                            placeholder="~/Content/Uploads/Novedades/imagen.jpg" />
                    </div>

                    <div class="newsletter-options">
                        <label class="newsletter-check-option">
                            <asp:CheckBox ID="chkPublicar" runat="server" Checked="true" />
                            <span>
                                <strong>Publicar en página de novedades</strong>
                                <small>La noticia queda visible para usuarios y visitantes.</small>
                            </span>
                        </label>

                        <label class="newsletter-check-option">
                            <asp:CheckBox ID="chkEnviarMail" runat="server" Checked="true" />
                            <span>
                                <strong>Enviar también por mail</strong>
                                <small>Usa la plantilla StageUp preparada en ServicioCorreo.</small>
                            </span>
                        </label>
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlDestinatarios.ClientID %>">Destinatarios</label>
                        <asp:DropDownList ID="ddlDestinatarios" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Usuarios activos" Value="Activos" Selected="True" />
                            <asp:ListItem Text="Gestores de espacios" Value="Gestores" />
                            <asp:ListItem Text="Solicitantes" Value="Solicitantes" />
                            <asp:ListItem Text="Todos los registrados" Value="Todos" />
                        </asp:DropDownList>
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnGuardarBorrador" runat="server" CssClass="button button-secondary" Text="Guardar borrador" CausesValidation="false" />
                        <asp:Button ID="btnPublicar" runat="server" CssClass="button button-primary" Text="Publicar" CausesValidation="false" />
                        <asp:Button ID="btnEnviarPrueba" runat="server" CssClass="button button-secondary" Text="Enviar prueba" CausesValidation="false" />
                    </div>
                </asp:Panel>

                <div class="newsletter-side-column">
                    <div class="auth-card admin-workspace-card newsletter-preview-card">
                        <div class="auth-card-header admin-workspace-header">
                            <div>
                                <span class="admin-card-eyebrow">Vista previa</span>
                                <h2>Mail newsletter</h2>
                            </div>
                            <span class="admin-workspace-hint">Plantilla StageUp</span>
                        </div>

                        <div class="newsletter-mail-preview" aria-label="Vista previa del email">
                            <div class="newsletter-mail-header">
                                <span class="newsletter-mail-logo" aria-hidden="true">S</span>
                                <div>
                                    <strong>StageUp</strong>
                                    <small>Novedades StageUp</small>
                                </div>
                            </div>
                            <div class="newsletter-mail-body">
                                <span class="newsletter-mail-tag">Nuevos espacios</span>
                                <h3>Nuevos espacios para crear esta temporada</h3>
                                <p>Hola artista,</p>
                                <p>Sumamos salas, estudios y teatros con disponibilidad actualizada para que cada artista encuentre un lugar acorde a su actividad.</p>
                                <a href="#" class="newsletter-mail-button">Ver novedades</a>
                                <small>Recibís este correo porque tenés una cuenta en StageUp.</small>
                            </div>
                        </div>
                    </div>

                    <div class="auth-card admin-list-card newsletter-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Historial</span>
                            <h2>Novedades recientes</h2>
                        </div>

                        <div class="newsletter-campaign-list">
                            <article class="newsletter-campaign-row">
                                <div>
                                    <h3>Nuevos espacios para crear esta temporada</h3>
                                    <p>Publicada · Envío pendiente</p>
                                </div>
                                <span class="admin-status-badge">Programada</span>
                            </article>

                            <article class="newsletter-campaign-row">
                                <div>
                                    <h3>Cómo preparar tu espacio para recibir reservas</h3>
                                    <p>Enviada a gestores · 18/09/2026</p>
                                </div>
                                <span class="admin-status-badge admin-status-badge-active">Enviada</span>
                            </article>

                            <article class="newsletter-campaign-row">
                                <div>
                                    <h3>Guía rápida para comparar espacios</h3>
                                    <p>Borrador · Sin publicar</p>
                                </div>
                                <span class="admin-status-badge admin-status-badge-inactive">Borrador</span>
                            </article>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
