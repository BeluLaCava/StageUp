<%@ Page Title="Ocurrió un error | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ErrorGeneral.aspx.cs" Inherits="StageUp.UI.ErrorGeneral" %>

<asp:Content ID="ErrorGeneralContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page institutional-page">
        <header class="institutional-hero container-wide">
            <div class="institutional-hero-copy">
                <span class="section-label">Error inesperado</span>
                <h1>Algo salió mal de nuestro lado</h1>
                <p class="institutional-lead">Ya quedó registrado y lo vamos a revisar. Volvé al inicio o intentá de nuevo en unos minutos.</p>
            </div>
        </header>
        <div class="institutional-layout container-wide">
            <div class="institutional-content">
                <a class="button button-primary" href="/Default.aspx">Volver al inicio</a>
            </div>
        </div>
    </section>
</asp:Content>
