<%@ Page Title="Verificando tu sesión | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="SesionNoVerificada.aspx.cs" Inherits="StageUp.UI.SesionNoVerificada" %>

<asp:Content ID="SesionNoVerificadaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label">Sesión en verificación</span>
                <h1>No pudimos comprobar tu sesión</h1>
                <p class="institutional-lead">La base de datos de StageUp se restauró desde una copia de seguridad y en este momento no pudimos comprobar tu cuenta contra esa copia. Hasta comprobarla, no se puede operar con tu sesión ni con tus permisos anteriores.</p>
            </div>
        </header>
        <div class="institutional-layout container-wide">
            <div class="institutional-content">
                <p>Volvé a intentar en unos segundos. Si tu cuenta existe y está activa en la base restaurada, vas a seguir con tu sesión; si no, vas a tener que iniciar sesión de nuevo.</p>
                <div class="comercial-acciones">
                    <asp:HyperLink ID="lnkReintentar" runat="server" CssClass="button button-primary" NavigateUrl="~/Default.aspx" Text="Volver a intentar" />
                    <asp:LinkButton ID="lnkCerrarSesion" runat="server" CssClass="button button-secondary" CausesValidation="false" OnClick="lnkCerrarSesion_Click" Text="Cerrar sesión" />
                </div>
            </div>
        </div>
    </section>
</asp:Content>
