<%@ Page Title="Novedades | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Novedades.aspx.cs" Inherits="StageUp.UI.Novedades" %>

<asp:Content ID="NovedadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page public-news-page">
        <header class="public-news-hero container-wide">
            <div>
                <span class="section-label">Novedades</span>
                <h1>Noticias y actualizaciones de StageUp.</h1>
                <p>Un espacio para publicar novedades del catálogo, avisos importantes y contenidos enviados por newsletter.</p>
            </div>
        </header>

        <div class="public-news-layout container-wide">
            <asp:Panel ID="pnlSinNovedades" runat="server" CssClass="admin-empty-hint" Visible="false">
                Todavía no hay novedades publicadas. Volvé a visitar esta sección más adelante.
            </asp:Panel>

            <asp:Panel ID="pnlDestacada" runat="server" CssClass="public-news-feature" Visible="false">
                <span class="public-news-category"><asp:Literal ID="litFeaturedCategoria" runat="server" /></span>
                <h2><asp:Literal ID="litFeaturedTitulo" runat="server" /></h2>
                <p><asp:Literal ID="litFeaturedResumen" runat="server" /></p>
                <a class="button button-primary" href="Explorar/ResultadosBusqueda.aspx">Explorar espacios</a>
            </asp:Panel>

            <div class="public-news-list" aria-label="Listado de novedades">
                <asp:Panel ID="pnlSinMasNovedades" runat="server" CssClass="admin-empty-hint" Visible="false">
                    Por ahora esta es la única novedad publicada.
                </asp:Panel>

                <asp:Repeater ID="rptNovedades" runat="server">
                    <ItemTemplate>
                        <article class="public-news-item">
                            <span><%#: ObtenerEtiquetaCategoria(Eval("Categoria").ToString()) %></span>
                            <h3><%#: Eval("Titulo") %></h3>
                            <p><%#: Eval("Resumen") %></p>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>
</asp:Content>
