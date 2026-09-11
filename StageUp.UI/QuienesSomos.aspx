<%@ Page Title="Quiénes somos | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="QuienesSomos.aspx.cs" Inherits="StageUp.UI.QuienesSomos" %>

<asp:Content ID="AboutContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page institutional-about">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label" data-i18n="About_Etiqueta">Quiénes somos</span>
                <h1 data-i18n="About_Titulo">Creamos encuentros entre espacios y artistas.</h1>
                <p class="institutional-lead" data-i18n="About_Introduccion">StageUp es la plataforma de Artera que conecta a quienes tienen un espacio creativo con quienes necesitan un lugar para ensayar, enseñar, producir o presentarse.</p>
            </div>
            <div class="institutional-hero-card">
                <span class="institutional-card-kicker" data-i18n="About_ManifiestoEtiqueta">Nuestro propósito</span>
                <p data-i18n="About_Manifiesto">Hacer que encontrar el lugar indicado sea tan inspirador como la actividad que va a suceder dentro de él.</p>
                <div class="institutional-signature">
                    <span class="institutional-signature-mark" aria-hidden="true">S</span>
                    <span>
                        <strong>StageUp</strong>
                        <small data-i18n="About_Lema">Encontrá tu espacio, potenciá tu arte.</small>
                    </span>
                </div>
            </div>
        </header>

        <div class="institutional-layout container-wide">
            <aside class="institutional-index" aria-label="Contenido de la página">
                <span data-i18n="Institutional_EnEstaPagina">En esta página</span>
                <nav>
                    <a href="#nuestra-historia" data-i18n="About_IndiceHistoria">Nuestra historia</a>
                    <a href="#como-funciona" data-i18n="About_IndiceComoFunciona">Cómo funciona</a>
                    <a href="#principios" data-i18n="About_IndicePrincipios">Nuestros principios</a>
                </nav>
            </aside>

            <div class="institutional-content">
                <section id="nuestra-historia" class="institutional-section">
                    <span class="institutional-section-number" aria-hidden="true">01</span>
                    <div>
                        <span class="institutional-eyebrow" data-i18n="About_OrigenEtiqueta">El origen</span>
                        <h2 data-i18n="About_OrigenTitulo">Una respuesta a una búsqueda demasiado difícil</h2>
                        <p data-i18n="About_OrigenTexto1">Encontrar una sala de ensayo, un estudio fotográfico o un escenario disponible todavía suele depender de recomendaciones, mensajes dispersos y datos incompletos. Al mismo tiempo, muchos espacios con enorme potencial permanecen invisibles para la comunidad que podría darles vida.</p>
                        <p data-i18n="About_OrigenTexto2">Artera creó StageUp para ordenar ese encuentro. Reunimos información, disponibilidad, características, precios y reputación en un mismo lugar para que cada decisión pueda tomarse con mayor claridad y confianza.</p>
                    </div>
                </section>

                <section id="como-funciona" class="institutional-section">
                    <span class="institutional-section-number" aria-hidden="true">02</span>
                    <div>
                        <span class="institutional-eyebrow" data-i18n="About_ExperienciaEtiqueta">La experiencia</span>
                        <h2 data-i18n="About_ExperienciaTitulo">Una plataforma pensada para las dos partes</h2>
                        <div class="institutional-step-grid">
                            <article class="institutional-step-card">
                                <span aria-hidden="true">1</span>
                                <h3 data-i18n="About_Paso1Titulo">Explorá y compará</h3>
                                <p data-i18n="About_Paso1Texto">Filtrá espacios por tipo, ubicación, capacidad, equipamiento, precio y disponibilidad.</p>
                            </article>
                            <article class="institutional-step-card">
                                <span aria-hidden="true">2</span>
                                <h3 data-i18n="About_Paso2Titulo">Coordiná tu reserva</h3>
                                <p data-i18n="About_Paso2Texto">Elegí fecha y duración, conocé el valor estimado y enviá tu solicitud al gestor.</p>
                            </article>
                            <article class="institutional-step-card">
                                <span aria-hidden="true">3</span>
                                <h3 data-i18n="About_Paso3Titulo">Construí confianza</h3>
                                <p data-i18n="About_Paso3Texto">Después de cada experiencia, las calificaciones ayudan a fortalecer toda la comunidad.</p>
                            </article>
                        </div>
                    </div>
                </section>

                <section id="principios" class="institutional-section">
                    <span class="institutional-section-number" aria-hidden="true">03</span>
                    <div>
                        <span class="institutional-eyebrow" data-i18n="About_PrincipiosEtiqueta">Lo que nos guía</span>
                        <h2 data-i18n="About_PrincipiosTitulo">Tecnología con sensibilidad creativa</h2>
                        <div class="institutional-value-grid">
                            <article>
                                <span class="institutional-value-icon" aria-hidden="true">◇</span>
                                <h3 data-i18n="About_ValorClaridadTitulo">Claridad</h3>
                                <p data-i18n="About_ValorClaridadTexto">Información completa y comparable para elegir sin sorpresas.</p>
                            </article>
                            <article>
                                <span class="institutional-value-icon" aria-hidden="true">◎</span>
                                <h3 data-i18n="About_ValorConfianzaTitulo">Confianza</h3>
                                <p data-i18n="About_ValorConfianzaTexto">Perfiles, solicitudes y reseñas que hacen más seguros los encuentros.</p>
                            </article>
                            <article>
                                <span class="institutional-value-icon" aria-hidden="true">✦</span>
                                <h3 data-i18n="About_ValorComunidadTitulo">Comunidad</h3>
                                <p data-i18n="About_ValorComunidadTexto">Más oportunidades para artistas y más movimiento para cada espacio.</p>
                            </article>
                        </div>
                    </div>
                </section>

                <section class="institutional-cta">
                    <div>
                        <span class="institutional-eyebrow" data-i18n="About_CtaEtiqueta">Tu próxima idea necesita un lugar</span>
                        <h2 data-i18n="About_CtaTitulo">Descubrí espacios preparados para hacerla realidad.</h2>
                    </div>
                    <a class="button button-primary" href="Explorar/ResultadosBusqueda.aspx" data-i18n="About_CtaBoton">Explorar espacios</a>
                </section>
            </div>
        </div>

        <nav class="static-page-links institutional-footer-nav container-wide" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx" data-i18n="Nav_Inicio">Inicio</a>
            <a class="text-link" href="Contactenos.aspx" data-i18n="Footer_Contactenos">Contáctenos</a>
            <a class="text-link" href="TerminosCondiciones.aspx" data-i18n="Footer_Terminos">Términos y condiciones</a>
            <a class="text-link" href="PoliticaPrivacidad.aspx" data-i18n="Footer_Privacidad">Política de privacidad</a>
        </nav>
    </section>
</asp:Content>
