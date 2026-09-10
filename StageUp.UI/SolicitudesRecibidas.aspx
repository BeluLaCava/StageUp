<%@ Page Title="Solicitudes recibidas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="SolicitudesRecibidas.aspx.cs" Inherits="StageUp.UI.SolicitudesRecibidas" %>

<asp:Content ID="SolicitudesRecibidasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="requests-page">
        <div class="container">
            <header class="requests-header">
                <div>
                    <span class="section-label">Panel del gestor</span>
                    <h1>Solicitudes de reserva</h1>
                    <p>Revisá quién quiere usar tus espacios, consultá los detalles y decidí cada solicitud.</p>
                </div>
                <a class="button button-secondary" href="MisEspacios.aspx">Ver mis espacios</a>
            </header>

            <nav class="gestor-subnav requests-subnav" aria-label="Navegación de gestor de espacios">
                <a href="~/MisEspacios.aspx" runat="server">Administrar espacios</a>
                <a href="~/SolicitudesRecibidas.aspx" runat="server" class="active">Solicitudes recibidas</a>
            </nav>

            <div class="requests-summary" aria-label="Resumen de solicitudes">
                <article>
                    <span class="requests-summary-icon requests-summary-pending" aria-hidden="true">◷</span>
                    <div><small>Pendientes</small><strong><asp:Literal ID="litCantidadPendientes" runat="server" /></strong></div>
                </article>
                <article>
                    <span class="requests-summary-icon requests-summary-resolved" aria-hidden="true">✓</span>
                    <div><small>Resueltas</small><strong><asp:Literal ID="litCantidadResueltas" runat="server" /></strong></div>
                </article>
                <article>
                    <span class="requests-summary-icon requests-summary-total" aria-hidden="true">≡</span>
                    <div><small>Total recibidas</small><strong><asp:Literal ID="litCantidadTotal" runat="server" /></strong></div>
                </article>
            </div>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message requests-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlSinSolicitudes" runat="server" CssClass="requests-empty" Visible="false">
                <span class="requests-empty-icon" aria-hidden="true">◇</span>
                <h2>Todavía no recibiste solicitudes</h2>
                <p>Cuando alguien quiera reservar uno de tus espacios, vas a poder conocer al solicitante y responderle desde acá.</p>
                <a class="button button-secondary" href="MisEspacios.aspx">Administrar espacios</a>
            </asp:Panel>

            <div class="requests-list">
                <asp:Repeater ID="rptSolicitudes" runat="server" OnItemCommand="rptSolicitudes_ItemCommand" OnItemDataBound="rptSolicitudes_ItemDataBound">
                    <ItemTemplate>
                        <article class='<%# ObtenerClaseSolicitud((StageUp.BE.Entidades.Reserva)Container.DataItem) %>'>
                            <header class="request-card-header">
                                <div>
                                    <span class="request-card-label">Solicitud para</span>
                                    <h2><a href='<%# "Explorar/DetalleEspacio.aspx?id=" + Eval("IdEspacioArtistico") %>'><%#: Eval("NombreEspacio") %></a></h2>
                                </div>
                                <span class='<%# ObtenerClaseEstado(Eval("EstadoReserva") as string) %>'><%#: Eval("EstadoReserva") %></span>
                            </header>

                            <div class="request-card-grid">
                                <section class="request-booking-details" aria-label="Datos solicitados">
                                    <div class="request-date-block">
                                        <span class="request-date-icon" aria-hidden="true">▦</span>
                                        <div><small>Fecha solicitada</small><strong><%# Eval("FechaSolicitada", "{0:dddd d 'de' MMMM 'de' yyyy}") %></strong></div>
                                    </div>
                                    <asp:Panel ID="pnlHorarioSolicitud" runat="server" CssClass="request-horario-block"
                                        Visible='<%# ((StageUp.BE.Entidades.Reserva)Container.DataItem).MinutoDesde.HasValue %>'>
                                        <span class="request-date-icon" aria-hidden="true">◴</span>
                                        <div><small>Horario y precio pactado</small><strong><%#: ObtenerHorarioTexto((StageUp.BE.Entidades.Reserva)Container.DataItem) %></strong></div>
                                    </asp:Panel>
                                    <div class="request-meta">
                                        <span>Recibida el <%# Eval("FechaCreacion", "{0:dd/MM/yyyy 'a las' HH:mm}") %></span>
                                        <span>Solicitud #<%#: Eval("IdReserva") %></span>
                                    </div>
                                    <asp:Panel ID="pnlDetalleSolicitud" runat="server" CssClass="request-message"
                                        Visible='<%# !string.IsNullOrWhiteSpace(Eval("ComentarioSolicitante") as string) %>'>
                                        <small>Detalle enviado por el solicitante</small>
                                        <p><%#: Eval("ComentarioSolicitante") %></p>
                                    </asp:Panel>
                                </section>

                                <aside class="request-applicant-card" aria-label="Información del solicitante">
                                    <div class="request-applicant-heading">
                                        <div class="request-applicant-avatar" aria-hidden="true"><%#: ObtenerIniciales((StageUp.BE.Entidades.Reserva)Container.DataItem) %></div>
                                        <div>
                                            <small>Solicitante</small>
                                            <strong><%#: Eval("NombreSolicitante") %></strong>
                                            <span><%#: Eval("CorreoSolicitante") %></span>
                                        </div>
                                    </div>
                                    <div class="request-reputation">
                                        <span class="request-reputation-stars" aria-hidden="true"><%#: ObtenerEstrellasSolicitante((StageUp.BE.Entidades.Reserva)Container.DataItem) %></span>
                                        <strong><%#: ObtenerResumenReputacionSolicitante((StageUp.BE.Entidades.Reserva)Container.DataItem) %></strong>
                                        <p><%#: ObtenerActividadSolicitante((StageUp.BE.Entidades.Reserva)Container.DataItem) %></p>
                                        <asp:Panel ID="pnlHistorialReputacion" runat="server" CssClass="request-reputation-history"
                                            Visible='<%# ((StageUp.BE.Entidades.Reserva)Container.DataItem).CalificacionesSolicitante.Count > 0 %>'>
                                            <small data-i18n="Calificacion_UltimasOpiniones">Últimas opiniones de gestores</small>
                                            <asp:Repeater ID="rptHistorialReputacion" runat="server"
                                                DataSource='<%# ((StageUp.BE.Entidades.Reserva)Container.DataItem).CalificacionesSolicitante %>'>
                                                <ItemTemplate>
                                                    <blockquote><span aria-hidden="true"><%#: ObtenerEstrellas(Convert.ToInt32(Eval("Puntaje"))) %></span><%#: Eval("Comentario") %></blockquote>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </asp:Panel>
                                    </div>
                                </aside>
                            </div>

                            <asp:Panel ID="pnlAcciones" runat="server" CssClass="request-card-actions">
                                <span>¿Querés aceptar esta solicitud?</span>
                                <div>
                                    <asp:LinkButton ID="lnkRechazar" runat="server" CssClass="button button-secondary request-reject"
                                        CausesValidation="false" CommandName="Rechazar" CommandArgument='<%# Eval("IdReserva") %>'
                                        Text="Rechazar" OnClientClick="return confirm('¿Querés rechazar esta solicitud?');" />
                                    <asp:LinkButton ID="lnkAceptar" runat="server" CssClass="button button-primary request-accept"
                                        CausesValidation="false" CommandName="Aceptar" CommandArgument='<%# Eval("IdReserva") %>'
                                        Text="Aceptar solicitud" OnClientClick="return confirm('¿Querés aceptar esta solicitud?');" />
                                </div>
                            </asp:Panel>
                            <asp:Panel ID="pnlAccionesCalificacion" runat="server" CssClass="request-card-actions request-card-review-actions">
                                <span data-i18n="Calificacion_FinalizadaGestor">La reserva finalizó. Ya podés contar cómo fue la experiencia con el solicitante.</span>
                                <asp:LinkButton ID="lnkCalificarSolicitante" runat="server" CssClass="button button-primary button-small"
                                    CausesValidation="false" CommandName="CalificarSolicitante" CommandArgument='<%# Eval("IdReserva") %>'
                                    Text="Calificar solicitante" data-i18n="Calificacion_CalificarSolicitante" />
                                <asp:Panel ID="pnlCalificacionSolicitanteRealizada" runat="server" CssClass="review-completed-badge">
                                    <span aria-hidden="true">✓</span> <span data-i18n="Calificacion_CalificacionEnviada">Calificación enviada</span>
                                </asp:Panel>
                            </asp:Panel>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <asp:Panel ID="pnlCalificarSolicitante" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog" open aria-labelledby="review-user-title">
            <asp:LinkButton ID="lnkCerrarCalificacionSolicitante" runat="server" CssClass="review-dialog-close" CausesValidation="false"
                OnClick="lnkCerrarCalificacionSolicitante_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label" data-i18n="Calificacion_ExperienciaVerificada">Experiencia verificada</span>
            <h2 id="review-user-title" data-i18n="Calificacion_TituloSolicitante">¿Cómo fue la experiencia con el solicitante?</h2>
            <p data-i18n="Calificacion_AyudaSolicitante">Tu calificación será visible para el usuario y ayudará a otros gestores a evaluar futuras solicitudes.</p>
            <div class="form-field">
                <label for="<%= ddlPuntajeSolicitante.ClientID %>" data-i18n="Calificacion_Puntaje">Calificación *</label>
                <asp:DropDownList ID="ddlPuntajeSolicitante" runat="server" CssClass="review-score-select">
                    <asp:ListItem Value="">Seleccioná una puntuación</asp:ListItem>
                    <asp:ListItem Value="5">★★★★★ · Excelente</asp:ListItem>
                    <asp:ListItem Value="4">★★★★☆ · Muy bueno</asp:ListItem>
                    <asp:ListItem Value="3">★★★☆☆ · Bueno</asp:ListItem>
                    <asp:ListItem Value="2">★★☆☆☆ · Regular</asp:ListItem>
                    <asp:ListItem Value="1">★☆☆☆☆ · Malo</asp:ListItem>
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvPuntajeSolicitante" runat="server" ControlToValidate="ddlPuntajeSolicitante"
                    InitialValue="" ValidationGroup="CalificarSolicitante" Display="Dynamic" CssClass="field-error-text"
                    ErrorMessage="Elegí una calificación entre 1 y 5 estrellas." />
            </div>
            <div class="form-field">
                <label for="<%= txtComentarioCalificacionSolicitante.ClientID %>" data-i18n="Calificacion_Comentario">Comentario *</label>
                <asp:TextBox ID="txtComentarioCalificacionSolicitante" runat="server" TextMode="MultiLine" Rows="5" MaxLength="1000"
                    CssClass="review-comment" placeholder="Contá si respetó el horario, el espacio y los acuerdos." data-i18n-placeholder="Calificacion_PlaceholderSolicitante" />
                <asp:RequiredFieldValidator ID="rfvComentarioCalificacionSolicitante" runat="server" ControlToValidate="txtComentarioCalificacionSolicitante"
                    ValidationGroup="CalificarSolicitante" Display="Dynamic" CssClass="field-error-text"
                    ErrorMessage="Escribí un comentario sobre tu experiencia." />
            </div>
            <div class="review-dialog-actions">
                <asp:LinkButton ID="lnkCancelarCalificacionSolicitante" runat="server" CssClass="button button-secondary" CausesValidation="false"
                    OnClick="lnkCerrarCalificacionSolicitante_Click" data-i18n="General_Cancelar">Cancelar</asp:LinkButton>
                <asp:Button ID="btnEnviarCalificacionSolicitante" runat="server" CssClass="button button-primary"
                    ValidationGroup="CalificarSolicitante" Text="Enviar calificación" OnClick="btnEnviarCalificacionSolicitante_Click" data-i18n="Calificacion_Enviar" />
            </div>
        </dialog>
    </asp:Panel>
</asp:Content>
