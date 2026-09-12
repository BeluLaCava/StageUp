<%@ Page Title="Organizar permisos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionGruposPermisos.aspx.cs" Inherits="StageUp.UI.Interno.GestionGruposPermisos" %>

<asp:Content ID="GestionGruposPermisosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Organizar permisos</h1>
            <p>Creá grupos, moveles permisos u otros grupos adentro, reorganizalos, y borrá los que ya no uses. Esto define la estructura que después ves en "Permisos" de cada rol.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="admin-two-column-layout">
                <div class="auth-card admin-list-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Estructura actual</span>
                        <h2>Árbol de permisos</h2>
                        <p>Hacé clic en un elemento para moverlo o borrarlo.</p>
                    </div>

                    <div class="groups-tree-wrapper">
                        <asp:TreeView ID="tvGrupos" runat="server" ShowLines="true" CssClass="permission-tree" OnSelectedNodeChanged="tvGrupos_SelectedNodeChanged">
                            <NodeStyle Font-Size="0.9rem" ForeColor="#3E2925" NodeSpacing="4px" VerticalPadding="4px" />
                            <ParentNodeStyle Font-Bold="true" ForeColor="#4D0B17" />
                            <SelectedNodeStyle BackColor="#FCF7F3" Font-Bold="true" ForeColor="#6D1021" />
                            <HoverNodeStyle ForeColor="#6D1021" />
                        </asp:TreeView>
                    </div>
                </div>

                <div>
                    <asp:Panel ID="pnlCrearGrupo" runat="server" CssClass="auth-card admin-editor-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Nuevo</span>
                            <h2>Crear grupo</h2>
                        </div>

                        <div class="form-field">
                            <label for="<%= txtNombreGrupo.ClientID %>">Nombre del grupo *</label>
                            <asp:TextBox ID="txtNombreGrupo" runat="server" TextMode="SingleLine" MaxLength="200" placeholder="Ej: Reportes" />
                            <asp:RequiredFieldValidator ID="rfvNombreGrupo" runat="server" ControlToValidate="txtNombreGrupo"
                                Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre del grupo." ValidationGroup="CrearGrupo" />
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlGrupoPadreNuevo.ClientID %>">Dentro de</label>
                            <asp:DropDownList ID="ddlGrupoPadreNuevo" runat="server" />
                        </div>

                        <div class="form-actions">
                            <asp:Button ID="btnCrearGrupo" runat="server" CssClass="button button-primary" Text="Crear grupo"
                                ValidationGroup="CrearGrupo" OnClick="btnCrearGrupo_Click" />
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlSeleccion" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Seleccionado</span>
                            <h2><asp:Literal ID="litNombreSeleccionado" runat="server" /></h2>
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlNuevoPadre.ClientID %>">Mover a</label>
                            <asp:DropDownList ID="ddlNuevoPadre" runat="server" />
                        </div>

                        <div class="form-actions">
                            <asp:Button ID="btnMover" runat="server" CssClass="button button-secondary" Text="Mover"
                                CausesValidation="false" OnClick="btnMover_Click" />
                            <asp:Button ID="btnEliminarGrupo" runat="server" CssClass="button button-secondary" Text="Eliminar grupo"
                                CausesValidation="false" OnClick="btnEliminarGrupo_Click"
                                OnClientClick="return confirm('¿Seguro que querés eliminar este grupo? Solo se puede si está vacío.');" />
                        </div>
                    </asp:Panel>

                    <p class="form-actions">
                        <a class="text-link" href="~/Interno/GestionRoles.aspx" runat="server">Volver a roles</a>
                    </p>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
