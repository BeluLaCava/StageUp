<%@ Page Title="Mis reservas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MisReservas.aspx.cs" Inherits="StageUp.UI.MisReservas" %>

<asp:Content ID="MisReservasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Mis reservas</span>
            <h1>Tus solicitudes de reserva</h1>
            <p>Acá vas a ver el historial de las reservas que solicitaste, con su estado actual.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="auth-card">
                <div class="auth-card-header">
                    <h2>Historial</h2>
                </div>

                <asp:Literal ID="litSinReservas" runat="server" Visible="false"
                    Text="Todavía no solicitaste ninguna reserva. Podés explorar espacios publicados y solicitar una desde su detalle." />

                <asp:Repeater ID="rptMisReservas" runat="server" OnItemCommand="rptMisReservas_ItemCommand" OnItemDataBound="rptMisReservas_ItemDataBound">
                    <ItemTemplate>
                        <div class="space-row">
                            <div class="space-row-info">
                                <h3><%# Eval("NombreEspacio") %></h3>
                                <p>Fecha solicitada: <%# Eval("FechaSolicitada", "{0:dd/MM/yyyy}") %> · Estado: <%# Eval("EstadoReserva") %></p>
                                <p class="space-row-horario">
                                    <asp:Literal ID="litHorarioImporte" runat="server"
                                        Visible='<%# ((StageUp.BE.Entidades.Reserva)Container.DataItem).MinutoDesde.HasValue %>' />
                                </p>
                                <p class="space-row-descripcion"><%# Eval("ComentarioSolicitante") %></p>
                                <asp:Literal ID="litComentarioResolucion" runat="server" Visible='<%# !string.IsNullOrEmpty(Eval("ComentarioResolucion") as string) %>' />
                            </div>
                            <div class="space-row-actions">
                                <asp:LinkButton ID="lnkCancelar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Cancelar" CommandArgument='<%# Eval("IdReserva") %>' Text="Cancelar" />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>
</asp:Content>
