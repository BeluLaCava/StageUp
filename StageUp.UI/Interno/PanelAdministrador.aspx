<%@ Page Title="Panel administrativo | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="PanelAdministrador.aspx.cs" Inherits="StageUp.UI.Interno.PanelAdministrador" %>

<asp:Content ID="PanelAdministradorContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Panel administrativo</span>
            <h1>Bienvenido/a, <asp:Literal ID="litNombreAdministrador" runat="server" /></h1>
            <p>Desde acá podés gestionar las solicitudes de habilitación como gestor y consultar la bitácora del sistema.</p>
        </div>

        <div class="static-page-body">
            <div class="internal-panel-grid">
                <asp:Repeater ID="rptAccesos" runat="server">
                    <ItemTemplate>
                        <a class="internal-panel-card" href="<%# Eval("Url") %>">
                            <h2><%# Eval("Nombre") %></h2>
                            <p><%# Eval("Descripcion") %></p>
                        </a>
                    </ItemTemplate>
                </asp:Repeater>
            </div>

            <asp:Panel ID="pnlSinAccesos" runat="server" CssClass="empty-state" Visible="false">
                <h3>Todavía no tenés secciones habilitadas</h3>
                <p>Tu rol no tiene permisos asignados. Pedile a un administrador que te habilite acceso a alguna sección.</p>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
