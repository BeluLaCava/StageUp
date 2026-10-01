<%@ Page Title="Mis reservas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MisReservas.aspx.cs" Inherits="StageUp.UI.MisReservas" %>

<asp:Content ID="MisReservasContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .reserva-historial-filtro { display: flex; gap: 1rem; align-items: center; justify-content: space-between; flex-wrap: wrap; }
        .reserva-historial-control { display: inline-flex; align-items: center; gap: 0.55rem; margin-left: auto; color: var(--color-text-muted, #765f55); font-size: 0.9rem; }
        .reserva-historial-control label { margin: 0; font-weight: 700; }
        .reserva-historial-control select { min-width: 10rem; padding: 0.55rem 0.8rem; color: var(--color-primary, #6d1021); background: #fff; border: 1px solid var(--color-border, #e2d4cc); border-radius: 0.75rem; font: inherit; font-weight: 700; }
        .reserva-historial-meta { color: var(--color-text-muted, #6b7280); font-size: 0.88em; margin: 2px 0; }
        .reserva-pago { margin: 6px 0; padding: 8px 12px; border-radius: 10px; font-size: 0.9em; background: #fcf7f3; border-left: 4px solid #896650; }
        .reserva-pago-pendiente { background: #fff4e5; border-left-color: #b45309; color: #78350f; }
    </style>
    <section class="static-page user-module-page">
        <div class="static-page-header">
            <span class="section-label">Mis reservas</span>
            <h1>Tus solicitudes de reserva</h1>
            <p>Acá vas a ver el historial completo de las reservas que solicitaste, con su estado y el seguimiento de cada una.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="auth-card">
                <div class="auth-card-header reserva-historial-filtro">
                    <h2>Historial</h2>
                    <div class="reserva-historial-control">
                        <label for="<%= ddlFiltroHistorial.ClientID %>">Mostrar</label>
                        <asp:DropDownList ID="ddlFiltroHistorial" runat="server" AutoPostBack="true"
                            OnSelectedIndexChanged="ddlFiltroHistorial_SelectedIndexChanged" />
                    </div>
                </div>

                <asp:Literal ID="litSinReservas" runat="server" Visible="false"
                    Text="Todavía no solicitaste ninguna reserva. Podés explorar espacios publicados y solicitar una desde su detalle." />

                <asp:Repeater ID="rptMisReservas" runat="server" OnItemCommand="rptMisReservas_ItemCommand" OnItemDataBound="rptMisReservas_ItemDataBound">
                    <ItemTemplate>
                        <div class="space-row">
                            <div class="space-row-info">
                                <h3><%# Eval("NombreEspacio") %></h3>
                                <p>Fecha del alquiler: <%# Eval("FechaSolicitada", "{0:dd/MM/yyyy}") %> · Estado: <%# Eval("EstadoReserva") %></p>
                                <p class="reserva-historial-meta">
                                    N° <%# Eval("IdReserva") %> · Solicitada el <%# Eval("FechaCreacion", "{0:dd/MM/yyyy HH:mm}") %>
                                    <%#: string.IsNullOrEmpty(Eval("NombreGestor") as string) ? "" : " · Gestor: " + Eval("NombreGestor") %>
                                </p>
                                <p class="space-row-horario">
                                    <asp:Literal ID="litHorarioImporte" runat="server"
                                        Visible='<%# ((StageUp.BE.Entidades.Reserva)Container.DataItem).MinutoDesde.HasValue %>' />
                                </p>
                                <p class="space-row-descripcion"><%# Eval("ComentarioSolicitante") %></p>
                                <asp:Literal ID="litComentarioResolucion" runat="server" Visible='<%# !string.IsNullOrEmpty(Eval("ComentarioResolucion") as string) %>' />
                                <asp:Panel ID="pnlEstadoPago" runat="server" CssClass="reserva-pago" Visible="false">
                                    <asp:Literal ID="litEstadoPago" runat="server" />
                                </asp:Panel>
                                <section class="booking-progress" aria-label="Seguimiento de la reserva">
                                    <ol class="booking-progress-steps">
                                        <asp:Repeater ID="rptSeguimiento" runat="server"
                                            DataSource='<%# StageUp.BLL.BLL_Reserva.ConstruirSeguimiento((StageUp.BE.Entidades.Reserva)Container.DataItem) %>'>
                                            <ItemTemplate>
                                                <li class='<%# "booking-progress-step booking-progress-step-" + Eval("Estado").ToString().ToLowerInvariant() %>'>
                                                    <span class="booking-progress-icon"><%#: ObtenerIconoSeguimiento(Eval("Titulo") as string) %></span>
                                                    <span class="booking-progress-title"><%#: Eval("Titulo") %></span>
                                                    <span class="booking-progress-date"><%#: Eval("Fecha", "{0:dd/MM HH:mm}") %></span>
                                                </li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ol>
                                    <details class="booking-progress-details">
                                        <summary>Ver detalle del seguimiento</summary>
                                        <ol>
                                            <asp:Repeater ID="rptSeguimientoDetalle" runat="server"
                                                DataSource='<%# StageUp.BLL.BLL_Reserva.ConstruirSeguimiento((StageUp.BE.Entidades.Reserva)Container.DataItem) %>'>
                                                <ItemTemplate>
                                                    <li class='<%# "booking-progress-detail booking-progress-detail-" + Eval("Estado").ToString().ToLowerInvariant() %>'>
                                                        <time><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %></time>
                                                        <div>
                                                            <strong><%#: Eval("Titulo") %></strong>
                                                            <span><%#: Eval("Detalle") %></span>
                                                        </div>
                                                    </li>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </ol>
                                    </details>
                                </section>
                            </div>
                            <div class="space-row-actions">
                                <asp:HyperLink ID="lnkPagar" runat="server" CssClass="button button-primary button-small" Text="Pagar reserva" Visible="false" />
                                <asp:LinkButton ID="lnkCancelar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Cancelar" CommandArgument='<%# Eval("IdReserva") %>' Text="Cancelar" />
                                <asp:LinkButton ID="lnkCalificarEspacio" runat="server" CssClass="button button-primary button-small" CausesValidation="false"
                                    CommandName="CalificarEspacio" CommandArgument='<%# Eval("IdReserva") %>' Text="Calificar espacio" data-i18n="Calificacion_CalificarEspacio" />
                                <asp:Panel ID="pnlCalificacionEspacioRealizada" runat="server" CssClass="review-completed-badge">
                                    <span aria-hidden="true">✓</span> <span data-i18n="Calificacion_ResenaEnviada">Reseña enviada</span>
                                </asp:Panel>
                                <a class="text-link" href='<%#: "Soporte.aspx?reserva=" + Eval("IdReserva") %>'>Contactar soporte</a>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <asp:Panel ID="pnlCalificarEspacio" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog" open aria-labelledby="review-space-title">
            <asp:LinkButton ID="lnkCerrarCalificacionEspacio" runat="server" CssClass="review-dialog-close" CausesValidation="false"
                OnClick="lnkCerrarCalificacionEspacio_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label" data-i18n="Calificacion_ExperienciaVerificada">Experiencia verificada</span>
            <h2 id="review-space-title" data-i18n="Calificacion_TituloEspacio">¿Cómo estuvo el espacio?</h2>
            <p data-i18n="Calificacion_AyudaEspacio">Tu opinión ayudará a otras personas a elegir y al gestor a seguir mejorando.</p>
            <div class="form-field">
                <label for="<%= ddlPuntajeEspacio.ClientID %>" data-i18n="Calificacion_Puntaje">Calificación *</label>
                <asp:DropDownList ID="ddlPuntajeEspacio" runat="server" CssClass="review-score-select">
                    <asp:ListItem Value="">Seleccioná una puntuación</asp:ListItem>
                    <asp:ListItem Value="5">★★★★★ · Excelente</asp:ListItem>
                    <asp:ListItem Value="4">★★★★☆ · Muy bueno</asp:ListItem>
                    <asp:ListItem Value="3">★★★☆☆ · Bueno</asp:ListItem>
                    <asp:ListItem Value="2">★★☆☆☆ · Regular</asp:ListItem>
                    <asp:ListItem Value="1">★☆☆☆☆ · Malo</asp:ListItem>
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="rfvPuntajeEspacio" runat="server" ControlToValidate="ddlPuntajeEspacio"
                    InitialValue="" ValidationGroup="CalificarEspacio" Display="Dynamic" CssClass="field-error-text"
                    ErrorMessage="Elegí una calificación entre 1 y 5 estrellas." />
            </div>
            <div class="form-field">
                <label for="<%= txtComentarioCalificacionEspacio.ClientID %>" data-i18n="Calificacion_Comentario">Comentario *</label>
                <asp:TextBox ID="txtComentarioCalificacionEspacio" runat="server" TextMode="MultiLine" Rows="5" MaxLength="1000"
                    CssClass="review-comment" placeholder="Contá cómo fue tu experiencia con el espacio." data-i18n-placeholder="Calificacion_PlaceholderEspacio" />
                <asp:RequiredFieldValidator ID="rfvComentarioCalificacionEspacio" runat="server" ControlToValidate="txtComentarioCalificacionEspacio"
                    ValidationGroup="CalificarEspacio" Display="Dynamic" CssClass="field-error-text"
                    ErrorMessage="Escribí un comentario sobre tu experiencia." />
            </div>
            <div class="review-dialog-actions">
                <asp:LinkButton ID="lnkCancelarCalificacionEspacio" runat="server" CssClass="button button-secondary" CausesValidation="false"
                    OnClick="lnkCerrarCalificacionEspacio_Click" data-i18n="General_Cancelar">Cancelar</asp:LinkButton>
                <asp:Button ID="btnEnviarCalificacionEspacio" runat="server" CssClass="button button-primary"
                    ValidationGroup="CalificarEspacio" Text="Publicar reseña" OnClick="btnEnviarCalificacionEspacio_Click" data-i18n="Calificacion_Publicar" />
            </div>
        </dialog>
    </asp:Panel>
</asp:Content>
