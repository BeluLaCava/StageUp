<%@ Page Title="Búsqueda en la plataforma | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="BusquedaInterna.aspx.cs" Inherits="StageUp.UI.Interno.BusquedaInterna" %>

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
        .busqueda-filtros { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 16px; margin-top: 14px; }
        .busqueda-filtros .form-field { margin: 0; min-width: 0; }
        .busqueda-filtros input, .busqueda-filtros select { width: 100%; box-sizing: border-box; min-width: 0; }
        .busqueda-filtros input { min-height: 2.75rem; padding: 0.55rem 0.8rem; color: var(--color-text); background: #fffaf7; border: 1px solid rgba(137, 102, 80, 0.32); border-radius: 0.75rem; font: inherit; }
        .busqueda-acciones-avanzadas { display: flex; flex-wrap: wrap; gap: 12px; align-items: center; margin-top: 14px; }
        @media (max-width: 60rem) { .busqueda-filtros { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
        @media (max-width: 40rem) { .busqueda-filtros { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Búsqueda en la plataforma</h1>
            <p>Buscá usuarios, espacios, reservas, tickets y contenidos. Solo se muestran los tipos de información que tu rol tiene permitido ver.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="auth-card" DefaultButton="btnBuscar">
                <div class="busqueda-form">
                    <asp:TextBox ID="txtBusqueda" runat="server" MaxLength="100" placeholder="Nombre, correo, espacio, N° de reserva o ticket..." aria-label="Qué querés buscar" />
                    <asp:Button ID="btnBuscar" runat="server" CssClass="button button-primary" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
                </div>
                <details class="busqueda-avanzada" id="busquedaAvanzada" runat="server">
                    <summary>Búsqueda avanzada</summary>
                    <p class="busqueda-detalle">Elegí en qué tipo de información buscar y sumá filtros. Si no elegís ningún tipo, se busca en todos los que tu rol puede ver. Con algún filtro, el texto es opcional.</p>
                    <asp:CheckBoxList ID="cblTipos" runat="server" RepeatDirection="Horizontal" />

                    <div class="busqueda-filtros">
                        <div class="form-field">
                            <label for="<%= txtDesde.ClientID %>">Desde</label>
                            <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtHasta.ClientID %>">Hasta</label>
                            <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlEstado.ClientID %>">Estado</label>
                            <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select" />
                        </div>
                        <div class="form-field"><label for="<%= ddlPerfil.ClientID %>">Perfil externo</label><asp:DropDownList ID="ddlPerfil" runat="server" CssClass="form-select"><asp:ListItem Value="">Todos</asp:ListItem><asp:ListItem Value="ExternoSolicitante">Solicitante</asp:ListItem><asp:ListItem Value="GestorEspacios">Gestor de espacios</asp:ListItem><asp:ListItem Value="PendienteHabilitacionGestor">Pendiente de habilitación</asp:ListItem></asp:DropDownList></div>
                        <div class="form-field"><label for="<%= txtRol.ClientID %>">Rol interno</label><asp:TextBox ID="txtRol" runat="server" MaxLength="100" placeholder="Nombre del rol" /></div>
                        <div class="form-field"><label for="<%= txtResponsable.ClientID %>">Responsable</label><asp:TextBox ID="txtResponsable" runat="server" MaxLength="150" placeholder="Nombre o correo" /></div>
                        <div class="form-field"><label for="<%= txtNombre.ClientID %>">Nombre o título</label><asp:TextBox ID="txtNombre" runat="server" MaxLength="150" /></div>
                        <div class="form-field"><label for="<%= txtCorreo.ClientID %>">Correo</label><asp:TextBox ID="txtCorreo" runat="server" MaxLength="150" TextMode="Email" /></div>
                        <div class="form-field"><label for="<%= txtOperacion.ClientID %>">Operación realizada</label><asp:TextBox ID="txtOperacion" runat="server" MaxLength="150" placeholder="Alta, modificación, login..." /></div>
                        <div class="form-field"><label for="<%= txtEntidad.ClientID %>">Entidad afectada</label><asp:TextBox ID="txtEntidad" runat="server" MaxLength="150" placeholder="Reserva, usuario, pago..." /></div>
                        <div class="form-field"><label for="<%= txtIdRegistro.ClientID %>">Identificador</label><asp:TextBox ID="txtIdRegistro" runat="server" TextMode="Number" min="1" step="1" /></div>
                        <div class="form-field">
                            <label for="<%= txtUbicacion.ClientID %>">Ubicación (espacios y reservas)</label>
                            <asp:TextBox ID="txtUbicacion" runat="server" MaxLength="100" placeholder="Ciudad, provincia o dirección" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlCategoria.ClientID %>">Categoría de novedad</label>
                            <asp:DropDownList ID="ddlCategoria" runat="server" CssClass="form-select" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlOrden.ClientID %>">Ordenar por</label>
                            <asp:DropDownList ID="ddlOrden" runat="server" CssClass="form-select" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlMaximo.ClientID %>">Resultados por tipo</label>
                            <asp:DropDownList ID="ddlMaximo" runat="server" CssClass="form-select" />
                        </div>
                    </div>
                    <p class="busqueda-detalle">Los filtros se pueden combinar. Perfil y rol se aplican a usuarios; operación y entidad afectada, a los registros de actividad. Al elegir un estado se busca solo en ese tipo.</p>

                    <div class="busqueda-acciones-avanzadas">
                        <asp:Button ID="btnBuscarAvanzado" runat="server" CssClass="button button-primary" Text="Buscar con filtros" CausesValidation="false" OnClick="btnBuscar_Click" />
                        <a class="text-link" href="BusquedaInterna.aspx">Limpiar filtros</a>
                    </div>
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
                                    <asp:HyperLink runat="server" NavigateUrl='<%# Eval("Url") %>' Text='<%#: Eval("Titulo") %>' Visible='<%# Eval("Url") != null %>' />
                                    <asp:Literal runat="server" Mode="Encode" Text='<%# Eval("Titulo") %>' Visible='<%# Eval("Url") == null %>' />
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
