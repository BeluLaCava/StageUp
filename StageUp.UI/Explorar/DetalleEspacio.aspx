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
                        <span class="reservation-card-label">¿Te interesa este espacio?</span>
                        <p>Consultá la disponibilidad antes de enviar una solicitud.</p>
                        <a class="button button-primary button-full" href="../IniciarSesion.aspx">Solicitar reserva</a>
                        <small>Se requiere una cuenta activa y una sesión iniciada.</small>
                    </aside>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
