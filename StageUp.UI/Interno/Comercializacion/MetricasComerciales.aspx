<%@ Page Title="Métricas comerciales | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="MetricasComerciales.aspx.cs" Inherits="StageUp.UI.Interno.Comercializacion.MetricasComerciales" %>
<%@ Register Src="~/Interno/Comercializacion/ComercializacionTabs.ascx" TagPrefix="su" TagName="Solapas" %>

<asp:Content ID="MetricasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comercialización</span>
            <h1>Métricas comerciales y origen de usuarios</h1>
            <p>Indicadores de crecimiento y captación de StageUp, y por qué canales llegan las personas. Es solo de consulta: no modifica ningún dato.</p>
        </div>

        <su:Solapas ID="solapas" runat="server" />

        <div class="static-page-body internal-admin-stack comercial-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- Pasos 7, 9 y 10, A3, A4 --%>
            <div class="auth-card admin-list-card">
                <asp:Panel ID="pnlFiltros" runat="server" CssClass="comercial-filtros" DefaultButton="btnAplicarFiltros" role="search">
                    <div class="form-field">
                        <label for="<%= txtDesde.ClientID %>">Desde</label>
                        <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtHasta.ClientID %>">Hasta</label>
                        <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlCampania.ClientID %>">Campaña comercial</label>
                        <asp:DropDownList ID="ddlCampania" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlCanal.ClientID %>">Canal de origen</label>
                        <asp:DropDownList ID="ddlCanal" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlTipoUsuario.ClientID %>">Tipo de usuario</label>
                        <asp:DropDownList ID="ddlTipoUsuario" runat="server" />
                    </div>
                    <div class="comercial-filtros-acciones">
                        <asp:Button ID="btnAplicarFiltros" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnAplicarFiltros_Click" />
                        <a class="text-link" href="MetricasComerciales.aspx">Limpiar filtros</a>
                        <span class="comercial-ayuda"><asp:Literal ID="litCriterios" runat="server" Mode="Encode" /></span>
                    </div>
                </asp:Panel>
            </div>

            <%-- A2 --%>
            <asp:Panel ID="pnlSinInformacion" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                <h3>Todavía no hay datos comerciales suficientes para consultar</h3>
                <p>Las métricas aparecen cuando hay usuarios registrados, campañas o contactos comerciales cargados.</p>
            </asp:Panel>

            <%-- A5 --%>
            <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                <h3>No se encontraron resultados para los criterios seleccionados</h3>
                <p>Modificá los filtros o limpialos para volver al tablero general.</p>
            </asp:Panel>

            <%-- Pasos 5 y 6 --%>
            <asp:Panel ID="pnlTablero" runat="server" Visible="false">
                <div class="metricas-tiles" aria-label="Indicadores comerciales">
                    <div class="metricas-tile">
                        <span>Nuevos usuarios registrados</span>
                        <strong><asp:Literal ID="litNuevosUsuarios" runat="server" /></strong>
                        <small><asp:Literal ID="litConOrigen" runat="server" Mode="Encode" /></small>
                        <small class="metricas-filtros"><asp:Literal ID="litFiltrosUsuarios" runat="server" Mode="Encode" /></small>
                    </div>
                    <div class="metricas-tile">
                        <span>Espacios incorporados</span>
                        <strong><asp:Literal ID="litEspacios" runat="server" /></strong>
                        <small>Espacios dados de alta por los gestores</small>
                        <small class="metricas-filtros"><asp:Literal ID="litFiltrosEspacios" runat="server" Mode="Encode" /></small>
                    </div>
                    <div class="metricas-tile">
                        <span><asp:Literal ID="litCampaniasTitulo" runat="server" Mode="Encode" Text="Campañas comerciales activas" /></span>
                        <strong><asp:Literal ID="litCampanias" runat="server" /></strong>
                        <small><asp:Literal ID="litCampaniasDetalle" runat="server" Mode="Encode" /></small>
                        <small class="metricas-filtros"><asp:Literal ID="litFiltrosCampanias" runat="server" Mode="Encode" /></small>
                    </div>
                    <div class="metricas-tile">
                        <span>Contactos convertidos en usuarios</span>
                        <strong><asp:Literal ID="litConvertidos" runat="server" /></strong>
                        <small><asp:Literal ID="litConvertidosDetalle" runat="server" Mode="Encode" /></small>
                        <small class="metricas-filtros"><asp:Literal ID="litFiltrosConvertidos" runat="server" Mode="Encode" /></small>
                    </div>
                </div>

                <%-- Pasos 12 a 16, A7, A8, A9, A10 --%>
                <div class="auth-card admin-list-card" id="origen">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Origen de usuarios</span>
                        <h2>¿Por dónde llegaron los usuarios?</h2>
                        <p class="comercial-ayuda">Usuarios registrados según su canal de origen. Los que no informaron cómo llegaron se agrupan en "Origen no informado".</p>
                    </div>

                    <%-- A7 --%>
                    <asp:Panel ID="pnlSinOrigen" runat="server" Visible="false" CssClass="comercial-aviso">
                        Todavía no hay información de origen de usuarios disponible: ninguno de los usuarios de este período llegó por un canal identificado.
                    </asp:Panel>

                    <asp:Repeater ID="rptCanales" runat="server">
                        <HeaderTemplate><ul class="metricas-barras" aria-label="Usuarios por canal de origen"></HeaderTemplate>
                        <ItemTemplate>
                            <li title='<%#: Eval("NombreCanal") + ": " + Eval("CantidadUsuarios") + " usuarios (" + Eval("Porcentaje", "{0:0.#}") + " %)" %>'>
                                <span class="metricas-barra-nombre"><%#: Eval("NombreCanal") %></span>
                                <span class="metricas-barra-pista"><span class='<%# Convert.ToString(Eval("CanalOrigen")) == "NoInformado" ? "metricas-barra metricas-barra-noinformado" : "metricas-barra" %>' style='<%# "width:" + AnchoBarra(Eval("CantidadUsuarios")) %>'></span></span>
                                <span class="metricas-barra-valor"><strong><%#: Eval("CantidadUsuarios") %></strong> <small><%#: Eval("Porcentaje", "{0:0.#}") %> %</small></span>
                            </li>
                        </ItemTemplate>
                        <FooterTemplate></ul></FooterTemplate>
                    </asp:Repeater>

                    <h3>Detalle por origen</h3>
                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptOrigenes" runat="server">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Canal</th><th>Origen</th><th>Vinculado a</th><th>Usuarios</th><th>De ellos, gestores</th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Canal"><%#: Eval("NombreCanal") %></td>
                                    <td data-label="Origen"><%#: Eval("Origen") %></td>
                                    <td data-label="Vinculado a"><%#: Eval("Vinculo") %></td>
                                    <td data-label="Usuarios" class="comercial-num"><%#: Eval("Usuarios") %></td>
                                    <td data-label="De ellos, gestores" class="comercial-num"><%#: Eval("Gestores") %></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
