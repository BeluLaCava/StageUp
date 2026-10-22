<%@ Page Title="Transacciones económicas | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="TransaccionesEconomicas.aspx.cs" Inherits="StageUp.UI.Interno.TransaccionesEconomicas" %>

<asp:Content ID="TransaccionesEconomicasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Transacciones económicas</h1>
            <p>Reservas abonadas y cargos por cancelación, con la comisión de StageUp y el importe que le corresponde al gestor.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="auth-card admin-list-card">
                <div class="auth-card-header internal-users-list-heading">
                    <div>
                        <span class="admin-card-eyebrow">Control y análisis</span>
                        <h2>Transacciones registradas</h2>
                    </div>
                </div>


                <asp:Panel ID="pnlFiltros" runat="server" CssClass="internal-filter-row" DefaultButton="btnFiltrar" role="search">
                    <div class="form-field">
                        <label for="<%= ddlTipo.ClientID %>">Tipo de operación</label>
                        <asp:DropDownList ID="ddlTipo" runat="server">
                            <asp:ListItem Text="Todas" Value="" />
                            <asp:ListItem Text="Reserva aceptada y abonada" Value="ReservaAbonada" />
                            <asp:ListItem Text="Cargo por cancelación" Value="CargoCancelacion" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlEstadoPago.ClientID %>">Estado de pago</label>
                        <asp:DropDownList ID="ddlEstadoPago" runat="server">
                            <asp:ListItem Text="Todos" Value="" />
                            <asp:ListItem Text="Pagado" Value="Pagado" />
                            <asp:ListItem Text="Cobrado" Value="Cobrado" />
                            <asp:ListItem Text="Devuelto al solicitante" Value="Devuelto" />
                            <asp:ListItem Text="Pendiente" Value="Pendiente" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label for="<%= txtDesde.ClientID %>">Desde</label>
                        <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtHasta.ClientID %>">Hasta</label>
                        <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtTexto.ClientID %>">Buscar</label>
                        <asp:TextBox ID="txtTexto" runat="server" MaxLength="100" placeholder="Usuario, gestor, espacio o código" />
                    </div>
                    <div class="internal-filter-actions">
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnFiltrar_Click" />
                        <asp:LinkButton ID="lnkLimpiar" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkLimpiar_Click">Limpiar filtros</asp:LinkButton>
                    </div>
                </asp:Panel>


                <asp:Panel ID="pnlSinTransacciones" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No hay transacciones económicas registradas</h3>
                    <p>Se generan cuando se confirma el pago de una reserva o cuando se cobra un cargo por cancelación.</p>
                </asp:Panel>
                <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No se encontraron transacciones para el filtro aplicado</h3>
                    <p>Modificá los filtros o limpialos para volver al listado general.</p>
                </asp:Panel>

                <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptTransacciones" runat="server" OnItemCommand="rptTransacciones_ItemCommand">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Transacción</th><th>Tipo</th><th>Monto total</th><th>Fecha</th><th>Solicitante</th><th>Gestor</th><th>Estado de pago</th><th>Comisión StageUp</th><th>Al gestor</th><th></th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><strong><%# Server.HtmlEncode(Convert.ToString(Eval("Codigo"))) %></strong><br /><small>Reserva N° <%# Eval("IdReserva") %></small></td>
                                <td><%# Server.HtmlEncode(DescribirTipo(Container.DataItem)) %><%# Convert.ToString(Eval("EstadoRegistro")) == "PendienteControl" ? "<br /><span class=\"pago-estado pago-estado-Pendiente\">Pendiente de control</span>" : "" %></td>
                                <td class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("MontoTotal"), Eval("Moneda"))) %></td>
                                <td><%# Convert.ToDateTime(Eval("FechaGeneracion")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("NombreSolicitante"))) %></td>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("NombreGestor"))) %></td>
                                <td><span class="pago-estado pago-estado-<%# Eval("EstadoPago") %>"><%# Server.HtmlEncode(StageUp.BLL.BLL_TransaccionEconomica.DescribirEstadoPago(Convert.ToString(Eval("EstadoPago")))) %></span></td>
                                <td class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("ImporteComision"), Eval("Moneda"))) %><br /><small><%# Porcentaje(Eval("PorcentajeComision")) %></small></td>
                                <td class="pagos-num"><%# Server.HtmlEncode(Importe(Eval("ImporteGestor"), Eval("Moneda"))) %></td>
                                <td><asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                    CommandName="Ver" CommandArgument='<%# Eval("IdTransaccion") %>' Text="Ver detalle" /></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
    </section>


    <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog internal-dialog" open aria-labelledby="tx-detalle-titulo">
            <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="review-dialog-close" CausesValidation="false" OnClick="lnkCerrarDetalle_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label">Detalle de transacción económica</span>
            <h2 id="tx-detalle-titulo"><asp:Literal ID="litDetalleTitulo" runat="server" Mode="Encode" /></h2>
            <p><asp:Label ID="lblDetalleEstado" runat="server" /></p>

            <dl class="internal-detail-grid">
                <div><dt>Tipo de operación</dt><dd><asp:Literal ID="litDetalleTipo" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Fecha de generación</dt><dd><asp:Literal ID="litDetalleFecha" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Monto total</dt><dd><asp:Literal ID="litDetalleMonto" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Comisión StageUp</dt><dd><asp:Literal ID="litDetalleComision" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Importe del gestor</dt><dd><asp:Literal ID="litDetalleGestorImporte" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Cargo por cancelación</dt><dd><asp:Literal ID="litDetalleCargo" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Reserva asociada</dt><dd><asp:Literal ID="litDetalleReserva" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Espacio artístico</dt><dd><asp:Literal ID="litDetalleEspacio" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Estado de la reserva</dt><dd><asp:Literal ID="litDetalleEstadoReserva" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Usuario solicitante</dt><dd><asp:Literal ID="litDetalleSolicitante" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Gestor del espacio</dt><dd><asp:Literal ID="litDetalleGestor" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Referencia al medio de pago</dt><dd><asp:Literal ID="litDetalleReferencia" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Medio de pago</dt><dd><asp:Literal ID="litDetalleMedio" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Comprobantes</dt><dd><asp:Literal ID="litDetalleComprobantes" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Registro</dt><dd><asp:Literal ID="litDetalleRegistro" runat="server" Mode="Encode" /></dd></div>
                <div class="internal-detail-wide"><dt>Observaciones</dt><dd><asp:Literal ID="litDetalleObservacion" runat="server" Mode="Encode" /></dd></div>
            </dl>

            <div class="review-dialog-actions">
                <asp:LinkButton ID="lnkVolverListado" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCerrarDetalle_Click">Volver a Transacciones económicas</asp:LinkButton>
            </div>
        </dialog>
    </asp:Panel>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .internal-filter-row { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.75rem; align-items: end; margin: 0.5rem 0 0.4rem; }
        .internal-filter-row .form-field { margin-bottom: 0; }
        .internal-filter-row input, .internal-filter-row select { width: 100%; box-sizing: border-box; padding: 0.65rem 0.75rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .internal-filter-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; grid-column: 1 / -1; }
        .internal-list-count { margin: 0.6rem 0; color: var(--color-text-muted); font-size: 0.8rem; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .pagos-tabla .admin-action-link { white-space: nowrap; }
        .pagos-num { font-variant-numeric: tabular-nums; white-space: nowrap; }
        .pago-estado { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 0.78rem; font-weight: 700; background: #eee; }
        .pago-estado-Pagado, .pago-estado-Cobrado, .pago-estado-Registrada { background: #e6f5eb; color: #236240; }
        .pago-estado-Pendiente, .pago-estado-PendienteControl { background: #fff4e5; color: #8a4b0f; }
        .pago-estado-Devuelto { background: #eef0fb; color: #33407a; }
        .review-dialog.internal-dialog { width: min(46rem, calc(100vw - 2rem)); max-height: calc(100vh - 2rem); }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 0 0 18px; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .internal-detail-wide { grid-column: 1 / -1; }
        body:has(.review-dialog-layer) { overflow: hidden; }
        @media (max-width: 700px) {
            .internal-filter-row, .internal-detail-grid { grid-template-columns: 1fr 1fr; }
        }
        @media (max-width: 480px) {
            .internal-filter-row, .internal-detail-grid { grid-template-columns: 1fr; }
            .review-dialog.internal-dialog { padding: 1.5rem 1.1rem; }
        }
    </style>
</asp:Content>
