<%@ Page Title="Política de privacidad | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PoliticaPrivacidad.aspx.cs" Inherits="StageUp.UI.PoliticaPrivacidad" %>

<asp:Content ID="PrivacyContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <header class="static-page-header container-narrow">
            <span class="section-label">Política de privacidad</span>
            <h1>Política de privacidad y seguridad</h1>
            <p>Última actualización: versión inicial correspondiente al Avance 1 del proyecto.</p>
        </header>

        <div class="static-page-body container-narrow">
            <section>
                <h2>1. Qué datos recolectamos</h2>
                <p>
                    Al registrarte en StageUp recolectamos tu nombre, apellido, correo electrónico y contraseña
                    (almacenada siempre de forma cifrada, nunca en texto plano). Opcionalmente, podés indicar un
                    número de teléfono de contacto.
                </p>
            </section>

            <section>
                <h2>2. Para qué usamos tus datos</h2>
                <p>
                    Utilizamos tus datos para crear y administrar tu cuenta, activar tu cuenta mediante un código
                    enviado por correo electrónico, permitirte iniciar sesión, y para el proceso de recuperación
                    de contraseña en caso de que lo necesites. También registramos, en una bitácora interna, las
                    acciones relevantes sobre tu cuenta (alta, activación, inicio de sesión, cambios de
                    contraseña) por motivos de seguridad y trazabilidad.
                </p>
            </section>

            <section>
                <h2>3. Con quién compartimos tus datos</h2>
                <p>
                    No vendemos ni compartimos tus datos personales con terceros ajenos a StageUp. Para el envío
                    de correos electrónicos (códigos de activación y recuperación, notificaciones) utilizamos un
                    servicio de correo electrónico como intermediario técnico.
                </p>
            </section>

            <section>
                <h2>4. Seguridad de la información</h2>
                <p>
                    Tu contraseña se guarda utilizando un algoritmo de hash con sal (PBKDF2), por lo que StageUp
                    nunca almacena ni puede recuperar tu contraseña en texto plano. Los códigos de activación y de
                    recuperación de contraseña tienen una vigencia limitada y solo pueden utilizarse una vez.
                </p>
            </section>

            <section>
                <h2>5. Tus derechos sobre tus datos</h2>
                <p>
                    Podés solicitar en cualquier momento la actualización o eliminación de tus datos personales
                    escribiéndonos desde la página de <a class="text-link" href="Contactenos.aspx">Contáctenos</a>.
                </p>
            </section>

            <section>
                <h2>6. Cambios en esta política</h2>
                <p>
                    Esta Política de privacidad podrá actualizarse a medida que se incorporen nuevas
                    funcionalidades a StageUp. Se recomienda revisar esta página periódicamente.
                </p>
            </section>
        </div>

        <nav class="static-page-links container-narrow" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx">Inicio</a>
            <a class="text-link" href="QuienesSomos.aspx">Quiénes somos</a>
            <a class="text-link" href="Contactenos.aspx">Contáctenos</a>
            <a class="text-link" href="TerminosCondiciones.aspx">Términos y condiciones</a>
        </nav>
    </section>
</asp:Content>
