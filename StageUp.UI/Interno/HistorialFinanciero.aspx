<%@ Page Title="Historial financiero | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="HistorialFinanciero.aspx.cs" Inherits="StageUp.UI.Interno.HistorialFinanciero" %>

<asp:Content ID="HistorialFinancieroContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Historial financiero</h1>
            <p>Reservas abonadas, pagos, cargos por cancelación, comisiones, facturas, liquidaciones y saldos de los gestores de espacios.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="auth-card admin-list-card">
                <div class="auth-card-header internal-users-list-heading">
                    <div>
                        <span class="admin-card-eyebrow">Control y análisis financiero</span>
                        <h2>Operaciones económicas</h2>
                    </div>
                </div>

                <%-- Paso 6 y A4: filtros opcionales (sin filtros se ve el listado general, A3). --%>
                <asp:Panel ID="pnlFiltros" runat="server" CssClass="internal-filter-row" DefaultButton="btnFiltrar" role="search">
                    <div class="form-field">
                        <label for="<%= txtDesde.ClientID %>">Desde</label>
                        <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtHasta.ClientID %>">Hasta</label>
                        <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlEstadoPago.ClientID %>">Estado de pago</label>
                        <asp:DropDownList ID="ddlEstadoPago" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlTipo.ClientID %>">Tipo de operación</label>
                        <asp:DropDownList ID="ddlTipo" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlGestor.ClientID %>">Gestor del espacio</label>
                        <asp:DropDownList ID="ddlGestor" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtTexto.ClientID %>">Buscar</label>
                        <asp:TextBox ID="txtTexto" runat="server" MaxLength="100" placeholder="Usuario, espacio, código o N° de reserva" />
                    </div>
                    <div class="internal-filter-actions">
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnFiltrar_Click" />
                        <asp:LinkButton ID="lnkLimpiar" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkLimpiar_Click">Limpiar filtros</asp:LinkButton>
                    </div>
                </asp:Panel>

                <%-- A2: no hay ninguna operación registrada. --%>
                <asp:Panel ID="pnlSinOperaciones" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No hay información financiera disponible para consultar</h3>
                    <p>Las operaciones aparecen cuando se acepta y se paga una reserva, se cobra un cargo por cancelación, se liquida a un gestor o se emite una nota manual.</p>
                </asp:Panel>
                <%-- A5: hay operaciones pero ninguna cumple con los filtros. --%>
                <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No se encontraron resultados para los filtros aplicados</h3>
                    <p>Modificá los filtros o limpialos para volver al listado general.</p>
                </asp:Panel>

                <asp:Panel ID="pnlResumen" runat="server" Visible="false" CssClass="historial-resumen">
                    <asp:Repeater ID="rptResumen" runat="server">
                        <ItemTemplate>
                            <div class="historial-resumen-moneda">
                                <span class="historial-resumen-titulo"><%# Eval("Cantidad") %> <%# Convert.ToInt32(Eval("Cantidad")) == 1 ? "operación" : "operaciones" %> en <%# Server.HtmlEncode(Convert.ToString(Eval("Moneda"))) %></span>
                                <dl>
                                    <div><dt>Cobrado</dt><dd><%# Server.HtmlEncode(Importe(Eval("TotalCobrado"), Eval("Moneda"))) %></dd></div>
                                    <div><dt>Comisión StageUp</dt><dd><%# Server.HtmlEncode(Importe(Eval("TotalComision"), Eval("Moneda"))) %></dd></div>
                                    <div><dt>Para los gestores</dt><dd><%# Server.HtmlEncode(Importe(Eval("TotalGestor"), Eval("Moneda"))) %></dd></div>
                                    <div><dt>Pendiente de pago</dt><dd><%# Server.HtmlEncode(Importe(Eval("TotalPendiente"), Eval("Moneda"))) %></dd></div>
                                    <div><dt>Liquidado a gestores</dt><dd><%# Server.HtmlEncode(Importe(Eval("TotalLiquidado"), Eval("Moneda"))) %></dd></div>
                                </dl>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                    <%-- Saldo actual del gestor elegido en el filtro. --%>
                    <asp:Panel ID="pnlSaldoGestor" runat="server" Visible="false" CssClass="historial-resumen-moneda historial-saldo-gestor">
                        <span class="historial-resumen-titulo">Saldo actual de <asp:Literal ID="litNombreGestor" runat="server" Mode="Encode" /></span>
                        <dl>
                            <asp:Repeater ID="rptSaldoGestor" runat="server">
                                <ItemTemplate>
                                    <div><dt>Cuenta de gestor en <%# Server.HtmlEncode(Convert.ToString(Eval("Moneda"))) %></dt><dd><%# Server.HtmlEncode(Importe(Eval("Saldo"), Eval("Moneda"))) %></dd></div>
                                </ItemTemplate>
                            </asp:Repeater>
                            <asp:Literal ID="litSinSaldoGestor" runat="server" Visible="false" Text="<div><dt>Cuenta de gestor</dt><dd>Sin movimientos</dd></div>" />
                        </dl>
                    </asp:Panel>
                </asp:Panel>

                <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                <%-- Paso 5: información resumida de cada operación. --%>
                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptOperaciones" runat="server">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Operación</th><th>Reserva y espacio</th><th>Solicitante</th><th>Gestor</th><th>Monto total</th><th>Comisión StageUp</th><th>Al gestor</th><th>Estado de pago</th><th>Factura</th><th>Fecha</th><th></th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td data-label="Operación"><strong class="historial-codigo"><%# Server.HtmlEncode(Convert.ToString(Eval("Codigo"))) %></strong><br /><small><%# Server.HtmlEncode(StageUp.BLL.BLL_HistorialFinanciero.DescribirTipo(Convert.ToString(Eval("TipoOperacion")))) %></small></td>
                                <td data-label="Reserva y espacio"><%# Server.HtmlEncode(Texto(Eval("NombreEspacio"))) %><%# Eval("IdReserva") == null ? "" : "<br /><small>Reserva N° " + Eval("IdReserva") + "</small>" %></td>
                                <td data-label="Solicitante"><%# Server.HtmlEncode(Texto(Eval("NombreSolicitante"))) %></td>
                                <td data-label="Gestor"><%# Server.HtmlEncode(Texto(Eval("NombreGestor"))) %></td>
                                <td data-label="Monto total" class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("MontoTotal"), Eval("Moneda"))) %></td>
                                <td data-label="Comisión StageUp" class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("ImporteComision"), Eval("Moneda"))) %></td>
                                <td data-label="Al gestor" class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("ImporteGestor"), Eval("Moneda"))) %></td>
                                <td data-label="Estado de pago"><span class="pago-estado pago-estado-<%# Eval("EstadoPago") %>"><%# Server.HtmlEncode(StageUp.BLL.BLL_HistorialFinanciero.DescribirEstado(Convert.ToString(Eval("EstadoPago")))) %></span></td>
                                <td data-label="Factura" class="historial-codigo"><%# Server.HtmlEncode(Factura(Container.DataItem)) %></td>
                                <td data-label="Fecha" class="pagos-num"><%# Convert.ToDateTime(Eval("FechaOperacion")).ToString("dd/MM/yyyy") %><br /><small><%# Convert.ToDateTime(Eval("FechaOperacion")).ToString("HH:mm") %> hs</small></td>
                                <td><a class="admin-action-link admin-action-link-primary" href="<%# Server.HtmlEncode(UrlDetalle(Eval("Codigo"))) %>">Ver detalle</a></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
    </section>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .internal-filter-row { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.75rem; align-items: end; margin: 0.5rem 0 0.4rem; }
        .internal-filter-row .form-field { margin-bottom: 0; }
        .internal-filter-row input, .internal-filter-row select { width: 100%; box-sizing: border-box; padding: 0.65rem 0.75rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .internal-filter-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; grid-column: 1 / -1; }
        .internal-list-count { margin: 0.6rem 0; color: var(--color-text-muted); font-size: 0.8rem; }
        .historial-resumen { display: grid; grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr)); gap: 12px; margin: 0.9rem 0 0.2rem; }
        .historial-resumen-moneda { border: 1px solid var(--color-border); border-radius: 12px; padding: 12px 14px; background: var(--color-surface); min-width: 0; }
        .historial-resumen-titulo { display: block; font-weight: 700; margin-bottom: 8px; color: var(--color-text); }
        .historial-resumen dl { display: grid; grid-template-columns: repeat(auto-fit, minmax(8.5rem, 1fr)); gap: 8px 14px; margin: 0; }
        .historial-resumen dt { color: var(--color-text-muted); font-size: 12px; }
        .historial-resumen dd { margin: 2px 0 0; font-weight: 700; font-variant-numeric: tabular-nums; }
        .historial-saldo-gestor { border-style: dashed; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .pagos-tabla .admin-action-link { white-space: nowrap; }
        .historial-codigo { white-space: nowrap; }
        .pagos-num { font-variant-numeric: tabular-nums; white-space: nowrap; }
        .pago-estado { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 0.78rem; font-weight: 700; background: #eee; white-space: nowrap; }
        .pago-estado-Pagado, .pago-estado-Cobrado, .pago-estado-Liquidado, .pago-estado-Aplicado { background: #e6f5eb; color: #236240; }
        .pago-estado-Pendiente, .pago-estado-EnRevision { background: #fff4e5; color: #8a4b0f; }
        .pago-estado-Fallido, .pago-estado-Vencido { background: #fdecec; color: #8f1d1d; }
        .pago-estado-Devuelto, .pago-estado-Anulado { background: #eef0fb; color: #33407a; }
        @media (max-width: 700px) {
            .internal-filter-row { grid-template-columns: 1fr 1fr; }
        }
        @media (max-width: 480px) {
            .internal-filter-row { grid-template-columns: 1fr; }
        }
    </style>
</asp:Content>
