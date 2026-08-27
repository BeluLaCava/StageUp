<%@ Page Title="Términos y condiciones | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="TerminosCondiciones.aspx.cs" Inherits="StageUp.UI.TerminosCondiciones" %>

<asp:Content ID="TermsContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <header class="static-page-header container-narrow">
            <span class="section-label">Términos y condiciones</span>
            <h1>Términos y condiciones de uso de StageUp</h1>
            <p>Última actualización: versión inicial correspondiente al Avance 1 del proyecto.</p>
        </header>

        <div class="static-page-body container-narrow">
            <section>
                <h2>1. Objeto</h2>
                <p>
                    StageUp es una plataforma web operada por Artera que intermedia entre personas que ofrecen
                    espacios físicos para actividades artísticas (gestores) y personas que buscan utilizarlos
                    (usuarios externos/solicitantes). Al crear una cuenta, la persona usuaria acepta estos
                    Términos y condiciones y la <a class="text-link" href="PoliticaPrivacidad.aspx">Política de
                    privacidad</a>.
                </p>
            </section>

            <section>
                <h2>2. Registro y cuenta de usuario</h2>
                <p>
                    Para utilizar las funcionalidades de StageUp que lo requieran, es necesario registrarse con
                    datos verídicos, definir una contraseña y activar la cuenta mediante el código enviado por
                    correo electrónico. La persona usuaria es responsable de mantener la confidencialidad de sus
                    credenciales y de toda actividad realizada desde su cuenta.
                </p>
                <p>
                    Toda cuenta nueva se registra inicialmente con perfil de usuario externo/solicitante. La
                    habilitación como gestor de espacios artísticos corresponde a un proceso posterior, disponible
                    únicamente para cuentas ya activas.
                </p>
            </section>

            <section>
                <h2>3. Uso de la plataforma</h2>
                <p>
                    La persona usuaria se compromete a utilizar StageUp de buena fe, a no publicar información
                    falsa o engañosa y a respetar a los demás usuarios de la comunidad artística. Artera podrá
                    suspender o dar de baja cuentas que incumplan estos Términos y condiciones.
                </p>
            </section>

            <section>
                <h2>4. Reservas y funcionalidades en desarrollo</h2>
                <p>
                    StageUp se encuentra en desarrollo progresivo. Algunas funcionalidades descriptas en la
                    plataforma, como la publicación de espacios, la gestión de reservas o los medios de pago,
                    podrán encontrarse simuladas, incompletas o no disponibles todavía según la etapa del
                    proyecto. Esta sección se irá actualizando a medida que esas funcionalidades se incorporen.
                </p>
            </section>

            <section>
                <h2>5. Propiedad intelectual</h2>
                <p>
                    Las marcas, logos y contenidos propios de StageUp y Artera pertenecen a sus respectivos
                    titulares. El contenido cargado por los usuarios (por ejemplo, la descripción de un espacio)
                    sigue siendo propiedad de quien lo publica, quien otorga a StageUp una licencia para mostrarlo
                    dentro de la plataforma.
                </p>
            </section>

            <section>
                <h2>6. Responsabilidad</h2>
                <p>
                    StageUp actúa como intermediario entre las partes. La calidad, el estado y las condiciones
                    reales de los espacios ofrecidos son responsabilidad exclusiva de quien los publica.
                </p>
            </section>

            <section>
                <h2>7. Modificaciones</h2>
                <p>
                    Estos Términos y condiciones podrán actualizarse a medida que evolucione la plataforma. Se
                    recomienda revisar esta página periódicamente.
                </p>
            </section>

            <section>
                <h2>8. Contacto</h2>
                <p>
                    Ante cualquier consulta sobre estos Términos y condiciones, podés escribirnos desde la página
                    de <a class="text-link" href="Contactenos.aspx">Contáctenos</a>.
                </p>
            </section>
        </div>

        <nav class="static-page-links container-narrow" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx">Inicio</a>
            <a class="text-link" href="QuienesSomos.aspx">Quiénes somos</a>
            <a class="text-link" href="Contactenos.aspx">Contáctenos</a>
            <a class="text-link" href="PoliticaPrivacidad.aspx">Política de privacidad</a>
        </nav>
    </section>
</asp:Content>
