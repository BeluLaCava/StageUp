<%@ Page Title="Herramientas de seguridad | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="HerramientasSeguridad.aspx.cs" Inherits="StageUp.UI.Interno.HerramientasSeguridad" %>

<asp:Content ID="HerramientasSeguridadContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .internal-admin-stack > * { min-width: 0; }
        .seg-estado { display: grid; grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr)); gap: 12px; margin: 10px 0 4px; }
        .seg-estado > div { border: 1px solid var(--color-border); border-radius: 12px; padding: 10px 14px; background: var(--color-surface); min-width: 0; }
        .seg-estado dt { color: var(--color-text-muted); font-size: 12px; }
        .seg-estado dd { margin: 3px 0 0; font-weight: 700; overflow-wrap: anywhere; }
        .seg-huella { font-family: Consolas, "Courier New", monospace; font-size: 0.85em; letter-spacing: 0.02em; }
        .seg-nota { color: var(--color-text-muted); font-size: 0.88em; }
        .seg-rotar { display: grid; gap: 10px; margin-top: 10px; }
        .seg-rotar textarea { width: 100%; box-sizing: border-box; padding: 0.6rem 0.75rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); font: inherit; }
        .seg-rotar .seg-confirmar { display: flex; gap: 8px; align-items: flex-start; }
        .seg-rotar .seg-confirmar input { width: auto; margin-top: 4px; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .clave-estado { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 0.78rem; font-weight: 700; white-space: nowrap; }
        .clave-estado-Activa { background: #e6f5eb; color: #236240; }
        .clave-estado-Retirada { background: #eef0fb; color: #33407a; }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Seguridad</span>
            <h1>Herramientas de seguridad</h1>
            <p>Claves de cifrado que protegen la billetera de tarjetas y las exportaciones de la bitácora (RSA de 2048 bits que envuelve una clave AES-256 por dato), rotación auditada y descifrado de exportaciones.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="auth-card admin-workspace-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Claves de cifrado</span>
                        <h2>Par de claves activo</h2>
                    </div>
                </div>
                <dl class="seg-estado">
                    <div><dt>Versión activa</dt><dd><asp:Literal ID="litVersionActiva" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Huella de la clave pública</dt><dd class="seg-huella"><asp:Literal ID="litHuellaActiva" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Generada</dt><dd><asp:Literal ID="litFechaActiva" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Tarjetas protegidas</dt><dd><asp:Literal ID="litTarjetasActiva" runat="server" Mode="Encode" /></dd></div>
                </dl>
                <p class="seg-nota">Las claves privadas no se muestran ni se descargan: se guardan cifradas con la clave maestra de la configuración privada y solo se usan en memoria para descifrar. Para identificar una versión se muestra la huella (SHA-256) de su clave pública.</p>
                <asp:Panel ID="pnlSinClaveMaestra" runat="server" Visible="false" CssClass="form-message form-message-error">
                    Falta la clave simétrica <strong>ClaveEncriptacionSimetrica</strong> en <code>AppSettings.private.config</code>: sin ella no se pueden generar ni usar las claves.
                </asp:Panel>
                <asp:Panel ID="pnlPendientes" runat="server" Visible="false" CssClass="form-message form-message-error">
                    <asp:Literal ID="litPendientes" runat="server" Mode="Encode" />
                    <asp:Button ID="btnReintentar" runat="server" CssClass="button button-secondary button-small" Text="Reintentar"
                        CausesValidation="false" OnClick="btnReintentar_Click" />
                </asp:Panel>

                <%-- Ítem 10: rotación auditada (responsable, motivo y fecha en la bitácora y en el historial). --%>
                <asp:Panel ID="pnlRotar" runat="server" CssClass="seg-rotar" DefaultButton="btnRotar">
                    <h3>Rotar el par de claves</h3>
                    <p class="seg-nota">Genera una versión nueva, que pasa a cifrar todo lo nuevo, y retira la actual. Las tarjetas guardadas se vuelven a proteger con la clave nueva sin descifrar sus datos; las exportaciones viejas se siguen pudiendo descifrar con su versión.</p>
                    <div class="form-field">
                        <label for="<%= txtMotivo.ClientID %>">Motivo (queda en la bitácora)</label>
                        <asp:TextBox ID="txtMotivo" runat="server" TextMode="MultiLine" Rows="2" MaxLength="300" />
                    </div>
                    <div class="seg-confirmar">
                        <asp:CheckBox ID="chkConfirmar" runat="server" />
                        <label for="<%= chkConfirmar.ClientID %>">Entiendo que se genera un par de claves nuevo y se retira el actual.</label>
                    </div>
                    <div>
                        <asp:Button ID="btnRotar" runat="server" CssClass="button button-primary" Text="Rotar claves"
                            CausesValidation="false" OnClick="btnRotar_Click"
                            OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Rotar las claves de cifrado? Se generará una nueva versión y el cambio quedará auditado.', { title: 'Rotar claves', confirmText: 'Sí, rotar claves', cancelText: 'Volver' })) return false;" />
                    </div>
                </asp:Panel>
            </div>

            <div class="auth-card admin-workspace-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Trazabilidad</span>
                        <h2>Historial de versiones</h2>
                    </div>
                </div>
                <asp:Panel ID="pnlSinVersiones" runat="server" CssClass="seg-nota">
                    Todavía no se generó ninguna versión. Se crea sola la primera vez que se guarda una tarjeta o se exporta la bitácora, o con "Rotar claves".
                </asp:Panel>
                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptVersiones" runat="server">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Versión</th><th>Huella</th><th>Estado</th><th>Generada</th><th>Retirada</th><th>Tarjetas</th><th>Motivo</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><strong><%# Eval("Version") %></strong></td>
                                <td class="seg-huella"><%# Server.HtmlEncode(Convert.ToString(Eval("Huella"))) %></td>
                                <td><span class="clave-estado clave-estado-<%# Eval("Estado") %>"><%# Server.HtmlEncode(Convert.ToString(Eval("Estado"))) %></span></td>
                                <td><%# Convert.ToDateTime(Eval("FechaAlta")).ToString("dd/MM/yyyy HH:mm") %><br /><small><%# Server.HtmlEncode(Responsable(Eval("NombreUsuarioAlta"))) %></small></td>
                                <td><%# Eval("FechaRetiro") == null ? "—" : Convert.ToDateTime(Eval("FechaRetiro")).ToString("dd/MM/yyyy HH:mm") %><%# Eval("FechaRetiro") == null ? "" : "<br /><small>" + Server.HtmlEncode(Responsable(Eval("NombreUsuarioRetiro"))) + "</small>" %></td>
                                <td><%# Eval("TarjetasProtegidas") %></td>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("Motivo"))) %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>

            <div class="auth-card admin-workspace-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Importación</span>
                        <h2>Importar y descifrar una exportación</h2>
                    </div>
                </div>
                <p>Subí un archivo generado desde "Exportar filtro actual a XML cifrado" en <a href="RegistrosActividad.aspx">Registros de actividad</a> para ver su contenido en claro. Se descifra con la versión de clave indicada en el archivo.</p>
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
