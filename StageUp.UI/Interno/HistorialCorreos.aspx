<%@ Page Title="Historial de correos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="HistorialCorreos.aspx.cs" Inherits="StageUp.UI.Interno.HistorialCorreos" %>
<asp:Content ID="Contenido" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .mail-history-table { width:100%; border-collapse:collapse; font-size:.9rem; }
        .mail-history-table th { text-align:left; color:#720e24; background:#faf3ef; white-space:nowrap; }
        .mail-history-table th, .mail-history-table td { padding:12px 14px; border-bottom:1px solid #e7d6cf; vertical-align:top; }
        .mail-history-table td { overflow-wrap:anywhere; }
        .mail-history-table tr:hover td { background:#fdf8f5; }
    </style>
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Historial de correos</h1>
            <p>Avisos de reservas, pagos y comprobantes. Un envío aceptado por el servidor no confirma que el destinatario lo haya recibido o leído.</p>
            <a class="text-link" href="TransaccionesEconomicas.aspx">← Volver a Transacciones económicas</a>
        </div>
        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" CssClass="form-message" Visible="false" role="status"><asp:Literal ID="litMensaje" runat="server" Mode="Encode" /></asp:Panel>
            <div class="auth-card admin-list-card">
                <h2>Comunicaciones registradas</h2>
                <p>Se muestran los últimos 200 resultados. Usá los filtros para consultar otros períodos. “Sin resultado confirmado” puede indicar un envío en curso o interrumpido.</p>
                <asp:Panel ID="Filtros" runat="server" CssClass="internal-filter-row" DefaultButton="btnFiltrar">
                    <div class="form-field"><label for="<%= ddlEstado.ClientID %>">Estado</label><asp:DropDownList ID="ddlEstado" runat="server">
                        <asp:ListItem Text="Todos" Value="" /><asp:ListItem Text="Enviado al servidor" Value="Enviado" /><asp:ListItem Text="Fallido" Value="Fallido" /><asp:ListItem Text="Sin resultado confirmado" Value="EnProceso" />
                    </asp:DropDownList></div>
                    <div class="form-field"><label for="<%= txtDesde.ClientID %>">Desde</label><asp:TextBox ID="txtDesde" runat="server" TextMode="Date" /></div>
                    <div class="form-field"><label for="<%= txtHasta.ClientID %>">Hasta</label><asp:TextBox ID="txtHasta" runat="server" TextMode="Date" /></div>
                    <div class="form-field"><label for="<%= txtReserva.ClientID %>">N° de reserva</label><asp:TextBox ID="txtReserva" runat="server" MaxLength="10" /></div>
                    <div class="form-field"><label for="<%= txtDestinatario.ClientID %>">Destinatario</label><asp:TextBox ID="txtDestinatario" runat="server" MaxLength="300" /></div>
                    <div class="internal-filter-actions"><asp:Button ID="btnFiltrar" runat="server" Text="Aplicar filtros" CssClass="button button-secondary" OnClick="Filtrar" /></div>
                </asp:Panel>
                <div style="overflow-x:auto">
                    <asp:GridView ID="grilla" runat="server" AutoGenerateColumns="false" CssClass="mail-history-table" GridLines="None" EmptyDataText="No hay correos registrados para estos filtros.">
                        <Columns>
                            <asp:BoundField DataField="FechaIntento" HeaderText="Fecha y hora" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                            <asp:BoundField DataField="Destinatario" HeaderText="Destinatario" />
                            <asp:BoundField DataField="Asunto" HeaderText="Asunto" />
                            <asp:BoundField DataField="IdReserva" HeaderText="Reserva" />
                            <asp:BoundField DataField="IdPago" HeaderText="Pago" />
                            <asp:BoundField DataField="IdComprobante" HeaderText="Comprobante" />
                            <asp:TemplateField HeaderText="Estado"><ItemTemplate><%#: Convert.ToString(Eval("Estado")) == "EnProceso" ? "Sin resultado confirmado" : Convert.ToString(Eval("Estado")) == "Enviado" ? "Enviado al servidor" : "Fallido" %></ItemTemplate></asp:TemplateField>
                            <asp:BoundField DataField="Detalle" HeaderText="Resultado" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
