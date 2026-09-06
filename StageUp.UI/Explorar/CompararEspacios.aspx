<%@ Page Title="Comparar espacios | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CompararEspacios.aspx.cs" Inherits="StageUp.UI.Explorar.CompararEspacios" %>

<asp:Content ID="CompareContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="space-detail-page">
        <div class="space-detail-container">
            <a class="back-to-results" href="ResultadosBusqueda.aspx">
                <span aria-hidden="true">←</span>
                Volver a resultados
            </a>

            <asp:Panel ID="pnlSinSeleccion" runat="server" CssClass="space-detail-empty" role="status">
                <div class="space-detail-empty-visual" aria-hidden="true"><span></span></div>
                <span class="section-label">Comparar espacios</span>
                <h1><asp:Literal ID="litTituloSinSeleccion" runat="server" Text="Elegí al menos 2 espacios para comparar" /></h1>
                <p><asp:Literal ID="litDescripcionSinSeleccion" runat="server" Text="Desde el catálogo, marcá la casilla &quot;Comparar&quot; en dos o más espacios publicados y después tocá &quot;Comparar seleccionados&quot;." /></p>
                <div class="space-detail-empty-actions">
                    <a class="button button-primary" href="ResultadosBusqueda.aspx">Explorar espacios</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlComparacion" runat="server" Visible="false">
                <header class="space-detail-heading">
                    <span class="section-label">Comparar espacios</span>
                    <h1>Comparación de espacios</h1>
                    <p>Comparamos los datos que ya están disponibles del catálogo público. Ubicación, capacidad, equipamiento y valores de referencia se van a poder comparar en una próxima entrega.</p>
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
