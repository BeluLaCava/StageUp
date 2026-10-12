<%@ Page Title="Parámetros administrativos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="ParametrosPlataforma.aspx.cs" Inherits="StageUp.UI.Interno.ParametrosPlataforma" %>

<asp:Content ID="ParametrosPlataformaContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .internal-admin-stack > * { min-width: 0; }
        .param-acciones { display: flex; justify-content: space-between; align-items: center; gap: 12px; flex-wrap: wrap; }
        .param-acciones p { margin: 0; color: var(--color-text-muted, #765f55); font-size: 0.9em; max-width: 46rem; }
        .param-grupo h2 { margin-bottom: 4px; }
        .param-grupo > p { margin: 0 0 6px; color: var(--color-text-muted, #765f55); font-size: 0.9em; }
        .param-fila { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 22rem); gap: 16px; align-items: center; padding: 12px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); }
        .param-fila:last-child { border-bottom: 0; }
        .param-fila p { margin: 3px 0 0; color: var(--color-text-muted, #765f55); font-size: 0.86em; }
        .param-subgrupo { display: inline-block; margin-right: 6px; padding: 1px 8px; border-radius: 999px; background: #f3ebe7; color: #6d1021; font-size: 0.72em; font-weight: 700; vertical-align: middle; }
        .param-valor { display: flex; align-items: center; gap: 8px; min-width: 0; }
        .param-valor input, .param-valor select { width: 100%; box-sizing: border-box; padding: 0.55rem 0.7rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .param-valor input.param-numero { width: 7rem; text-align: right; }
        .param-valor-texto { font-weight: 700; overflow-wrap: anywhere; }
        .param-programado { display: block; margin-top: 4px; color: #8a4b0f; font-size: 0.8em; font-weight: 600; }
        .param-vigencia { display: grid; gap: 10px; }
        .param-vigencia ul { list-style: none; margin: 0; padding: 0; display: flex; gap: 18px; flex-wrap: wrap; }
        .param-vigencia li { display: flex; align-items: center; gap: 6px; }
        .param-vigencia input[type=date] { max-width: 12rem; padding: 0.55rem 0.7rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); font: inherit; }
        .param-historial-filtro { display: flex; gap: 10px; align-items: end; flex-wrap: wrap; margin-bottom: 8px; }
        .param-historial-filtro select { padding: 0.55rem 0.7rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); font: inherit; max-width: 100%; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; overflow-wrap: anywhere; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .cambio-estado { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: 0.78rem; font-weight: 700; white-space: nowrap; background: #eee; }
        .cambio-estado-Aplicado { background: #e6f5eb; color: #236240; }
        .cambio-estado-Programado { background: #fff4e5; color: #8a4b0f; }
        .cambio-estado-Reemplazado { background: #eef0fb; color: #33407a; }
        @media (max-width: 700px) { .param-fila { grid-template-columns: 1fr; gap: 8px; } }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Configuración de parámetros administrativos</h1>
            <p>Comisión de StageUp, política de cancelación, estados administrativos de pagos y facturas, datos de facturación de Artera y correos administrativos. Los cambios se aplican a las operaciones nuevas desde su vigencia; las operaciones anteriores conservan la configuración con la que se generaron.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="auth-card param-acciones">
                <p><asp:Literal ID="litModo" runat="server" Mode="Encode" /></p>
                <asp:Button ID="btnEditar" runat="server" CssClass="button button-primary" Text="Editar parámetros" CausesValidation="false" OnClick="btnEditar_Click" />
            </div>

            <%-- Paso 4: grupos de parámetros configurables. --%>
            <asp:Repeater ID="rptGrupos" runat="server" OnItemDataBound="rptGrupos_ItemDataBound">
                <ItemTemplate>
                    <div class="auth-card param-grupo">
                        <span class="admin-card-eyebrow">Parámetros</span>
                        <h2><%# Server.HtmlEncode(Convert.ToString(Eval("Titulo"))) %></h2>
                        <p><%# Server.HtmlEncode(Convert.ToString(Eval("Descripcion"))) %></p>
                        <asp:Repeater ID="rptParametros" runat="server" OnItemDataBound="rptParametros_ItemDataBound">
                            <ItemTemplate>
                                <div class="param-fila">
                                    <div>
                                        <strong><asp:Literal ID="litSubgrupo" runat="server" /><%# Server.HtmlEncode(StageUp.BLL.BLL_ParametroPlataforma.ObtenerNombre(Convert.ToString(Eval("Clave")))) %></strong>
                                        <p><%# Server.HtmlEncode(Convert.ToString(Eval("Descripcion"))) %></p>
                                        <asp:Literal ID="litProgramado" runat="server" />
                                    </div>
                                    <div class="param-valor">
                                        <asp:HiddenField ID="hfClave" runat="server" Value='<%# Eval("Clave") %>' />
                                        <asp:Literal ID="litValor" runat="server" />
                                        <asp:TextBox ID="txtValor" runat="server" Visible="false" />
                                        <asp:DropDownList ID="ddlSiNo" runat="server" Visible="false">
                                            <asp:ListItem Text="Sí" Value="Si" />
                                            <asp:ListItem Text="No" Value="No" />
                                        </asp:DropDownList>
                                        <asp:Literal ID="litUnidad" runat="server" Mode="Encode" />
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </ItemTemplate>
            </asp:Repeater>

            <%-- Pasos 9 y 10, A8. --%>
            <asp:Panel ID="pnlGuardar" runat="server" Visible="false" CssClass="auth-card param-vigencia" DefaultButton="btnGuardar">
                <h2>Vigencia de los cambios</h2>
                <asp:RadioButtonList ID="rblVigencia" runat="server" RepeatLayout="UnorderedList">
                    <asp:ListItem Text="Desde ahora" Value="Ahora" Selected="True" />
                    <asp:ListItem Text="Desde una fecha" Value="Fecha" />
                </asp:RadioButtonList>
                <div class="form-field">
                    <label for="<%= txtVigencia.ClientID %>">Fecha desde la que rigen (si elegiste una fecha)</label>
                    <asp:TextBox ID="txtVigencia" runat="server" TextMode="Date" />
                </div>
                <div class="form-actions">
                    <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar cambios" CausesValidation="false" OnClick="btnGuardar_Click" />
                    <asp:Button ID="btnCancelar" runat="server" CssClass="button button-secondary" Text="Cancelar" CausesValidation="false" OnClick="btnCancelar_Click" />
                </div>
            </asp:Panel>

            <%-- Paso 13: historial de cambios. --%>
            <div class="auth-card">
                <span class="admin-card-eyebrow">Trazabilidad</span>
                <h2>Historial de cambios</h2>
                <div class="param-historial-filtro">
                    <div class="form-field">
                        <label for="<%= ddlHistorialParametro.ClientID %>">Parámetro</label>
                        <asp:DropDownList ID="ddlHistorialParametro" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlHistorialParametro_SelectedIndexChanged" />
                    </div>
                </div>
                <asp:Panel ID="pnlSinCambios" runat="server" CssClass="empty-state compact-empty-state">
                    <p>Todavía no se registraron cambios.</p>
                </asp:Panel>
                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptCambios" runat="server">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Fecha</th><th>Parámetro</th><th>Valor anterior</th><th>Valor nuevo</th><th>Vigencia</th><th>Estado</th><th>Responsable</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><%# Convert.ToDateTime(Eval("FechaRegistro")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td><%# Server.HtmlEncode(StageUp.BLL.BLL_ParametroPlataforma.ObtenerNombre(Convert.ToString(Eval("Clave")))) %></td>
                                <td><%# Server.HtmlEncode(StageUp.BLL.BLL_ParametroPlataforma.FormatearValor(Convert.ToString(Eval("Clave")), Convert.ToString(Eval("ValorAnterior")))) %></td>
                                <td><%# Server.HtmlEncode(StageUp.BLL.BLL_ParametroPlataforma.FormatearValor(Convert.ToString(Eval("Clave")), Convert.ToString(Eval("ValorNuevo")))) %></td>
                                <td><%# Convert.ToDateTime(Eval("FechaVigencia")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td><span class="cambio-estado cambio-estado-<%# Eval("Estado") %>"><%# Server.HtmlEncode(DescribirEstadoCambio(Convert.ToString(Eval("Estado")))) %></span></td>
                                <td><%# Server.HtmlEncode(string.IsNullOrWhiteSpace(Convert.ToString(Eval("NombreResponsable"))) ? "—" : Convert.ToString(Eval("NombreResponsable"))) %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
