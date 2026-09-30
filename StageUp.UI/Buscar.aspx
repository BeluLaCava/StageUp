<%@ Page Title="Buscar | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Buscar.aspx.cs" Inherits="StageUp.UI.Buscar" %>

<asp:Content ID="BuscarContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .busqueda-form { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
        .busqueda-form input[type=text] { flex: 1 1 320px; padding: 0.7rem 0.9rem; border-radius: 0.7rem; border: 1px solid var(--color-border, #e2d4cc); font-size: 1rem; }
        .busqueda-avanzada { margin-top: 12px; }
        .busqueda-avanzada summary { cursor: pointer; font-weight: bold; color: #7a0c20; }
        .busqueda-avanzada table { margin-top: 8px; }
        .busqueda-avanzada td { padding-right: 18px; }
        .busqueda-grupo { margin-top: 26px; }
        .busqueda-grupo h2 { font-size: 1.3rem; color: #7a0c20; margin-bottom: 6px; }
        .busqueda-item { padding: 12px 0; border-bottom: 1px solid var(--color-border, #eee); }
        .busqueda-item a { font-weight: bold; text-decoration: none; color: inherit; }
        .busqueda-item a:hover { color: #7a0c20; }
        .busqueda-tipo { display: inline-block; padding: 2px 10px; margin-right: 8px; border-radius: 999px; background: #f3dfd4; color: #7a0c20; font-size: 0.75em; font-weight: bold; text-transform: uppercase; letter-spacing: 0.4px; }
        .busqueda-detalle { margin: 4px 0 0; color: var(--color-text-muted, #6b7280); font-size: 0.92em; }
    </style>

    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Buscar</span>
            <h1>Buscá en todo StageUp</h1>
            <p>Encontrá espacios, novedades y respuestas del centro de ayuda desde un solo lugar.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="auth-card" DefaultButton="btnBuscar">
                <div class="busqueda-form">
                    <asp:TextBox ID="txtBusqueda" runat="server" MaxLength="100" placeholder="Ej: teatro, reservas, Palermo..." aria-label="Qué querés buscar" />
                    <asp:Button ID="btnBuscar" runat="server" CssClass="button button-primary" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
                </div>
                <details class="busqueda-avanzada" id="busquedaAvanzada" runat="server">
                    <summary>Búsqueda avanzada</summary>
                    <p class="busqueda-detalle">Elegí en qué tipo de contenido buscar. Si no elegís ninguno, se busca en todos.</p>
                    <asp:CheckBoxList ID="cblTipos" runat="server" RepeatDirection="Horizontal" />
                </details>
            </asp:Panel>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlResumen" runat="server" Visible="false">
                <p class="busqueda-detalle"><asp:Literal ID="litResumen" runat="server" /></p>
            </asp:Panel>

            <asp:Repeater ID="rptGrupos" runat="server">
                <ItemTemplate>
                    <div class="busqueda-grupo">
                        <h2><%#: Eval("EtiquetaTipo") %> (<%# ((StageUp.BE.Entidades.GrupoResultadosBusqueda)Container.DataItem).Resultados.Count %>)</h2>
                        <asp:Repeater ID="rptResultados" runat="server"
                            DataSource='<%# ((StageUp.BE.Entidades.GrupoResultadosBusqueda)Container.DataItem).Resultados %>'>
                            <ItemTemplate>
                                <div class="busqueda-item">
                                    <span class="busqueda-tipo"><%#: Eval("EtiquetaTipo") %></span>
                                    <a href='<%#: ResolveUrl((string)Eval("Url")) %>'><%#: Eval("Titulo") %></a>
                                    <p class="busqueda-detalle"><%#: Eval("Detalle") %></p>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </section>
</asp:Content>
