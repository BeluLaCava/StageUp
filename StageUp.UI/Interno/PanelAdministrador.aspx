<%@ Page Title="Panel administrativo | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="PanelAdministrador.aspx.cs" Inherits="StageUp.UI.Interno.PanelAdministrador" %>

<asp:Content ID="PanelAdministradorContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-dashboard">
        <div class="static-page-header internal-page-hero internal-dashboard-hero">
            <span class="section-label">Panel administrativo</span>
            <h1>Bienvenido/a, <asp:Literal ID="litNombreAdministrador" runat="server" /></h1>
            <p>Desde acá podés gestionar las solicitudes de habilitación como gestor y consultar la bitácora del sistema.</p>
        </div>

        <div class="static-page-body internal-dashboard-body">
            <div class="internal-section-heading">
                <div>
                    <span class="internal-section-kicker" data-i18n="AdminUI_HerramientasDisponibles">Herramientas disponibles</span>
                    <h2 data-i18n="AdminUI_QueGestionar">¿Qué querés gestionar hoy?</h2>
                </div>
                <span class="internal-section-hint" data-i18n="AdminUI_AccesosSegunRol">Los accesos dependen de los permisos de tu rol.</span>
            </div>
            <div class="internal-panel-grid">
                <asp:Repeater ID="rptAccesos" runat="server">
                    <ItemTemplate>
                        <a class="internal-panel-card" href='<%# ResolveUrl(Eval("Url").ToString()) %>'>
                            <span class="internal-panel-card-icon" aria-hidden="true"></span>
                            <span class="internal-panel-card-content">
                                <h2><%# Eval("Nombre") %></h2>
                                <p><%# Eval("Descripcion") %></p>
                            </span>
                            <span class="internal-panel-card-arrow" aria-hidden="true">→</span>
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
