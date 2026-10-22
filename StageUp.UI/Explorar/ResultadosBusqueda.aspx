<%@ Page Title="Explorar espacios | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ResultadosBusqueda.aspx.cs" Inherits="StageUp.UI.Explorar.ResultadosBusqueda" %>

<asp:Content ID="ExploreContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .results-sort { display: inline-flex; align-items: center; gap: 0.55rem; padding: 0.28rem 0.35rem 0.28rem 0.75rem; color: var(--color-text-muted); background: rgba(255, 252, 250, 0.92); border: 1px solid rgba(109, 16, 33, 0.16); border-radius: 999px; box-shadow: 0 0.4rem 1rem rgba(77, 11, 23, 0.04); font-size: 0.82rem; }
        .results-sort label { margin: 0; font-weight: 750; white-space: nowrap; }
        .results-sort select { min-width: 9.5rem; padding: 0.48rem 2rem 0.48rem 0.75rem; color: var(--color-primary-dark); background-color: rgba(243, 223, 212, 0.55); border: 0; border-radius: 999px; font: inherit; font-weight: 800; cursor: pointer; outline: none; }
        .results-sort select:focus-visible { box-shadow: 0 0 0 3px rgba(109, 16, 33, 0.13); }
        .ranking-panel { margin-top: clamp(1.5rem, 3vw, 2.25rem); padding: 1.4rem 1.6rem; background: #fffaf7; border: 1px solid var(--color-border, #e7d5cc); border-radius: 1.1rem; }
        .ranking-panel h2 { margin: 0.2rem 0 0.3rem; color: var(--color-primary, #7a0c20); font-family: var(--font-display, Georgia, serif); font-weight: 500; }
        .ranking-panel p.ranking-criterio { margin: 0 0 1rem; color: #8a6a5e; font-size: 0.9rem; }
        .ranking-lista { list-style: none; margin: 0; padding: 0; display: grid; gap: 0.7rem; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); }
        .ranking-item a { min-height: 4.6rem; display: grid; grid-template-columns: 2.35rem minmax(0, 1fr); gap: 0.85rem; align-items: center; padding: 0.75rem 1rem; background: #fff; border: 1px solid #efe1d9; border-radius: 0.9rem; text-decoration: none; color: inherit; box-sizing: border-box; }
        .ranking-item a:hover { border-color: var(--color-primary, #7a0c20); }
        .ranking-posicion { width: 2.35rem; height: 2.35rem; border-radius: 50%; background: #7a0c20; color: #fff; display: flex; align-items: center; justify-content: center; font-weight: bold; }
        .ranking-datos { min-width: 0; }
        .ranking-datos strong { display: block; line-height: 1.25; }
        .ranking-datos span { font-size: 0.85rem; color: #8a6a5e; }
        .explore-page .explore-content { display: flex; flex-direction: column; }
        .explore-page .filters-toolbar { order: 1; }
        .explore-page .explore-quick-tools { order: 2; display: flex; flex-wrap: wrap; gap: 0.65rem; margin-top: 0.7rem; }
        .explore-page .results-section { order: 3; margin-top: 0.9rem; }
        .explore-page .service-compare-panel, .explore-page .ranking-panel { flex: 1 1 20rem; min-width: 0; display: block; margin: 0; padding: 0; overflow: hidden; background: rgba(255, 252, 250, 0.9); border: 1px solid rgba(109, 16, 33, 0.14); border-radius: 999px; box-shadow: 0 0.45rem 1.2rem rgba(77, 11, 23, 0.045); transition: border-radius 180ms ease, border-color 180ms ease, box-shadow 180ms ease, flex-basis 180ms ease; }
        .explore-page .service-compare-panel:has(.explore-tool-disclosure[open]), .explore-page .ranking-panel:has(.explore-tool-disclosure[open]) { flex-basis: 100%; border-radius: 1rem; border-color: rgba(109, 16, 33, 0.24); box-shadow: 0 0.8rem 1.9rem rgba(77, 11, 23, 0.075); }
        .explore-tool-disclosure > summary { display: grid; grid-template-columns: auto minmax(0, 1fr) auto; align-items: center; gap: 0.75rem; padding: 0.62rem 0.75rem; cursor: pointer; list-style: none; }
        .explore-tool-disclosure > summary::-webkit-details-marker { display: none; }
        .explore-tool-summary { display: grid; gap: 0.12rem; min-width: 0; }
        .explore-tool-summary strong { color: var(--color-primary-dark); font-size: 0.94rem; }
        .explore-tool-summary small { overflow: hidden; color: var(--color-text-muted); font-size: 0.78rem; line-height: 1.3; text-overflow: ellipsis; white-space: nowrap; }
        .explore-tool-symbol { display: grid; place-items: center; width: 2.25rem; height: 2.25rem; color: #fff; background: linear-gradient(145deg, var(--color-primary), #9f3549); border-radius: 50%; box-shadow: 0 0.4rem 0.9rem rgba(109, 16, 33, 0.18); font-size: 1rem; font-weight: 900; }
        .explore-tool-symbol-ranking { color: #fff4c8; }
        .explore-tool-toggle { position: relative; width: 1.85rem; height: 1.85rem; flex: 0 0 auto; background: rgba(243, 223, 212, 0.62); border-radius: 50%; transition: transform 180ms ease; }
        .explore-tool-toggle::before, .explore-tool-toggle::after { content: ""; position: absolute; top: 50%; left: 50%; width: 0.72rem; height: 2px; background: var(--color-primary); transform: translate(-50%, -50%); }
        .explore-tool-toggle::after { transform: translate(-50%, -50%) rotate(90deg); transition: opacity 160ms ease; }
        .explore-tool-disclosure[open] .explore-tool-toggle { transform: rotate(180deg); }
        .explore-tool-disclosure[open] .explore-tool-toggle::after { opacity: 0; }
        .explore-tool-body { padding: 0 1rem 1rem; border-top: 1px solid rgba(137, 102, 80, 0.14); animation: explore-tool-in 200ms ease both; }
        .explore-tool-body .service-compare-actions { justify-content: flex-start; margin-top: 0.9rem; }
        .explore-tool-body .service-type-options { justify-content: flex-start; }
        .explore-tool-body .service-compare-status { display: block; margin-top: 0.65rem; }
        .explore-tool-body .ranking-criterio { margin-top: 0.85rem; }
        .explore-page .space-card { animation: explore-card-in 360ms ease both; }
        .explore-page .space-card:nth-child(2) { animation-delay: 50ms; }
        .explore-page .space-card:nth-child(3) { animation-delay: 100ms; }
        .explore-page .explore-header { padding-block: clamp(0.58rem, 1.2vw, 0.9rem); }
        .explore-page .explore-header .explore-page-container { grid-template-columns: minmax(17rem, 24rem) minmax(24rem, 1fr); gap: clamp(1.2rem, 3vw, 2.6rem); }
        .explore-page .explore-heading h1 { max-width: 24rem; font-size: clamp(1.85rem, 2.6vw, 2.55rem); line-height: 0.98; }
        .explore-page .explore-heading p { margin-top: 0.35rem; font-size: 0.9rem; }
        .explore-page .explore-search-stage .marketplace-search { min-height: 4.15rem; }
        .explore-page .results-heading { display: flex; align-items: end; justify-content: space-between; gap: 1rem; }
        @keyframes explore-tool-in { from { opacity: 0; transform: translateY(-0.3rem); } to { opacity: 1; transform: translateY(0); } }
        @keyframes explore-card-in { from { opacity: 0; transform: translateY(0.5rem); } to { opacity: 1; transform: translateY(0); } }
        @media (max-width: 62rem) {
            .explore-page .explore-header .explore-page-container { grid-template-columns: 1fr; gap: 0.65rem; }
            .explore-page .explore-heading h1 { max-width: 34rem; }
        }
        @media (max-width: 45rem) {
            .explore-tool-body .service-compare-actions { align-items: stretch; flex-direction: column; }
            .explore-tool-body .service-compare-actions .button { width: 100%; }
            .explore-page .explore-quick-tools { display: grid; grid-template-columns: 1fr; }
            .explore-page .results-heading { align-items: stretch; flex-direction: column; }
            .results-sort { align-self: flex-start; }
        }
        @media (prefers-reduced-motion: reduce) { .explore-tool-body, .explore-page .space-card { animation: none; } }
    </style>
    <section class="explore-page" data-results-page>
        <header class="explore-header">
            <div class="explore-container explore-page-container">
                <div class="explore-heading">
                    <h1>Encontrá el espacio para tu próxima creación</h1>
                    <p>Buscá por palabra clave y afiná por tipo, ubicación o precio.</p>
                </div>
                <div class="explore-search-stage">
                    <div class="marketplace-search" role="search" aria-label="Buscar espacios" data-search-box>
                        <label class="sr-only" for="results-search">Buscar por palabras clave</label>
                        <div class="search-input-wrap"><span class="search-icon" aria-hidden="true"></span><input id="results-search" class="explore-search-input" type="search" placeholder="¿Qué tipo de espacio estás buscando?" autocomplete="off" data-results-search-input /></div>
                        <button class="button button-primary search-submit explore-search-button" type="button" data-search-redirect data-search-target="ResultadosBusqueda.aspx">Buscar</button>
                    </div>
                </div>
            </div>
        </header>

        <div class="explore-content explore-container explore-page-container" id="filtros">
            <section class="filters-toolbar" aria-labelledby="filters-title">
                <div class="filters-toolbar-heading">
                    <span class="eyebrow" id="filters-title">Afiná tu búsqueda</span>
                    <span class="filters-hint">Elegí uno o varios criterios</span>
                </div>
                <div class="quick-filters" aria-label="Filtros rápidos">
                    <button class="filter-chip explore-filter-chip" type="button" aria-pressed="false" data-filter-chip data-filter-label="Tipo de espacio">Tipo de espacio</button>
                    <button class="filter-chip explore-filter-chip" type="button" aria-pressed="false" data-filter-chip data-filter-label="Ubicación">Ubicación</button>
                    <button class="filter-chip explore-filter-chip" type="button" aria-pressed="false" data-filter-chip data-filter-label="Disponibilidad">Disponibilidad</button>
                    <button class="filter-chip explore-filter-chip" type="button" aria-pressed="false" data-filter-chip data-filter-label="Capacidad">Capacidad</button>
                    <button class="filter-chip explore-filter-chip" type="button" aria-pressed="false" data-filter-chip data-filter-label="Precio">Precio</button>
                    <button class="more-filters-button explore-more-filters" type="button" data-filter-open><span class="sliders-icon" aria-hidden="true"></span>Más filtros</button>
                </div>
                <div class="active-filters" data-active-filters hidden>
                    <div class="active-filters-heading"><span>Filtros seleccionados</span><button class="text-button" type="button" data-clear-filters>Limpiar todos</button></div>
                    <div class="active-filter-list" data-active-filter-list aria-live="polite"></div>
                </div>
            </section>

            <div class="explore-quick-tools" aria-label="Herramientas para explorar">
            <asp:Panel ID="pnlComparacionServicios" runat="server" CssClass="service-compare-panel" aria-labelledby="service-compare-title">
                <details class="explore-tool-disclosure">
                    <summary>
                        <span class="explore-tool-symbol" aria-hidden="true">⇄</span>
                        <span class="explore-tool-summary"><strong id="service-compare-title">Compará servicios</strong><small>Herramienta opcional para contrastar hasta 3 categorías del catálogo.</small></span>
                        <span class="explore-tool-toggle" aria-hidden="true"></span>
                    </summary>
                    <div class="explore-tool-body">
                        <div class="service-compare-actions">
                            <div class="service-type-options" aria-label="Tipos de servicio disponibles">
                                <asp:Repeater ID="rptTiposServicio" runat="server">
                                    <ItemTemplate>
                                        <label class="service-type-option">
                                            <input type="checkbox" data-service-compare-checkbox value='<%#: Container.DataItem %>' />
                                            <span><%#: Container.DataItem %></span>
                                        </label>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                            <button class="button button-secondary button-small" type="button" data-service-compare-go disabled>Comparar servicios</button>
                        </div>
                        <span class="service-compare-status" data-service-compare-status aria-live="polite">Elegí al menos 2 servicios para comparar</span>
                    </div>
                </details>
            </asp:Panel>

            <asp:Panel ID="pnlRanking" runat="server" CssClass="ranking-panel" Visible="false">
                <details class="explore-tool-disclosure">
                    <summary>
                        <span class="explore-tool-symbol explore-tool-symbol-ranking" aria-hidden="true">★</span>
                        <span class="explore-tool-summary"><strong>Ranking de la comunidad</strong><small>Consultá los espacios mejor valorados cuando quieras.</small></span>
                        <span class="explore-tool-toggle" aria-hidden="true"></span>
                    </summary>
                    <div class="explore-tool-body">
                        <p class="ranking-criterio"><asp:Literal ID="litCriterioRanking" runat="server" /></p>
                        <ol class="ranking-lista">
                            <asp:Repeater ID="rptRanking" runat="server">
                                <ItemTemplate>
                                    <li class="ranking-item">
                                        <a href='<%# "DetalleEspacio.aspx?id=" + Eval("IdEspacioArtistico") %>'>
                                            <span class="ranking-posicion"><%# Container.ItemIndex + 1 %></span>
                                            <span class="ranking-datos">
                                                <strong><%#: Eval("NombreEspacio") %></strong>
                                                <span><%#: Eval("TipoEspacio") %> · <%#: ObtenerReputacion((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></span>
                                            </span>
                                        </a>
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ol>
                    </div>
                </details>
            </asp:Panel>
            </div>

            <section class="results-section" aria-labelledby="results-title">
                <div class="results-surface">
                    <div class="results-heading">
                        <h2 id="results-title">Espacios para explorar</h2>
                        <div class="results-sort">
                            <label for="<%= ddlOrden.ClientID %>">Ordenar por</label>
                            <asp:DropDownList ID="ddlOrden" runat="server" onchange="stageUpCambiarOrden(this.value);" />
                        </div>
                    </div>
                    <div class="results-grid" aria-live="polite" data-results-grid>
                        <asp:Repeater ID="rptEspaciosPublicados" runat="server">
                            <ItemTemplate>
                                <article class="space-card">
                                    <label class="space-card-compare">
                                        <input type="checkbox" data-compare-checkbox value='<%# Eval("IdEspacioArtistico") %>' />
                                        <span>Comparar</span>
                                    </label>
                                    <a class="space-card-link" href='<%# "DetalleEspacio.aspx?id=" + Eval("IdEspacioArtistico") %>'>
                                        <div class="space-card-media">
                                            <asp:Image ID="imgEspacio" runat="server" CssClass="space-card-image"
                                                AlternateText='<%# Eval("NombreEspacio") %>'
                                                ImageUrl='<%# ObtenerFoto((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %>'
                                                Visible='<%# !string.IsNullOrEmpty(ObtenerFoto((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem)) %>' />
                                            <asp:Panel ID="pnlSinFoto" runat="server" CssClass="space-card-photo-empty"
                                                Visible='<%# string.IsNullOrEmpty(ObtenerFoto((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem)) %>'>
                                                <span aria-hidden="true">◇</span>
                                                <small>Fotografía pendiente</small>
                                            </asp:Panel>
                                            <span class="space-card-rating"><%#: ObtenerReputacion((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></span>
                                        </div>
                                        <div class="space-card-body">
                                            <p class="space-card-location"><%#: ObtenerUbicacion((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></p>
                                            <h3><%#: Eval("NombreEspacio") %></h3>
                                            <div class="space-card-details">
                                                <span><%#: Eval("TipoEspacio") %></span>
                                                <span><%#: ObtenerCapacidad((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></span>
                                            </div>
                                            <p class="space-card-excerpt"><%# ObtenerResumen(Eval("Descripcion") as string) %></p>
                                            <p class="space-card-gestor"><%# ObtenerInfoGestor(Container.DataItem) %></p>
                                            <div class="space-card-footer">
                                                <span class="space-card-price"><%#: ObtenerPrecio((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></span>
                                                <span class="space-card-action">Ver detalle <span aria-hidden="true">→</span></span>
                                            </div>
                                        </div>
                                    </a>
                                </article>
                            </ItemTemplate>
                        </asp:Repeater>

                        <asp:Panel ID="pnlSinResultados" runat="server" CssClass="empty-state explore-empty-state">
                            <div class="empty-state-icon" aria-hidden="true"><span></span></div>
                            <h3><asp:Literal ID="litTituloSinResultados" runat="server" Text="Todavía no hay espacios para mostrar" /></h3>
                            <p><asp:Literal ID="litDescripcionSinResultados" runat="server" Text="Cuando existan espacios publicados, vas a poder encontrarlos acá según tus preferencias." /></p>
                        </asp:Panel>
                    </div>
                </div>
            </section>
        </div>

        <div class="compare-bar" data-compare-bar hidden aria-live="polite">
            <span data-compare-bar-text>0 espacios seleccionados</span>
            <div class="compare-bar-actions">
                <button class="text-button compare-bar-clear" type="button" data-compare-clear>Limpiar</button>
                <button class="button button-primary button-small" type="button" data-compare-go disabled>Comparar seleccionados</button>
            </div>
        </div>
    </section>

    <script type="text/javascript">


        function stageUpCambiarOrden(valor) {
            const params = new URLSearchParams(window.location.search);
            if (valor) { params.set("orden", valor); } else { params.delete("orden"); }
            const query = params.toString();
            window.location.href = window.location.pathname + (query ? "?" + query : "");
        }
    </script>

    <div class="filter-backdrop" data-filter-backdrop hidden></div>
    <aside class="filter-drawer" data-filter-panel hidden aria-labelledby="advanced-filters-title" aria-modal="true" role="dialog">
        <div class="filter-drawer-header"><div><span class="eyebrow">Combiná tus necesidades</span><h2 id="advanced-filters-title">Más filtros</h2></div><button class="icon-button" type="button" aria-label="Cerrar filtros" data-filter-close>×</button></div>
        <div class="filter-drawer-body">
            <section class="filter-group" aria-labelledby="general-filters-title">
                <div class="filter-group-heading"><h3 id="general-filters-title">Filtros generales</h3><p>Definí las condiciones básicas de tu búsqueda.</p></div>
                <div class="filter-form-grid">
                    <div class="form-field"><label for="filter-type">Tipo de espacio</label><select id="filter-type"><option value="">Seleccionar tipo</option><option value="Estudio">Estudio</option><option value="Sala">Sala</option><option value="Teatro">Teatro</option><option value="Salón de danza">Salón de danza</option></select></div>
                    <div class="form-field"><label for="filter-location">Ubicación general</label><input id="filter-location" type="text" placeholder="Ciudad, provincia, zona o dirección" /></div>
                    <div class="form-field"><label for="filter-province">Provincia</label><input id="filter-province" type="text" placeholder="Ej: Buenos Aires" /></div>
                    <div class="form-field"><label for="filter-city">Ciudad</label><input id="filter-city" type="text" placeholder="Ej: Vicente López" /></div>
                    <div class="form-field"><label for="filter-zone">Zona o barrio</label><input id="filter-zone" type="text" placeholder="Ej: Palermo" /></div>
                    <div class="form-field"><label for="filter-capacity">Capacidad mínima</label><input id="filter-capacity" type="number" min="1" placeholder="Cantidad de personas" /></div>
                    <div class="form-field"><label for="filter-price-min">Precio mínimo por hora</label><input id="filter-price-min" type="number" min="0" placeholder="Desde" /></div>
                    <div class="form-field"><label for="filter-price">Precio máximo por hora</label><input id="filter-price" type="number" min="0" placeholder="Hasta" /></div>
                    <div class="form-field"><label for="filter-rating">Calificación mínima</label><select id="filter-rating"><option value="">Cualquiera</option><option value="3">3 estrellas o más</option><option value="4">4 estrellas o más</option><option value="4.5">4,5 estrellas o más</option></select></div>
                    <div class="form-field"><label for="filter-availability">Fecha disponible</label><input id="filter-availability" type="date" /></div>
                    <div class="form-field"><label for="filter-time-from">Horario desde</label><input id="filter-time-from" type="time" step="1800" /></div>
                    <div class="form-field"><label for="filter-time-to">Horario hasta</label><input id="filter-time-to" type="time" step="1800" /></div>
                </div>
            </section>
            <section class="filter-group artistic-filter-group" aria-labelledby="artistic-filters-title">
                <div class="filter-group-heading"><span class="artistic-filter-badge">Diferencial StageUp</span><h3 id="artistic-filters-title">Características artísticas</h3><p>Combiná simultáneamente las condiciones necesarias para desarrollar tu actividad.</p></div>
                <div class="form-field"><label for="filter-floor">Tipo de piso</label><input id="filter-floor" type="text" placeholder="Ingresar tipo de piso" data-advanced-select /></div>
                <div class="form-field"><label for="filter-features">Otras características</label><input id="filter-features" type="text" placeholder="Ej: camarines, accesibilidad, reglas de uso" /></div>
                <div class="artistic-options">
                    <label class="feature-option"><input type="checkbox" value="Espejos" data-advanced-filter data-equip-code="ESPEJOS" /><span>Espejos</span></label>
                    <label class="feature-option"><input type="checkbox" value="Iluminación" data-advanced-filter data-equip-code="ILUMINACION" /><span>Iluminación</span></label>
                    <label class="feature-option"><input type="checkbox" value="Sonido / acústica" data-advanced-filter data-equip-code="SONIDO" /><span>Sonido / acústica</span></label>
                    <label class="feature-option"><input type="checkbox" value="Escenario" data-advanced-filter data-equip-code="ESCENARIO" /><span>Escenario</span></label>
                    <label class="feature-option"><input type="checkbox" value="Instrumentos" data-advanced-filter data-equip-code="INSTRUMENTOS" /><span>Instrumentos</span></label>
                    <label class="feature-option"><input type="checkbox" value="Equipamiento" data-advanced-filter data-equip-code="EQUIPAMIENTO" /><span>Equipamiento</span></label>
                </div>
                <div class="filter-combination-note"><strong>Una búsqueda, varios criterios.</strong><span>Por ejemplo: espejos + tipo de piso + sonido, o escenario + iluminación + capacidad.</span></div>
            </section>
        </div>
        <div class="filter-drawer-footer"><button class="text-button" type="button" data-clear-filters>Limpiar</button><button class="button button-primary" type="button" data-apply-filters>Aplicar filtros visuales</button></div>
    </aside>
</asp:Content>
