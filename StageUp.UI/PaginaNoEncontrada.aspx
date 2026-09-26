<%@ Page Title="Página no encontrada | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PaginaNoEncontrada.aspx.cs" Inherits="StageUp.UI.PaginaNoEncontrada" %>

<asp:Content ID="PaginaNoEncontradaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label">Error 404</span>
                <h1>No encontramos esta página</h1>
                <p class="institutional-lead">El enlace puede estar roto o la página puede haberse movido. Volvé al inicio o probá desde el buscador de espacios.</p>
            </div>
        </header>
        <div class="institutional-layout container-wide">
            <div class="institutional-content">
                <a class="button button-primary" href="/Default.aspx">Volver al inicio</a>
            </div>
        </div>
    </section>
</asp:Content>
