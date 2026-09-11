<%@ Page Title="Términos y condiciones | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="TerminosCondiciones.aspx.cs" Inherits="StageUp.UI.TerminosCondiciones" %>

<asp:Content ID="TermsContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page institutional-legal-page">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label" data-i18n="Terms_Etiqueta">Términos y condiciones</span>
                <h1 data-i18n="Terms_Titulo">Reglas claras para una comunidad que crea y comparte.</h1>
                <p class="institutional-lead" data-i18n="Terms_Introduccion">Estos términos explican cómo funciona StageUp y cuáles son los compromisos de quienes buscan, publican y reservan espacios.</p>
                <span class="institutional-updated" data-i18n="Terms_Actualizacion">Última actualización: 10 de septiembre de 2026</span>
            </div>
            <div class="institutional-hero-card institutional-summary-card">
                <span class="institutional-card-kicker" data-i18n="Terms_ResumenEtiqueta">Puntos esenciales</span>
                <ul>
                    <li data-i18n="Terms_Resumen1">StageUp conecta a solicitantes y gestores.</li>
                    <li data-i18n="Terms_Resumen2">Cada persona es responsable de la información que publica.</li>
                    <li data-i18n="Terms_Resumen3">Las reservas se confirman cuando el gestor acepta la solicitud.</li>
                </ul>
            </div>
        </header>

        <div class="institutional-layout container-wide">
            <aside class="institutional-index institutional-index-legal" aria-label="Contenido de los términos">
                <span data-i18n="Institutional_Contenido">Contenido</span>
                <nav>
                    <a href="#objeto" data-i18n="Terms_IndiceObjeto">Alcance del servicio</a>
                    <a href="#cuentas" data-i18n="Terms_IndiceCuentas">Cuentas y perfiles</a>
                    <a href="#espacios" data-i18n="Terms_IndiceEspacios">Publicación de espacios</a>
                    <a href="#reservas" data-i18n="Terms_IndiceReservas">Reservas</a>
                    <a href="#precios" data-i18n="Terms_IndicePrecios">Precios y cancelaciones</a>
                    <a href="#resenas" data-i18n="Terms_IndiceResenas">Reseñas</a>
                    <a href="#conducta" data-i18n="Terms_IndiceConducta">Conducta</a>
                    <a href="#responsabilidad" data-i18n="Terms_IndiceResponsabilidad">Responsabilidad</a>
                    <a href="#propiedad" data-i18n="Terms_IndicePropiedad">Propiedad intelectual</a>
                    <a href="#vigencia" data-i18n="Terms_IndiceVigencia">Vigencia y contacto</a>
                </nav>
            </aside>

            <article class="institutional-content institutional-legal-content">
                <section id="objeto" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">01</span>
                    <div>
                        <h2 data-i18n="Terms_ObjetoTitulo">Alcance del servicio</h2>
                        <p data-i18n="Terms_ObjetoTexto">StageUp es una plataforma operada por Artera que facilita el encuentro entre personas que ofrecen espacios para actividades artísticas y personas que desean utilizarlos. Al crear una cuenta o usar la plataforma aceptás estos términos y nuestra Política de privacidad.</p>
                    </div>
                </section>

                <section id="cuentas" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">02</span>
                    <div>
                        <h2 data-i18n="Terms_CuentasTitulo">Cuentas y perfiles</h2>
                        <p data-i18n="Terms_CuentasTexto">Para acceder a funciones protegidas debés registrarte con datos verídicos, activar tu cuenta y mantener seguras tus credenciales. Toda cuenta externa comienza como solicitante. La habilitación como gestor requiere una solicitud y la aprobación del equipo interno de StageUp.</p>
                    </div>
                </section>

                <section id="espacios" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">03</span>
                    <div>
                        <h2 data-i18n="Terms_EspaciosTitulo">Publicación de espacios</h2>
                        <p data-i18n="Terms_EspaciosTexto">Los gestores deben mantener actualizados el nombre, descripción, ubicación, fotografías, capacidad, equipamiento, disponibilidad y precio de sus espacios. Queda prohibido publicar contenido falso, engañoso, ofensivo o sobre lugares que no se tenga derecho a ofrecer.</p>
                    </div>
                </section>

                <section id="reservas" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">04</span>
                    <div>
                        <h2 data-i18n="Terms_ReservasTitulo">Solicitudes y confirmación de reservas</h2>
                        <p data-i18n="Terms_ReservasTexto">Una solicitud indica el interés del solicitante en una fecha, horario y duración determinados. La reserva queda confirmada únicamente cuando el gestor la acepta. Ambas partes se comprometen a respetar la disponibilidad informada y a comunicar cualquier inconveniente con la mayor anticipación posible.</p>
                    </div>
                </section>

                <section id="precios" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">05</span>
                    <div>
                        <h2 data-i18n="Terms_PreciosTitulo">Precios y cancelaciones</h2>
                        <p data-i18n="Terms_PreciosTexto">El valor estimado se calcula a partir del precio por hora publicado y la duración solicitada. Las condiciones de pago deben ser informadas con claridad. Si una reserva aceptada se cancela con menos de 24 horas de anticipación, la plataforma puede calcular una comisión del 10 por ciento del valor total.</p>
                    </div>
                </section>

                <section id="resenas" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">06</span>
                    <div>
                        <h2 data-i18n="Terms_ResenasTitulo">Calificaciones y reseñas</h2>
                        <p data-i18n="Terms_ResenasTexto">Después de una reserva finalizada, las partes pueden calificar su experiencia. Las reseñas deben ser honestas, respetuosas y estar relacionadas con la reserva. StageUp puede moderar contenido que incluya datos personales, amenazas, discriminación, publicidad o información manifiestamente falsa.</p>
                    </div>
                </section>

                <section id="conducta" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">07</span>
                    <div>
                        <h2 data-i18n="Terms_ConductaTitulo">Uso responsable de la plataforma</h2>
                        <p data-i18n="Terms_ConductaTexto">La comunidad debe actuar de buena fe, respetar a otras personas y utilizar la plataforma solo para fines lícitos. No se permite intentar vulnerar la seguridad, acceder a cuentas ajenas, alterar la reputación de forma artificial ni usar información de otros usuarios fuera del propósito de una reserva.</p>
                    </div>
                </section>

                <section id="responsabilidad" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">08</span>
                    <div>
                        <h2 data-i18n="Terms_ResponsabilidadTitulo">Rol y responsabilidad de StageUp</h2>
                        <p data-i18n="Terms_ResponsabilidadTexto">StageUp actúa como intermediario tecnológico y no es propietario ni operador de los espacios publicados. Cada gestor responde por la veracidad de su publicación, las condiciones del lugar y el cumplimiento de la reserva. Cada solicitante responde por el uso adecuado del espacio durante el período acordado.</p>
                    </div>
                </section>

                <section id="propiedad" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">09</span>
                    <div>
                        <h2 data-i18n="Terms_PropiedadTitulo">Propiedad intelectual</h2>
                        <p data-i18n="Terms_PropiedadTexto">La identidad, el diseño y los contenidos propios de StageUp y Artera pertenecen a sus titulares. Quien publica textos o fotografías conserva sus derechos y autoriza a StageUp a mostrarlos dentro de la plataforma con el fin de brindar el servicio.</p>
                    </div>
                </section>

                <section id="vigencia" class="legal-section">
                    <span class="legal-section-number" aria-hidden="true">10</span>
                    <div>
                        <h2 data-i18n="Terms_VigenciaTitulo">Vigencia, modificaciones y contacto</h2>
                        <p data-i18n="Terms_VigenciaTexto">Podemos actualizar estos términos para reflejar cambios en el servicio. La versión vigente estará disponible en esta página con su fecha de actualización. El incumplimiento grave puede dar lugar a la suspensión o baja de una cuenta.</p>
                        <a class="button button-secondary" href="Contactenos.aspx" data-i18n="Terms_ContactoBoton">Hacer una consulta</a>
                    </div>
                </section>
            </article>
        </div>

        <nav class="static-page-links institutional-footer-nav container-wide" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx" data-i18n="Nav_Inicio">Inicio</a>
            <a class="text-link" href="QuienesSomos.aspx" data-i18n="Footer_QuienesSomos">Quiénes somos</a>
            <a class="text-link" href="Contactenos.aspx" data-i18n="Footer_Contactenos">Contáctenos</a>
            <a class="text-link" href="PoliticaPrivacidad.aspx" data-i18n="Footer_Privacidad">Política de privacidad</a>
        </nav>
    </section>
</asp:Content>
