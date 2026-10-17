<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ComercializacionTabs.ascx.cs" Inherits="StageUp.UI.Interno.Comercializacion.ComercializacionTabs" %>
<%-- Solapas del módulo Comercialización: solo las secciones que el rol puede usar. --%>
<nav class="comercial-tabs" aria-label="Secciones de Comercialización">
    <asp:Repeater ID="rptSolapas" runat="server">
        <ItemTemplate>
            <a href='<%#: ResolveUrl((string)Eval("Url")) %>' class='<%# (bool)Eval("Actual") ? "comercial-tab comercial-tab-actual" : "comercial-tab" %>'
               <%# (bool)Eval("Actual") ? "aria-current=\"page\"" : string.Empty %>><%#: Eval("Texto") %></a>
        </ItemTemplate>
    </asp:Repeater>
</nav>
