<%@ Page Title="Novedades | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Novedades.aspx.cs" Inherits="StageUp.UI.Novedades" %>

<asp:Content ID="NovedadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page user-module-page">
        <div class="static-page-header">
            <span class="section-label">Novedades</span>
            <h1>Noticias y actualizaciones de StageUp</h1>
            <p>Un espacio para publicar novedades del catálogo, avisos importantes y contenidos enviados por newsletter.</p>
        </div>

        <div class="static-page-body public-news-layout">
            <asp:Panel ID="pnlSinNovedades" runat="server" CssClass="admin-empty-hint" Visible="false">
                Todavía no hay novedades publicadas. Volvé a visitar esta sección más adelante.
            </asp:Panel>

            <asp:Panel ID="pnlDestacada" runat="server" CssClass="public-news-feature" Visible="false">
                <asp:Image ID="imgFeatured" runat="server" CssClass="public-news-image public-news-image-featured" AlternateText="" Visible="false" />
                <span class="public-news-category"><asp:Literal ID="litFeaturedCategoria" runat="server" /></span>
                <h2><asp:Literal ID="litFeaturedTitulo" runat="server" /></h2>
                <p><asp:Literal ID="litFeaturedResumen" runat="server" /></p>
                <div class="public-news-actions">
                    <button class="button button-primary" type="button" data-news-open="novedad-destacada">Leer nota completa</button>
                    <a class="button button-secondary" href="Explorar/ResultadosBusqueda.aspx">Explorar espacios</a>
                </div>
                <dialog class="public-news-dialog" id="novedad-destacada" aria-labelledby="novedad-destacada-titulo">
                    <button class="public-news-dialog-close" type="button" data-news-close aria-label="Cerrar novedad">×</button>
                    <span class="public-news-dialog-category"><asp:Literal ID="litFeaturedCategoriaDialogo" runat="server" /></span>
                    <h2 id="novedad-destacada-titulo"><asp:Literal ID="litFeaturedTituloDialogo" runat="server" /></h2>
                    <p class="public-news-dialog-lead"><asp:Literal ID="litFeaturedResumenDialogo" runat="server" /></p>
                    <div class="public-news-dialog-content"><asp:Literal ID="litFeaturedContenido" runat="server" /></div>
                    <div class="public-news-dialog-footer"><button class="button button-secondary" type="button" data-news-close>Cerrar</button></div>
                </dialog>
            </asp:Panel>

            <div class="auth-card public-news-newsletter">
                <strong>¿Querés recibirlas por correo?</strong>
                <span>Suscribite al newsletter, elegí las categorías que te interesan y date de baja cuando quieras. No hace falta tener cuenta.</span>
                <a class="button button-secondary button-small" href="Newsletter.aspx">Suscribirme al newsletter</a>
            </div>

            <div class="public-news-list" aria-label="Listado de novedades">
                <asp:Panel ID="pnlSinMasNovedades" runat="server" CssClass="admin-empty-hint" Visible="false">
                    Por ahora esta es la única novedad publicada.
                </asp:Panel>

                <asp:Repeater ID="rptNovedades" runat="server">
                    <ItemTemplate>
                        <article class="public-news-item">
                            <asp:Image runat="server" CssClass="public-news-image" ImageUrl='<%# ResolverUrlImagen(Eval("UrlImagen")) %>'
                                AlternateText='<%# Eval("Titulo") %>' Visible='<%# TieneImagen(Eval("UrlImagen")) %>' />
                            <span><%#: ObtenerEtiquetaCategoria(Eval("Categoria").ToString()) %></span>
                            <h3><%#: Eval("Titulo") %></h3>
                            <p><%#: Eval("Resumen") %></p>
                            <button class="public-news-read" type="button" data-news-open='<%# "novedad-" + Eval("IdNovedad") %>'>Leer nota completa <span aria-hidden="true">→</span></button>
                            <dialog class="public-news-dialog" id='<%# "novedad-" + Eval("IdNovedad") %>' aria-labelledby='<%# "novedad-titulo-" + Eval("IdNovedad") %>'>
                                <button class="public-news-dialog-close" type="button" data-news-close aria-label="Cerrar novedad">×</button>
                                <span class="public-news-dialog-category"><%#: ObtenerEtiquetaCategoria(Eval("Categoria").ToString()) %></span>
                                <h2 id='<%# "novedad-titulo-" + Eval("IdNovedad") %>'><%#: Eval("Titulo") %></h2>
                                <p class="public-news-dialog-lead"><%#: Eval("Resumen") %></p>
                                <div class="public-news-dialog-content"><%#: Eval("Contenido") %></div>
                                <div class="public-news-dialog-footer"><button class="button button-secondary" type="button" data-news-close>Cerrar</button></div>
                            </dialog>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <style type="text/css">
        .public-news-actions { display: flex; flex-wrap: wrap; gap: 0.7rem; margin-top: 1.25rem; margin-bottom: clamp(1.35rem, 3vw, 2rem); }
        .public-news-actions .button { margin: 0; }
        .public-news-read { display: inline-flex; align-items: center; gap: 0.45rem; margin-top: 0.9rem; padding: 0; color: var(--color-primary); background: transparent; border: 0; font: inherit; font-weight: 800; cursor: pointer; }
        .public-news-read span { margin: 0; color: inherit; font-size: 1rem; transition: transform 160ms ease; }
        .public-news-read:hover span { transform: translateX(0.2rem); }
        .public-news-dialog { width: min(42rem, calc(100vw - 2rem)); max-height: min(80vh, 44rem); margin: auto !important; padding: clamp(1.4rem, 4vw, 2.3rem); color: var(--color-text); background: #fffaf7; border: 1px solid rgba(122, 12, 32, 0.16); border-radius: 1.25rem; box-shadow: 0 2rem 5rem rgba(50, 8, 16, 0.28); overflow: auto; }
        .public-news-dialog[open] { animation: news-dialog-in 220ms ease both; }
        .public-news-dialog::backdrop { background: rgba(39, 15, 19, 0.5); backdrop-filter: blur(3px); }
        .public-news-dialog-close { position: absolute; top: 0.85rem; right: 0.85rem; display: grid; place-items: center; width: 2.35rem; height: 2.35rem; color: var(--color-primary); background: #fff; border: 1px solid rgba(122, 12, 32, 0.15); border-radius: 50%; font-size: 1.35rem; cursor: pointer; }
        .public-news-dialog-category { display: inline-flex; margin: 0 3rem 0.65rem 0; color: var(--color-primary-soft); font-size: 0.75rem; font-weight: 850; letter-spacing: 0.07em; text-transform: uppercase; }
        .public-news-dialog h2 { margin: 0 2.5rem 0.75rem 0; color: var(--color-primary-dark); font-family: var(--font-display); font-size: clamp(1.75rem, 5vw, 2.7rem); line-height: 1.05; }
        .public-news-dialog-lead { margin: 0 0 1rem; color: var(--color-text-muted); font-size: 1.02rem; line-height: 1.55; }
        .public-news-dialog-content { padding-top: 1rem; border-top: 1px solid var(--color-border); color: var(--color-text); font-size: 1rem; line-height: 1.75; white-space: pre-line; }
        .public-news-dialog-footer { display: flex; justify-content: flex-end; margin-top: 1.4rem; }
        .public-news-dialog .button { margin: 0; }
        @keyframes news-dialog-in { from { opacity: 0; transform: translateY(0.8rem) scale(0.98); } to { opacity: 1; transform: translateY(0) scale(1); } }
        @media (prefers-reduced-motion: reduce) { .public-news-dialog[open] { animation: none; } }
    </style>

    <script type="text/javascript">
        (function () {
            document.addEventListener("click", function (event) {
                var opener = event.target.closest("[data-news-open]");
                if (opener) {
                    var dialog = document.getElementById(opener.getAttribute("data-news-open"));
                    if (dialog) {
                        if (typeof dialog.showModal === "function") dialog.showModal();
                        else dialog.setAttribute("open", "open");
                    }
                    return;
                }

                var closer = event.target.closest("[data-news-close]");
                if (closer) {
                    var currentDialog = closer.closest("dialog");
                    if (currentDialog) currentDialog.close();
                    return;
                }

                if (event.target instanceof HTMLDialogElement) {
                    var bounds = event.target.getBoundingClientRect();
                    var outside = event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom;
                    if (outside) event.target.close();
                }
            });
        })();
    </script>
</asp:Content>
