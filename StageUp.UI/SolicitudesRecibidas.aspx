<%@ Page Title="Solicitudes recibidas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="SolicitudesRecibidas.aspx.cs" Inherits="StageUp.UI.SolicitudesRecibidas" %>

<asp:Content ID="SolicitudesRecibidasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page managed-spaces">
        <div class="static-page-header">
            <span class="section-label">Tu espacio creativo</span>
            <h1>Solicitudes de reserva recibidas</h1>
            <p>Aceptá o rechazá las solicitudes que te envíen sobre tus espacios.</p>
        </div>

        <div class="static-page-body">
            <nav class="gestor-subnav" aria-label="Navegación de gestor de espacios">
                <a href="~/MisEspacios.aspx" runat="server">Administrar espacios</a>
                <a href="~/SolicitudesRecibidas.aspx" runat="server" class="active">Solicitudes recibidas</a>
            </nav>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="auth-card">
                <asp:Literal ID="litSinSolicitudes" runat="server" Visible="false" Text="Todavía no recibiste ninguna solicitud de reserva." />

                <asp:Repeater ID="rptSolicitudes" runat="server" OnItemCommand="rptSolicitudes_ItemCommand" OnItemDataBound="rptSolicitudes_ItemDataBound">
                    <ItemTemplate>
                        <div class="space-row">
                            <div class="space-row-info">
                                <h3><%# Eval("NombreEspacio") %></h3>
                                <p>Solicitada por <%# Eval("NombreSolicitante") %> (<%# Eval("CorreoSolicitante") %>) para el <%# Eval("FechaSolicitada", "{0:dd/MM/yyyy}") %> · Estado: <%# Eval("EstadoReserva") %></p>
                                <p class="space-row-descripcion"><%# Eval("ComentarioSolicitante") %></p>
                            </div>
                            <div class="space-row-actions">
                                <asp:LinkButton ID="lnkAceptar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Aceptar" CommandArgument='<%# Eval("IdReserva") %>' Text="Aceptar" />
                                <asp:LinkButton ID="lnkRechazar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Rechazar" CommandArgument='<%# Eval("IdReserva") %>' Text="Rechazar" />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>
</asp:Content>
