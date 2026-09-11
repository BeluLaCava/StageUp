<%@ Page Title="Contáctenos | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contactenos.aspx.cs" Inherits="StageUp.UI.Contactenos" %>

<asp:Content ID="ContactContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page institutional-contact">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label" data-i18n="Contact_Etiqueta">Contáctenos</span>
                <h1 data-i18n="Contact_Titulo">Estamos para ayudarte a encontrar el próximo escenario.</h1>
                <p class="institutional-lead" data-i18n="Contact_Introduccion">Elegí el canal que mejor se adapte a tu consulta. Nuestro equipo acompaña a artistas, gestores y organizaciones que forman parte de StageUp.</p>
            </div>
            <div class="institutional-hero-card contact-availability-card">
                <span class="contact-status-dot" aria-hidden="true"></span>
                <div>
                    <span class="institutional-card-kicker" data-i18n="Contact_DisponibilidadEtiqueta">Atención personalizada</span>
                    <p data-i18n="Contact_DisponibilidadTexto">Respondemos de lunes a viernes, de 9 a 18 h, hora de Argentina.</p>
                    <small data-i18n="Contact_TiempoRespuesta">Tiempo estimado de respuesta: hasta 2 días hábiles.</small>
                </div>
            </div>
        </header>

        <div class="institutional-contact-grid container-wide">
            <article class="contact-channel-card">
                <span class="contact-channel-icon" aria-hidden="true">?</span>
                <span class="institutional-card-kicker" data-i18n="Contact_CanalGeneralEtiqueta">Consultas generales</span>
                <h2 data-i18n="Contact_CanalGeneralTitulo">¿Querés conocer más sobre StageUp?</h2>
                <p data-i18n="Contact_CanalGeneralTexto">Escribinos por dudas sobre la plataforma, alianzas, prensa o propuestas para la comunidad.</p>
                <a class="text-link contact-channel-link" href="mailto:contacto@stageup.com.ar">contacto@stageup.com.ar</a>
            </article>

            <article class="contact-channel-card contact-channel-featured">
                <span class="contact-channel-icon" aria-hidden="true">✓</span>
                <span class="institutional-card-kicker" data-i18n="Contact_CanalSoporteEtiqueta">Soporte de cuenta</span>
                <h2 data-i18n="Contact_CanalSoporteTitulo">¿Necesitás ayuda con una reserva?</h2>
                <p data-i18n="Contact_CanalSoporteTexto">Consultá respuestas rápidas y accedé al canal de soporte desde nuestro Centro de ayuda.</p>
                <a class="button button-primary" href="Ayuda/CentroAyuda.aspx" data-i18n="Contact_CanalSoporteBoton">Ir al Centro de ayuda</a>
            </article>

            <article class="contact-channel-card">
                <span class="contact-channel-icon" aria-hidden="true">+</span>
                <span class="institutional-card-kicker" data-i18n="Contact_CanalGestorEtiqueta">Espacios y gestores</span>
                <h2 data-i18n="Contact_CanalGestorTitulo">¿Querés publicar un espacio?</h2>
                <p data-i18n="Contact_CanalGestorTexto">Te orientamos sobre el alta como gestor y la información necesaria para presentar tu espacio.</p>
                <a class="text-link contact-channel-link" href="mailto:contacto@stageup.com.ar?subject=Quiero%20publicar%20un%20espacio" data-i18n="Contact_CanalGestorLink">Hablar con el equipo</a>
            </article>
        </div>

        <div class="institutional-layout container-wide">
            <aside class="institutional-index" aria-label="Contenido de la página">
                <span data-i18n="Institutional_EnEstaPagina">En esta página</span>
                <nav>
                    <a href="#antes-de-escribir" data-i18n="Contact_IndiceAntes">Antes de escribirnos</a>
                    <a href="#datos-consulta" data-i18n="Contact_IndiceDatos">Datos útiles</a>
                    <a href="#compromiso" data-i18n="Contact_IndiceCompromiso">Nuestro compromiso</a>
                </nav>
            </aside>

            <div class="institutional-content">
                <section id="antes-de-escribir" class="institutional-section">
                    <span class="institutional-section-number" aria-hidden="true">01</span>
                    <div>
                        <span class="institutional-eyebrow" data-i18n="Contact_AntesEtiqueta">Respuestas más rápidas</span>
                        <h2 data-i18n="Contact_AntesTitulo">Quizás la solución ya está disponible</h2>
                        <p data-i18n="Contact_AntesTexto">En el Centro de ayuda reunimos información sobre cuentas, perfiles de gestor, publicación de espacios, reservas y seguridad. Revisarlo antes de contactarnos puede ahorrarte tiempo.</p>
                        <a class="button button-secondary" href="Ayuda/CentroAyuda.aspx" data-i18n="Contact_AntesBoton">Consultar preguntas frecuentes</a>
                    </div>
                </section>

                <section id="datos-consulta" class="institutional-section">
                    <span class="institutional-section-number" aria-hidden="true">02</span>
                    <div>
                        <span class="institutional-eyebrow" data-i18n="Contact_DatosEtiqueta">Para poder ayudarte</span>
                        <h2 data-i18n="Contact_DatosTitulo">Incluí la información importante</h2>
                        <p data-i18n="Contact_DatosTexto">Indicá el correo asociado a tu cuenta y describí brevemente lo ocurrido. Si la consulta corresponde a un espacio o una reserva, agregá su nombre y la fecha involucrada. No envíes contraseñas ni códigos de verificación.</p>
                    </div>
                </section>

                <section id="compromiso" class="institutional-section">
                    <span class="institutional-section-number" aria-hidden="true">03</span>
                    <div>
                        <span class="institutional-eyebrow" data-i18n="Contact_CompromisoEtiqueta">Cuidamos cada conversación</span>
                        <h2 data-i18n="Contact_CompromisoTitulo">Un equipo humano detrás de la plataforma</h2>
                        <p data-i18n="Contact_CompromisoTexto">Leemos cada consulta para entender el contexto y brindar una respuesta clara. Los comentarios de nuestra comunidad también nos ayudan a mejorar StageUp de manera continua.</p>
                    </div>
                </section>
            </div>
        </div>

        <nav class="static-page-links institutional-footer-nav container-wide" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx" data-i18n="Nav_Inicio">Inicio</a>
            <a class="text-link" href="QuienesSomos.aspx" data-i18n="Footer_QuienesSomos">Quiénes somos</a>
            <a class="text-link" href="TerminosCondiciones.aspx" data-i18n="Footer_Terminos">Términos y condiciones</a>
            <a class="text-link" href="PoliticaPrivacidad.aspx" data-i18n="Footer_Privacidad">Política de privacidad</a>
        </nav>
    </section>
</asp:Content>
