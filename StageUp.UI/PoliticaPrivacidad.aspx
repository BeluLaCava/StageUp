<%@ Page Title="Política de privacidad | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PoliticaPrivacidad.aspx.cs" Inherits="StageUp.UI.PoliticaPrivacidad" %>

<asp:Content ID="PrivacyContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page institutional-legal-page">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label" data-i18n="Privacy_Etiqueta">Privacidad y seguridad</span>
                <h1 data-i18n="Privacy_Titulo">Tus datos son parte de la confianza que construimos.</h1>
                <p class="institutional-lead" data-i18n="Privacy_Introduccion">Esta política explica qué información utiliza StageUp, para qué la necesita y qué medidas aplica para protegerla.</p>
                <span class="institutional-updated" data-i18n="Privacy_Actualizacion">Última actualización: 10 de septiembre de 2026</span>
            </div>
            <div class="institutional-hero-card institutional-summary-card">
                <span class="institutional-card-kicker" data-i18n="Privacy_ResumenEtiqueta">En pocas palabras</span>
                <ul>
                    <li data-i18n="Privacy_Resumen1">Pedimos únicamente los datos necesarios para operar la plataforma.</li>
                    <li data-i18n="Privacy_Resumen2">No vendemos información personal.</li>
                    <li data-i18n="Privacy_Resumen3">Las contraseñas nunca se almacenan en texto plano.</li>
                </ul>
            </div>
        </header>

        <div class="institutional-layout container-wide">
            <aside class="institutional-index institutional-index-legal" aria-label="Contenido de la política">
                <span data-i18n="Institutional_Contenido">Contenido</span>
                <nav>
                    <a href="#datos" data-i18n="Privacy_IndiceDatos">Datos que recopilamos</a>
                    <a href="#uso" data-i18n="Privacy_IndiceUso">Cómo los usamos</a>
                    <a href="#visibilidad" data-i18n="Privacy_IndiceVisibilidad">Información visible</a>
                    <a href="#terceros" data-i18n="Privacy_IndiceTerceros">Servicios de terceros</a>
                    <a href="#seguridad" data-i18n="Privacy_IndiceSeguridad">Seguridad</a>
                    <a href="#conservacion" data-i18n="Privacy_IndiceConservacion">Conservación</a>
                    <a href="#derechos" data-i18n="Privacy_IndiceDerechos">Tus derechos</a>
                    <a href="#cambios" data-i18n="Privacy_IndiceCambios">Cambios y contacto</a>
                </nav>
            </aside>

            <article class="institutional-content institutional-legal-content">
                <section id="datos" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">01</span>
                    <div>
                        <h2 data-i18n="Privacy_DatosTitulo">Datos que recopilamos</h2>
                        <p data-i18n="Privacy_DatosTexto">Al crear una cuenta podemos solicitar nombre, apellido, correo electrónico, contraseña y datos de contacto. También almacenamos la foto de perfil que elijas, información sobre espacios publicados, solicitudes de reserva, calificaciones y comunicaciones necesarias para brindar el servicio.</p>
                    </div>
                </section>

                <section id="uso" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">02</span>
                    <div>
                        <h2 data-i18n="Privacy_UsoTitulo">Cómo usamos la información</h2>
                        <p data-i18n="Privacy_UsoTexto">La utilizamos para crear y administrar cuentas, verificar identidades, gestionar perfiles y espacios, procesar solicitudes de reserva, enviar notificaciones, recuperar o cambiar contraseñas, brindar soporte y mejorar la experiencia dentro de StageUp.</p>
                    </div>
                </section>

                <section id="visibilidad" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">03</span>
                    <div>
                        <h2 data-i18n="Privacy_VisibilidadTitulo">Información visible para la comunidad</h2>
                        <p data-i18n="Privacy_VisibilidadTexto">Algunos datos son públicos porque permiten evaluar una propuesta: nombre del gestor, información y fotografías del espacio, disponibilidad, precio, equipamiento y reseñas verificadas. Nunca mostramos tu contraseña, códigos de seguridad ni información administrativa interna.</p>
                    </div>
                </section>

                <section id="terceros" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">04</span>
                    <div>
                        <h2 data-i18n="Privacy_TercerosTitulo">Servicios de terceros</h2>
                        <p data-i18n="Privacy_TercerosTexto">No vendemos datos personales. Para funciones específicas podemos utilizar proveedores tecnológicos, como servicios de correo electrónico y Google reCAPTCHA. Estos proveedores reciben solo la información necesaria para prestar su servicio y aplican sus propias políticas de privacidad.</p>
                    </div>
                </section>

                <section id="seguridad" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">05</span>
                    <div>
                        <h2 data-i18n="Privacy_SeguridadTitulo">Seguridad de la información</h2>
                        <p data-i18n="Privacy_SeguridadTexto">Las contraseñas se protegen mediante un hash con sal basado en PBKDF2 y no pueden recuperarse en texto plano. Los códigos de activación y recuperación tienen vigencia limitada y un único uso. Además, registramos eventos relevantes en una bitácora para facilitar la trazabilidad y detectar comportamientos inusuales.</p>
                    </div>
                </section>

                <section id="conservacion" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">06</span>
                    <div>
                        <h2 data-i18n="Privacy_ConservacionTitulo">Conservación de datos</h2>
                        <p data-i18n="Privacy_ConservacionTexto">Conservamos la información mientras tu cuenta permanezca activa o durante el tiempo necesario para cumplir la finalidad para la que fue obtenida, atender obligaciones de seguridad y resolver controversias. Cuando deja de ser necesaria, se elimina o anonimiza de forma segura.</p>
                    </div>
                </section>

                <section id="derechos" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">07</span>
                    <div>
                        <h2 data-i18n="Privacy_DerechosTitulo">Tus derechos y decisiones</h2>
                        <p data-i18n="Privacy_DerechosTexto">Podés consultar y actualizar tus datos desde tu perfil. También podés solicitar acceso, rectificación o eliminación de información personal mediante nuestros canales de contacto. Antes de responder una solicitud podremos verificar tu identidad para proteger la cuenta.</p>
                    </div>
                </section>

                <section id="cambios" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">08</span>
                    <div>
                        <h2 data-i18n="Privacy_CambiosTitulo">Cambios y contacto</h2>
                        <p data-i18n="Privacy_CambiosTexto">Podemos actualizar esta política cuando cambien las funcionalidades o las medidas de protección de StageUp. La versión vigente siempre estará disponible en esta página, junto con su fecha de actualización.</p>
                        <a class="button button-secondary" href="Contactenos.aspx" data-i18n="Privacy_ContactoBoton">Contactar al equipo</a>
                    </div>
                </section>
            </article>
        </div>

        <nav class="static-page-links institutional-footer-nav container-wide" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx" data-i18n="Nav_Inicio">Inicio</a>
            <a class="text-link" href="QuienesSomos.aspx" data-i18n="Footer_QuienesSomos">Quiénes somos</a>
            <a class="text-link" href="Contactenos.aspx" data-i18n="Footer_Contactenos">Contáctenos</a>
            <a class="text-link" href="TerminosCondiciones.aspx" data-i18n="Footer_Terminos">Términos y condiciones</a>
        </nav>
    </section>
</asp:Content>
