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
                <a class="internal-panel-card" href="~/Interno/AprobacionGestores.aspx" runat="server">
                    <h2>Aprobación de gestores</h2>
                    <p>Revisá y aprobá o rechazá las solicitudes de usuarios que piden habilitarse como gestores de espacios.</p>
                </a>
                <a class="internal-panel-card" href="~/Interno/RegistrosActividad.aspx" runat="server">
                    <h2>Registros de actividad</h2>
                    <p>Consultá la bitácora completa del sistema, con filtros por usuario, fecha y tipo de operación.</p>
                </a>
            </div>
        </div>
    </section>
</asp:Content>
