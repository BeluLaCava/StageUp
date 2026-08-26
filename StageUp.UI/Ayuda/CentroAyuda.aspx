<%@ Page Title="Centro de ayuda | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CentroAyuda.aspx.cs" Inherits="StageUp.UI.Ayuda.CentroAyuda" %>

<asp:Content ID="HelpContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="help-page">
        <header class="help-hero">
            <div class="container-narrow">
                <span class="section-label">Centro de ayuda</span>
                <h1>Empecemos por una respuesta simple</h1>
                <p>Recorré la ayuda paso a paso: consultá las preguntas frecuentes, probá el asistente y, si todavía lo necesitás, contactá a soporte.</p>
                <nav class="help-journey" aria-label="Recorrido de ayuda">
                    <span class="help-journey-item is-current"><strong>1</strong>Preguntas frecuentes</span>
                    <span class="help-journey-line" aria-hidden="true"></span>
                    <span class="help-journey-item"><strong>2</strong>Asistente</span>
                    <span class="help-journey-line" aria-hidden="true"></span>
                    <span class="help-journey-item"><strong>3</strong>Soporte</span>
                </nav>
            </div>
        </header>

        <div class="help-content container-narrow">
            <section class="help-section faq-section" aria-labelledby="faq-title">
                <div class="faq-introduction">
                    <div><span class="eyebrow">Primera opción</span><h2 id="faq-title">Preguntas frecuentes</h2></div>
                    <p>Reunimos respuestas esenciales para ayudarte a comprender las funciones principales de StageUp.</p>
                </div>
                <div class="faq-list">
                    <article class="faq-item"><h3><button type="button" aria-expanded="false" aria-controls="faq-answer-1" data-accordion-trigger><span class="faq-question"><span class="faq-number">01</span>¿Qué es StageUp?</span><span class="faq-toggle" aria-hidden="true">+</span></button></h3><div class="faq-answer" id="faq-answer-1" hidden><p>StageUp es una plataforma de intermediación digital que conecta a personas que necesitan espacios para actividades artísticas con gestores que desean ofrecerlos temporalmente.</p></div></article>
                    <article class="faq-item"><h3><button type="button" aria-expanded="false" aria-controls="faq-answer-2" data-accordion-trigger><span class="faq-question"><span class="faq-number">02</span>¿Cómo puedo buscar un espacio?</span><span class="faq-toggle" aria-hidden="true">+</span></button></h3><div class="faq-answer" id="faq-answer-2" hidden><p>La exploración pública permite buscar por palabras clave y combinar filtros generales con características artísticas del espacio.</p></div></article>
                    <article class="faq-item"><h3><button type="button" aria-expanded="false" aria-controls="faq-answer-3" data-accordion-trigger><span class="faq-question"><span class="faq-number">03</span>¿Cómo funcionan las reservas?</span><span class="faq-toggle" aria-hidden="true">+</span></button></h3><div class="faq-answer" id="faq-answer-3" hidden><p>Un usuario autenticado podrá seleccionar un día y una franja disponible para enviar una solicitud. El gestor correspondiente podrá aceptarla o rechazarla.</p></div></article>
                    <article class="faq-item"><h3><button type="button" aria-expanded="false" aria-controls="faq-answer-4" data-accordion-trigger><span class="faq-question"><span class="faq-number">04</span>¿Qué significa ser gestor?</span><span class="faq-toggle" aria-hidden="true">+</span></button></h3><div class="faq-answer" id="faq-answer-4" hidden><p>Es un usuario habilitado para publicar y administrar espacios artísticos, configurar su disponibilidad y gestionar las solicitudes recibidas.</p></div></article>
                    <article class="faq-item"><h3><button type="button" aria-expanded="false" aria-controls="faq-answer-5" data-accordion-trigger><span class="faq-question"><span class="faq-number">05</span>¿Para qué sirve una cuenta?</span><span class="faq-toggle" aria-hidden="true">+</span></button></h3><div class="faq-answer" id="faq-answer-5" hidden><p>La cuenta permitirá acceder a funciones que requieren identificación, como solicitar reservas, gestionar operaciones y registrar solicitudes de soporte.</p></div></article>
                </div>
            </section>

            <section class="help-next-step" aria-labelledby="assistant-invitation-title">
                <div class="help-next-icon" aria-hidden="true"><span></span></div>
                <div class="help-next-copy"><span class="eyebrow">Segunda opción</span><h2 id="assistant-invitation-title">¿No encontraste tu respuesta?</h2><p>Probá con el asistente automatizado de StageUp. Podrás explorar consultas guiadas dentro de una vista previa conversacional.</p></div>
                <button class="button button-primary" type="button" data-assistant-open>Abrir Asistente StageUp</button>
            </section>

            <section class="support-path" aria-labelledby="support-title">
                <div class="support-path-marker"><span aria-hidden="true">3</span></div>
                <div><span class="eyebrow">Última instancia</span><h2 id="support-title">Contactar a soporte</h2><p>Si la asistencia automatizada no resuelve tu consulta, podrás registrar una solicitud. Esta acción requiere una cuenta activa y una sesión iniciada.</p></div>
                <a class="button button-secondary" href="../IniciarSesion.aspx">Iniciar sesión para contactar a soporte</a>
            </section>
        </div>
    </section>

    <div class="assistant-backdrop" data-assistant-backdrop hidden></div>
    <section class="assistant-modal" data-assistant-modal hidden role="dialog" aria-modal="true" aria-labelledby="assistant-dialog-title">
        <header class="assistant-modal-header">
            <div class="assistant-identity"><span class="assistant-avatar" aria-hidden="true">S</span><div><strong id="assistant-dialog-title">Asistente StageUp</strong><span>Asistencia automatizada · Vista previa</span></div></div>
            <button class="icon-button" type="button" aria-label="Cerrar asistente" data-assistant-close>×</button>
        </header>
        <div class="assistant-conversation" aria-label="Vista previa de conversación">
            <div class="assistant-message">Hola, soy el Asistente StageUp. En el futuro podré orientarte mediante contenidos predefinidos sobre el uso de la plataforma.</div>
            <p class="assistant-prompt">Elegí una consulta para ver cómo se organizará la asistencia:</p>
            <div class="assistant-suggestions" aria-label="Consultas rápidas">
                <button type="button" aria-pressed="false" data-assistant-option>Buscar un espacio</button>
                <button type="button" aria-pressed="false" data-assistant-option>Conocer las reservas</button>
                <button type="button" aria-pressed="false" data-assistant-option>Ser gestor</button>
            </div>
        </div>
        <footer class="assistant-modal-footer">
            <div class="assistant-input-preview"><input type="text" placeholder="El asistente todavía no recibe consultas" disabled aria-label="Consulta al asistente" /><button class="button button-primary button-small" type="button" disabled>Enviar</button></div>
            <span>Esta es una representación visual. No genera respuestas ni guarda conversaciones.</span>
        </footer>
    </section>
</asp:Content>
