<%@ Page Title="Herramientas de seguridad | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="HerramientasSeguridad.aspx.cs" Inherits="StageUp.UI.Interno.HerramientasSeguridad" %>

<asp:Content ID="HerramientasSeguridadContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Seguridad</span>
            <h1>Herramientas de seguridad</h1>
            <p>Generación del par de claves asimétricas que protegen las exportaciones cifradas de StageUp, y descifrado de archivos exportados desde Registros de actividad.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="auth-card admin-workspace-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Par de claves</span>
                        <h2>Claves asimétricas (RSA)</h2>
                    </div>
                </div>
                <p>Estado actual: <asp:Literal ID="litEstadoClaves" runat="server" /></p>
                <p>Generar un par nuevo reemplaza al anterior: los archivos ya exportados con la clave vieja dejan de poder descifrarse en cuanto reemplaces la clave privada en la configuración.</p>
                <asp:Button ID="btnGenerarClaves" runat="server" CssClass="button button-secondary" Text="Generar nuevo par de claves"
                    CausesValidation="false" OnClick="btnGenerarClaves_Click" />

                <asp:Panel ID="pnlClavesGeneradas" runat="server" Visible="false" CssClass="form-message form-message-success">
                    <p><strong>Copiá estos dos valores a StageUp.UI/AppSettings.private.config y no los pierdas: la clave privada no queda guardada en ningún lado, si salís de esta página sin copiarla no hay forma de recuperarla.</strong></p>
                    <div class="form-field">
                        <label for="<%= txtClavePublicaGenerada.ClientID %>">ClaveAsimetricaPublica</label>
                        <asp:TextBox ID="txtClavePublicaGenerada" runat="server" TextMode="MultiLine" Rows="3" ReadOnly="true" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtClavePrivadaGenerada.ClientID %>">ClaveAsimetricaPrivada</label>
                        <asp:TextBox ID="txtClavePrivadaGenerada" runat="server" TextMode="MultiLine" Rows="3" ReadOnly="true" />
                    </div>
                </asp:Panel>
            </div>

            <div class="auth-card admin-workspace-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Importación</span>
                        <h2>Importar y descifrar una exportación</h2>
                    </div>
                </div>
                <p>Subí un archivo generado desde "Exportar filtro actual a XML cifrado" en <a href="RegistrosActividad.aspx">Registros de actividad</a> para ver su contenido en claro.</p>
                <asp:FileUpload ID="fuArchivoCifrado" runat="server" />
                <asp:Button ID="btnDescifrar" runat="server" CssClass="button button-secondary" Text="Descifrar"
                    CausesValidation="false" OnClick="btnDescifrar_Click" />

                <asp:Panel ID="pnlResultadoDescifrado" runat="server" Visible="false">
                    <div class="form-field">
                        <label for="<%= txtXmlDescifrado.ClientID %>">Contenido descifrado</label>
                        <asp:TextBox ID="txtXmlDescifrado" runat="server" TextMode="MultiLine" Rows="12" ReadOnly="true" />
                    </div>
                </asp:Panel>
            </div>
        </div>
    </section>
</asp:Content>
