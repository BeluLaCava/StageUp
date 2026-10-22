<%@ Page Title="Pagos de reservas | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="PagosReservas.aspx.cs" Inherits="StageUp.UI.Interno.PagosReservas" %>

<asp:Content ID="PagosReservasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Pagos de reservas</h1>
            <p>Consultá y controlá los pagos de reservas y de cargos por cancelación: estado, medio utilizado, reserva asociada y factura.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>


            <div class="auth-card admin-list-card">
                <div class="auth-card-header internal-users-list-heading">
                    <div>
                        <span class="admin-card-eyebrow">Control</span>
                        <h2>Pagos registrados</h2>
                    </div>
                </div>

                <asp:Panel ID="pnlFiltros" runat="server" CssClass="internal-filter-row" DefaultButton="btnFiltrar" role="search">
                    <div class="form-field">
                        <label for="<%= ddlEstado.ClientID %>">Estado del pago</label>
                        <asp:DropDownList ID="ddlEstado" runat="server">
                            <asp:ListItem Text="Todos" Value="Todos" />
                            <asp:ListItem Text="Pendiente" Value="Pendiente" />
                            <asp:ListItem Text="Confirmado" Value="Confirmado" />
                            <asp:ListItem Text="Fallido" Value="Fallido" />
                            <asp:ListItem Text="Cancelado" Value="Cancelado" />
                            <asp:ListItem Text="En revisión" Value="EnRevision" />
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
                        <asp:TextBox ID="txtTexto" runat="server" MaxLength="100" placeholder="Usuario, espacio, N° o identificador" />
                    </div>
                    <div class="internal-filter-actions">
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnFiltrar_Click" />
                        <asp:LinkButton ID="lnkLimpiar" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkLimpiar_Click">Limpiar filtros</asp:LinkButton>
                    </div>
                </asp:Panel>


                <asp:Panel ID="pnlSinPagos" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No hay pagos disponibles para consultar</h3>
                    <p>Todavía no se registró ningún pago de reservas.</p>
                </asp:Panel>
                <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3>No se encontraron pagos para el filtro aplicado</h3>
                    <p>Modificá los filtros o limpialos para volver al listado general.</p>
                </asp:Panel>

                <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptPagos" runat="server" OnItemCommand="rptPagos_ItemCommand">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Pago</th><th>Reserva o cargo</th><th>Usuario</th><th>Monto</th><th>Fecha</th><th>Medio</th><th>Estado</th><th></th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><%# Eval("IdPago") == null ? "—" : "N° " + Eval("IdPago") %><br /><small><%# Server.HtmlEncode(Convert.ToString(Eval("IdentificadorExterno"))) %></small></td>
                                <td><%# DescribirReserva(Container.DataItem) %></td>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("NombreUsuario"))) %><br /><small><%# Server.HtmlEncode(Convert.ToString(Eval("CorreoUsuario"))) %></small></td>
                                <td class="pagos-num"><%# Server.HtmlEncode(StageUp.BLL.BLL_CuentaCorriente.FormatearImporte(Convert.ToDecimal(Eval("ImporteTotal")), Convert.ToString(Eval("Moneda")))) %></td>
                                <td><%# Convert.ToDateTime(Eval("Fecha")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("Medio") ?? "—")) %></td>
                                <td><span class="pago-estado pago-estado-<%# Eval("Grupo") %>"><%# Server.HtmlEncode(StageUp.BLL.BLL_Pago.DescribirEstado(Convert.ToString(Eval("Estado")))) %></span></td>
                                <td><asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                    CommandName="Ver" CommandArgument='<%# ArgumentoDetalle(Container.DataItem) %>' Text="Ver detalle" /></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>


            <details class="auth-card admin-list-card pagos-aviso" <%= AvisoAbierto ? "open" : "" %>>
                <summary><strong>Registrar aviso del medio de pago (simulación)</strong></summary>
                <p class="field-help">StageUp no está conectado a una pasarela real. Desde acá se carga lo que informaría el medio de pago externo para una operación: el sistema valida los datos, busca la reserva y clasifica el pago.</p>
                <asp:Panel ID="pnlAviso" runat="server" CssClass="pagos-aviso-grid" DefaultButton="btnRegistrarAviso">
                    <div class="form-field">
                        <label for="<%= txtAvisoReserva.ClientID %>">N° de reserva</label>
                        <asp:TextBox ID="txtAvisoReserva" runat="server" MaxLength="10" inputmode="numeric" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtAvisoIdentificador.ClientID %>">Identificador del pago</label>
                        <asp:TextBox ID="txtAvisoIdentificador" runat="server" MaxLength="60" placeholder="Ej: MP-123456" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtAvisoImporte.ClientID %>">Monto abonado</label>
                        <asp:TextBox ID="txtAvisoImporte" runat="server" MaxLength="15" inputmode="decimal" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlAvisoMoneda.ClientID %>">Moneda</label>
                        <asp:DropDownList ID="ddlAvisoMoneda" runat="server">
                            <asp:ListItem Text="ARS" Value="ARS" />
                            <asp:ListItem Text="USD" Value="USD" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label for="<%= txtAvisoFecha.ClientID %>">Fecha de la operación</label>
                        <asp:TextBox ID="txtAvisoFecha" runat="server" TextMode="DateTimeLocal" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlAvisoMedio.ClientID %>">Medio utilizado</label>
                        <asp:DropDownList ID="ddlAvisoMedio" runat="server">
                            <asp:ListItem Text="Sin informar" Value="" />
                            <asp:ListItem Text="Tarjeta de crédito" Value="Tarjeta de crédito" />
                            <asp:ListItem Text="Tarjeta de débito" Value="Tarjeta de débito" />
                            <asp:ListItem Text="Transferencia" Value="Transferencia" />
                            <asp:ListItem Text="Billetera virtual" Value="Billetera virtual" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlAvisoEstado.ClientID %>">Estado informado</label>
                        <asp:DropDownList ID="ddlAvisoEstado" runat="server">
                            <asp:ListItem Text="Sin informar" Value="" />
                            <asp:ListItem Text="Aprobado (confirmado)" Value="Aprobado" />
                            <asp:ListItem Text="Pendiente" Value="Pendiente" />
                            <asp:ListItem Text="Rechazado (fallido)" Value="Rechazado" />
                            <asp:ListItem Text="Cancelado" Value="Cancelado" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-field pagos-aviso-ancho">
                        <label for="<%= txtAvisoMotivo.ClientID %>">Motivo o detalle informado (opcional)</label>
                        <asp:TextBox ID="txtAvisoMotivo" runat="server" MaxLength="300" />
                    </div>
                    <div class="form-actions pagos-aviso-ancho">
                        <asp:Button ID="btnRegistrarAviso" runat="server" CssClass="button button-primary" Text="Registrar aviso" CausesValidation="false" OnClick="btnRegistrarAviso_Click" />
                    </div>
                </asp:Panel>
            </details>
        </div>
    </section>


    <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="review-dialog-layer">
        <dialog class="review-dialog internal-dialog" open aria-labelledby="pago-detalle-titulo">
            <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="review-dialog-close" CausesValidation="false" OnClick="lnkCerrarDetalle_Click" aria-label="Cerrar">×</asp:LinkButton>
            <span class="section-label">Detalle de pago</span>
            <h2 id="pago-detalle-titulo"><asp:Literal ID="litDetalleTitulo" runat="server" Mode="Encode" /></h2>
            <p><asp:Label ID="lblDetalleEstado" runat="server" /></p>

            <dl class="internal-detail-grid">
                <div><dt>Monto abonado</dt><dd><asp:Literal ID="litDetalleImporte" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Fecha de la operación</dt><dd><asp:Literal ID="litDetalleFecha" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Identificador del pago</dt><dd><asp:Literal ID="litDetalleIdentificador" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Medio utilizado</dt><dd><asp:Literal ID="litDetalleMedio" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Estado informado por el proveedor</dt><dd><asp:Literal ID="litDetalleEstadoInformado" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Origen</dt><dd><asp:Literal ID="litDetalleOrigen" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Reserva o cargo asociado</dt><dd><asp:Literal ID="litDetalleReserva" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Espacio y fecha</dt><dd><asp:Literal ID="litDetalleEspacio" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Estado de la reserva</dt><dd><asp:Literal ID="litDetalleEstadoReserva" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Solicitante</dt><dd><asp:Literal ID="litDetalleSolicitante" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Gestor del espacio</dt><dd><asp:Literal ID="litDetalleGestor" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Transacción económica</dt><dd><asp:Literal ID="litDetalleTransaccion" runat="server" Mode="Encode" /></dd></div>
                <div><dt>Factura</dt><dd><asp:Literal ID="litDetalleFactura" runat="server" Mode="Encode" /></dd></div>
                <div class="internal-detail-wide"><dt>Observación / motivo</dt><dd><asp:Literal ID="litDetalleObservacion" runat="server" Mode="Encode" /></dd></div>
            </dl>

            <div class="review-dialog-actions">
                <asp:LinkButton ID="lnkVolverListado" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCerrarDetalle_Click">Volver a Pagos de reservas</asp:LinkButton>
            </div>
        </dialog>
    </asp:Panel>

    <style>
        .internal-filter-row { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)) auto; gap: 0.75rem; align-items: end; margin: 0.5rem 0 0.4rem; }
        .internal-filter-row .form-field { margin-bottom: 0; }
        .internal-filter-row input, .internal-filter-row select, .pagos-aviso-grid input, .pagos-aviso-grid select { width: 100%; box-sizing: border-box; padding: 0.65rem 0.75rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .internal-filter-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
        .internal-list-count { margin: 0.6rem 0; color: var(--color-text-muted); font-size: 0.8rem; }
        .internal-admin-stack > * { min-width: 0; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla .admin-action-link { white-space: nowrap; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .pagos-num { font-variant-numeric: tabular-nums; white-space: nowrap; }
        .pago-estado { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 0.78rem; font-weight: 700; background: #eee; }
        .pago-estado-Confirmado { background: #e6f5eb; color: #236240; }
        .pago-estado-Pendiente { background: #fff4e5; color: #8a4b0f; }
        .pago-estado-Fallido, .pago-estado-Cancelado { background: #fae9ec; color: #7a3340; }
        .pago-estado-EnRevision { background: #eef0fb; color: #33407a; }
        .pagos-aviso summary { cursor: pointer; padding: 4px 0; }
        .pagos-aviso-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 0.75rem; margin-top: 0.75rem; }
        .pagos-aviso-ancho { grid-column: 1 / -1; }
        .review-dialog.internal-dialog { width: min(46rem, calc(100vw - 2rem)); max-height: calc(100vh - 2rem); }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 0 0 18px; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .internal-detail-wide { grid-column: 1 / -1; }
        body:has(.review-dialog-layer) { overflow: hidden; }
        @media (max-width: 900px) {
            .internal-filter-row, .pagos-aviso-grid { grid-template-columns: 1fr 1fr; }
            .internal-detail-grid { grid-template-columns: 1fr 1fr; }
        }
        @media (max-width: 560px) {
            .internal-filter-row, .pagos-aviso-grid, .internal-detail-grid { grid-template-columns: 1fr; }
            .review-dialog.internal-dialog { padding: 1.5rem 1.1rem; }
        }
    </style>
</asp:Content>
