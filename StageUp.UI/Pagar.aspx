<%@ Page Title="Pagar reserva | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Pagar.aspx.cs" Inherits="StageUp.UI.Pagar" %>

<asp:Content ID="PagarContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .pago-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.3fr); gap: 16px; align-items: start; }
        .pago-resumen dl { display: grid; grid-template-columns: auto 1fr; gap: 6px 14px; margin: 0; }
        .pago-resumen dt { color: var(--color-text-muted, #765f55); font-size: 0.9em; }
        .pago-resumen dd { margin: 0; font-weight: 600; }
        .pago-total { font-size: 1.6em; font-weight: 700; color: var(--color-primary, #6d1021); margin: 12px 0 4px; font-variant-numeric: tabular-nums; }
        .pago-vence { padding: 8px 12px; border-radius: 10px; background: #fff4e5; border-left: 4px solid #b45309; color: #78350f; font-size: 0.9em; margin-top: 12px; }
        .pago-saldo { padding: 12px 14px; border-radius: 12px; background: var(--color-nude-light, #fcf7f3); border: 1px solid var(--color-border, #e2e2e2); margin-bottom: 14px; }
        .pago-desglose { margin: 8px 0 0; font-size: 0.92em; }
        .pago-desglose span { display: flex; justify-content: space-between; padding: 2px 0; font-variant-numeric: tabular-nums; }
        .pago-tarjeta-fila { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 0 12px; }
        .pago-prueba { font-size: 0.82em; color: var(--color-text-muted, #765f55); margin-top: 10px; }
        .pago-prueba summary { cursor: pointer; }
        .pago-seguro { font-size: 0.82em; color: var(--color-text-muted, #765f55); margin-top: 8px; }
        @media (max-width: 800px) { .pago-grid { grid-template-columns: 1fr; } }
        @media (max-width: 480px) { .pago-tarjeta-fila { grid-template-columns: 1fr 1fr; } }
    </style>

    <section class="static-page user-module-page">
        <div class="static-page-header">
            <span class="section-label">Pago</span>
            <h1>Pagar reserva</h1>
            <p>Confirmá tu reserva pagando con tarjeta, con tu saldo a favor o combinando los dos.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlNoDisponible" runat="server" Visible="false" CssClass="auth-card">
                <p><asp:Literal ID="litNoDisponible" runat="server" /></p>
                <a class="button button-primary" href="MisReservas.aspx">Volver a Mis reservas</a>
            </asp:Panel>

            <asp:Panel ID="pnlExito" runat="server" Visible="false" CssClass="auth-card">
                <span class="section-label">Comprobante de pago</span>
                <h2>¡Pago recibido! Tu reserva está confirmada</h2>
                <div class="pago-resumen">
                    <dl>
                        <dt>N° de pago</dt><dd><asp:Literal ID="litExitoNumero" runat="server" /></dd>
                        <dt>Reserva</dt><dd><asp:Literal ID="litExitoReserva" runat="server" /></dd>
                        <dt>Total</dt><dd><asp:Literal ID="litExitoTotal" runat="server" /></dd>
                        <dt>Medio de pago</dt><dd><asp:Literal ID="litExitoMedios" runat="server" /></dd>
                    </dl>
                </div>
                <p>Te mandamos el comprobante por mail.</p>
                <div class="form-actions">
                    <a class="button button-primary" href="MisReservas.aspx">Ir a Mis reservas</a>
                    <a class="text-link" href="MiCuentaCorriente.aspx">Ver mi cuenta corriente</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlPago" runat="server" CssClass="pago-grid" DefaultButton="btnPagar">
                <div class="auth-card pago-resumen">
                    <span class="section-label">Tu reserva</span>
                    <h2><asp:Literal ID="litEspacio" runat="server" /></h2>
                    <dl>
                        <dt>N° de reserva</dt><dd><asp:Literal ID="litNumeroReserva" runat="server" /></dd>
                        <dt>Fecha</dt><dd><asp:Literal ID="litFecha" runat="server" /></dd>
                        <dt>Horario</dt><dd><asp:Literal ID="litHorario" runat="server" /></dd>
                    </dl>
                    <div class="pago-total"><asp:Literal ID="litTotal" runat="server" /></div>
                    <div class="pago-vence"><asp:Literal ID="litVence" runat="server" /></div>
                </div>

                <div class="auth-card">
                    <asp:Panel ID="pnlSaldo" runat="server" CssClass="pago-saldo" Visible="false">
                        <asp:CheckBox ID="chkUsarSaldo" runat="server" AutoPostBack="true" OnCheckedChanged="chkUsarSaldo_CheckedChanged" />
                        <div class="pago-desglose">
                            <span>Con saldo a favor <strong><asp:Literal ID="litConSaldo" runat="server" /></strong></span>
                            <span>Con tarjeta <strong><asp:Literal ID="litConTarjeta" runat="server" /></strong></span>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlTarjeta" runat="server">
                        <h2>Tarjeta de crédito</h2>
                        <div class="form-field">
                            <label for="<%= txtTitular.ClientID %>">Titular (como figura en la tarjeta)</label>
                            <asp:TextBox ID="txtTitular" runat="server" MaxLength="100" autocomplete="cc-name" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtNumero.ClientID %>">Número de tarjeta</label>
                            <asp:TextBox ID="txtNumero" runat="server" MaxLength="23" autocomplete="cc-number" inputmode="numeric" placeholder="1234 5678 9012 3456" />
                        </div>
                        <div class="pago-tarjeta-fila">
                            <div class="form-field">
                                <label for="<%= ddlMes.ClientID %>">Mes</label>
                                <asp:DropDownList ID="ddlMes" runat="server" autocomplete="cc-exp-month" />
                            </div>
                            <div class="form-field">
                                <label for="<%= ddlAnio.ClientID %>">Año</label>
                                <asp:DropDownList ID="ddlAnio" runat="server" autocomplete="cc-exp-year" />
                            </div>
                            <div class="form-field">
                                <label for="<%= txtCodigo.ClientID %>">Código</label>
                                <asp:TextBox ID="txtCodigo" runat="server" TextMode="Password" MaxLength="4" autocomplete="cc-csc" inputmode="numeric" />
                            </div>
                        </div>
                        <p class="pago-seguro">No guardamos el número completo ni el código de seguridad de tu tarjeta: solo la marca y los últimos 4 dígitos.</p>
                        <details class="pago-prueba">
                            <summary>Tarjetas de prueba</summary>
                            <p>Aprobadas: Visa 4111 1111 1111 1111 · Mastercard 5555 5555 5555 4444 · American Express 3782 822463 10005.<br />
                               Rechazadas: terminada en 0002 (rechazo del banco), 9995 (sin fondos) o 0119 (error del procesador), por ejemplo 4000 0000 0000 0002.<br />
                               Cualquier vencimiento futuro y cualquier código.</p>
                        </details>
                    </asp:Panel>

                    <div class="form-actions">
                        <asp:Button ID="btnPagar" runat="server" CssClass="button button-primary" CausesValidation="false"
                            UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Procesando pago...';"
                            OnClick="btnPagar_Click" />
                        <a class="text-link" href="MisReservas.aspx">Volver</a>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
