<%@ Page Title="Mi cuenta corriente | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MiCuentaCorriente.aspx.cs" Inherits="StageUp.UI.MiCuentaCorriente" %>

<asp:Content ID="MiCuentaCorrienteContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .cc-tabs { display: flex; gap: 8px; margin-bottom: 14px; flex-wrap: wrap; }
        .cc-tab { padding: 8px 16px; border-radius: 999px; border: 1px solid var(--color-border, #e2e2e2); text-decoration: none; color: var(--color-text, #3e2925); }
        .cc-tab.is-active { background: var(--color-primary, #6d1021); border-color: var(--color-primary, #6d1021); color: #fff; }
        .cc-saldos { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px; }
        .cc-saldo { border: 1px solid var(--color-border, #e2e2e2); border-radius: var(--radius-medium, 1rem); padding: 14px 16px; background: var(--color-surface, #fffcfa); }
        .cc-saldo-label { display: block; color: var(--color-text-muted, #765f55); font-size: 0.85em; }
        .cc-saldo-valor { display: block; font-size: 1.5em; font-weight: 700; margin: 4px 0; font-variant-numeric: tabular-nums; white-space: nowrap; }
        .cc-saldo-sub { display: block; color: var(--color-text-muted, #765f55); font-size: 0.8em; }
        .cc-nota { color: var(--color-text-muted, #765f55); font-size: 0.88em; margin-top: 10px; }
        .cc-deuda { padding: 14px 16px; border-radius: 12px; background: #fff4e5; border-left: 4px solid #b45309; margin-top: 14px; }
        .cc-filtros { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 0 12px; }
        .cc-tarjeta { display: grid; grid-template-columns: 2fr 2fr 1fr 1fr 1fr; gap: 0 10px; }
        .cc-tabla-wrap { overflow-x: auto; }
        .cc-tabla { width: 100%; border-collapse: collapse; font-size: 0.9em; }
        .cc-tabla th, .cc-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border, #e2e2e2); text-align: left; vertical-align: top; }
        .cc-tabla .num { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
        .cc-comprobante { display: inline-block; font-size: 0.8em; padding: 1px 8px; border-radius: 999px; border: 1px solid var(--color-border, #e2e2e2); margin-top: 3px; }
        .cc-vacio { color: var(--color-text-muted, #765f55); padding: 14px 0; }
        .cc-filtros > *, .cc-tarjeta > * { min-width: 0; }
        .cc-filtros input, .cc-tarjeta input, .cc-filtros select, .cc-tarjeta select { width: 100%; box-sizing: border-box; }
        @media (max-width: 520px) { .cc-filtros, .cc-tarjeta { grid-template-columns: 1fr !important; } }
        @media (max-width: 800px) {
            .cc-filtros, .cc-tarjeta { grid-template-columns: 1fr 1fr; }
            .cc-tabla thead { display: none; }
            .cc-tabla tr { display: block; padding: 8px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); }
            .cc-tabla td { display: flex; justify-content: space-between; gap: 12px; border: 0; padding: 3px 0; }
            .cc-tabla td::before { content: attr(data-label); color: var(--color-text-muted, #765f55); }
            .cc-tabla .num { text-align: right; }
        }
    </style>

    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Cuenta corriente</span>
            <h1>Mi cuenta corriente</h1>
            <p>Tus pagos, notas de crédito y débito y saldo en StageUp.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlTabs" runat="server" CssClass="cc-tabs" Visible="false">
                <asp:HyperLink ID="lnkCuentaCliente" runat="server" NavigateUrl="~/MiCuentaCorriente.aspx" Text="Como cliente" />
                <asp:HyperLink ID="lnkCuentaGestor" runat="server" NavigateUrl="~/MiCuentaCorriente.aspx?cuenta=Gestor" Text="Como gestor de espacios" />
            </asp:Panel>

            <div class="auth-card">
                <div class="auth-card-header">
                    <h2><asp:Literal ID="litTituloSaldo" runat="server" /></h2>
                </div>
                <asp:Panel ID="pnlSinSaldos" runat="server" CssClass="cc-vacio" Visible="false">
                    Todavía no tenés movimientos en esta cuenta.
                </asp:Panel>
                <div class="cc-saldos">
                    <asp:Repeater ID="rptSaldos" runat="server">
                        <ItemTemplate>
                            <div class="cc-saldo">
                                <span class="cc-saldo-label"><%#: DescribirSaldo(Container.DataItem) %></span>
                                <span class="cc-saldo-valor"><%#: Importe(Math.Abs((decimal)Eval("Saldo")), Eval("Moneda") as string) %></span>
                                <span class="cc-saldo-sub"><%#: Eval("CantidadMovimientos") %> movimientos · último el <%#: Eval("UltimoMovimiento", "{0:dd/MM/yyyy}") %></span>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <p class="cc-nota"><asp:Literal ID="litNotaCuenta" runat="server" /></p>

                <asp:Panel ID="pnlDeuda" runat="server" CssClass="cc-deuda" Visible="false" DefaultButton="btnPagarDeuda">
                    <h3>Pagar saldo deudor</h3>
                    <p>Tenés <strong><asp:Literal ID="litDeuda" runat="server" /></strong> pendiente de pago. Podés cancelarlo con tarjeta.</p>
                    <asp:HiddenField ID="hfMonedaDeuda" runat="server" />
                    <div class="cc-tarjeta">
                        <div class="form-field">
                            <label for="<%= txtTitular.ClientID %>">Titular</label>
                            <asp:TextBox ID="txtTitular" runat="server" MaxLength="100" autocomplete="cc-name" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtNumero.ClientID %>">Número de tarjeta</label>
                            <asp:TextBox ID="txtNumero" runat="server" MaxLength="23" autocomplete="cc-number" inputmode="numeric" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlMes.ClientID %>">Mes</label>
                            <asp:DropDownList ID="ddlMes" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlAnio.ClientID %>">Año</label>
                            <asp:DropDownList ID="ddlAnio" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCodigo.ClientID %>">Código</label>
                            <asp:TextBox ID="txtCodigo" runat="server" TextMode="Password" MaxLength="4" autocomplete="cc-csc" inputmode="numeric" />
                        </div>
                    </div>
                    <asp:Button ID="btnPagarDeuda" runat="server" CssClass="button button-primary" CausesValidation="false"
                        UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Procesando pago...';"
                        OnClick="btnPagarDeuda_Click" />
                </asp:Panel>
            </div>

            <div class="auth-card">
                <div class="auth-card-header">
                    <h2>Movimientos</h2>
                </div>

                <asp:Panel ID="pnlFiltros" runat="server" DefaultButton="btnFiltrar">
                    <div class="cc-filtros">
                        <div class="form-field">
                            <label for="<%= txtDesde.ClientID %>">Desde</label>
                            <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtHasta.ClientID %>">Hasta</label>
                            <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlMoneda.ClientID %>">Moneda</label>
                            <asp:DropDownList ID="ddlMoneda" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlTipo.ClientID %>">Tipo de movimiento</label>
                            <asp:DropDownList ID="ddlTipo" runat="server" />
                        </div>
                    </div>
                    <div class="form-actions">
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-primary" Text="Filtrar" CausesValidation="false" OnClick="btnFiltrar_Click" />
                        <asp:LinkButton ID="lnkLimpiar" runat="server" CssClass="text-link" Text="Limpiar filtros" CausesValidation="false" OnClick="lnkLimpiar_Click" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlSinMovimientos" runat="server" CssClass="cc-vacio" Visible="false">
                    No hay movimientos con esos filtros.
                </asp:Panel>

                <div class="cc-tabla-wrap">
                    <table class="cc-tabla">
                        <thead>
                            <tr>
                                <th scope="col">Fecha</th>
                                <th scope="col">Concepto</th>
                                <th scope="col">Detalle</th>
                                <th scope="col" class="num">Importe</th>
                                <th scope="col" class="num">Saldo</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptMovimientos" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Fecha"><%#: Eval("FechaMovimiento", "{0:dd/MM/yyyy HH:mm}") %></td>
                                        <td data-label="Concepto"><%#: StageUp.BLL.BLL_CuentaCorriente.EtiquetaMovimiento(Eval("TipoMovimiento") as string) %></td>
                                        <td data-label="Detalle">
                                            <%#: Eval("Descripcion") %>
                                            <%# string.IsNullOrEmpty(Eval("NumeroComprobante") as string) ? "" :
                                                "<br /><span class=\"cc-comprobante\">" + Server.HtmlEncode(Eval("NumeroComprobante") + ((Eval("EstadoComprobante") as string) == "Anulado" ? " · anulado" : "")) + "</span>" %>
                                        </td>
                                        <td data-label="Importe" class="num"><%#: ImporteConSigno((decimal)Eval("Importe"), Eval("Moneda") as string) %></td>
                                        <td data-label="Saldo" class="num"><%#: ImporteConSigno((decimal)Eval("SaldoAcumulado"), Eval("Moneda") as string) %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
