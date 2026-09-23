<%@ Page Title="Novedades y newsletter | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionNovedades.aspx.cs" Inherits="StageUp.UI.Interno.GestionNovedades" %>

<asp:Content ID="GestionNovedadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page newsletter-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comunicación</span>
            <h1>Novedades y newsletter</h1>
            <p>Administrá noticias públicas de StageUp y prepará envíos por correo para usuarios activos.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <div class="newsletter-dashboard-strip" aria-label="Resumen de newsletters">
                <article>
                    <span>Borradores</span>
                    <strong><asp:Literal ID="litCantidadBorradores" runat="server" /></strong>
                </article>
                <article>
                    <span>Publicadas</span>
                    <strong><asp:Literal ID="litCantidadPublicadas" runat="server" /></strong>
                </article>
                <article>
                    <span>Último envío</span>
                    <strong><asp:Literal ID="litUltimoEnvio" runat="server" /></strong>
                </article>
            </div>

            <div class="newsletter-admin-layout">
                <asp:Panel ID="pnlFormularioNovedad" runat="server" CssClass="auth-card admin-editor-card newsletter-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Contenido</span>
                        <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva novedad" /></h2>
                        <p>Se publica en la página de novedades y, si lo tildás, también se envía por correo.</p>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtTitulo.ClientID %>">Título *</label>
                        <asp:TextBox ID="txtTitulo" runat="server" TextMode="SingleLine" MaxLength="180"
                            placeholder="Ej: Nuevos espacios para crear esta temporada" />
                        <asp:RequiredFieldValidator ID="rfvTitulo" runat="server" ControlToValidate="txtTitulo"
                            ValidationGroup="Novedad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el título." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtResumen.ClientID %>">Resumen para listado y correo *</label>
                        <asp:TextBox ID="txtResumen" runat="server" TextMode="MultiLine" Rows="3" MaxLength="300"
                            placeholder="Un párrafo breve: es lo que se ve en el listado y en el cuerpo del mail." />
                        <asp:RequiredFieldValidator ID="rfvResumen" runat="server" ControlToValidate="txtResumen"
                            ValidationGroup="Novedad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el resumen." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtContenido.ClientID %>">Contenido de la novedad *</label>
                        <asp:TextBox ID="txtContenido" runat="server" TextMode="MultiLine" Rows="8"
                            placeholder="El texto completo que se muestra en la página pública de novedades." />
                        <asp:RequiredFieldValidator ID="rfvContenido" runat="server" ControlToValidate="txtContenido"
                            ValidationGroup="Novedad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el contenido." />
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
                            <small class="field-help">Si la dejás vacía y publicás, se usa la fecha de hoy.</small>
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtUrlImagen.ClientID %>">Imagen destacada</label>
                        <asp:TextBox ID="txtUrlImagen" runat="server" TextMode="SingleLine" MaxLength="500"
                            placeholder="~/Content/Uploads/Novedades/imagen.jpg" />
                    </div>

                    <div class="newsletter-options">
                        <label class="newsletter-check-option">
                            <asp:CheckBox ID="chkEnviarMail" runat="server" />
                            <span>
                                <strong>Enviar también por mail al publicar</strong>
                                <small>Usa la plantilla StageUp y la categoría de destinatarios elegida abajo. Solo se envía una vez por novedad.</small>
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
                        <asp:Button ID="btnGuardarBorrador" runat="server" CssClass="button button-secondary" Text="Guardar como borrador"
                            ValidationGroup="Novedad" OnClick="btnGuardarBorrador_Click" />
                        <asp:Button ID="btnPublicar" runat="server" CssClass="button button-primary" Text="Guardar y publicar"
                            ValidationGroup="Novedad" OnClick="btnPublicar_Click" />
                        <asp:Button ID="btnEnviarPrueba" runat="server" CssClass="button button-secondary" Text="Enviar prueba"
                            ValidationGroup="Novedad" OnClick="btnEnviarPrueba_Click" />
                        <asp:LinkButton ID="lnkCancelarNovedad" runat="server" CssClass="text-link" Text="Cancelar edición"
                            CausesValidation="false" Visible="false" OnClick="lnkCancelarNovedad_Click" />
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
                                    <small><asp:Literal ID="litPreviewCategoria" runat="server" Text="Novedades StageUp" /></small>
                                </div>
                            </div>
                            <div class="newsletter-mail-body">
                                <h3><asp:Literal ID="litPreviewTitulo" runat="server" Text="Así se va a ver el correo" /></h3>
                                <p>Hola artista,</p>
                                <p><asp:Literal ID="litPreviewResumen" runat="server" Text="Completá el formulario y guardá para actualizar esta vista previa." /></p>
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

                        <asp:Panel ID="pnlSinNovedades" runat="server" CssClass="admin-empty-hint" Visible="false">
                            Todavía no cargaste ninguna novedad. Creá la primera desde el formulario.
                        </asp:Panel>

                        <div class="newsletter-campaign-list">
                            <asp:Repeater ID="rptNovedades" runat="server" OnItemCommand="rptNovedades_ItemCommand">
                                <ItemTemplate>
                                    <article class='<%# ObtenerClaseFilaNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %>'>
                                        <div>
                                            <h3><%#: Eval("Titulo") %></h3>
                                            <p><%#: ObtenerTextoFechaNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %></p>
                                            <div class="space-row-actions">
                                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                                    CommandName="Editar" CommandArgument='<%# Eval("IdNovedad") %>' Text="Editar" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                    CommandName="Publicar" CommandArgument='<%# Eval("IdNovedad") %>' Text="Publicar"
                                                    Visible='<%# !Convert.ToBoolean(Eval("Publicado")) %>' />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                                    CommandName="VolverABorrador" CommandArgument='<%# Eval("IdNovedad") %>' Text="Volver a borrador"
                                                    Visible='<%# Convert.ToBoolean(Eval("Publicado")) %>'
                                                    OnClientClick="return confirm('¿Volver esta novedad a borrador? Deja de mostrarse en la página pública.');" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                    CommandName="EnviarNewsletter" CommandArgument='<%# Eval("IdNovedad") %>' Text="Enviar newsletter"
                                                    Visible='<%# Convert.ToBoolean(Eval("Publicado")) && !Convert.ToBoolean(Eval("EnviadaPorCorreo")) %>'
                                                    OnClientClick="return confirm('¿Enviar el newsletter de esta novedad a la categoría de destinatarios seleccionada arriba?');" />
                                            </div>
                                        </div>
                                        <span class='<%# ObtenerClaseEstadoNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %>'>
                                            <%#: ObtenerTextoEstadoNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %>
                                        </span>
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
