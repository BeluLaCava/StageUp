<%@ Page Title="Solicitudes recibidas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Culture="es-AR" UICulture="es-AR" CodeBehind="SolicitudesRecibidas.aspx.cs" Inherits="StageUp.UI.SolicitudesRecibidas" %>

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
                                        <span class="request-reputation-stars" aria-hidden="true">☆☆☆☆☆</span>
                                        <strong>Sin calificaciones todavía</strong>
                                        <p><%#: ObtenerActividadSolicitante((StageUp.BE.Entidades.Reserva)Container.DataItem) %></p>
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
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>
</asp:Content>
