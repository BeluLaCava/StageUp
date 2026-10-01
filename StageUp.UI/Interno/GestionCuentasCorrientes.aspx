<%@ Page Title="Pagos y cuentas corrientes | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionCuentasCorrientes.aspx.cs" Inherits="StageUp.UI.Interno.GestionCuentasCorrientes" %>

<asp:Content ID="GestionCuentasCorrientesContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .gcc-stack { grid-template-columns: minmax(0, 1fr); }
        .gcc-stack > * { min-width: 0; }
        .gcc-busqueda { display: flex; gap: 10px; align-items: flex-end; flex-wrap: wrap; }
        .gcc-busqueda .form-field { flex: 1 1 260px; margin: 0; }
        .gcc-tabla-wrap { overflow-x: auto; }
        .gcc-tabla { width: 100%; border-collapse: collapse; font-size: 0.88em; }
        .gcc-tabla th, .gcc-tabla td { padding: 7px 8px; border-bottom: 1px solid var(--color-border, #e2e2e2); text-align: left; vertical-align: top; }
        .gcc-tabla .num { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
        .gcc-meta { color: var(--color-text-muted, #765f55); font-size: 0.85em; }
        .gcc-saldos { display: flex; gap: 10px; flex-wrap: wrap; margin: 8px 0 14px; }
        .gcc-saldo { padding: 8px 14px; border-radius: 10px; border: 1px solid var(--color-border, #e2e2e2); font-variant-numeric: tabular-nums; }
        .gcc-formularios { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
        .gcc-form-fila { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0 10px; }
        .gcc-filtros { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0 10px; }
        .gcc-alerta { padding: 12px 14px; border-radius: 12px; background: #fff4e5; border-left: 4px solid #b45309; color: #78350f; margin-bottom: 12px; }
        .gcc-estado-rechazado { color: #9a3412; font-weight: 600; }
        @media (max-width: 900px) { .gcc-formularios, .gcc-form-fila, .gcc-filtros { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Pagos y cuentas corrientes</h1>
            <p>Consultá las cuentas de clientes y gestores, emití o anulá notas de crédito y débito, registrá liquidaciones y revisá los pagos.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack gcc-stack">
            <!-- Búsqueda de cuentas -->
            <asp:Panel ID="pnlBusqueda" runat="server" CssClass="auth-card admin-list-card" DefaultButton="btnBuscar">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Cuentas</span>
                    <h2>Buscar usuario</h2>
                    <p>Sin texto se listan los usuarios que ya tienen movimientos. Buscando por nombre o correo aparecen también los que todavía no tienen.</p>
                </div>
                <div class="gcc-busqueda">
                    <div class="form-field">
                        <label for="<%= txtBuscar.ClientID %>">Nombre o correo</label>
                        <asp:TextBox ID="txtBuscar" runat="server" MaxLength="100" />
                    </div>
                    <asp:Button ID="btnBuscar" runat="server" CssClass="button button-primary" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
                </div>

                <asp:Panel ID="pnlSinCuentas" runat="server" CssClass="admin-empty-hint" Visible="false">No se encontraron usuarios.</asp:Panel>
                <div class="gcc-tabla-wrap">
                    <table class="gcc-tabla">
                        <thead>
                            <tr><th>Usuario</th><th>Cuenta</th><th class="num">Saldo</th><th></th></tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptCuentas" runat="server" OnItemCommand="rptCuentas_ItemCommand">
                                <ItemTemplate>
                                    <tr>
                                        <td><strong><%#: Eval("NombreUsuario") %></strong><br /><span class="gcc-meta"><%#: Eval("CorreoUsuario") %></span></td>
                                        <td><%#: Eval("RolCuenta") == null ? "Sin movimientos" : Eval("RolCuenta") + " · " + Eval("Moneda") %></td>
                                        <td class="num"><%#: Eval("Moneda") == null ? "-" : StageUp.BLL.BLL_CuentaCorriente.FormatearImporte((decimal)Eval("Saldo"), Eval("Moneda") as string) %></td>
                                        <td>
                                            <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false" CommandName="Ver"
                                                CommandArgument='<%# Eval("IdUsuarioExterno") + "|" + (Eval("RolCuenta") ?? "Cliente") %>' Text="Ver cuenta" />
                                            <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false" CommandName="Ver"
                                                Visible='<%# Eval("RolCuenta") == null %>'
                                                CommandArgument='<%# Eval("IdUsuarioExterno") + "|Gestor" %>' Text="Ver cuenta de gestor" />
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </asp:Panel>

            <!-- Cuenta elegida -->
            <asp:Panel ID="pnlCuenta" runat="server" CssClass="auth-card admin-list-card" Visible="false">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow"><asp:Literal ID="litRolCuenta" runat="server" /></span>
                    <h2><asp:Literal ID="litUsuarioCuenta" runat="server" /></h2>
                </div>
                <div class="gcc-saldos">
                    <asp:Repeater ID="rptSaldosCuenta" runat="server">
                        <ItemTemplate>
                            <span class="gcc-saldo"><%#: Eval("Moneda") %>: <strong><%#: StageUp.BLL.BLL_CuentaCorriente.FormatearImporte((decimal)Eval("Saldo"), Eval("Moneda") as string) %></strong></span>
                        </ItemTemplate>
                    </asp:Repeater>
                    <asp:Literal ID="litSinSaldosCuenta" runat="server" Text="Sin movimientos todavía." Visible="false" />
                </div>

                <asp:Panel ID="pnlAnular" runat="server" CssClass="gcc-alerta" Visible="false" DefaultButton="btnConfirmarAnulacion">
                    Vas a anular <strong><asp:Literal ID="litComprobanteAAnular" runat="server" /></strong>. Se registra un movimiento inverso y el comprobante queda como anulado.
                    <div class="form-field">
                        <label for="<%= txtMotivoAnulacion.ClientID %>">Motivo de la anulación</label>
                        <asp:TextBox ID="txtMotivoAnulacion" runat="server" MaxLength="500" />
                    </div>
                    <asp:Button ID="btnConfirmarAnulacion" runat="server" CssClass="button button-primary" Text="Anular comprobante" CausesValidation="false" OnClick="btnConfirmarAnulacion_Click" />
                    <asp:LinkButton ID="lnkCancelarAnulacion" runat="server" CssClass="text-link" Text="Cancelar" CausesValidation="false" OnClick="lnkCancelarAnulacion_Click" />
                </asp:Panel>

                <div class="gcc-formularios">
                    <asp:Panel ID="pnlEmitir" runat="server" DefaultButton="btnEmitir">
                        <h3>Emitir nota de crédito o débito</h3>
                        <div class="gcc-form-fila">
                            <div class="form-field">
                                <label for="<%= ddlTipoComprobante.ClientID %>">Tipo</label>
                                <asp:DropDownList ID="ddlTipoComprobante" runat="server">
                                    <asp:ListItem Value="NC">Nota de crédito</asp:ListItem>
                                    <asp:ListItem Value="ND">Nota de débito</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="form-field">
                                <label for="<%= ddlMonedaComprobante.ClientID %>">Moneda</label>
                                <asp:DropDownList ID="ddlMonedaComprobante" runat="server">
                                    <asp:ListItem Value="ARS">ARS</asp:ListItem>
                                    <asp:ListItem Value="USD">USD</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="form-field">
                                <label for="<%= txtImporteComprobante.ClientID %>">Importe</label>
                                <asp:TextBox ID="txtImporteComprobante" runat="server" MaxLength="15" placeholder="Ej: 1500,50" inputmode="decimal" />
                            </div>
                        </div>
                        <div class="form-field">
                            <label for="<%= txtReservaComprobante.ClientID %>">N° de reserva (opcional)</label>
                            <asp:TextBox ID="txtReservaComprobante" runat="server" MaxLength="10" inputmode="numeric" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtMotivoComprobante.ClientID %>">Motivo</label>
                            <asp:TextBox ID="txtMotivoComprobante" runat="server" MaxLength="500" placeholder="Ej: Bonificación por inconveniente con el espacio" />
                        </div>
                        <asp:Button ID="btnEmitir" runat="server" CssClass="button button-primary" Text="Emitir comprobante" CausesValidation="false" OnClick="btnEmitir_Click" />
                    </asp:Panel>

                    <asp:Panel ID="pnlLiquidar" runat="server" Visible="false" DefaultButton="btnLiquidar">
                        <h3>Registrar liquidación al gestor</h3>
                        <p class="gcc-meta">Registrá acá la transferencia que StageUp le hizo al gestor. No puede superar su saldo a favor.</p>
                        <div class="gcc-form-fila">
                            <div class="form-field">
                                <label for="<%= ddlMonedaLiquidacion.ClientID %>">Moneda</label>
                                <asp:DropDownList ID="ddlMonedaLiquidacion" runat="server">
                                    <asp:ListItem Value="ARS">ARS</asp:ListItem>
                                    <asp:ListItem Value="USD">USD</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="form-field">
                                <label for="<%= txtImporteLiquidacion.ClientID %>">Importe</label>
                                <asp:TextBox ID="txtImporteLiquidacion" runat="server" MaxLength="15" inputmode="decimal" />
                            </div>
                        </div>
                        <div class="form-field">
                            <label for="<%= txtDetalleLiquidacion.ClientID %>">Detalle (opcional)</label>
                            <asp:TextBox ID="txtDetalleLiquidacion" runat="server" MaxLength="300" placeholder="Ej: Transferencia bancaria del 30/09" />
                        </div>
                        <asp:Button ID="btnLiquidar" runat="server" CssClass="button button-primary" Text="Registrar liquidación" CausesValidation="false" OnClick="btnLiquidar_Click" />
                    </asp:Panel>
                </div>

                <h3>Movimientos</h3>
                <asp:Panel ID="pnlSinMovimientosCuenta" runat="server" CssClass="admin-empty-hint" Visible="false">La cuenta no tiene movimientos.</asp:Panel>
                <div class="gcc-tabla-wrap">
                    <table class="gcc-tabla">
                        <thead>
                            <tr><th>Fecha</th><th>Concepto</th><th>Detalle</th><th class="num">Importe</th><th class="num">Saldo</th><th></th></tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptMovimientosCuenta" runat="server" OnItemCommand="rptMovimientosCuenta_ItemCommand">
                                <ItemTemplate>
                                    <tr>
                                        <td><%#: Eval("FechaMovimiento", "{0:dd/MM/yyyy HH:mm}") %></td>
                                        <td><%#: StageUp.BLL.BLL_CuentaCorriente.EtiquetaMovimiento(Eval("TipoMovimiento") as string) %></td>
                                        <td><%#: Eval("Descripcion") %>
                                            <%# string.IsNullOrEmpty(Eval("NumeroComprobante") as string) ? "" : "<br /><span class=\"gcc-meta\">" + Server.HtmlEncode(Eval("NumeroComprobante") + " · " + Eval("EstadoComprobante")) + "</span>" %>
                                        </td>
                                        <td class="num"><%#: StageUp.BLL.BLL_CuentaCorriente.FormatearImporte((decimal)Eval("Importe"), Eval("Moneda") as string) %></td>
                                        <td class="num"><%#: StageUp.BLL.BLL_CuentaCorriente.FormatearImporte((decimal)Eval("SaldoAcumulado"), Eval("Moneda") as string) %></td>
                                        <td>
                                            <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                                CommandName="Anular" CommandArgument='<%# Eval("IdComprobante") + "|" + Eval("NumeroComprobante") %>' Text="Anular"
                                                Visible='<%# EsAnulable(Container.DataItem) %>' />
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </asp:Panel>

            <!-- Pagos -->
            <asp:Panel ID="pnlPagos" runat="server" CssClass="auth-card admin-list-card" DefaultButton="btnFiltrarPagos">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Pagos</span>
                    <h2>Pagos recibidos y rechazados</h2>
                </div>
                <div class="gcc-filtros">
                    <div class="form-field">
                        <label for="<%= txtPagosDesde.ClientID %>">Desde</label>
                        <asp:TextBox ID="txtPagosDesde" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtPagosHasta.ClientID %>">Hasta</label>
                        <asp:TextBox ID="txtPagosHasta" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlEstadoPago.ClientID %>">Estado</label>
                        <asp:DropDownList ID="ddlEstadoPago" runat="server">
                            <asp:ListItem Value="">Todos</asp:ListItem>
                            <asp:ListItem Value="Aprobado">Aprobados</asp:ListItem>
                            <asp:ListItem Value="Rechazado">Rechazados</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>
                <div class="form-actions">
                    <asp:Button ID="btnFiltrarPagos" runat="server" CssClass="button button-primary" Text="Filtrar" CausesValidation="false" OnClick="btnFiltrarPagos_Click" />
                </div>
                <asp:Panel ID="pnlSinPagos" runat="server" CssClass="admin-empty-hint" Visible="false">No hay pagos en ese período.</asp:Panel>
                <div class="gcc-tabla-wrap">
                    <table class="gcc-tabla">
                        <thead>
                            <tr><th>Fecha</th><th>Usuario</th><th>Concepto</th><th class="num">Total</th><th>Medio</th><th>Estado</th></tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptPagos" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td><%#: Eval("FechaPago", "{0:dd/MM/yyyy HH:mm}") %><br /><span class="gcc-meta">N° <%#: Eval("IdPago") %></span></td>
                                        <td><%#: Eval("NombreUsuario") %><br /><span class="gcc-meta"><%#: Eval("CorreoUsuario") %></span></td>
                                        <td><%#: DescribirConcepto(Container.DataItem) %></td>
                                        <td class="num"><%#: StageUp.BLL.BLL_CuentaCorriente.FormatearImporte((decimal)Eval("ImporteTotal"), Eval("Moneda") as string) %></td>
                                        <td><%#: DescribirMedio(Container.DataItem) %></td>
                                        <td class='<%# (Eval("Estado") as string) == "Rechazado" ? "gcc-estado-rechazado" : "" %>'>
                                            <%#: Eval("Estado") %><%#: string.IsNullOrEmpty(Eval("MotivoRechazo") as string) ? "" : ": " + Eval("MotivoRechazo") %>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
