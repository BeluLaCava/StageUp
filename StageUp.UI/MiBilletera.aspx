<%@ Page Title="Mi billetera | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MiBilletera.aspx.cs" Inherits="StageUp.UI.MiBilletera" %>

<asp:Content ID="MiBilleteraContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .billetera-grid { display: grid; grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr); gap: 16px; align-items: start; }
        .billetera-lista { display: grid; gap: 12px; }
        .billetera-tarjeta { border: 1px solid var(--color-border, #e2e2e2); border-radius: 16px; padding: 14px 16px; background: var(--color-surface, #fff); display: grid; gap: 8px; min-width: 0; }
        .billetera-tarjeta.es-preferida { border-color: var(--color-primary, #6d1021); box-shadow: 0 0 0 1px var(--color-primary, #6d1021) inset; }
        .billetera-cabecera { display: flex; justify-content: space-between; align-items: flex-start; gap: 10px; flex-wrap: wrap; }
        .billetera-alias { font-weight: 700; font-size: 1.05em; overflow-wrap: anywhere; }
        .billetera-numero { font-family: Consolas, "Courier New", monospace; letter-spacing: 0.06em; color: var(--color-text-muted, #765f55); }
        .billetera-marcas { display: flex; gap: 6px; flex-wrap: wrap; }
        .billetera-chip { display: inline-block; padding: 2px 9px; border-radius: 999px; font-size: 0.75em; font-weight: 700; background: #f3ebe7; color: #6d1021; }
        .billetera-chip-vencida { background: #fdecec; color: #8f1d1d; }
        .billetera-acciones { display: flex; gap: 14px; flex-wrap: wrap; align-items: center; font-size: 0.9em; }
        .billetera-alias-editor { display: flex; gap: 8px; flex-wrap: wrap; align-items: center; }
        .billetera-alias-editor input { flex: 1 1 12rem; padding: 0.5rem 0.7rem; border: 1px solid var(--color-border, #e2e2e2); border-radius: 8px; font: inherit; }
        .billetera-confirmar { padding: 8px 12px; border-radius: 10px; background: #fff4e5; color: #78350f; display: flex; gap: 12px; flex-wrap: wrap; align-items: center; font-size: 0.9em; }
        .billetera-fila { display: grid; grid-template-columns: 1fr 1fr; gap: 0 12px; }
        .billetera-check { display: flex; gap: 8px; align-items: center; margin: 4px 0 8px; }
        .billetera-check input { width: auto; margin: 0; }
        .billetera-seguro { font-size: 0.84em; color: var(--color-text-muted, #765f55); }
        @media (max-width: 860px) { .billetera-grid { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page user-module-page">
        <div class="static-page-header">
            <span class="section-label">Pagos</span>
            <h1>Mi billetera</h1>
            <p>Guardá tus tarjetas para pagar más rápido. En cada pago te vamos a pedir solo el código de seguridad.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="billetera-grid">
                <div class="auth-card">
                    <h2>Tus tarjetas</h2>
                    <asp:Panel ID="pnlSinTarjetas" runat="server" CssClass="empty-state compact-empty-state">
                        <h3>Todavía no guardaste ninguna tarjeta</h3>
                        <p>Agregala acá o tildá "Guardar esta tarjeta" la próxima vez que pagues una reserva.</p>
                    </asp:Panel>
                    <div class="billetera-lista">
                        <asp:Repeater ID="rptTarjetas" runat="server" OnItemCommand="rptTarjetas_ItemCommand" OnItemDataBound="rptTarjetas_ItemDataBound">
                            <ItemTemplate>
                                <asp:Panel ID="pnlTarjeta" runat="server" CssClass="billetera-tarjeta">
                                    <div class="billetera-cabecera">
                                        <div>
                                            <div class="billetera-alias"><%# Server.HtmlEncode(Convert.ToString(Eval("Alias"))) %></div>
                                            <div class="billetera-numero"><%# Server.HtmlEncode(Convert.ToString(Eval("Marca"))) %> •••• <%# Server.HtmlEncode(Convert.ToString(Eval("UltimosDigitos"))) %></div>
                                        </div>
                                        <div class="billetera-marcas">
                                            <asp:Literal ID="litChips" runat="server" />
                                        </div>
                                    </div>
                                    <div class="billetera-seguro"><asp:Literal ID="litDetalle" runat="server" Mode="Encode" /></div>

                                    <asp:Panel ID="pnlAcciones" runat="server" CssClass="billetera-acciones">
                                        <asp:LinkButton ID="lnkPreferida" runat="server" CssClass="text-link" CommandName="Preferida" CommandArgument='<%# Eval("IdTarjetaGuardada") %>' CausesValidation="false">Marcar como preferida</asp:LinkButton>
                                        <asp:LinkButton ID="lnkAlias" runat="server" CssClass="text-link" CommandName="EditarAlias" CommandArgument='<%# Eval("IdTarjetaGuardada") %>' CausesValidation="false">Cambiar alias</asp:LinkButton>
                                        <asp:LinkButton ID="lnkEliminar" runat="server" CssClass="text-link" CommandName="PedirEliminar" CommandArgument='<%# Eval("IdTarjetaGuardada") %>' CausesValidation="false">Eliminar</asp:LinkButton>
                                    </asp:Panel>

                                    <asp:Panel ID="pnlAlias" runat="server" Visible="false" CssClass="billetera-alias-editor">
                                        <asp:TextBox ID="txtAlias" runat="server" MaxLength="40" Text='<%# Eval("Alias") %>' aria-label="Nuevo alias" />
                                        <asp:Button runat="server" CssClass="button button-primary button-small" Text="Guardar" CommandName="GuardarAlias" CommandArgument='<%# Eval("IdTarjetaGuardada") %>' CausesValidation="false" />
                                        <asp:LinkButton runat="server" CssClass="text-link" CommandName="Cancelar" CausesValidation="false">Cancelar</asp:LinkButton>
                                    </asp:Panel>

                                    <asp:Panel ID="pnlConfirmar" runat="server" Visible="false" CssClass="billetera-confirmar">
                                        <span>¿Eliminar esta tarjeta de tu billetera? Se borran sus datos cifrados.</span>
                                        <asp:Button runat="server" CssClass="button button-primary button-small" Text="Sí, eliminar" CommandName="Eliminar" CommandArgument='<%# Eval("IdTarjetaGuardada") %>' CausesValidation="false" />
                                        <asp:LinkButton runat="server" CssClass="text-link" CommandName="Cancelar" CausesValidation="false">No</asp:LinkButton>
                                    </asp:Panel>
                                </asp:Panel>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>

                <div class="auth-card">
                    <h2>Agregar una tarjeta</h2>
                    <asp:Panel ID="pnlLimite" runat="server" Visible="false" CssClass="form-message">
                        <asp:Literal ID="litLimite" runat="server" Mode="Encode" />
                    </asp:Panel>
                    <asp:Panel ID="pnlAgregar" runat="server" DefaultButton="btnAgregar">
                        <div class="form-field">
                            <label for="<%= txtTitular.ClientID %>">Titular (como figura en la tarjeta)</label>
                            <asp:TextBox ID="txtTitular" runat="server" MaxLength="100" autocomplete="cc-name" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtNumero.ClientID %>">Número de tarjeta</label>
                            <asp:TextBox ID="txtNumero" runat="server" MaxLength="23" autocomplete="cc-number" inputmode="numeric" placeholder="1234 5678 9012 3456" />
                        </div>
                        <div class="billetera-fila">
                            <div class="form-field">
                                <label for="<%= ddlMes.ClientID %>">Mes de vencimiento</label>
                                <asp:DropDownList ID="ddlMes" runat="server" autocomplete="cc-exp-month" />
                            </div>
                            <div class="form-field">
                                <label for="<%= ddlAnio.ClientID %>">Año</label>
                                <asp:DropDownList ID="ddlAnio" runat="server" autocomplete="cc-exp-year" />
                            </div>
                        </div>
                        <div class="form-field">
                            <label for="<%= txtAliasNueva.ClientID %>">Alias (opcional)</label>
                            <asp:TextBox ID="txtAliasNueva" runat="server" MaxLength="40" placeholder="Por ejemplo: Visa del trabajo" />
                        </div>
                        <div class="billetera-check">
                            <asp:CheckBox ID="chkPreferida" runat="server" />
                            <label for="<%= chkPreferida.ClientID %>">Usarla como tarjeta preferida</label>
                        </div>
                        <div class="form-actions">
                            <asp:Button ID="btnAgregar" runat="server" CssClass="button button-primary" Text="Guardar tarjeta" CausesValidation="false" OnClick="btnAgregar_Click" />
                        </div>
                    </asp:Panel>
                    <p class="billetera-seguro">El código de seguridad no se pide ni se guarda: te lo pedimos en cada pago. El número y el titular se guardan cifrados (AES-256 con una clave propia por tarjeta, protegida con RSA de 2048 bits); en pantalla solo se ven la marca y los últimos 4 dígitos.</p>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
