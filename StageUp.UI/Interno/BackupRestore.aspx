<%@ Page Title="Backup y restauración | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="BackupRestore.aspx.cs" Inherits="StageUp.UI.Interno.BackupRestore" %>

<asp:Content ID="BackupRestoreContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .backup-row { display: flex; justify-content: space-between; align-items: center; gap: 12px; padding: 12px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); flex-wrap: wrap; }
        .backup-meta { color: var(--color-text-muted, #6b7280); font-size: 0.85em; }
        .backup-alerta { padding: 14px 16px; border-radius: 12px; background: #fff4e5; border-left: 4px solid #b45309; color: #78350f; margin-bottom: 14px; }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Backup y restauración</h1>
            <p>Generá copias de seguridad de la base de datos de StageUp y, si hace falta, restaurala desde una de ellas.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <asp:Panel ID="pnlRestaurado" runat="server" Visible="false" CssClass="auth-card">
            <h2>Base restaurada</h2>
            <p>La base de datos se restauró correctamente. Por seguridad se cerró tu sesión: volvé a iniciar sesión para seguir trabajando.</p>
            <a class="button button-primary" href="../IniciarSesion.aspx">Iniciar sesión</a>
        </asp:Panel>

        <asp:Panel ID="pnlContenido" runat="server" CssClass="static-page-body internal-admin-stack">
            <div class="auth-card admin-editor-card">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Backup</span>
                    <h2>Generar backup</h2>
                    <p>Se guarda en la carpeta de backups de la instancia de SQL Server.</p>
                </div>
                <div class="form-field">
                    <label for="<%= txtDescripcion.ClientID %>">Descripción (opcional)</label>
                    <asp:TextBox ID="txtDescripcion" runat="server" MaxLength="200" placeholder="Ej: Antes de la demo de la segunda entrega" />
                </div>
                <div class="form-actions">
                    <asp:Button ID="btnGenerar" runat="server" CssClass="button button-primary" Text="Generar backup ahora"
                        CausesValidation="false" OnClick="btnGenerar_Click" />
                </div>
            </div>

            <asp:Panel ID="pnlConfirmarRestauracion" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Restauración</span>
                    <h2>Confirmar restauración</h2>
                </div>
                <div class="backup-alerta">
                    Vas a reemplazar <strong>toda</strong> la base de datos por el backup
                    <strong><asp:Literal ID="litBackupElegido" runat="server" /></strong>.
                    Todo lo que se haya cargado después de ese backup se pierde de la base activa
                    (antes de restaurar se guarda automáticamente un backup del estado actual).
                    Los usuarios conectados pierden su sesión.
                </div>
                <div class="form-field">
                    <label for="<%= txtConfirmacion.ClientID %>">Para confirmar, escribí RESTAURAR</label>
                    <asp:TextBox ID="txtConfirmacion" runat="server" MaxLength="20" autocomplete="off" />
                </div>
                <div class="form-actions">
                    <asp:Button ID="btnConfirmarRestauracion" runat="server" CssClass="button button-primary" Text="Restaurar la base"
                        CausesValidation="false" OnClick="btnConfirmarRestauracion_Click" />
                    <asp:LinkButton ID="lnkCancelarRestauracion" runat="server" CssClass="text-link" Text="Cancelar"
                        CausesValidation="false" OnClick="lnkCancelarRestauracion_Click" />
                </div>
            </asp:Panel>

            <div class="auth-card admin-list-card">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Historial</span>
                    <h2>Backups disponibles</h2>
                    <p>Últimos backups completos de esta base registrados en SQL Server.</p>
                </div>

                <asp:Panel ID="pnlSinBackups" runat="server" CssClass="admin-empty-hint" Visible="false">
                    Todavía no hay backups registrados. Generá el primero desde arriba.
                </asp:Panel>

                <asp:Repeater ID="rptBackups" runat="server" OnItemCommand="rptBackups_ItemCommand">
                    <ItemTemplate>
                        <div class="backup-row">
                            <div>
                                <strong><%#: Eval("NombreArchivo") %></strong>
                                <div class="backup-meta">
                                    <%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm:ss}") %> · <%#: FormatearTamanio(Eval("TamanioBytes")) %>
                                    <%#: string.IsNullOrEmpty(Eval("Descripcion") as string) ? "" : " · " + Eval("Descripcion") %>
                                </div>
                            </div>
                            <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                CommandName="Restaurar" CommandArgument='<%# Eval("IdBackup") %>' Text="Restaurar desde este backup" />
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </asp:Panel>
    </section>
</asp:Content>
