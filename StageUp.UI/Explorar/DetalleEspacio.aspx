<%@ Page Title="Detalle del espacio | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetalleEspacio.aspx.cs" Inherits="StageUp.UI.Explorar.DetalleEspacio" %>

<asp:Content ID="SpaceDetailContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="space-detail-page">
        <div class="space-detail-container">
            <a class="back-to-results" href="ResultadosBusqueda.aspx">
                <span aria-hidden="true">←</span>
                Volver a resultados
            </a>

            <div class="space-detail-empty" role="status">
                <div class="space-detail-empty-visual" aria-hidden="true"><span></span></div>
                <span class="section-label">Detalle del espacio</span>
                <h1>No hay un espacio seleccionado</h1>
                <p>Elegí un espacio desde los resultados para consultar sus imágenes, características, disponibilidad y condiciones de uso.</p>
                <div class="space-detail-empty-actions">
                    <a class="button button-primary" href="ResultadosBusqueda.aspx">Explorar espacios</a>
                    <a class="button button-secondary" href="../IniciarSesion.aspx">Iniciar sesión para reservar</a>
                </div>
                <small>Para solicitar una reserva necesitás una cuenta activa y una sesión iniciada.</small>
            </div>

            <template id="space-detail-template">
                <article class="space-detail" data-space-detail>
                    <div class="space-gallery" aria-label="Imágenes del espacio">
                        <div class="space-gallery-main" data-space-main-image></div>
                        <div class="space-gallery-secondary" data-space-gallery></div>
                    </div>

                    <div class="space-detail-layout">
                        <div class="space-detail-main">
                            <header class="space-detail-heading">
                                <p class="space-detail-location" data-space-location></p>
                                <h1 data-space-name></h1>
                                <div class="space-detail-summary">
                                    <span data-space-type></span>
                                    <span data-space-capacity></span>
                                    <span data-space-measures></span>
                                    <span data-space-rating></span>
                                </div>
                            </header>

                            <section class="space-detail-section" aria-labelledby="space-description-title">
                                <h2 id="space-description-title">Sobre el espacio</h2>
                                <div data-space-description></div>
                            </section>

                            <section class="space-detail-section" aria-labelledby="space-features-title">
                                <h2 id="space-features-title">Características físicas y equipamiento</h2>
                                <div class="space-feature-grid" data-space-features></div>
                            </section>

                            <section class="space-detail-section" aria-labelledby="space-conditions-title">
                                <h2 id="space-conditions-title">Condiciones de uso</h2>
                                <div data-space-conditions></div>
                            </section>

                            <section class="space-detail-section" aria-labelledby="space-availability-title">
                                <h2 id="space-availability-title">Disponibilidad</h2>
                                <div class="space-availability" data-space-availability></div>
                            </section>

                            <section class="space-detail-section" aria-labelledby="space-reputation-title">
                                <h2 id="space-reputation-title">Calificaciones</h2>
                                <div class="space-reputation" data-space-reviews></div>
                            </section>
                        </div>

                        <aside class="reservation-card" aria-label="Solicitud de reserva">
                            <span class="reservation-card-label">Valor de referencia</span>
                            <div class="reservation-card-price" data-space-price></div>
                            <p>Consultá la disponibilidad antes de enviar una solicitud.</p>
                            <a class="button button-primary button-full" href="../IniciarSesion.aspx">Solicitar reserva</a>
                            <small>Se requiere una cuenta activa y una sesión iniciada.</small>
                        </aside>
                    </div>
                </article>
            </template>
        </div>
    </section>
</asp:Content>
