<%@ Page Title="Detalle del espacio | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetalleEspacio.aspx.cs" Inherits="StageUp.UI.Explorar.DetalleEspacio" %>

<asp:Content ID="SpaceDetailContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="space-detail-page">
        <div class="space-detail-container">
            <a class="back-to-results" href="ResultadosBusqueda.aspx">
                <span aria-hidden="true">←</span>
                Volver a resultados
            </a>

            <asp:Panel ID="pnlEspacioNoEncontrado" runat="server" CssClass="space-detail-empty" role="status">
                <div class="space-detail-empty-visual" aria-hidden="true"><span></span></div>
                <span class="section-label">Detalle del espacio</span>
                <h1><asp:Literal ID="litTituloNoEncontrado" runat="server" Text="No hay un espacio seleccionado" /></h1>
                <p><asp:Literal ID="litDescripcionNoEncontrado" runat="server" Text="Elegí un espacio desde los resultados para consultar su información." /></p>
                <div class="space-detail-empty-actions">
                    <a class="button button-primary" href="ResultadosBusqueda.aspx">Explorar espacios</a>
                    <a class="button button-secondary" href="../IniciarSesion.aspx">Iniciar sesión para reservar</a>
                </div>
                <small>Para solicitar una reserva necesitás una cuenta activa y una sesión iniciada.</small>
            </asp:Panel>

            <asp:Panel ID="pnlDetalleEspacio" runat="server" CssClass="space-detail" Visible="false">
                <div class="space-detail-layout">
                    <div class="space-detail-main">
                        <header class="space-detail-heading">
                            <p class="space-detail-location"><asp:Literal ID="litTipoEspacio" runat="server" /></p>
                            <h1><asp:Literal ID="litNombreEspacio" runat="server" /></h1>
                            <p class="space-detail-gestor"><asp:Literal ID="litInfoGestor" runat="server" /></p>
                            <div class="space-detail-summary">
                                <span><asp:Literal ID="litFechaPublicacion" runat="server" /></span>
                            </div>
                        </header>

                        <section class="space-detail-section" aria-labelledby="space-description-title">
                            <h2 id="space-description-title">Sobre el espacio</h2>
                            <div><asp:Literal ID="litDescripcion" runat="server" /></div>
                        </section>

                        <section class="space-detail-section" aria-labelledby="space-more-info-title">
                            <h2 id="space-more-info-title">Más información</h2>
                            <p>La ubicación, capacidad, equipamiento, imágenes, disponibilidad y calificaciones de este espacio se van a poder consultar acá en una próxima entrega.</p>
                        </section>
                    </div>

                    <aside class="reservation-card" aria-label="Solicitud de reserva">
                        <asp:Panel ID="pnlReservarInvitado" runat="server">
                            <span class="reservation-card-label">¿Te interesa este espacio?</span>
                            <p>Consultá la disponibilidad antes de enviar una solicitud.</p>
                            <a class="button button-primary button-full" href="../IniciarSesion.aspx">Solicitar reserva</a>
                            <small>Se requiere una cuenta activa y una sesión iniciada.</small>
                        </asp:Panel>

                        <asp:Panel ID="pnlReservarPropio" runat="server" Visible="false">
                            <span class="reservation-card-label">Este es tu espacio</span>
                            <p>Administralo desde <a href="../MisEspacios.aspx">Mis espacios</a>.</p>
                        </asp:Panel>

                        <asp:Panel ID="pnlReservarMensaje" runat="server" Visible="false" CssClass="form-message">
                            <asp:Literal ID="litReservarMensaje" runat="server" />
                        </asp:Panel>

                        <asp:Panel ID="pnlReservarFormulario" runat="server" Visible="false">
                            <span class="reservation-card-label">¿Te interesa este espacio?</span>
                            <p>Elegí una fecha y enviá tu solicitud. El gestor la va a aceptar o rechazar.</p>

                            <div class="form-field">
                                <label for="<%= txtFechaReserva.ClientID %>">Fecha *</label>
                                <asp:TextBox ID="txtFechaReserva" runat="server" TextMode="Date" />
                                <asp:RequiredFieldValidator ID="rfvFechaReserva" runat="server" ControlToValidate="txtFechaReserva"
                                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="Elegí una fecha." ValidationGroup="Reserva" />
                            </div>

                            <div class="form-field">
                                <label for="<%= txtComentarioReserva.ClientID %>">Comentario (opcional)</label>
                                <asp:TextBox ID="txtComentarioReserva" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000"
                                    placeholder="Contale al gestor para qué necesitás el espacio." />
                            </div>

                            <asp:Button ID="btnSolicitarReserva" runat="server" CssClass="button button-primary button-full"
                                Text="Enviar solicitud de reserva" ValidationGroup="Reserva" OnClick="btnSolicitarReserva_Click" />
                            <small>Se requiere una cuenta activa y una sesión iniciada.</small>
                        </asp:Panel>
                    </aside>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
