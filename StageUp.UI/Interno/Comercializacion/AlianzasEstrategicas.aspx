<%@ Page Title="Alianzas estratégicas | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="AlianzasEstrategicas.aspx.cs" Inherits="StageUp.UI.Interno.Comercializacion.AlianzasEstrategicas" %>
<%@ Register Src="~/Interno/Comercializacion/ComercializacionTabs.ascx" TagPrefix="su" TagName="Solapas" %>

<asp:Content ID="AlianzasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comercialización</span>
            <h1><asp:Literal ID="litTitulo" runat="server" Text="Alianzas estratégicas" /></h1>
            <p><asp:Literal ID="litBajada" runat="server" Text="Vínculos institucionales y comerciales estables con escuelas, teatros, centros culturales, organismos y referentes del sector. Es un seguimiento interno, no un registro legal o contractual." /></p>
        </div>

        <su:Solapas ID="solapas" runat="server" />

        <div class="static-page-body internal-admin-stack comercial-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
                <asp:HyperLink ID="lnkDuplicada" runat="server" Visible="false" CssClass="text-link" Text="Ver la alianza existente" />
            </asp:Panel>


            <asp:Panel ID="pnlListado" runat="server" Visible="false">
                <div class="comercial-resumen" aria-label="Resumen de alianzas">
                    <a href="AlianzasEstrategicas.aspx"><span>En gestión</span><strong><asp:Literal ID="litEnGestion" runat="server" /></strong></a>
                    <a href="AlianzasEstrategicas.aspx?estado=Activa"><span>Activas</span><strong><asp:Literal ID="litActivas" runat="server" /></strong></a>
                    <a href="AlianzasEstrategicas.aspx?estado=EnEvaluacion"><span>En evaluación</span><strong><asp:Literal ID="litEnEvaluacion" runat="server" /></strong></a>
                    <div><span>Con la vigencia vencida</span><strong><asp:Literal ID="litVencidas" runat="server" /></strong></div>
                    <a href="AlianzasEstrategicas.aspx?vista=Historicas"><span>Finalizadas o dadas de baja</span><strong><asp:Literal ID="litHistoricas" runat="server" /></strong></a>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header comercial-toolbar">
                        <div>
                            <span class="admin-card-eyebrow">Gestión comercial</span>
                            <h2>Alianzas registradas</h2>
                        </div>
                        <div class="comercial-toolbar-acciones">
                            <a class="button button-primary" href="AlianzasEstrategicas.aspx?nuevo=1">Agregar alianza</a>
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiltros" runat="server" CssClass="comercial-filtros" DefaultButton="btnAplicarFiltros" role="search">
                        <div class="form-field">
                            <label for="<%= ddlFiltroVista.ClientID %>">Mostrar</label>
                            <asp:DropDownList ID="ddlFiltroVista" runat="server">
                                <asp:ListItem Value="EnGestion" Text="En gestión" />
                                <asp:ListItem Value="Historicas" Text="Finalizadas o dadas de baja" />
                                <asp:ListItem Value="Todas" Text="Todas" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                            <asp:DropDownList ID="ddlFiltroEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroTipo.ClientID %>">Tipo de institución</label>
                            <asp:DropDownList ID="ddlFiltroTipo" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroResponsable.ClientID %>">Responsable interno</label>
                            <asp:DropDownList ID="ddlFiltroResponsable" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFiltroDesde.ClientID %>">Vigente desde</label>
                            <asp:TextBox ID="txtFiltroDesde" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFiltroHasta.ClientID %>">Vigente hasta</label>
                            <asp:TextBox ID="txtFiltroHasta" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFiltroTexto.ClientID %>">Entidad u objetivo</label>
                            <asp:TextBox ID="txtFiltroTexto" runat="server" MaxLength="100" />
                        </div>
                        <div class="comercial-filtros-acciones">
                            <asp:Button ID="btnAplicarFiltros" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnAplicarFiltros_Click" />
                            <a class="text-link" href="AlianzasEstrategicas.aspx">Limpiar filtros</a>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlSinAlianzas" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No hay alianzas estratégicas cargadas</h3>
                        <p>Registrá la primera con "Agregar alianza".</p>
                    </asp:Panel>
                    <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No se encontraron alianzas para los criterios seleccionados</h3>
                        <p>Modificá los filtros o limpialos para volver al listado general.</p>
                    </asp:Panel>

                    <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptAlianzas" runat="server">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Entidad</th><th>Tipo de institución</th><th>Responsable</th><th>Inicio</th><th>Vigencia</th><th>Estado</th><th></th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr class='<%# (bool)Eval("EnGestion") ? "" : "comercial-fila-baja" %>'>
                                    <td data-label="Entidad"><strong><%#: Eval("NombreEntidad") %></strong><br /><small><%#: Resumir(Eval("ObjetivoComercial"), 80) %></small></td>
                                    <td data-label="Tipo de institución"><%#: TextoCatalogo("TipoInstitucion", Eval("TipoInstitucion")) %></td>
                                    <td data-label="Responsable"><%#: Eval("NombreResponsable") %></td>
                                    <td data-label="Inicio" class="comercial-num"><%#: Eval("FechaInicio", "{0:dd/MM/yyyy}") %></td>
                                    <td data-label="Vigencia"><%#: TextoHasta(Container.DataItem) %></td>
                                    <td data-label="Estado"><span class='<%# "comercial-badge " + StageUp.BLL.BLL_AlianzaEstrategica.ClaseEstado(Convert.ToString(Eval("EstadoAlianza"))) %>'><%#: StageUp.BLL.BLL_AlianzaEstrategica.TextoEstado(Convert.ToString(Eval("EstadoAlianza"))) %></span></td>
                                    <td><a class="admin-action-link admin-action-link-primary" href='<%#: "AlianzasEstrategicas.aspx?ver=" + Eval("IdAlianzaEstrategica") %>'>Ver detalle</a></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header comercial-detalle-cabecera">
                        <div>
                            <span class="admin-card-eyebrow">Detalle de alianza estratégica</span>
                            <h2><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></h2>
                        </div>
                        <asp:Label ID="lblEstado" runat="server" CssClass="comercial-badge" />
                    </div>

                    <asp:Panel ID="pnlAviso" runat="server" Visible="false" CssClass="comercial-aviso">
                        <asp:Literal ID="litAviso" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <dl class="comercial-datos">
                        <div><dt>Tipo de institución</dt><dd><asp:Literal ID="litTipo" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Responsable interno</dt><dd><asp:Literal ID="litResponsable" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Vigencia</dt><dd><asp:Literal ID="litVigencia" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Datos de contacto</dt><dd class="comercial-texto"><asp:Literal ID="litDatosContacto" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Objetivo comercial</dt><dd class="comercial-texto"><asp:Literal ID="litObjetivo" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Alcance</dt><dd class="comercial-texto"><asp:Literal ID="litAlcance" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Descripción del acuerdo</dt><dd class="comercial-texto"><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Beneficios o aportes</dt><dd class="comercial-texto"><asp:Literal ID="litBeneficios" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Observaciones</dt><dd class="comercial-texto"><asp:Literal ID="litObservaciones" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Fecha de alta</dt><dd><asp:Literal ID="litAlta" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Última actualización</dt><dd><asp:Literal ID="litActualizacion" runat="server" Mode="Encode" /></dd></div>
                    </dl>

                    <h3>Vínculos comerciales</h3>
                    <asp:Panel ID="pnlSinVinculos" runat="server" Visible="false">
                        <p class="comercial-lista-vacia">La alianza todavía no tiene contactos, campañas, contenido promocional ni orígenes asociados.</p>
                    </asp:Panel>
                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptVinculos" runat="server">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Tipo</th><th>Nombre</th><th>Estado</th><th>Usuarios registrados</th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Tipo"><%#: TextoTipoVinculo(Eval("Tipo")) %></td>
                                    <td data-label="Nombre"><%# EnlaceVinculo(Container.DataItem) %></td>
                                    <td data-label="Estado"><%#: TextoEstadoVinculo(Container.DataItem) %></td>
                                    <td data-label="Usuarios registrados" class="comercial-num"><%#: Eval("Cantidad") == null ? "—" : Eval("Cantidad").ToString() %></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>

                    <div class="comercial-acciones">
                        <asp:HyperLink ID="lnkEditar" runat="server" CssClass="button button-primary" Text="Editar alianza" />
                        <asp:HyperLink ID="lnkSeguimiento" runat="server" CssClass="button button-secondary" Text="Registrar seguimiento" />
                        <asp:HyperLink ID="lnkRenovar" runat="server" CssClass="button button-secondary" Text="Renovar alianza" />
                        <asp:HyperLink ID="lnkFinalizar" runat="server" CssClass="button button-secondary" Text="Finalizar alianza" />
                        <asp:HyperLink ID="lnkBaja" runat="server" CssClass="button button-secondary" Text="Dar de baja alianza" />
                        <a class="text-link" href="AlianzasEstrategicas.aspx">Volver al listado</a>
                    </div>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Seguimiento</span>
                        <h2>Interacciones, cambios de estado y renovaciones</h2>
                    </div>
                    <asp:Panel ID="pnlSinSeguimiento" runat="server" Visible="false">
                        <p class="comercial-lista-vacia">Todavía no hay seguimiento registrado para esta alianza.</p>
                    </asp:Panel>
                    <asp:Repeater ID="rptSeguimientos" runat="server">
                        <HeaderTemplate><ul class="comercial-historial"></HeaderTemplate>
                        <ItemTemplate>
                            <li>
                                <strong><%#: TituloSeguimiento(Container.DataItem) %></strong>
                                <div class="comercial-historial-meta"><%#: MetaSeguimiento(Container.DataItem) %></div>
                                <p class="comercial-texto"><%#: Eval("Observacion") %></p>
                                <%# DetalleSeguimiento(Container.DataItem) %>
                            </li>
                        </ItemTemplate>
                        <FooterTemplate></ul></FooterTemplate>
                    </asp:Repeater>

                    <asp:Panel ID="pnlVigencias" runat="server" Visible="false">
                        <h3>Vigencias anteriores</h3>
                        <div class="comercial-tabla-wrap">
                            <asp:Repeater ID="rptVigencias" runat="server">
                                <HeaderTemplate>
                                    <table class="comercial-tabla">
                                        <thead><tr><th>Período anterior</th><th>Nuevo período</th><th>Registrado</th><th>Observación</th></tr></thead>
                                        <tbody>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <tr>
                                        <td data-label="Período anterior" class="comercial-num"><%#: Periodo(Eval("FechaInicioAnterior"), Eval("FechaFinAnterior")) %></td>
                                        <td data-label="Nuevo período" class="comercial-num"><%#: Periodo(Eval("FechaInicioNueva"), Eval("FechaFinNueva")) %></td>
                                        <td data-label="Registrado"><%#: Eval("FechaRegistro", "{0:dd/MM/yyyy HH:mm}") %><br /><small><%#: Eval("NombreUsuario") %></small></td>
                                        <td data-label="Observación"><%#: TextoOGuion(Eval("Observacion")) %></td>
                                    </tr>
                                </ItemTemplate>
                                <FooterTemplate></tbody></table></FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlFormulario" runat="server" Visible="false" DefaultButton="btnGuardar">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow"><asp:Literal ID="litFormularioEyebrow" runat="server" Text="Nueva alianza" /></span>
                        <h2><asp:Literal ID="litFormularioTitulo" runat="server" Text="Registrar alianza estratégica" /></h2>
                        <p>Los campos con * son obligatorios.</p>
                    </div>

                    <asp:Panel ID="pnlSinCatalogos" runat="server" Visible="false" CssClass="comercial-aviso">
                        No se pudieron cargar los tipos de institución o los responsables, por eso no se puede guardar. Probá nuevamente en unos minutos.
                    </asp:Panel>

                    <div class="comercial-form-grid">
                        <div class="form-field">
                            <label for="<%= txtEntidad.ClientID %>">Entidad asociada *</label>
                            <asp:TextBox ID="txtEntidad" runat="server" MaxLength="200" placeholder="Ej: Escuela Municipal de Danza" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlTipo.ClientID %>">Tipo de institución *</label>
                            <asp:DropDownList ID="ddlTipo" runat="server" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtDatosContacto.ClientID %>">Datos de contacto *</label>
                            <asp:TextBox ID="txtDatosContacto" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500" placeholder="Referente, correo, teléfono, dirección" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlResponsable.ClientID %>">Responsable interno *</label>
                            <asp:DropDownList ID="ddlResponsable" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlEstado.ClientID %>">Estado *</label>
                            <asp:DropDownList ID="ddlEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtInicio.ClientID %>">Fecha de inicio *</label>
                            <asp:TextBox ID="txtInicio" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFin.ClientID %>">Fecha de finalización</label>
                            <asp:TextBox ID="txtFin" runat="server" TextMode="Date" />
                            <p class="comercial-ayuda">Dejala vacía si la alianza no tiene fecha de cierre definida.</p>
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtObjetivo.ClientID %>">Objetivo comercial *</label>
                            <asp:TextBox ID="txtObjetivo" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtAlcance.ClientID %>">Alcance *</label>
                            <asp:TextBox ID="txtAlcance" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtDescripcion.ClientID %>">Descripción del acuerdo *</label>
                            <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtBeneficios.ClientID %>">Beneficios o aportes asociados *</label>
                            <asp:TextBox ID="txtBeneficios" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000" placeholder="Difusión institucional, recomendación de la plataforma, incorporación de espacios…" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtObservaciones.ClientID %>"><asp:Literal ID="litEtiquetaObservaciones" runat="server" Text="Observaciones iniciales" /></label>
                            <asp:TextBox ID="txtObservaciones" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000" />
                        </div>
                        <asp:Panel ID="pnlObservacionCambio" runat="server" Visible="false" CssClass="form-field comercial-campo-ancho">
                            <label for="<%= txtObservacionCambio.ClientID %>">Si cambiás el estado, contá por qué</label>
                            <asp:TextBox ID="txtObservacionCambio" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000" />
                            <p class="comercial-ayuda">Queda en el seguimiento de la alianza con el estado anterior y el nuevo.</p>
                        </asp:Panel>
                    </div>

                    <h3>Vínculos comerciales <small>(opcionales)</small></h3>
                    <p class="comercial-ayuda">Un contacto, una pieza promocional o un origen pertenecen a una sola alianza: si ya estaban en otra, pasan a esta. Las campañas pueden compartir varias alianzas.</p>
                    <div class="comercial-vinculos-grid">
                        <fieldset>
                            <legend>Contactos comerciales</legend>
                            <asp:CheckBoxList ID="cblContactos" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                            <asp:Literal ID="litSinContactos" runat="server" Visible="false" Text="<p class='comercial-lista-vacia'>No hay contactos comerciales cargados.</p>" />
                        </fieldset>
                        <fieldset>
                            <legend>Campañas comerciales</legend>
                            <asp:CheckBoxList ID="cblCampanias" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                            <asp:Literal ID="litSinCampanias" runat="server" Visible="false" Text="<p class='comercial-lista-vacia'>No hay campañas disponibles para asociar.</p>" />
                        </fieldset>
                        <fieldset>
                            <legend>Contenido promocional</legend>
                            <asp:CheckBoxList ID="cblContenidos" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                            <asp:Literal ID="litSinContenidos" runat="server" Visible="false" Text="<p class='comercial-lista-vacia'>No hay piezas promocionales vigentes para asociar.</p>" />
                        </fieldset>
                        <fieldset>
                            <legend>Orígenes de usuario</legend>
                            <asp:CheckBoxList ID="cblOrigenes" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                            <asp:Literal ID="litSinOrigenes" runat="server" Visible="false" Text="<p class='comercial-lista-vacia'>No hay orígenes de usuario activos.</p>" />
                        </fieldset>
                    </div>

                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar alianza" CausesValidation="false" OnClick="btnGuardar_Click" />
                        <asp:HyperLink ID="lnkCancelar" runat="server" CssClass="button button-secondary" Text="Cancelar" NavigateUrl="AlianzasEstrategicas.aspx" />
                    </div>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlSeguimiento" runat="server" Visible="false" DefaultButton="btnGuardarSeguimiento">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Nueva observación de seguimiento</span>
                        <h2><asp:Literal ID="litSeguimientoTitulo" runat="server" Mode="Encode" /></h2>
                        <p><asp:Literal ID="litSeguimientoEstado" runat="server" Mode="Encode" /></p>
                    </div>
                    <div class="comercial-form-grid">
                        <div class="form-field">
                            <label for="<%= txtFechaInteraccion.ClientID %>">Fecha de interacción *</label>
                            <asp:TextBox ID="txtFechaInteraccion" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlTipoInteraccion.ClientID %>">Tipo de interacción *</label>
                            <asp:DropDownList ID="ddlTipoInteraccion" runat="server" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtObservacion.ClientID %>">Observación *</label>
                            <asp:TextBox ID="txtObservacion" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtAvance.ClientID %>">Avance obtenido o compromisos asumidos</label>
                            <asp:TextBox ID="txtAvance" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtAccionPendiente.ClientID %>">Acción pendiente</label>
                            <asp:TextBox ID="txtAccionPendiente" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlAsignado.ClientID %>">Responsable asignado</label>
                            <asp:DropDownList ID="ddlAsignado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlEstadoResultante.ClientID %>">Estado resultante</label>
                            <asp:DropDownList ID="ddlEstadoResultante" runat="server" />
                        </div>
                    </div>
                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardarSeguimiento" runat="server" CssClass="button button-primary" Text="Guardar seguimiento" CausesValidation="false" OnClick="btnGuardarSeguimiento_Click" />
                        <asp:HyperLink ID="lnkCancelarSeguimiento" runat="server" CssClass="button button-secondary" Text="Cancelar" />
                    </div>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlRenovacion" runat="server" Visible="false" DefaultButton="btnGuardarRenovacion">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow"><asp:Literal ID="litRenovacionEyebrow" runat="server" Text="Renovación" /></span>
                        <h2><asp:Literal ID="litRenovacionTitulo" runat="server" Mode="Encode" /></h2>
                        <p><asp:Literal ID="litVigenciaActual" runat="server" Mode="Encode" /></p>
                    </div>
                    <div class="comercial-form-grid">
                        <div class="form-field">
                            <label for="<%= txtRenovInicio.ClientID %>">Inicio del nuevo período *</label>
                            <asp:TextBox ID="txtRenovInicio" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtRenovFin.ClientID %>">Nueva fecha de finalización</label>
                            <asp:TextBox ID="txtRenovFin" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlRenovResponsable.ClientID %>">Responsable interno</label>
                            <asp:DropDownList ID="ddlRenovResponsable" runat="server" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtRenovAlcance.ClientID %>">Alcance</label>
                            <asp:TextBox ID="txtRenovAlcance" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtRenovBeneficios.ClientID %>">Beneficios o aportes</label>
                            <asp:TextBox ID="txtRenovBeneficios" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtRenovObservacion.ClientID %>">Observación</label>
                            <asp:TextBox ID="txtRenovObservacion" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                    </div>
                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardarRenovacion" runat="server" CssClass="button button-primary" Text="Guardar renovación" CausesValidation="false" OnClick="btnGuardarRenovacion_Click" />
                        <asp:HyperLink ID="lnkCancelarRenovacion" runat="server" CssClass="button button-secondary" Text="Cancelar" />
                    </div>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlCierre" runat="server" Visible="false">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow"><asp:Literal ID="litCierreEyebrow" runat="server" /></span>
                        <h2><asp:Literal ID="litCierreTitulo" runat="server" Mode="Encode" /></h2>
                    </div>
                    <div class="comercial-aviso"><asp:Literal ID="litCierreAviso" runat="server" Mode="Encode" /></div>
                    <div class="form-field">
                        <label for="<%= txtMotivo.ClientID %>"><asp:Literal ID="litEtiquetaMotivo" runat="server" /></label>
                        <asp:TextBox ID="txtMotivo" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
                    </div>
                    <div class="comercial-acciones">
                        <asp:Button ID="btnConfirmarCierre" runat="server" CssClass="button button-primary" CausesValidation="false" OnClick="btnConfirmarCierre_Click" />
                        <asp:HyperLink ID="lnkCancelarCierre" runat="server" CssClass="button button-secondary" Text="Cancelar" />
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
