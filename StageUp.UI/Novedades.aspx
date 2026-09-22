<%@ Page Title="Novedades | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Novedades.aspx.cs" Inherits="StageUp.UI.Novedades" %>

<asp:Content ID="NovedadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page public-news-page">
        <header class="public-news-hero container-wide">
            <div>
                <span class="section-label">Novedades</span>
                <h1>Noticias y actualizaciones de StageUp.</h1>
                <p>Un espacio preparado para publicar novedades del catálogo, avisos importantes y contenidos enviados por newsletter.</p>
            </div>
        </header>

        <div class="public-news-layout container-wide">
            <article class="public-news-feature">
                <span class="public-news-category">Nuevos espacios</span>
                <h2>Nuevos espacios para crear esta temporada</h2>
                <p>Sumamos salas, estudios y teatros con disponibilidad actualizada para que cada artista encuentre un lugar acorde a su actividad.</p>
                <a class="button button-primary" href="Explorar/ResultadosBusqueda.aspx">Explorar espacios</a>
            </article>

            <div class="public-news-list" aria-label="Listado de novedades">
                <article class="public-news-item">
                    <span>Comunidad artística</span>
                    <h3>Cómo preparar tu espacio para recibir reservas</h3>
                    <p>Recomendaciones para que los gestores mantengan sus publicaciones claras y actualizadas.</p>
                </article>
                <article class="public-news-item">
                    <span>Guías StageUp</span>
                    <h3>Guía rápida para comparar espacios</h3>
                    <p>Una ayuda para elegir entre teatro, sala o estudio según el tipo de actividad.</p>
                </article>
                <article class="public-news-item">
                    <span>Institucional</span>
                    <h3>Newsletter StageUp</h3>
                    <p>Las próximas publicaciones podrán enviarse por mail y quedar visibles en esta sección.</p>
                </article>
            </div>
        </div>
    </section>
</asp:Content>
