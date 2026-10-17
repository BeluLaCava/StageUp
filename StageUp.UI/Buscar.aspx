<%@ Page Title="Buscar | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Buscar.aspx.cs" Inherits="StageUp.UI.Buscar" %>

<asp:Content ID="BuscarContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .busqueda-page-body { display: grid; gap: 1.25rem; }
        .busqueda-card { padding: clamp(1.1rem, 2vw, 1.45rem); border-radius: 1rem; }
        .busqueda-form { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 0.75rem; align-items: center; }
        .busqueda-form input[type=text] {
            width: 100%;
            min-height: 3.15rem;
            padding: 0.85rem 1rem;
            color: var(--color-text);
            background: #fffaf7;
            border: 1px solid rgba(137, 102, 80, 0.32);
            border-radius: 0.85rem;
            font-size: 1rem;
            outline: none;
        }
        .busqueda-form input[type=text]:focus { border-color: var(--color-primary); box-shadow: 0 0 0 4px rgba(109, 16, 33, 0.1); }
        .busqueda-form .button { min-height: 3.15rem; padding-inline: 1.35rem; }
        .busqueda-avanzada { margin-top: 0.85rem; }
        .busqueda-avanzada summary {
            display: inline-flex;
            align-items: center;
            gap: 0.45rem;
            cursor: pointer;
            color: var(--color-primary);
            font-weight: 800;
        }
        .busqueda-avanzada summary::before {
            content: "";
            width: 0.85rem;
            height: 0.85rem;
            border: 2px solid currentColor;
            border-top: 0;
            border-left: 0;
            transform: rotate(45deg) translateY(-0.18rem);
        }
        .busqueda-avanzada[open] summary::before { transform: rotate(225deg) translate(-0.08rem, -0.1rem); }
        .busqueda-tipos { display: flex; flex-wrap: wrap; gap: 0.55rem; margin-top: 0.75rem; }
        .busqueda-tipos label {
            display: inline-flex;
            align-items: center;
            gap: 0.4rem;
            min-height: 2.4rem;
            padding: 0.45rem 0.75rem;
            background: rgba(243, 223, 212, 0.56);
            border: 1px solid rgba(137, 102, 80, 0.2);
            border-radius: 999px;
            color: var(--color-primary-dark);
            font-weight: 750;
        }
        .busqueda-resumen { font-size: 0.95rem; }
        .busqueda-grupo { margin-top: 0.35rem; }
        .busqueda-grupo h2 { margin: 0 0 0.8rem; color: var(--color-primary); font-size: 1.35rem; }
        .busqueda-item {
            display: grid;
            gap: 0.35rem;
            padding: 1rem;
            background: rgba(255, 252, 250, 0.82);
            border: 1px solid var(--color-border, #eee);
            border-radius: 1rem;
            box-shadow: 0 0.45rem 1.4rem rgba(77, 11, 23, 0.035);
        }
        .busqueda-item + .busqueda-item { margin-top: 0.75rem; }
        .busqueda-item a { font-size: 1.02rem; font-weight: 850; text-decoration: none; color: var(--color-primary-dark); }
        .busqueda-item a:hover { color: var(--color-primary); text-decoration: underline; text-underline-offset: 0.2rem; }
        .busqueda-tipo { display: inline-flex; width: fit-content; padding: 0.22rem 0.65rem; border-radius: 999px; background: #f3dfd4; color: #7a0c20; font-size: 0.72rem; font-weight: 850; text-transform: uppercase; letter-spacing: 0.04em; }
        .busqueda-detalle { margin: 0.2rem 0 0; color: var(--color-text-muted, #6b7280); font-size: 0.92rem; line-height: 1.5; }
        .busqueda-filtros { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.85rem 1rem; margin-top: 1rem; }
        .busqueda-filtros .form-field { margin: 0; min-width: 0; }
        .busqueda-filtros input, .busqueda-filtros select { width: 100%; box-sizing: border-box; min-width: 0; }
        .busqueda-filtros input { min-height: 2.75rem; padding: 0.55rem 0.8rem; color: var(--color-text); background: #fffaf7; border: 1px solid rgba(137, 102, 80, 0.32); border-radius: 0.75rem; font: inherit; }
        .busqueda-filtros-grupo { grid-column: 1 / -1; margin: 0.4rem 0 -0.2rem; color: var(--color-primary); font-size: 0.82rem; font-weight: 850; text-transform: uppercase; letter-spacing: 0.04em; }
        .busqueda-acciones-avanzadas { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; margin-top: 1rem; }
        @media (max-width: 60rem) { .busqueda-filtros { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
        @media (max-width: 46rem) {
            .busqueda-filtros { grid-template-columns: 1fr; }
            .busqueda-form { grid-template-columns: 1fr; }
            .busqueda-form .button { width: 100%; }
        }
    </style>

    <section class="static-page user-module-page busqueda-page">
        <div class="static-page-header">
            <span class="section-label">Buscar</span>
            <h1>Buscá en todo StageUp</h1>
            <p>Encontrá espacios, novedades y respuestas del centro de ayuda desde un solo lugar.</p>
        </div>

        <div class="static-page-body busqueda-page-body">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="auth-card busqueda-card" DefaultButton="btnBuscar">
                <div class="busqueda-form">
                    <asp:TextBox ID="txtBusqueda" runat="server" MaxLength="100" placeholder="Ej: teatro, reservas, Palermo..." aria-label="Qué querés buscar" />
                    <asp:Button ID="btnBuscar" runat="server" CssClass="button button-primary" Text="Buscar" CausesValidation="false" OnClick="btnBuscar_Click" />
                </div>
                <details class="busqueda-avanzada" id="busquedaAvanzada" runat="server">
                    <summary>Búsqueda avanzada</summary>
                    <p class="busqueda-detalle">Elegí en qué tipo de contenido buscar y sumá filtros. Si no elegís ningún tipo, se busca en todos. Con algún filtro, el texto es opcional.</p>
                    <asp:CheckBoxList ID="cblTipos" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="busqueda-tipos" />

                    <div class="busqueda-filtros">
                        <span class="busqueda-filtros-grupo">Fechas</span>
                        <div class="form-field">
                            <label for="<%= txtDesde.ClientID %>">Desde</label>
                            <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtHasta.ClientID %>">Hasta</label>
                            <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                        </div>
                        <p class="busqueda-detalle">Fecha de publicación del espacio o de la novedad. La ayuda no tiene fecha.</p>

                        <span class="busqueda-filtros-grupo">Espacios</span>
                        <div class="form-field">
                            <label for="<%= txtUbicacion.ClientID %>">Ubicación</label>
                            <asp:TextBox ID="txtUbicacion" runat="server" MaxLength="100" placeholder="Ciudad, provincia o dirección" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCapacidad.ClientID %>">Capacidad mínima</label>
                            <asp:TextBox ID="txtCapacidad" runat="server" TextMode="Number" min="1" step="1" placeholder="Personas" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtPrecio.ClientID %>">Precio máximo por hora</label>
                            <asp:TextBox ID="txtPrecio" runat="server" TextMode="Number" min="1" step="any" placeholder="Ej: 15000" />
                        </div>

                        <span class="busqueda-filtros-grupo">Novedades y resultados</span>
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

                    <div class="busqueda-acciones-avanzadas">
                        <asp:Button ID="btnBuscarAvanzado" runat="server" CssClass="button button-primary" Text="Buscar con filtros" CausesValidation="false" OnClick="btnBuscar_Click" />
                        <a class="text-link" href="Buscar.aspx">Limpiar filtros</a>
                    </div>
                </details>
            </asp:Panel>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlResumen" runat="server" Visible="false">
                <p class="busqueda-detalle busqueda-resumen"><asp:Literal ID="litResumen" runat="server" /></p>
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
