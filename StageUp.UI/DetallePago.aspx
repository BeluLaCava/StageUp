<%@ Page Title="Detalle de pago | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetallePago.aspx.cs" Inherits="StageUp.UI.DetallePago" %>

<asp:Content ID="DetallePagoContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .detalle-pago-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 16px; align-items: start; }
        .factura dl, .detalle-pago dl { display: grid; grid-template-columns: auto 1fr; gap: 6px 14px; margin: 0; }
        .factura dt, .detalle-pago dt { color: var(--color-text-muted, #765f55); font-size: 0.9em; }
        .factura dd, .detalle-pago dd { margin: 0; font-weight: 600; overflow-wrap: anywhere; }
        .factura { border: 2px dashed var(--color-border, #d9c7bd); border-radius: 14px; padding: 18px; background: #fff; }
        .factura-cabecera { display: flex; justify-content: space-between; gap: 12px; flex-wrap: wrap; border-bottom: 1px solid var(--color-border, #e2e2e2); padding-bottom: 10px; margin-bottom: 12px; }
        .factura-cabecera strong { font-size: 1.2em; color: var(--color-primary, #6d1021); }
        .factura-tipo { font-size: 2em; font-weight: 800; border: 2px solid currentColor; border-radius: 8px; padding: 0 10px; line-height: 1.3; color: var(--color-primary, #6d1021); }
        .factura-total { display: flex; justify-content: space-between; font-size: 1.2em; font-weight: 700; border-top: 1px solid var(--color-border, #e2e2e2); margin-top: 12px; padding-top: 10px; font-variant-numeric: tabular-nums; }
        .factura-leyenda { font-size: 0.82em; color: var(--color-text-muted, #765f55); margin-top: 10px; }
        @media (max-width: 800px) { .detalle-pago-grid { grid-template-columns: 1fr; } }
        @media print { header, footer, nav, .form-actions, .no-imprimir { display: none !important; } .detalle-pago-grid { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page user-module-page">
        <div class="static-page-header no-imprimir">
            <span class="section-label">Pagos</span>
            <h1>Detalle de pago</h1>
            <p>Datos del pago de la reserva y la factura simulada que emitió StageUp.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlNoDisponible" runat="server" Visible="false" CssClass="auth-card">
                <p><asp:Literal ID="litNoDisponible" runat="server" Mode="Encode" /></p>
                <a class="button button-primary" href="MisReservas.aspx">Volver a Mis reservas</a>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" CssClass="detalle-pago-grid">
                <div class="auth-card detalle-pago">
                    <span class="section-label">Pago N° <asp:Literal ID="litNumeroPago" runat="server" Mode="Encode" /></span>
                    <h2><asp:Literal ID="litEstado" runat="server" Mode="Encode" /></h2>
                    <dl>
                        <dt>Identificador</dt><dd><asp:Literal ID="litIdentificador" runat="server" Mode="Encode" /></dd>
                        <dt>Fecha de la operación</dt><dd><asp:Literal ID="litFecha" runat="server" Mode="Encode" /></dd>
                        <dt>Medio utilizado</dt><dd><asp:Literal ID="litMedio" runat="server" Mode="Encode" /></dd>
                        <dt>Monto abonado</dt><dd><asp:Literal ID="litImporte" runat="server" Mode="Encode" /></dd>
                        <dt>Reserva</dt><dd><asp:HyperLink ID="lnkReserva" runat="server" /></dd>
                        <dt>Espacio</dt><dd><asp:Literal ID="litEspacio" runat="server" Mode="Encode" /></dd>
                        <dt>Fecha y horario</dt><dd><asp:Literal ID="litFechaReserva" runat="server" Mode="Encode" /></dd>
                        <dt>Solicitante</dt><dd><asp:Literal ID="litSolicitante" runat="server" Mode="Encode" /></dd>
                        <dt>Gestor del espacio</dt><dd><asp:Literal ID="litGestor" runat="server" Mode="Encode" /></dd>
                    </dl>
                    <asp:Panel ID="pnlLiquidacionGestor" runat="server" Visible="false">
                        <h3>Para vos como gestor</h3>
                        <dl>
                            <dt>Comisión StageUp</dt><dd><asp:Literal ID="litComision" runat="server" Mode="Encode" /></dd>
                            <dt>Te acreditamos</dt><dd><asp:Literal ID="litNeto" runat="server" Mode="Encode" /></dd>
                        </dl>
                    </asp:Panel>
                </div>

                <asp:Panel ID="pnlFactura" runat="server" CssClass="factura">
                    <div class="factura-cabecera">
                        <div>
                            <strong><asp:Literal ID="litEmisor" runat="server" Mode="Encode" /></strong>
                            <div><asp:Literal ID="litEmisorDatos" runat="server" Mode="Encode" /></div>
                        </div>
                        <div>
                            <span class="factura-tipo">FC</span>
                            <div>N° <asp:Literal ID="litNumeroFactura" runat="server" Mode="Encode" /></div>
                            <div><asp:Literal ID="litFechaFactura" runat="server" Mode="Encode" /></div>
                        </div>
                    </div>
                    <dl>
                        <dt>Cliente</dt><dd><asp:Literal ID="litFacturaCliente" runat="server" Mode="Encode" /></dd>
                        <dt>Concepto</dt><dd><asp:Literal ID="litFacturaConcepto" runat="server" Mode="Encode" /></dd>
                        <dt>Forma de pago</dt><dd><asp:Literal ID="litFacturaFormaPago" runat="server" Mode="Encode" /></dd>
                    </dl>
                    <div class="factura-total"><span>Total</span><span><asp:Literal ID="litFacturaTotal" runat="server" Mode="Encode" /></span></div>
                    <p class="factura-leyenda"><asp:Literal ID="litFacturaLeyenda" runat="server" Mode="Encode" /></p>
                </asp:Panel>
            </asp:Panel>

            <div class="form-actions no-imprimir">
                <button type="button" class="button button-primary" onclick="window.print();">Imprimir o guardar en PDF</button>
                <asp:HyperLink ID="lnkVolver" runat="server" CssClass="text-link" NavigateUrl="~/MisReservas.aspx" Text="Volver a Mis reservas" />
            </div>
        </div>
    </section>
</asp:Content>
