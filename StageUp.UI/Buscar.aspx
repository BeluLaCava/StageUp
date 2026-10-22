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
        .busqueda-avanzada { margin-top: 0.85rem; border-top: 1px solid rgba(137, 102, 80, 0.17); }
        .busqueda-avanzada > summary { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: 0.9rem 0.15rem 0.1rem; cursor: pointer; color: var(--color-primary); font-weight: 850; list-style: none; }
        .busqueda-avanzada > summary::-webkit-details-marker, .busqueda-filter-section > summary::-webkit-details-marker { display: none; }
        .busqueda-summary-copy { display: grid; gap: 0.08rem; }
        .busqueda-summary-copy small { color: var(--color-text-muted); font-weight: 500; }
        .busqueda-summary-icon, .busqueda-filter-chevron { display: grid; place-items: center; width: 2rem; height: 2rem; flex: 0 0 auto; border-radius: 50%; background: rgba(243, 223, 212, 0.7); transition: transform 180ms ease, background 180ms ease; }
        .busqueda-summary-icon::before, .busqueda-filter-chevron::before { content: ""; width: 0.48rem; height: 0.48rem; border: 2px solid currentColor; border-top: 0; border-left: 0; transform: rotate(45deg) translate(-0.1rem, -0.1rem); }
        .busqueda-avanzada[open] > summary .busqueda-summary-icon, .busqueda-filter-section[open] > summary .busqueda-filter-chevron { transform: rotate(180deg); background: rgba(122, 12, 32, 0.1); }
        .busqueda-advanced-panel { display: grid; gap: 0.75rem; margin-top: 0.85rem; animation: busqueda-panel-in 220ms ease both; }
        .busqueda-content-types { padding: 0.85rem 0.95rem; background: rgba(248, 235, 228, 0.48); border: 1px solid rgba(137, 102, 80, 0.16); border-radius: 0.9rem; }
        .busqueda-content-types strong { display: block; margin-bottom: 0.15rem; color: var(--color-primary-dark); }
        .busqueda-choice-list { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-top: 0.65rem; }
        .busqueda-choice-list > span { position: relative; display: inline-flex; }
        .busqueda-choice-list input[type="checkbox"] { position: absolute !important; width: 1px !important; height: 1px !important; min-height: 0 !important; margin: 0 !important; padding: 0 !important; opacity: 0; pointer-events: none; }
        .busqueda-choice-list label { display: inline-flex; align-items: center; min-height: 2.25rem; padding: 0.4rem 0.75rem; color: var(--color-primary-dark); background: #fffaf7; border: 1px solid rgba(137, 102, 80, 0.24); border-radius: 999px; font-weight: 750; cursor: pointer; transition: color 160ms ease, background 160ms ease, border-color 160ms ease, transform 160ms ease; }
        .busqueda-choice-list label:hover { transform: translateY(-0.08rem); border-color: rgba(122, 12, 32, 0.38); }
        .busqueda-choice-list input[type="checkbox"]:checked + label { color: #fff; background: var(--color-primary); border-color: var(--color-primary); box-shadow: 0 0.35rem 0.9rem rgba(109, 16, 33, 0.15); }
        .busqueda-choice-list input[type="checkbox"]:focus-visible + label { outline: 3px solid rgba(109, 16, 33, 0.18); outline-offset: 2px; }
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
        .busqueda-filter-sections { display: grid; gap: 0.6rem; }
        .busqueda-filter-section { overflow: hidden; background: rgba(255, 252, 250, 0.76); border: 1px solid rgba(137, 102, 80, 0.18); border-radius: 0.9rem; transition: border-color 180ms ease, box-shadow 180ms ease; }
        .busqueda-filter-section[open] { border-color: rgba(122, 12, 32, 0.22); box-shadow: 0 0.55rem 1.4rem rgba(77, 11, 23, 0.045); }
        .busqueda-filter-section > summary { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: 0.8rem 0.95rem; cursor: pointer; list-style: none; }
        .busqueda-filter-title { display: grid; gap: 0.06rem; color: var(--color-primary-dark); font-weight: 850; }
        .busqueda-filter-title small { color: var(--color-text-muted); font-size: 0.8rem; font-weight: 500; }
        .busqueda-filtros { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.75rem 0.85rem; padding: 0 0.95rem 0.95rem; animation: busqueda-panel-in 180ms ease both; }
        .busqueda-filtros .form-field { margin: 0; min-width: 0; }
        .busqueda-filtros input:not([type="checkbox"]), .busqueda-filtros select { width: 100%; box-sizing: border-box; min-width: 0; }
        .busqueda-filtros input:not([type="checkbox"]) { min-height: 2.75rem; padding: 0.55rem 0.8rem; color: var(--color-text); background: #fffaf7; border: 1px solid rgba(137, 102, 80, 0.32); border-radius: 0.75rem; font: inherit; }
        .busqueda-field-wide { grid-column: 1 / -1; }
        .busqueda-acciones-avanzadas { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; margin-top: 1rem; }
        @keyframes busqueda-panel-in { from { opacity: 0; transform: translateY(-0.25rem); } to { opacity: 1; transform: translateY(0); } }
        @media (max-width: 60rem) { .busqueda-filtros { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
        @media (max-width: 46rem) {
            .busqueda-filtros { grid-template-columns: 1fr; }
            .busqueda-form { grid-template-columns: 1fr; }
            .busqueda-form .button { width: 100%; }
            .busqueda-summary-copy small, .busqueda-filter-title small { display: none; }
        }
        @media (prefers-reduced-motion: reduce) { .busqueda-advanced-panel, .busqueda-filtros { animation: none; } }
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
                    <summary>
                        <span class="busqueda-summary-copy"><span>Búsqueda avanzada</span><small>Abrí solamente los grupos que necesitás.</small></span>
                        <span class="busqueda-summary-icon" aria-hidden="true"></span>
                    </summary>
                    <div class="busqueda-advanced-panel">
                        <div class="busqueda-content-types">
                            <strong>¿Qué querés encontrar?</strong>
                            <span class="busqueda-detalle">Podés elegir más de un tipo. Si no marcás ninguno, se busca en todo StageUp.</span>
                            <asp:CheckBoxList ID="cblTipos" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="busqueda-choice-list" />
                        </div>

                        <div class="busqueda-filter-sections">
                            <details class="busqueda-filter-section">
                                <summary><span class="busqueda-filter-title">Lugar y tipo de espacio<small>Tipo, ubicación, provincia, ciudad, zona y capacidad.</small></span><span class="busqueda-filter-chevron" aria-hidden="true"></span></summary>
                                <div class="busqueda-filtros">
                                    <div class="form-field"><label for="<%= txtTipoEspacio.ClientID %>">Tipo de espacio</label><asp:TextBox ID="txtTipoEspacio" runat="server" MaxLength="100" placeholder="Estudio, sala, teatro..." /></div>
                                    <div class="form-field"><label for="<%= txtUbicacion.ClientID %>">Ubicación general</label><asp:TextBox ID="txtUbicacion" runat="server" MaxLength="100" placeholder="Ciudad, provincia o dirección" /></div>
                                    <div class="form-field"><label for="<%= txtCapacidad.ClientID %>">Capacidad mínima</label><asp:TextBox ID="txtCapacidad" runat="server" TextMode="Number" min="1" step="1" placeholder="Personas" /></div>
                                    <div class="form-field"><label for="<%= txtProvincia.ClientID %>">Provincia</label><asp:TextBox ID="txtProvincia" runat="server" MaxLength="100" /></div>
                                    <div class="form-field"><label for="<%= txtCiudad.ClientID %>">Ciudad</label><asp:TextBox ID="txtCiudad" runat="server" MaxLength="100" /></div>
                                    <div class="form-field"><label for="<%= txtZona.ClientID %>">Zona o barrio</label><asp:TextBox ID="txtZona" runat="server" MaxLength="100" /></div>
                                </div>
                            </details>

                            <details class="busqueda-filter-section">
                                <summary><span class="busqueda-filter-title">Precio y disponibilidad<small>Presupuesto, fecha, horario y reputación mínima.</small></span><span class="busqueda-filter-chevron" aria-hidden="true"></span></summary>
                                <div class="busqueda-filtros">
                                    <div class="form-field"><label for="<%= txtPrecioMinimo.ClientID %>">Precio mínimo por hora</label><asp:TextBox ID="txtPrecioMinimo" runat="server" TextMode="Number" min="0" step="any" /></div>
                                    <div class="form-field"><label for="<%= txtPrecio.ClientID %>">Precio máximo por hora</label><asp:TextBox ID="txtPrecio" runat="server" TextMode="Number" min="1" step="any" placeholder="Ej: 15000" /></div>
                                    <div class="form-field"><label for="<%= ddlCalificacion.ClientID %>">Calificación mínima</label><asp:DropDownList ID="ddlCalificacion" runat="server" CssClass="form-select"><asp:ListItem Value="">Cualquiera</asp:ListItem><asp:ListItem Value="3">3 estrellas o más</asp:ListItem><asp:ListItem Value="4">4 estrellas o más</asp:ListItem><asp:ListItem Value="4.5">4,5 estrellas o más</asp:ListItem></asp:DropDownList></div>
                                    <div class="form-field"><label for="<%= txtFechaDisponible.ClientID %>">Fecha disponible</label><asp:TextBox ID="txtFechaDisponible" runat="server" TextMode="Date" /></div>
                                    <div class="form-field"><label for="<%= txtHoraDesde.ClientID %>">Horario desde</label><asp:TextBox ID="txtHoraDesde" runat="server" TextMode="Time" step="1800" /></div>
                                    <div class="form-field"><label for="<%= txtHoraHasta.ClientID %>">Horario hasta</label><asp:TextBox ID="txtHoraHasta" runat="server" TextMode="Time" step="1800" /></div>
                                </div>
                            </details>

                            <details class="busqueda-filter-section">
                                <summary><span class="busqueda-filter-title">Características y equipamiento<small>Piso, camarines, accesibilidad y recursos técnicos.</small></span><span class="busqueda-filter-chevron" aria-hidden="true"></span></summary>
                                <div class="busqueda-filtros">
                                    <div class="form-field busqueda-field-wide"><label for="<%= txtCaracteristicas.ClientID %>">Características</label><asp:TextBox ID="txtCaracteristicas" runat="server" MaxLength="150" placeholder="Piso, camarines, accesibilidad..." /></div>
                                    <div class="form-field busqueda-field-wide"><label>Equipamiento</label><asp:CheckBoxList ID="cblEquipamiento" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" CssClass="busqueda-choice-list" /></div>
                                </div>
                            </details>

                            <details class="busqueda-filter-section">
                                <summary><span class="busqueda-filter-title">Fechas y presentación de resultados<small>Vigencia, categoría de novedad, orden y cantidad.</small></span><span class="busqueda-filter-chevron" aria-hidden="true"></span></summary>
                                <div class="busqueda-filtros">
                                    <div class="form-field"><label for="<%= txtDesde.ClientID %>">Publicado desde</label><asp:TextBox ID="txtDesde" runat="server" TextMode="Date" /></div>
                                    <div class="form-field"><label for="<%= txtHasta.ClientID %>">Publicado hasta</label><asp:TextBox ID="txtHasta" runat="server" TextMode="Date" /></div>
                                    <div class="form-field"><label for="<%= ddlCategoria.ClientID %>">Categoría de novedad</label><asp:DropDownList ID="ddlCategoria" runat="server" CssClass="form-select" /></div>
                                    <div class="form-field"><label for="<%= ddlOrden.ClientID %>">Ordenar por</label><asp:DropDownList ID="ddlOrden" runat="server" CssClass="form-select" /></div>
                                    <div class="form-field"><label for="<%= ddlMaximo.ClientID %>">Resultados por tipo</label><asp:DropDownList ID="ddlMaximo" runat="server" CssClass="form-select" /></div>
                                </div>
                            </details>
                        </div>

                        <div class="busqueda-acciones-avanzadas">
                            <asp:Button ID="btnBuscarAvanzado" runat="server" CssClass="button button-primary" Text="Buscar con filtros" CausesValidation="false" OnClick="btnBuscar_Click" />
                            <a class="text-link" href="Buscar.aspx">Limpiar filtros</a>
                        </div>
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
