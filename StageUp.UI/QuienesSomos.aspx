<%@ Page Title="Quiénes somos | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="QuienesSomos.aspx.cs" Inherits="StageUp.UI.QuienesSomos" %>

<asp:Content ID="AboutContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <header class="static-page-header container-narrow">
            <span class="section-label">Quiénes somos</span>
            <h1>StageUp, una plataforma de Artera.</h1>
            <p>Encontrá tu espacio, potenciá tu arte.</p>
        </header>

        <div class="static-page-body container-narrow">
            <section>
                <h2>Nuestro negocio</h2>
                <p>
                    StageUp es la primera plataforma desarrollada por Artera, una empresa orientada a la
                    comercialización digital cuyo objetivo es intermediar entre la oferta y la demanda dentro de
                    distintos mercados. Artera no se posiciona como proveedora de un producto específico, sino como
                    facilitadora de procesos de comercialización, generando valor a través de la conexión entre
                    quienes ofrecen un recurso y quienes lo necesitan.
                </p>
                <p>
                    Con StageUp, ese modelo se aplica por primera vez al ámbito artístico: conectamos a personas o
                    grupos que necesitan un espacio físico para ensayar, dar clases, dictar talleres o realizar
                    presentaciones, con quienes disponen de estudios, salas o centros culturales y buscan darles
                    mayor visibilidad y aprovechamiento.
                </p>
            </section>

            <section>
                <h2>Por qué existimos</h2>
                <p>
                    Hoy, la búsqueda y reserva de espacios artísticos suele resolverse de manera informal, a través
                    de redes sociales, contactos personales o aplicaciones de mensajería. Esto dificulta comparar
                    opciones, acceder a información completa y coordinar actividades con confianza. StageUp busca
                    centralizar esa interacción en un mismo entorno, con información clara y organizada, para que el
                    proceso sea más simple tanto para quien busca un espacio como para quien lo ofrece.
                </p>
            </section>

            <section>
                <h2>Cómo funciona</h2>
                <p>
                    La plataforma pone en contacto a dos tipos de usuarios: quienes ofrecen espacios artísticos y
                    quienes los necesitan para desarrollar su actividad. A través de StageUp es posible explorar la
                    oferta disponible, consultar las características de cada espacio y coordinar una reserva dentro
                    de un entorno pensado especialmente para la comunidad artística.
                </p>
            </section>
        </div>

        <nav class="static-page-links container-narrow" aria-label="Otras páginas informativas">
            <a class="text-link" href="Default.aspx">Inicio</a>
            <a class="text-link" href="Contactenos.aspx">Contáctenos</a>
            <a class="text-link" href="TerminosCondiciones.aspx">Términos y condiciones</a>
            <a class="text-link" href="PoliticaPrivacidad.aspx">Política de privacidad</a>
        </nav>
    </section>
</asp:Content>
