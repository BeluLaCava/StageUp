<%@ Page Title="Detalle de pago | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetallePago.aspx.cs" Inherits="StageUp.UI.DetallePago" %>

<asp:Content ID="DetallePagoContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .detalle-pago-page { --pago-vino: var(--color-primary, #6d1021); --pago-tinta: #4a302b; --pago-nude: #f8eee8; }
        .detalle-pago-grid { display: grid; grid-template-columns: minmax(19rem, .86fr) minmax(25rem, 1.14fr); gap: 1.15rem; align-items: stretch; }
        .detalle-pago-grid > * { min-width: 0; }
        .detalle-pago-card { position: relative; overflow: hidden; margin: 0; border: 1px solid rgba(109,16,33,.13); border-radius: 1.3rem; background: linear-gradient(145deg, #fffdfb, #f8eee8); box-shadow: 0 1rem 2.4rem rgba(77,11,23,.08); }
        .detalle-pago-card::before { content: ""; position: absolute; inset: 0 auto 0 0; width: .32rem; background: linear-gradient(180deg, var(--pago-vino), #d59a87); }
        .detalle-pago-card h2 { margin: .35rem 0 1.15rem; font-family: var(--font-display); font-size: clamp(1.8rem, 3vw, 2.35rem); font-weight: 500; color: var(--pago-vino); }
        .factura dl, .detalle-pago dl { display: grid; grid-template-columns: minmax(7.5rem, auto) 1fr; gap: .65rem 1rem; margin: 0; }
        .detalle-pago dl > dt, .detalle-pago dl > dd { padding: .68rem .75rem; background: rgba(255,255,255,.65); }
        .detalle-pago dl > dt { border-radius: .7rem 0 0 .7rem; }
        .detalle-pago dl > dd { border-radius: 0 .7rem .7rem 0; }
        .factura dt, .detalle-pago dt { color: var(--color-text-muted, #765f55); font-size: .82rem; }
        .factura dd, .detalle-pago dd { margin: 0; font-weight: 700; overflow-wrap: anywhere; color: var(--pago-tinta); }
        .factura { position: relative; isolation: isolate; overflow: hidden; display: flex; flex-direction: column; min-height: 100%; box-sizing: border-box; border: 1px solid rgba(109,16,33,.2); border-radius: 1.35rem; padding: clamp(1.25rem, 2.4vw, 1.8rem); background: linear-gradient(155deg, #fff 0%, #fffaf7 58%, #f7ebe5 100%); box-shadow: 0 1.2rem 3rem rgba(77,11,23,.13); animation: factura-entrada .42s ease-out both; transition: transform .22s ease, box-shadow .22s ease; }
        .factura:hover { transform: translateY(-3px); box-shadow: 0 1.55rem 3.5rem rgba(77,11,23,.17); }
        .factura::before { content: "S"; position: absolute; z-index: -1; right: -1.2rem; bottom: -4rem; color: rgba(109,16,33,.045); font-family: var(--font-display); font-size: 17rem; line-height: 1; transform: rotate(-8deg); }
        .factura::after { content: ""; position: absolute; z-index: 2; inset: .55rem; pointer-events: none; border: 1px dashed rgba(109,16,33,.2); border-radius: 1rem; }
        .factura-cabecera { position: relative; z-index: 3; display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; flex-wrap: wrap; border-bottom: 1px solid rgba(109,16,33,.16); padding: .2rem .25rem 1rem; margin-bottom: 1rem; }
        .factura-emisor { display: grid; gap: .4rem; max-width: 19rem; }
        .factura-emisor > div:last-child { max-width: 16rem; line-height: 1.5; }
        .factura-marca { display: inline-flex; align-items: center; gap: .55rem; }
        .factura-marca::before { content: "S"; display: grid; place-items: center; width: 2.25rem; height: 2.25rem; border-radius: .75rem; background: var(--pago-vino); color: #fff; font-family: var(--font-display); font-size: 1.35rem; }
        .factura-cabecera strong { font-family: var(--font-display); font-size: 1.55rem; font-weight: 600; color: var(--pago-vino); }
        .factura-identificacion { display: grid; grid-template-columns: auto 1fr; gap: .2rem .65rem; align-items: center; text-align: right; }
        .factura-tipo { grid-row: 1 / span 2; display: grid; place-items: center; width: 3rem; height: 3rem; font-size: 1.25rem; font-weight: 900; border: 2px solid currentColor; border-radius: .8rem; color: var(--pago-vino); }
        .factura-contenido { position: relative; z-index: 3; }
        .factura dl { grid-template-columns: minmax(7rem, auto) 1fr; gap: .75rem 1rem; }
        .factura dl > dt, .factura dl > dd { padding-bottom: .7rem; border-bottom: 1px solid rgba(109,16,33,.09); }
        .factura-total { position: relative; z-index: 3; display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; margin-top: auto; padding: 1rem 1.05rem; border: 0; border-radius: .9rem; background: linear-gradient(135deg, var(--pago-vino), #8f2941); color: #fff; font-size: 1rem; font-weight: 700; font-variant-numeric: tabular-nums; }
        .factura-contenido { margin-bottom: 1.15rem; }
        .factura-total span:last-child { font-family: var(--font-display); font-size: 1.65rem; }
        .factura-leyenda { position: relative; z-index: 3; margin: .85rem .2rem .1rem; color: var(--color-text-muted, #765f55); font-size: .78rem; line-height: 1.45; }
        .detalle-pago-page .form-actions { margin-top: 1.25rem; }
        @keyframes factura-entrada { from { opacity: 0; transform: translateY(.8rem) rotate(.3deg); } to { opacity: 1; transform: none; } }
        @media (max-width: 900px) { .detalle-pago-grid { grid-template-columns: 1fr; } }
        @media (prefers-reduced-motion: reduce) { .factura { animation: none; transition: none; } .factura:hover { transform: none; } }
        @page { size: A4 portrait; margin: 14mm; }
        @media print {
            html, body, #form1, .app-shell, .page-content, #main-content { width: 100% !important; min-width: 0 !important; min-height: 0 !important; margin: 0 !important; padding: 0 !important; background: #fff !important; }
            .app-shell, .page-content, #main-content { display: block !important; }
            .site-header, .site-footer, .app-sidebar, header, footer, nav, .form-actions, .no-imprimir, .detalle-pago-card, .skip-link { display: none !important; }
            .static-page.user-module-page.detalle-pago-page { max-width: none !important; margin: 0 !important; padding: 0 !important; }
            .static-page-body { width: 100% !important; margin: 0 !important; padding: 0 !important; }
            .detalle-pago-grid { display: block !important; width: 100% !important; }
            .factura { width: auto !important; min-height: 0; margin: 0 !important; padding: 14mm 12mm 11mm !important; border: 1px solid #d9c7bd; border-radius: 4mm; box-shadow: none !important; animation: none !important; transform: none !important; break-inside: avoid; print-color-adjust: exact; -webkit-print-color-adjust: exact; }
            .factura::after { inset: 4mm; border-radius: 3mm; }
            .factura::before { color: rgba(109,16,33,.035); }
            .factura-cabecera { break-inside: avoid; }
            .factura-total { background: #6d1021 !important; color: #fff !important; }
        }
    </style>

    <section class="static-page user-module-page detalle-pago-page">
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
                <div class="auth-card detalle-pago detalle-pago-card">
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
                        <div class="factura-emisor">
                            <div class="factura-marca"><strong><asp:Literal ID="litEmisor" runat="server" Mode="Encode" /></strong></div>
                            <div><asp:Literal ID="litEmisorDatos" runat="server" Mode="Encode" /></div>
                        </div>
                        <div class="factura-identificacion">
                            <span class="factura-tipo">FC</span>
                            <div>N° <asp:Literal ID="litNumeroFactura" runat="server" Mode="Encode" /></div>
                            <div><asp:Literal ID="litFechaFactura" runat="server" Mode="Encode" /></div>
                        </div>
                    </div>
                    <div class="factura-contenido">
                        <dl>
                            <dt>Cliente</dt><dd><asp:Literal ID="litFacturaCliente" runat="server" Mode="Encode" /></dd>
                            <dt>Concepto</dt><dd><asp:Literal ID="litFacturaConcepto" runat="server" Mode="Encode" /></dd>
                            <dt>Forma de pago</dt><dd><asp:Literal ID="litFacturaFormaPago" runat="server" Mode="Encode" /></dd>
                            <dt>Operación</dt><dd><asp:Literal ID="litFacturaOperacion" runat="server" Mode="Encode" /></dd>
                        </dl>
                    </div>
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
