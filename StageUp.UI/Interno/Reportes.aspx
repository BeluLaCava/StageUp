<%@ Page Title="Reportes y estadísticas | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="Reportes.aspx.cs" Inherits="StageUp.UI.Interno.Reportes" %>

<asp:Content ID="ReportesContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .rep-stack { grid-template-columns: minmax(0, 1fr); gap: 1.55rem; }
        .rep-stack > * { min-width: 0; }
        .rep-filtros { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0 1rem; }
        .rep-aviso { padding: 12px 16px; border-radius: 12px; background: var(--color-nude-light, #fcf7f3); border-left: 4px solid var(--color-brown, #896650); color: var(--color-text, #3e2925); margin-bottom: 14px; }
        .rep-nota { color: var(--color-text-muted, #765f55); font-size: 0.85em; margin: 6px 0 0; }

        .rep-report-stack { display: grid; gap: 1.55rem; }
        .rep-dashboard-card { overflow: hidden; }
        .rep-tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(215px, 1fr)); gap: 1rem; }
        .rep-tile { position: relative; overflow: hidden; min-height: 8.3rem; background: linear-gradient(145deg, rgba(255, 252, 250, 0.98), rgba(252, 247, 243, 0.82)); border: 1px solid var(--color-border, #e2e2e2); border-radius: var(--radius-medium, 1rem); padding: 1.1rem 1.15rem; box-shadow: 0 0.75rem 1.8rem rgba(77, 11, 23, 0.05); }
        .rep-tile::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 5px; background: var(--rep-accent, var(--color-primary, #6d1021)); }
        .rep-tile::after { content: ""; position: absolute; right: -2.5rem; top: -2.5rem; width: 7rem; height: 7rem; border-radius: 50%; background: color-mix(in srgb, var(--rep-accent, #6d1021) 12%, transparent); }
        .rep-tile:nth-child(1) { --rep-accent: #7a0c20; }
        .rep-tile:nth-child(2) { --rep-accent: #a83d52; }
        .rep-tile:nth-child(3) { --rep-accent: #8a5a44; }
        .rep-tile:nth-child(4) { --rep-accent: #c0796b; }
        .rep-tile:nth-child(5) { --rep-accent: #5f6f52; }
        .rep-tile:nth-child(6) { --rep-accent: #b98224; }
        .rep-tile:nth-child(7) { --rep-accent: #6d1021; }
        .rep-tile-label { display: block; color: var(--color-text-muted, #765f55); font-size: 0.85em; }
        .rep-tile-valor { display: block; font-size: clamp(1.35em, 4vw, 1.85em); white-space: nowrap; font-weight: 800; color: var(--color-text, #3e2925); margin: 0.55rem 0 0.3rem; font-variant-numeric: tabular-nums; }
        .rep-tile-sub { display: block; color: var(--color-text-muted, #765f55); font-size: 0.8em; }
        .rep-tile a { color: var(--color-primary, #6d1021); }

        .rep-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1.35rem; }
        .rep-total { color: var(--color-text-muted, #765f55); font-size: 0.9em; }
        .rep-total strong { color: var(--color-text, #3e2925); }

        /* Columnas (serie temporal): una sola serie, un solo color. */
        .rep-columnas { position: relative; display: flex; align-items: stretch; gap: 2px; height: 220px; padding-top: 18px; border-bottom: 1px solid var(--color-brown-soft, #b99d88); }
        .rep-columnas-max { position: absolute; top: 0; left: 0; font-size: 0.75em; color: var(--color-text-muted, #765f55); }
        .rep-columnas::before { content: ""; position: absolute; top: 18px; left: 0; right: 0; border-top: 1px dashed var(--color-border, #e2e2e2); }
        .rep-col { position: relative; flex: 1 1 0; min-width: 0; display: flex; flex-direction: column; justify-content: flex-end; align-items: center; cursor: default; }
        .rep-col-barra { width: 100%; max-width: 44px; background: linear-gradient(180deg, #a83d52, var(--color-primary, #6d1021)); border-radius: 4px 4px 0 0; }
        .rep-col:hover .rep-col-barra, .rep-col:focus .rep-col-barra { background: var(--color-primary-dark, #4d0b17); }
        .rep-ejes { display: flex; gap: 2px; margin-top: 4px; }
        .rep-eje { flex: 1 1 0; min-width: 0; text-align: center; font-size: 0.72em; color: var(--color-text-muted, #765f55); white-space: nowrap; overflow: hidden; }

        /* Barras horizontales (categorías). */
        .rep-barras { display: flex; flex-direction: column; gap: 10px; }
        .rep-barra-fila { position: relative; display: grid; grid-template-columns: minmax(0, 38%) minmax(0, 1fr) minmax(8.5em, auto); align-items: center; gap: 10px; }
        .rep-barra-etiqueta { font-size: 0.9em; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .rep-barra-etiqueta small { color: var(--color-text-muted, #765f55); }
        .rep-barra-track { height: 14px; background: var(--color-nude-light, #fcf7f3); border-radius: 0 4px 4px 0; }
        .rep-barra-fill { height: 100%; background: var(--rep-bar-color, var(--color-primary, #6d1021)); border-radius: 0 4px 4px 0; }
        .rep-barra-fila:nth-child(5n+1) { --rep-bar-color: #7a0c20; }
        .rep-barra-fila:nth-child(5n+2) { --rep-bar-color: #a83d52; }
        .rep-barra-fila:nth-child(5n+3) { --rep-bar-color: #8a5a44; }
        .rep-barra-fila:nth-child(5n+4) { --rep-bar-color: #c0796b; }
        .rep-barra-fila:nth-child(5n+5) { --rep-bar-color: #5f6f52; }
        .rep-barra-fila:hover .rep-barra-fill { background: var(--color-primary-dark, #4d0b17); }
        .rep-barra-valor { text-align: right; font-size: 0.85em; font-variant-numeric: tabular-nums; white-space: nowrap; color: var(--color-text, #3e2925); }

        /* Tooltip propio (también funciona con teclado: las barras tienen tabindex). */
        [data-tooltip]:hover::after, [data-tooltip]:focus::after {
            content: attr(data-tooltip); position: absolute; bottom: calc(100% + 6px); left: 50%; transform: translateX(-50%);
            background: var(--color-text, #3e2925); color: #fff; padding: 6px 10px; border-radius: 8px; font-size: 0.78rem;
            white-space: pre; z-index: 5; pointer-events: none; box-shadow: var(--shadow-soft);
        }
        .rep-barra-fila[data-tooltip]:hover::after, .rep-barra-fila[data-tooltip]:focus::after { left: auto; right: 0; transform: none; }

        .rep-tabla-wrap { overflow-x: auto; margin-top: 12px; }
        .rep-tabla { width: 100%; border-collapse: collapse; font-size: 0.88em; }
        .rep-tabla th, .rep-tabla td { padding: 6px 8px; border-bottom: 1px solid var(--color-border, #e2e2e2); text-align: left; }
        .rep-tabla td.num, .rep-tabla th.num { text-align: right; font-variant-numeric: tabular-nums; }
        .rep-tabla tfoot td { font-weight: 700; }
        .rep-detalles summary { cursor: pointer; color: var(--color-primary, #6d1021); margin-top: 10px; font-size: 0.9em; }
        .rep-vacio { color: var(--color-text-muted, #765f55); padding: 18px 0; }
        .rep-estado-encuesta { font-size: 0.75em; padding: 1px 8px; border-radius: 999px; border: 1px solid var(--color-border, #e2e2e2); color: var(--color-text-muted, #765f55); margin-left: 4px; }

        @media (max-width: 900px) {
            .rep-grid, .rep-filtros { grid-template-columns: 1fr; }
            .rep-report-stack { gap: 1.15rem; }
            .rep-barra-fila { grid-template-columns: minmax(0, 1fr) auto; }
            .rep-barra-track { grid-column: 1 / -1; grid-row: 2; }
        }
        @media (max-width: 600px) {
            /* En pantallas angostas, una etiqueta del eje cada dos (el valor de cada barra sigue en el tooltip y en la tabla). */
            .rep-eje { overflow: visible; }
            .rep-eje:nth-child(even) { visibility: hidden; }
        }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1>Reportes y estadísticas</h1>
            <p>Indicadores de la plataforma, ingresos por período y por zona, reservas por estado y participación en encuestas.</p>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack rep-stack">
            <asp:Panel ID="pnlFiltros" runat="server" CssClass="auth-card admin-filter-card" role="search" aria-labelledby="filtros-reportes-title" DefaultButton="btnAplicar">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Filtros</span>
                    <h2 id="filtros-reportes-title">Período y criterios</h2>
                    <p>Por defecto se muestran los últimos 12 meses, agrupados por mes y en pesos.</p>
                </div>

                <div class="rep-filtros">
                    <div class="form-field">
                        <label for="<%= txtDesde.ClientID %>">Desde</label>
                        <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtHasta.ClientID %>">Hasta</label>
                        <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlAgrupacion.ClientID %>">Agrupar ingresos por</label>
                        <asp:DropDownList ID="ddlAgrupacion" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlMoneda.ClientID %>">Moneda</label>
                        <asp:DropDownList ID="ddlMoneda" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlProvincia.ClientID %>">Zona (provincia)</label>
                        <asp:DropDownList ID="ddlProvincia" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlTipoEspacio.ClientID %>">Tipo de espacio</label>
                        <asp:DropDownList ID="ddlTipoEspacio" runat="server" />
                    </div>
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnAplicar" runat="server" CssClass="button button-primary" Text="Actualizar reporte"
                        CausesValidation="false" OnClick="btnAplicar_Click" />
                    <asp:LinkButton ID="lnkUltimos12Meses" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Volver a los últimos 12 meses" OnClick="lnkUltimos12Meses_Click" />
                    <asp:LinkButton ID="lnkExportarCsv" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Exportar reporte completo a CSV" OnClick="lnkExportarCsv_Click" />
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlReporte" runat="server" Visible="false" CssClass="rep-report-stack">
                <asp:Panel ID="pnlAviso" runat="server" CssClass="rep-aviso" Visible="false">
                    <asp:Literal ID="litAviso" runat="server" />
                </asp:Panel>

                <!-- Tablero -->
                <div class="auth-card admin-list-card rep-dashboard-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Tablero</span>
                        <h2>Indicadores</h2>
                        <p class="rep-nota">Los indicadores usan el período y la moneda elegidos para toda la plataforma. La zona y el tipo de espacio se aplican a los gráficos de abajo.</p>
                    </div>
                    <div class="rep-tiles">
                        <div class="rep-tile">
                            <span class="rep-tile-label">Ingresos por reservas</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiIngresos" runat="server" /></span>
                            <span class="rep-tile-sub"><asp:Literal ID="litKpiConfirmadas" runat="server" /></span>
                        </div>
                        <div class="rep-tile">
                            <span class="rep-tile-label">Comisiones por cancelación</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiComisiones" runat="server" /></span>
                            <span class="rep-tile-sub">Cargos por cancelar fuera del plazo sin cargo</span>
                        </div>
                        <div class="rep-tile">
                            <span class="rep-tile-label">Reservas solicitadas</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiSolicitadas" runat="server" /></span>
                            <span class="rep-tile-sub">Pedidas dentro del período</span>
                        </div>
                        <div class="rep-tile">
                            <span class="rep-tile-label">Usuarios nuevos</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiUsuariosNuevos" runat="server" /></span>
                            <span class="rep-tile-sub"><asp:Literal ID="litKpiUsuariosActivos" runat="server" /></span>
                        </div>
                        <div class="rep-tile">
                            <span class="rep-tile-label">Espacios publicados</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiEspacios" runat="server" /></span>
                            <span class="rep-tile-sub">Visibles hoy en el catálogo</span>
                        </div>
                        <div class="rep-tile">
                            <span class="rep-tile-label">Calificación promedio</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiCalificacion" runat="server" /></span>
                            <span class="rep-tile-sub">Reseñas de espacios, histórico</span>
                        </div>
                        <div class="rep-tile">
                            <span class="rep-tile-label">Tickets de soporte pendientes</span>
                            <span class="rep-tile-valor"><asp:Literal ID="litKpiTickets" runat="server" /></span>
                            <span class="rep-tile-sub"><a href="GestionSoporte.aspx">Ir a gestión de soporte</a></span>
                        </div>
                    </div>
                </div>

                <!-- Ingresos por período -->
                <div class="auth-card admin-list-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Ganancias</span>
                        <h2><asp:Literal ID="litTituloIngresos" runat="server" /></h2>
                        <p class="rep-total">
                            Total del período: <strong><asp:Literal ID="litTotalIngresos" runat="server" /></strong>
                            · <asp:Literal ID="litTotalReservas" runat="server" /> reservas
                            · Comisiones: <strong><asp:Literal ID="litTotalComisiones" runat="server" /></strong>
                        </p>
                    </div>

                    <asp:Panel ID="pnlIngresosVacio" runat="server" CssClass="rep-vacio" Visible="false">
                        No hay reservas confirmadas ni comisiones en este período con los filtros elegidos.
                    </asp:Panel>

                    <asp:Panel ID="pnlIngresosGrafico" runat="server">
                        <div class="rep-columnas" role="img" aria-label='<%: DescripcionGraficoIngresos %>'>
                            <span class="rep-columnas-max">Máx. <asp:Literal ID="litMaximoIngresos" runat="server" /></span>
                            <asp:Repeater ID="rptIngresosColumnas" runat="server">
                                <ItemTemplate>
                                    <div class="rep-col" tabindex="0" data-tooltip='<%# TooltipPeriodo(Container.DataItem) %>'>
                                        <div class="rep-col-barra" style='<%# EstiloAlto(Eval("PorcentajeBarra")) %>'></div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                        <div class="rep-ejes" aria-hidden="true">
                            <asp:Repeater ID="rptIngresosEjes" runat="server">
                                <ItemTemplate>
                                    <span class="rep-eje"><%#: MostrarEtiquetaEje(Container.ItemIndex, Eval("Etiqueta") as string) %></span>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <details class="rep-detalles">
                        <summary>Ver como tabla</summary>
                        <div class="rep-tabla-wrap">
                            <table class="rep-tabla">
                                <thead>
                                    <tr>
                                        <th scope="col">Período</th>
                                        <th scope="col" class="num">Reservas</th>
                                        <th scope="col" class="num">Importe reservas</th>
                                        <th scope="col" class="num">Comisiones</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptIngresosTabla" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%#: DescribirPeriodo((DateTime)Eval("InicioPeriodo")) %></td>
                                                <td class="num"><%#: Eval("CantidadReservas") %></td>
                                                <td class="num"><%#: Importe(Eval("ImporteReservas")) %></td>
                                                <td class="num"><%#: Importe(Eval("ImporteComisiones")) %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                                <tfoot>
                                    <tr>
                                        <td>Total</td>
                                        <td class="num"><asp:Literal ID="litTablaTotalReservas" runat="server" /></td>
                                        <td class="num"><asp:Literal ID="litTablaTotalIngresos" runat="server" /></td>
                                        <td class="num"><asp:Literal ID="litTablaTotalComisiones" runat="server" /></td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </details>
                </div>

                <div class="rep-grid">
                    <!-- Por zona -->
                    <div class="auth-card admin-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Zonas</span>
                            <h2>Ingresos por zona</h2>
                            <p class="rep-nota">Reservas confirmadas del período, por provincia y ciudad del espacio.</p>
                        </div>
                        <asp:Panel ID="pnlZonasVacio" runat="server" CssClass="rep-vacio" Visible="false">
                            Sin ingresos en el período.
                        </asp:Panel>
                        <div class="rep-barras">
                            <asp:Repeater ID="rptZonas" runat="server">
                                <ItemTemplate>
                                    <div class="rep-barra-fila" tabindex="0" data-tooltip='<%# TooltipZona(Container.DataItem) %>'>
                                        <span class="rep-barra-etiqueta"><%#: Eval("Provincia") %> <small>· <%#: Eval("Ciudad") %></small></span>
                                        <div class="rep-barra-track"><div class="rep-barra-fill" style='<%# EstiloAncho(Eval("PorcentajeBarra")) %>'></div></div>
                                        <span class="rep-barra-valor"><%#: Importe(Eval("ImporteReservas")) %> · <%#: Porcentaje(Eval("PorcentajeDelTotal")) %></span>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                        <details class="rep-detalles">
                            <summary>Ver como tabla</summary>
                            <div class="rep-tabla-wrap">
                                <table class="rep-tabla">
                                    <thead>
                                        <tr>
                                            <th scope="col">Provincia</th>
                                            <th scope="col">Ciudad</th>
                                            <th scope="col" class="num">Reservas</th>
                                            <th scope="col" class="num">Importe</th>
                                            <th scope="col" class="num">%</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="rptZonasTabla" runat="server">
                                            <ItemTemplate>
                                                <tr>
                                                    <td><%#: Eval("Provincia") %></td>
                                                    <td><%#: Eval("Ciudad") %></td>
                                                    <td class="num"><%#: Eval("CantidadReservas") %></td>
                                                    <td class="num"><%#: Importe(Eval("ImporteReservas")) %></td>
                                                    <td class="num"><%#: Porcentaje(Eval("PorcentajeDelTotal")) %></td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </div>
                        </details>
                    </div>

                    <!-- Por estado -->
                    <div class="auth-card admin-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Reservas</span>
                            <h2>Reservas por estado</h2>
                            <p class="rep-nota">Reservas pedidas dentro del período, según su estado actual.</p>
                        </div>
                        <asp:Panel ID="pnlEstadosVacio" runat="server" CssClass="rep-vacio" Visible="false">
                            No se pidieron reservas en el período.
                        </asp:Panel>
                        <div class="rep-barras">
                            <asp:Repeater ID="rptEstados" runat="server">
                                <ItemTemplate>
                                    <div class="rep-barra-fila" tabindex="0" data-tooltip='<%# TooltipEstado(Container.DataItem) %>'>
                                        <span class="rep-barra-etiqueta"><%#: NombreEstado(Eval("Estado") as string) %></span>
                                        <div class="rep-barra-track"><div class="rep-barra-fill" style='<%# EstiloAncho(Eval("PorcentajeBarra")) %>'></div></div>
                                        <span class="rep-barra-valor"><%#: Eval("Cantidad") %> · <%#: Porcentaje(Eval("PorcentajeDelTotal")) %></span>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                        <details class="rep-detalles">
                            <summary>Ver como tabla</summary>
                            <div class="rep-tabla-wrap">
                                <table class="rep-tabla">
                                    <thead>
                                        <tr>
                                            <th scope="col">Estado</th>
                                            <th scope="col" class="num">Cantidad</th>
                                            <th scope="col" class="num">%</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="rptEstadosTabla" runat="server">
                                            <ItemTemplate>
                                                <tr>
                                                    <td><%#: NombreEstado(Eval("Estado") as string) %></td>
                                                    <td class="num"><%#: Eval("Cantidad") %></td>
                                                    <td class="num"><%#: Porcentaje(Eval("PorcentajeDelTotal")) %></td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </div>
                        </details>
                    </div>
                </div>

                <!-- Encuestas -->
                <div class="auth-card admin-list-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Encuestas</span>
                        <h2>Comparación de participación en encuestas</h2>
                        <p class="rep-nota">Porcentaje de usuarios del público objetivo que respondió cada encuesta publicada. Los resultados de cada pregunta se ven en <a href="GestionEncuestas.aspx">Gestión de encuestas</a>.</p>
                    </div>
                    <asp:Panel ID="pnlEncuestasVacio" runat="server" CssClass="rep-vacio" Visible="false">
                        Todavía no hay encuestas publicadas.
                    </asp:Panel>
                    <div class="rep-barras">
                        <asp:Repeater ID="rptEncuestas" runat="server">
                            <ItemTemplate>
                                <div class="rep-barra-fila" tabindex="0" data-tooltip='<%# TooltipEncuesta(Container.DataItem) %>'>
                                    <span class="rep-barra-etiqueta"><%#: Eval("Titulo") %><span class="rep-estado-encuesta"><%#: Eval("Estado") %></span></span>
                                    <div class="rep-barra-track"><div class="rep-barra-fill" style='<%# EstiloAncho(Eval("PorcentajeBarra")) %>'></div></div>
                                    <span class="rep-barra-valor"><%#: DescribirParticipacion(Container.DataItem) %></span>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <details class="rep-detalles">
                        <summary>Ver como tabla</summary>
                        <div class="rep-tabla-wrap">
                            <table class="rep-tabla">
                                <thead>
                                    <tr>
                                        <th scope="col">Encuesta</th>
                                        <th scope="col">Estado</th>
                                        <th scope="col">Público objetivo</th>
                                        <th scope="col" class="num">Respuestas</th>
                                        <th scope="col" class="num">Destinatarios</th>
                                        <th scope="col" class="num">Participación</th>
                                        <th scope="col">Vencimiento</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptEncuestasTabla" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%#: Eval("Titulo") %></td>
                                                <td><%#: Eval("Estado") %></td>
                                                <td><%#: NombrePublico(Eval("PublicoObjetivo") as string) %></td>
                                                <td class="num"><%#: Eval("CantidadRespuestas") %></td>
                                                <td class="num"><%#: Eval("CantidadDestinatarios") %></td>
                                                <td class="num"><%#: TasaParticipacion(Eval("TasaParticipacion")) %></td>
                                                <td><%#: Eval("FechaVencimiento", "{0:dd/MM/yyyy}") %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </details>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
