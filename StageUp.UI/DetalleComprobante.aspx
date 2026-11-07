<%@ Page Title="Comprobante | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" EnableViewState="false" CodeBehind="DetalleComprobante.aspx.cs" Inherits="StageUp.UI.DetalleComprobante" %>
<asp:Content ID="ComprobanteContent" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/Css/comprobante.css") %>?v=20261106-1" />
    <section class="static-page user-module-page nota-page">
        <div class="static-page-header no-imprimir">
            <span class="section-label">Cuenta corriente</span>
            <h1>Comprobante</h1>
            <p>Consultá tu nota de crédito o débito y guardá una copia.</p>
        </div>
        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="auth-card" role="status">
            <asp:Literal ID="litError" runat="server" Mode="Encode" />
        </asp:Panel>
        <asp:Panel ID="pnlNota" runat="server" Visible="false" CssClass="nota-documento">
            <div class="nota-cabecera">
                <div class="nota-emisor"><strong class="nota-marca">StageUp</strong><strong><asp:Literal ID="litEmisor" runat="server" Mode="Encode" /></strong><span><asp:Literal ID="litEmisorDatos" runat="server" Mode="Encode" /></span></div>
                <div class="nota-identidad"><span class="nota-sello"><asp:Literal ID="litTipo" runat="server" Mode="Encode" /></span><h2><asp:Literal ID="litTitulo" runat="server" Mode="Encode" /></h2><strong><asp:Literal ID="litNumero" runat="server" Mode="Encode" /></strong><span>Emitida el <asp:Literal ID="litFecha" runat="server" Mode="Encode" /></span></div>
            </div>
            <asp:Panel ID="pnlAnulada" runat="server" Visible="false" CssClass="nota-anulada">
                <strong>COMPROBANTE ANULADO</strong>
                <p><asp:Literal ID="litAnulacion" runat="server" Mode="Encode" /></p>
                <p>Se conserva como antecedente. Este comprobante ya no está vigente.</p>
            </asp:Panel>
            <dl class="nota-datos">
                <dt>Destinatario</dt><dd><asp:Literal ID="litUsuario" runat="server" Mode="Encode" /></dd>
                <dt>Correo</dt><dd><asp:Literal ID="litCorreo" runat="server" Mode="Encode" /></dd>
                <dt>Cuenta</dt><dd><asp:Literal ID="litCuenta" runat="server" Mode="Encode" /></dd>
                <dt>Reserva asociada</dt><dd><asp:Literal ID="litReserva" runat="server" Mode="Encode" /></dd>
                <dt>Origen</dt><dd><asp:Literal ID="litOrigen" runat="server" Mode="Encode" /></dd>
                <dt>Estado</dt><dd><asp:Literal ID="litEstado" runat="server" Mode="Encode" /></dd>
                <dt>Motivo</dt><dd><asp:Literal ID="litMotivo" runat="server" Mode="Encode" /></dd>
            </dl>
            <div class="nota-total"><span><asp:Literal ID="litEtiquetaImporte" runat="server" Mode="Encode" /></span><strong><asp:Literal ID="litImporte" runat="server" Mode="Encode" /></strong></div>
            <p class="nota-aclaracion"><asp:Literal ID="litEfecto" runat="server" Mode="Encode" /></p>
            <p class="nota-leyenda">Comprobante simulado de StageUp, sin validez fiscal.</p>
        </asp:Panel>
        <div class="form-actions no-imprimir">
            <asp:PlaceHolder ID="phImprimir" runat="server" Visible="false"><button type="button" class="button button-primary" onclick="window.print()">Imprimir o guardar en PDF</button></asp:PlaceHolder>
            <asp:HyperLink ID="lnkReserva" runat="server" Visible="false" CssClass="button button-outline" Text="Ver reserva" />
            <asp:HyperLink ID="lnkVolver" runat="server" CssClass="link-muted" Text="Volver a Mi cuenta corriente" NavigateUrl="~/MiCuentaCorriente.aspx" />
        </div>
    </section>
</asp:Content>
