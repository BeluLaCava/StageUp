<%@ Page Title="Parámetros de la plataforma | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="ParametrosPlataforma.aspx.cs" Inherits="StageUp.UI.Interno.ParametrosPlataforma" %>

<asp:Content ID="ParametrosPlataformaContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .param-fila { display: grid; grid-template-columns: minmax(0, 1fr) 180px; gap: 16px; align-items: center; padding: 14px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); }
        .param-fila p { margin: 4px 0 0; color: var(--color-text-muted, #765f55); font-size: 0.88em; }
        .param-valor { display: flex; align-items: center; gap: 8px; }
        .param-valor input { width: 100px; text-align: right; }
        .param-meta { font-size: 0.78em; color: var(--color-text-muted, #765f55); }
        @media (max-width: 700px) { .param-fila { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Parámetros de la plataforma</h1>
            <p>Comisión de StageUp, plazo de pago de las reservas y política de cancelación (plazos y cargos). La comisión y el plazo de pago se aplican a las operaciones nuevas; la política de cancelación, a las cancelaciones que se hagan desde ahora.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlParametros" runat="server" CssClass="auth-card admin-editor-card" DefaultButton="btnGuardar">
                <asp:Repeater ID="rptParametros" runat="server">
                    <ItemTemplate>
                        <div class="param-fila">
                            <div>
                                <strong><%#: StageUp.BLL.BLL_ParametroPlataforma.ObtenerNombre(Eval("Clave") as string) %></strong>
                                <p><%#: Eval("Descripcion") %></p>
                                <span class="param-meta">Última modificación: <%#: Eval("FechaUltimaModificacion", "{0:dd/MM/yyyy HH:mm}") %></span>
                            </div>
                            <div class="param-valor">
                                <asp:HiddenField ID="hfClave" runat="server" Value='<%# Eval("Clave") %>' />
                                <asp:TextBox ID="txtValor" runat="server" MaxLength="10" Text='<%# Eval("Valor") %>' inputmode="decimal"
                                    aria-label='<%# StageUp.BLL.BLL_ParametroPlataforma.ObtenerNombre(Eval("Clave") as string) %>' />
                                <span><%#: StageUp.BLL.BLL_ParametroPlataforma.ObtenerUnidad(Eval("Clave") as string) %></span>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
                <div class="form-actions">
                    <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar cambios"
                        CausesValidation="false" OnClick="btnGuardar_Click" />
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
