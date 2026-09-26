<%@ Page Title="Gestión de encuestas | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionEncuestas.aspx.cs" Inherits="StageUp.UI.Interno.GestionEncuestas" %>

<asp:Content ID="GestionEncuestasContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .encuesta-resultado-pregunta { margin-bottom: 20px; }
        .encuesta-resultado-pregunta h4 { margin: 0 0 8px 0; }
        .encuesta-resultado-opcion { margin-bottom: 10px; }
        .encuesta-resultado-opcion-etiqueta { display: flex; justify-content: space-between; font-size: 0.9em; margin-bottom: 4px; }
        .encuesta-resultado-barra-track { background: var(--color-border, #e2e2e2); border-radius: 6px; height: 14px; overflow: hidden; }
        .encuesta-resultado-barra-fill { background: var(--color-primary, #4f46e5); height: 100%; border-radius: 6px; }
        .encuesta-resultado-total { color: var(--color-text-muted, #6b7280); margin-bottom: 14px; }
        .encuesta-pregunta-item { border-bottom: 1px solid var(--color-border, #e2e2e2); padding: 10px 0; }
        .encuesta-pregunta-item ul { margin: 6px 0 0 20px; padding: 0; }
        .encuesta-datos-solo-lectura dt { font-weight: bold; margin-top: 8px; }
        .encuesta-datos-solo-lectura dd { margin: 0; }
    </style>

    <section class="static-page internal-page internal-management-page faq-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comunidad</span>
            <h1>Gestión de encuestas</h1>
            <p>Creá encuestas dinámicas con fecha de vencimiento y mirá los resultados actualizados al instante.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <div class="faq-admin-summary" aria-label="Resumen de encuestas">
                <article>
                    <span>Activas</span>
                    <strong><asp:Literal ID="litCantidadActivas" runat="server" /></strong>
                </article>
                <article>
                    <span>En borrador</span>
                    <strong><asp:Literal ID="litCantidadBorrador" runat="server" /></strong>
                </article>
                <article>
                    <span>Cerradas</span>
                    <strong><asp:Literal ID="litCantidadCerradas" runat="server" /></strong>
                </article>
            </div>

            <div class="faq-admin-layout">
                <div>
                    <asp:Panel ID="pnlFormularioEncuesta" runat="server" CssClass="auth-card admin-editor-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Datos generales</span>
                            <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva encuesta" /></h2>
                            <p>Mientras la encuesta esté en borrador podés editar estos datos las veces que quieras.</p>
                        </div>

                        <div class="form-field">
                            <label for="<%= txtTitulo.ClientID %>">Título *</label>
                            <asp:TextBox ID="txtTitulo" runat="server" TextMode="SingleLine" MaxLength="200"
                                placeholder="Ej: ¿Qué mejorarías de StageUp?" />
                            <asp:RequiredFieldValidator ID="rfvTitulo" runat="server" ControlToValidate="txtTitulo"
                                ValidationGroup="Encuesta" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el título." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtDescripcion.ClientID %>">Descripción</label>
                            <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="3"
                                placeholder="Contexto opcional para quien responde." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtFechaInicio.ClientID %>">Fecha de inicio *</label>
                            <asp:TextBox ID="txtFechaInicio" runat="server" TextMode="DateTimeLocal" />
                            <asp:RequiredFieldValidator ID="rfvFechaInicio" runat="server" ControlToValidate="txtFechaInicio"
                                ValidationGroup="Encuesta" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la fecha de inicio." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtFechaVencimiento.ClientID %>">Fecha de vencimiento *</label>
                            <asp:TextBox ID="txtFechaVencimiento" runat="server" TextMode="DateTimeLocal" />
                            <small class="field-help">Pasada esta fecha la encuesta deja de aceptar respuestas automáticamente.</small>
                            <asp:RequiredFieldValidator ID="rfvFechaVencimiento" runat="server" ControlToValidate="txtFechaVencimiento"
                                ValidationGroup="Encuesta" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la fecha de vencimiento." />
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlPublicoObjetivo.ClientID %>">Dirigida a *</label>
                            <asp:DropDownList ID="ddlPublicoObjetivo" runat="server">
                                <asp:ListItem Text="Todos los usuarios" Value="Todos" />
                                <asp:ListItem Text="Gestores de espacios" Value="GestorEspacios" />
                                <asp:ListItem Text="Solicitantes" Value="ExternoSolicitante" />
                            </asp:DropDownList>
                        </div>

                        <div class="form-actions">
                            <asp:Button ID="btnGuardarEncuesta" runat="server" CssClass="button button-primary" Text="Crear encuesta"
                                ValidationGroup="Encuesta" OnClick="btnGuardarEncuesta_Click" />
                            <asp:LinkButton ID="lnkCancelarEncuesta" runat="server" CssClass="text-link" Text="Cancelar edición"
                                CausesValidation="false" Visible="false" OnClick="lnkCancelarEncuesta_Click" />
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlDatosSoloLectura" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Datos generales</span>
                            <h2><asp:Literal ID="litTituloSoloLectura" runat="server" /></h2>
                            <p>Esta encuesta ya fue publicada, así que sus datos y preguntas no se pueden modificar.</p>
                        </div>
                        <dl class="encuesta-datos-solo-lectura">
                            <dt>Descripción</dt>
                            <dd><asp:Literal ID="litDescripcionSoloLectura" runat="server" /></dd>
                            <dt>Vigencia</dt>
                            <dd><asp:Literal ID="litVigenciaSoloLectura" runat="server" /></dd>
                            <dt>Dirigida a</dt>
                            <dd><asp:Literal ID="litPublicoSoloLectura" runat="server" /></dd>
                        </dl>
                        <div class="form-actions">
                            <asp:LinkButton ID="lnkCancelarSoloLectura" runat="server" CssClass="text-link" Text="Volver a nueva encuesta"
                                CausesValidation="false" OnClick="lnkCancelarEncuesta_Click" />
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlPreguntas" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Preguntas</span>
                            <h2>Preguntas de la encuesta</h2>
                        </div>

                        <asp:Panel ID="pnlSinPreguntas" runat="server" CssClass="admin-empty-hint" Visible="false">
                            Todavía no cargaste ninguna pregunta.
                        </asp:Panel>

                        <asp:Repeater ID="rptPreguntas" runat="server" OnItemCommand="rptPreguntas_ItemCommand" OnItemDataBound="rptPreguntas_ItemDataBound">
                            <ItemTemplate>
                                <div class="encuesta-pregunta-item">
                                    <strong><%#: Eval("Texto") %></strong>
                                    <ul>
                                        <asp:Repeater ID="rptOpcionesPregunta" runat="server" DataSource='<%# Eval("Opciones") %>'>
                                            <ItemTemplate>
                                                <li><%#: Eval("Texto") %></li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ul>
                                    <asp:LinkButton ID="lnkQuitarPregunta" runat="server" CssClass="admin-action-link admin-action-link-danger"
                                        CausesValidation="false" CommandName="Quitar" CommandArgument='<%# Eval("IdPreguntaEncuesta") %>'
                                        Text="Quitar pregunta" Visible='<%# EsBorrador %>'
                                        OnClientClick="return confirm('¿Seguro que querés quitar esta pregunta?');" />
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>

                        <asp:Panel ID="pnlAgregarPregunta" runat="server">
                            <hr />
                            <div class="form-field">
                                <label for="<%= txtTextoPregunta.ClientID %>">Nueva pregunta</label>
                                <asp:TextBox ID="txtTextoPregunta" runat="server" TextMode="SingleLine" MaxLength="300"
                                    placeholder="Ej: ¿Cómo calificarías tu experiencia?" />
                            </div>
                            <div class="form-field">
                                <label for="<%= txtOpcionesPregunta.ClientID %>">Opciones (una por línea, mínimo 2)</label>
                                <asp:TextBox ID="txtOpcionesPregunta" runat="server" TextMode="MultiLine" Rows="4"
                                    placeholder="Muy buena&#10;Buena&#10;Regular&#10;Mala" />
                            </div>
                            <div class="form-actions">
                                <asp:Button ID="btnAgregarPregunta" runat="server" CssClass="button button-secondary" Text="Agregar pregunta"
                                    CausesValidation="false" OnClick="btnAgregarPregunta_Click" />
                            </div>
                        </asp:Panel>
                    </asp:Panel>

                    <asp:Panel ID="pnlEstadoAcciones" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Estado</span>
                            <h2><asp:Literal ID="litEstadoBadge" runat="server" /></h2>
                        </div>
                        <asp:Button ID="btnPublicar" runat="server" CssClass="button button-primary" Text="Publicar encuesta"
                            CausesValidation="false" OnClick="btnPublicar_Click" />
                        <asp:Button ID="btnCerrar" runat="server" CssClass="button button-secondary" Text="Cerrar encuesta"
                            CausesValidation="false" OnClick="btnCerrar_Click"
                            OnClientClick="return confirm('¿Seguro que querés cerrar la encuesta? Deja de aceptar respuestas.');" />
                        <asp:Literal ID="litEstadoSinAcciones" runat="server" Visible="false" Text="Esta encuesta está cerrada." />
                    </asp:Panel>

                    <asp:Panel ID="pnlResultados" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Resultados</span>
                            <h2>Resultados al instante</h2>
                        </div>
                        <p class="encuesta-resultado-total"><asp:Literal ID="litTotalRespuestas" runat="server" /></p>
                        <asp:Repeater ID="rptResultados" runat="server" OnItemDataBound="rptResultados_ItemDataBound">
                            <ItemTemplate>
                                <div class="encuesta-resultado-pregunta">
                                    <h4><%#: Eval("TextoPregunta") %></h4>
                                    <asp:Repeater ID="rptOpcionesResultado" runat="server" DataSource='<%# Eval("Opciones") %>'>
                                        <ItemTemplate>
                                            <div class="encuesta-resultado-opcion">
                                                <div class="encuesta-resultado-opcion-etiqueta">
                                                    <span><%#: Eval("TextoOpcion") %></span>
                                                    <span><%#: Eval("CantidadRespuestas") %> (<%#: Eval("Porcentaje") %>%)</span>
                                                </div>
                                                <div class="encuesta-resultado-barra-track">
                                                    <div class="encuesta-resultado-barra-fill" style='<%# "width:" + Eval("Porcentaje").ToString().Replace(",", ".") + "%;" %>'></div>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>
                </div>

                <div class="faq-admin-side">
                    <div class="auth-card admin-list-card faq-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Directorio</span>
                            <h2>Encuestas cargadas</h2>
                        </div>

                        <asp:Panel ID="pnlSinEncuestas" runat="server" CssClass="admin-empty-hint" Visible="false">
                            Todavía no cargaste ninguna encuesta. Creá la primera desde el formulario.
                        </asp:Panel>

                        <div class="faq-admin-list">
                            <asp:Repeater ID="rptEncuestas" runat="server" OnItemCommand="rptEncuestas_ItemCommand">
                                <ItemTemplate>
                                    <article class="faq-admin-row">
                                        <div class="faq-admin-row-main">
                                            <div>
                                                <h3><%#: Eval("Titulo") %></h3>
                                                <span class='<%# ClaseBadgeEstado((StageUp.BE.Entidades.Encuesta)Container.DataItem) %>'>
                                                    <%#: TextoEstado((StageUp.BE.Entidades.Encuesta)Container.DataItem) %>
                                                </span>
                                            </div>
                                        </div>
                                        <div class="space-row-actions">
                                            <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                                CommandName="Gestionar" CommandArgument='<%# Eval("IdEncuesta") %>' Text="Gestionar" />
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
