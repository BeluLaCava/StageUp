<%@ Page Title="Detalle de operación financiera | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="DetalleOperacionFinanciera.aspx.cs" Inherits="StageUp.UI.Interno.DetalleOperacionFinanciera" %>

<asp:Content ID="DetalleOperacionFinancieraContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Historial financiero</span>
            <h1>Detalle de operación financiera</h1>
            <p>Información económica completa de la operación seleccionada. Esta consulta no modifica ningún dato.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <div class="auth-card admin-list-card">
                <div class="auth-card-header detalle-op-cabecera">
                    <div>
                        <span class="admin-card-eyebrow"><asp:Literal ID="litTipo" runat="server" Mode="Encode" /></span>
                        <h2><asp:Literal ID="litCodigo" runat="server" Mode="Encode" /></h2>
                    </div>
                    <asp:Label ID="lblEstado" runat="server" />
                </div>

                <%-- Paso 13: estados o datos particulares de la operación. --%>
                <asp:Repeater ID="rptAvisos" runat="server">
                    <HeaderTemplate><ul class="detalle-op-avisos"></HeaderTemplate>
                    <ItemTemplate><li><%# Server.HtmlEncode(Convert.ToString(Container.DataItem)) %></li></ItemTemplate>
                    <FooterTemplate></ul></FooterTemplate>
                </asp:Repeater>

                <%-- Paso 12. --%>
                <h3 class="detalle-op-subtitulo">Importes</h3>
                <dl class="internal-detail-grid">
                    <div><dt>Monto total</dt><dd><asp:Literal ID="litMonto" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Comisión aplicada</dt><dd><asp:Literal ID="litComision" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Importe del gestor</dt><dd><asp:Literal ID="litImporteGestor" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Cargo por cancelación</dt><dd><asp:Literal ID="litCargo" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Estado del pago</dt><dd><asp:Literal ID="litEstadoPago" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Fecha de operación</dt><dd><asp:Literal ID="litFecha" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <h3 class="detalle-op-subtitulo">Reserva y personas</h3>
                <dl class="internal-detail-grid">
                    <div><dt>Reserva asociada</dt><dd><asp:Literal ID="litReserva" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Espacio artístico</dt><dd><asp:Literal ID="litEspacio" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Estado de la reserva</dt><dd><asp:Literal ID="litEstadoReserva" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Usuario solicitante</dt><dd><asp:Literal ID="litSolicitante" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Gestor del espacio</dt><dd><asp:Literal ID="litGestor" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Valor de la reserva</dt><dd><asp:Literal ID="litValorReserva" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <h3 class="detalle-op-subtitulo">Pago y factura</h3>
                <dl class="internal-detail-grid">
                    <div><dt>Referencia al medio de pago</dt><dd><asp:Literal ID="litReferencia" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Medio de pago</dt><dd><asp:Literal ID="litMedio" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Forma de pago</dt><dd><asp:Literal ID="litFormaPago" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Factura asociada</dt><dd><asp:Literal ID="litFactura" runat="server" Mode="Encode" /></dd></div>
                    <div class="internal-detail-wide"><dt>Observaciones</dt><dd><asp:Literal ID="litObservacion" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <h3 class="detalle-op-subtitulo">Comprobantes relacionados</h3>
                <asp:Panel ID="pnlSinComprobantes" runat="server" CssClass="detalle-op-vacio">No hay comprobantes asociados a esta operación.</asp:Panel>
                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptComprobantes" runat="server">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Comprobante</th><th>Tipo</th><th>Cuenta</th><th>Importe</th><th>Estado</th><th>Emisión</th><th>Motivo</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td data-label="Comprobante"><strong><%# Server.HtmlEncode(Convert.ToString(Eval("Numero"))) %></strong></td>
                                <td data-label="Tipo"><%# Server.HtmlEncode(TipoComprobante(Convert.ToString(Eval("Tipo")))) %></td>
                                <td data-label="Cuenta"><%# Server.HtmlEncode(Convert.ToString(Eval("NombreUsuario"))) %><br /><small><%# Server.HtmlEncode(Convert.ToString(Eval("RolCuenta"))) %></small></td>
                                <td data-label="Importe" class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("Importe"), Eval("Moneda"))) %></td>
                                <td data-label="Estado"><%# Server.HtmlEncode(Convert.ToString(Eval("Estado"))) %></td>
                                <td data-label="Emisión"><%# Convert.ToDateTime(Eval("FechaEmision")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td data-label="Motivo"><%# Server.HtmlEncode(Convert.ToString(Eval("Motivo"))) %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>

                <h3 class="detalle-op-subtitulo">Impacto en saldos</h3>
                <asp:Panel ID="pnlSinMovimientos" runat="server" CssClass="detalle-op-vacio">La operación no generó movimientos en cuentas corrientes.</asp:Panel>
                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptMovimientos" runat="server">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Fecha</th><th>Cuenta</th><th>Movimiento</th><th>Importe</th><th>Descripción</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td data-label="Fecha"><%# Convert.ToDateTime(Eval("FechaMovimiento")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td data-label="Cuenta"><%# Server.HtmlEncode(Convert.ToString(Eval("NombreUsuario"))) %><br /><small>Cuenta de <%# Server.HtmlEncode(Convert.ToString(Eval("RolCuenta")).ToLowerInvariant()) %></small></td>
                                <td data-label="Movimiento"><%# Server.HtmlEncode(StageUp.BLL.BLL_CuentaCorriente.EtiquetaMovimiento(Convert.ToString(Eval("TipoMovimiento")))) %></td>
                                <td data-label="Importe" class="pagos-num <%# Convert.ToDecimal(Eval("Importe")) < 0 ? "importe-negativo" : "importe-positivo" %>"><%# Server.HtmlEncode(ImporteConSigno(Eval("Importe"), Eval("Moneda"))) %></td>
                                <td data-label="Descripción"><%# Server.HtmlEncode(Convert.ToString(Eval("Descripcion"))) %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>

                <asp:Panel ID="pnlSaldoGestor" runat="server" Visible="false" CssClass="detalle-op-saldo">
                    <strong>Saldo actual del gestor:</strong>
                    <asp:Literal ID="litSaldoGestor" runat="server" Mode="Encode" />
                </asp:Panel>

                <div class="form-actions">
                    <asp:HyperLink ID="lnkVolver" runat="server" CssClass="button button-secondary" Text="Volver a Historial financiero" />
                </div>
            </div>
        </div>
    </section>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .detalle-op-cabecera { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap; }
        .detalle-op-subtitulo { margin: 1.3rem 0 0.6rem; font-size: 1rem; }
        .detalle-op-avisos { margin: 0.4rem 0 0.2rem; padding: 10px 14px 10px 30px; border-radius: 10px; background: #fff8ee; border: 1px solid #f1d7b5; color: #6b3d0c; }
        .detalle-op-avisos li + li { margin-top: 4px; }
        .detalle-op-vacio { color: var(--color-text-muted); font-size: 0.9rem; margin-bottom: 0.4rem; }
        .detalle-op-saldo { margin-top: 0.9rem; padding: 10px 14px; border: 1px dashed var(--color-border); border-radius: 10px; }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 0; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .internal-detail-wide { grid-column: 1 / -1; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .pagos-num { font-variant-numeric: tabular-nums; white-space: nowrap; }
        .importe-negativo { color: #8f1d1d; }
        .importe-positivo { color: #236240; }
        .pago-estado { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 0.82rem; font-weight: 700; background: #eee; white-space: nowrap; }
        .pago-estado-Pagado, .pago-estado-Cobrado, .pago-estado-Liquidado, .pago-estado-Aplicado { background: #e6f5eb; color: #236240; }
        .pago-estado-Pendiente, .pago-estado-EnRevision { background: #fff4e5; color: #8a4b0f; }
        .pago-estado-Fallido, .pago-estado-Vencido { background: #fdecec; color: #8f1d1d; }
        .pago-estado-Devuelto, .pago-estado-Anulado { background: #eef0fb; color: #33407a; }
        @media (max-width: 700px) { .internal-detail-grid { grid-template-columns: 1fr 1fr; } }
        @media (max-width: 480px) { .internal-detail-grid { grid-template-columns: 1fr; } }
    </style>
</asp:Content>
