<%@ Page Title="Inicio | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="StageUp.UI._Default" %>

<asp:Content ID="HomeContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="hero-section">
        <div class="hero-decoration hero-decoration-one" aria-hidden="true"></div>
        <div class="hero-decoration hero-decoration-two" aria-hidden="true"></div>

        <div class="hero-content container-wide">
            <div class="hero-copy">
                <span class="section-label">Plataforma de espacios artísticos</span>
                <h1>Encontrá tu espacio,<br /><em>potenciá tu arte.</em></h1>
                <p>
                    StageUp conecta a quienes necesitan un espacio para crear, ensayar, enseñar o presentar
                    con gestores que quieren dar mayor visibilidad a sus espacios artísticos.
                </p>
                <div class="hero-actions">
                    <span class="button button-primary" aria-disabled="true" title="Disponible próximamente">Explorar espacios</span>
                    <a class="button button-secondary" href="Registrarse.aspx">Registrarse</a>
                </div>
            </div>

            <div class="hero-visual">
                <div class="logo-frame">
                    <asp:Image ID="HeroLogo" runat="server" CssClass="hero-logo" ImageUrl="~/Content/Images/stageup-logo.png" AlternateText="StageUp. Encontrá tu espacio, potenciá tu arte." />
                </div>
            </div>
        </div>
    </section>
</asp:Content>
