<%@ Page Title="Comparar servicios | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CompararServicios.aspx.cs" Inherits="StageUp.UI.Explorar.CompararServicios" %>

<asp:Content ID="CompareServicesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="space-detail-page">
        <div class="space-detail-container">
            <a class="back-to-results" href="ResultadosBusqueda.aspx">
                <span aria-hidden="true">←</span>
                Volver a resultados
            </a>

            <asp:Panel ID="pnlSinSeleccion" runat="server" CssClass="space-detail-empty" role="status">
                <div class="space-detail-empty-visual" aria-hidden="true"><span></span></div>
                <span class="section-label">Comparar servicios</span>
                <h1><asp:Literal ID="litTituloSinSeleccion" runat="server" Text="Elegí al menos 2 servicios para comparar" /></h1>
                <p><asp:Literal ID="litDescripcionSinSeleccion" runat="server" Text="Desde el catálogo, seleccioná dos o tres categorías generales y después tocá &quot;Comparar servicios&quot;." /></p>
                <div class="space-detail-empty-actions">
                    <a class="button button-primary" href="ResultadosBusqueda.aspx">Explorar espacios</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlComparacion" runat="server" Visible="false">
                <header class="space-detail-heading">
                    <span class="section-label">Catálogo de servicios</span>
                    <h1>Comparación de servicios</h1>
                    <p>Esta comparación muestra promedios, rangos y características frecuentes calculados desde los espacios publicados de cada categoría.</p>
                </header>

                <div class="comparison-table-wrap">
                    <asp:Literal ID="litTablaComparacion" runat="server" />
                </div>

                <asp:Panel ID="pnlAlgunosNoEncontrados" runat="server" CssClass="comparison-note" Visible="false">
                    <asp:Literal ID="litAlgunosNoEncontrados" runat="server" />
                </asp:Panel>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
