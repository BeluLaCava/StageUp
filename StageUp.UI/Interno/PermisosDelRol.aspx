<%@ Page Title="Permisos del rol | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="PermisosDelRol.aspx.cs" Inherits="StageUp.UI.Interno.PermisosDelRol" %>

<%-- CU-001-012: los permisos del rol se editan junto con el rol, en
     Interno/GestionRoles.aspx (A11 y A12). Esta dirección se conserva para
     que los enlaces viejos sigan funcionando y redirige a esa pantalla. --%>
<asp:Content ID="PermisosDelRolContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-body internal-admin-stack">
            <p>Los permisos de cada rol ahora se editan desde <a href="~/Interno/GestionRoles.aspx" runat="server">Roles y permisos</a>.</p>
        </div>
    </section>
</asp:Content>
