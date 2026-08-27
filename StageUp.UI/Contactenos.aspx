<%@ Page Title="Contáctenos | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Contactenos.aspx.cs" Inherits="StageUp.UI.Contactenos" %>

<asp:Content ID="ContactContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <header class="static-page-header container-narrow">
            <span class="section-label">Contáctenos</span>
            <h1>¿Tenés alguna consulta?</h1>
            <p>Estas son las vías de contacto disponibles mientras StageUp está en desarrollo.</p>
        </header>

        <div class="static-page-body container-narrow">
            <section>
                <h2>Correo electrónico</h2>
                <p>
                    Para consultas generales sobre la plataforma, escribinos a
                    <a class="text-link" href="mailto:contacto@stageup.com.ar">contacto@stageup.com.ar</a>.
                    Esta dirección es un dato de referencia: el proyecto todavía no tiene una casilla de
                    correo del negocio configurada de forma definitiva.
                </p>
            </section>

            <section>
                <h2>Soporte dentro de la plataforma</h2>
                <p>
                    Una vez que tengas una cuenta activa y hayas iniciado sesión, vas a poder acceder al
                    <a class="text-link" href="Ayuda/CentroAyuda.aspx">Centro de ayuda</a> para consultar las
                    preguntas frecuentes o registrar una solicitud de soporte.
                </p>
            </section>

            <section>
                <h2>Sobre esta página</h2>
                <p>
                    Esta es una página de contacto estática, sin envío real de mensajes todavía. La
                    posibilidad de enviar consultas desde un formulario y recibir una respuesta dentro de la
                    plataforma corresponde a una etapa posterior del desarrollo.
                </p>
            </section>
        </div>

        <nav class="static-page-links container-narrow" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx">Inicio</a>
            <a class="text-link" href="QuienesSomos.aspx">Quiénes somos</a>
            <a class="text-link" href="TerminosCondiciones.aspx">Términos y condiciones</a>
            <a class="text-link" href="PoliticaPrivacidad.aspx">Política de privacidad</a>
        </nav>
    </section>
</asp:Content>
