<%@ Page Title="Explorar espacios | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ResultadosBusqueda.aspx.cs" Inherits="StageUp.UI.Explorar.ResultadosBusqueda" %>

<asp:Content ID="ExploreContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="explore-page" data-results-page>
        <header class="explore-header">
            <div class="explore-container explore-page-container">
                <div class="explore-heading">
                    <span class="section-label">Explorar espacios</span>
                    <h1>Encontrá el escenario para tu próxima creación</h1>
                    <p>Buscá por palabras clave o combiná características para descubrir espacios alineados con tu actividad artística.</p>
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

            <section class="results-section" aria-labelledby="results-title">
                <div class="results-surface">
                    <div class="results-heading">
                        <h2 id="results-title">Espacios para explorar</h2>
                    </div>
                    <div class="results-grid" aria-live="polite" data-results-grid>
                        <div class="empty-state explore-empty-state">
                            <div class="empty-state-icon" aria-hidden="true"><span></span></div>
                            <h3>Todavía no hay espacios para mostrar</h3>
                            <p>Cuando existan espacios publicados, vas a poder encontrarlos acá según tus preferencias.</p>
                            <button class="button button-secondary" type="button" data-filter-open>Revisar filtros</button>
                        </div>
                    </div>
                </div>
                <template id="space-card-template">
                    <article class="space-card" data-space-card>
                        <a class="space-card-link" href="DetalleEspacio.aspx" data-space-link>
                            <div class="space-card-media">
                                <div class="space-card-image" data-space-image></div>
                                <span class="space-card-rating" data-space-rating></span>
                            </div>
                            <div class="space-card-body">
                                <p class="space-card-location" data-space-location></p>
                                <h3 data-space-name></h3>
                                <div class="space-card-footer">
                                    <span class="space-card-price" data-space-price></span>
                                    <span class="space-card-action">Ver detalle <span aria-hidden="true">→</span></span>
                                </div>
                            </div>
                        </a>
                    </article>
                </template>
            </section>
        </div>
    </section>

    <div class="filter-backdrop" data-filter-backdrop hidden></div>
    <aside class="filter-drawer" data-filter-panel hidden aria-labelledby="advanced-filters-title" aria-modal="true" role="dialog">
        <div class="filter-drawer-header"><div><span class="eyebrow">Combiná tus necesidades</span><h2 id="advanced-filters-title">Más filtros</h2></div><button class="icon-button" type="button" aria-label="Cerrar filtros" data-filter-close>×</button></div>
        <div class="filter-drawer-body">
            <section class="filter-group" aria-labelledby="general-filters-title">
                <div class="filter-group-heading"><h3 id="general-filters-title">Filtros generales</h3><p>Definí las condiciones básicas de tu búsqueda.</p></div>
                <div class="filter-form-grid">
                    <div class="form-field"><label for="filter-type">Tipo de espacio</label><select id="filter-type"><option value="">Seleccionar tipo</option><option value="Teatro">Teatro</option><option value="Salón de danza">Salón de danza</option><option value="Estudio">Estudio</option><option value="Sala de ensayo">Sala de ensayo</option></select></div>
                    <div class="form-field"><label for="filter-location">Ubicación</label><input id="filter-location" type="text" placeholder="Ciudad o zona" /></div>
                    <div class="form-field"><label for="filter-capacity">Capacidad mínima</label><input id="filter-capacity" type="number" min="1" placeholder="Cantidad de personas" /></div>
                    <div class="form-field"><label for="filter-price">Valor máximo de referencia</label><input id="filter-price" type="number" min="0" placeholder="Ingresar valor" /></div>
                    <div class="form-field form-field-wide"><label for="filter-availability">Disponibilidad</label><input id="filter-availability" type="date" /></div>
                </div>
            </section>
            <section class="filter-group artistic-filter-group" aria-labelledby="artistic-filters-title">
                <div class="filter-group-heading"><span class="artistic-filter-badge">Diferencial StageUp</span><h3 id="artistic-filters-title">Características artísticas</h3><p>Combiná simultáneamente las condiciones necesarias para desarrollar tu actividad.</p></div>
                <div class="form-field"><label for="filter-floor">Tipo de piso</label><input id="filter-floor" type="text" placeholder="Ingresar tipo de piso" data-advanced-select /></div>
                <div class="artistic-options">
                    <label class="feature-option"><input type="checkbox" value="Espejos" data-advanced-filter /><span>Espejos</span></label>
                    <label class="feature-option"><input type="checkbox" value="Iluminación" data-advanced-filter /><span>Iluminación</span></label>
                    <label class="feature-option"><input type="checkbox" value="Sonido / acústica" data-advanced-filter /><span>Sonido / acústica</span></label>
                    <label class="feature-option"><input type="checkbox" value="Escenario" data-advanced-filter /><span>Escenario</span></label>
                    <label class="feature-option"><input type="checkbox" value="Instrumentos" data-advanced-filter /><span>Instrumentos</span></label>
                    <label class="feature-option"><input type="checkbox" value="Equipamiento" data-advanced-filter /><span>Equipamiento</span></label>
                </div>
                <div class="filter-combination-note"><strong>Una búsqueda, varios criterios.</strong><span>Por ejemplo: espejos + tipo de piso + sonido, o escenario + iluminación + capacidad.</span></div>
            </section>
        </div>
        <div class="filter-drawer-footer"><button class="text-button" type="button" data-clear-filters>Limpiar</button><button class="button button-primary" type="button" data-apply-filters>Aplicar filtros visuales</button></div>
    </aside>
</asp:Content>
