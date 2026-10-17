<%@ Page Title="Campañas comerciales | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="CampaniasComerciales.aspx.cs" Inherits="StageUp.UI.Interno.Comercializacion.CampaniasComerciales" %>
<%@ Register Src="~/Interno/Comercializacion/ComercializacionTabs.ascx" TagPrefix="su" TagName="Solapas" %>

<asp:Content ID="CampaniasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comercialización</span>
            <h1><asp:Literal ID="litTitulo" runat="server" Text="Campañas comerciales" /></h1>
            <p><asp:Literal ID="litBajada" runat="server" Text="Organizá las acciones de difusión y captación de StageUp: a quién apuntan, por qué canal, cuándo y con qué contactos, alianzas, piezas y orígenes se relacionan." /></p>
        </div>

        <su:Solapas ID="solapas" runat="server" />

        <div class="static-page-body internal-admin-stack comercial-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- CU-004-001 pasos 2 a 7, A2, A3 y A4. --%>
            <asp:Panel ID="pnlListado" runat="server" Visible="false">
                <div class="comercial-resumen" aria-label="Resumen de campañas">
                    <a href="CampaniasComerciales.aspx"><span>En gestión</span><strong><asp:Literal ID="litActivas" runat="server" /></strong></a>
                    <a href="CampaniasComerciales.aspx?estado=Activa"><span>Activas</span><strong><asp:Literal ID="litEnCurso" runat="server" /></strong></a>
                    <a href="CampaniasComerciales.aspx?estado=Planificada"><span>Planificadas</span><strong><asp:Literal ID="litPlanificadas" runat="server" /></strong></a>
                    <a href="CampaniasComerciales.aspx?vista=Bajas"><span>Dadas de baja</span><strong><asp:Literal ID="litBajas" runat="server" /></strong></a>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header comercial-toolbar">
                        <div>
                            <span class="admin-card-eyebrow">Gestión comercial</span>
                            <h2>Campañas registradas</h2>
                        </div>
                        <div class="comercial-toolbar-acciones">
                            <a class="button button-secondary" href="CampaniasComerciales.aspx?vista=origenes">Orígenes de usuario</a>
                            <a class="button button-primary" href="CampaniasComerciales.aspx?nuevo=1">Agregar campaña</a>
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiltros" runat="server" CssClass="comercial-filtros" DefaultButton="btnAplicarFiltros" role="search">
                        <div class="form-field">
                            <label for="<%= ddlFiltroVista.ClientID %>">Mostrar</label>
                            <asp:DropDownList ID="ddlFiltroVista" runat="server">
                                <asp:ListItem Value="Activas" Text="En gestión (sin las dadas de baja)" />
                                <asp:ListItem Value="Bajas" Text="Dadas de baja" />
                                <asp:ListItem Value="Todas" Text="Todas" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                            <asp:DropDownList ID="ddlFiltroEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroCanal.ClientID %>">Canal de difusión</label>
                            <asp:DropDownList ID="ddlFiltroCanal" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroPublico.ClientID %>">Público objetivo</label>
                            <asp:DropDownList ID="ddlFiltroPublico" runat="server" />
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
                            <label for="<%= txtFiltroTexto.ClientID %>">Nombre o descripción</label>
                            <asp:TextBox ID="txtFiltroTexto" runat="server" MaxLength="100" />
                        </div>
                        <div class="comercial-filtros-acciones">
                            <asp:Button ID="btnAplicarFiltros" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnAplicarFiltros_Click" />
                            <a class="text-link" href="CampaniasComerciales.aspx">Limpiar filtros</a>
                        </div>
                    </asp:Panel>

                    <%-- A2 --%>
                    <asp:Panel ID="pnlSinCampanias" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No hay campañas comerciales cargadas</h3>
                        <p>Registrá la primera con "Agregar campaña".</p>
                    </asp:Panel>
                    <%-- A4 --%>
                    <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No se encontraron campañas para los criterios seleccionados</h3>
                        <p>Modificá los filtros o limpialos para volver al listado general.</p>
                    </asp:Panel>

                    <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptCampanias" runat="server">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Campaña</th><th>Público objetivo</th><th>Canal</th><th>Vigencia</th><th>Estado</th><th>Vínculos</th><th></th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr class='<%# (bool)Eval("Activa") ? "" : "comercial-fila-baja" %>'>
                                    <td data-label="Campaña"><strong><%#: Eval("Nombre") %></strong><br /><small><%#: Resumir(Eval("Descripcion"), 90) %></small></td>
                                    <td data-label="Público objetivo"><%#: TextoPublico(Container.DataItem) %></td>
                                    <td data-label="Canal"><%#: TextoCatalogo("CanalDifusion", Eval("CanalDifusion")) %></td>
                                    <td data-label="Vigencia" class="comercial-num"><%#: Eval("FechaInicio", "{0:dd/MM/yyyy}") %><br /><small>al <%#: Eval("FechaFin", "{0:dd/MM/yyyy}") %></small></td>
                                    <td data-label="Estado"><span class='<%# "comercial-badge " + ClaseEstado(Container.DataItem) %>'><%#: TextoEstado(Container.DataItem) %></span></td>
                                    <td data-label="Vínculos"><small><%#: TextoVinculos(Container.DataItem) %></small></td>
                                    <td><a class="admin-action-link admin-action-link-primary" href='<%#: "CampaniasComerciales.aspx?ver=" + Eval("IdCampaniaComercial") %>'>Ver detalle</a></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </asp:Panel>

            <%-- A5, A7, A8 y A9 --%>
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header comercial-detalle-cabecera">
                        <div>
                            <span class="admin-card-eyebrow">Detalle de campaña comercial</span>
                            <h2><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></h2>
                        </div>
                        <asp:Label ID="lblEstado" runat="server" CssClass="comercial-badge" />
                    </div>

                    <asp:Panel ID="pnlAvisoEstado" runat="server" Visible="false" CssClass="comercial-aviso">
                        <asp:Literal ID="litAvisoEstado" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <dl class="comercial-datos">
                        <div class="comercial-dato-ancho"><dt>Descripción</dt><dd class="comercial-texto"><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Público objetivo</dt><dd><asp:Literal ID="litPublico" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Canal de difusión</dt><dd><asp:Literal ID="litCanal" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Fecha de inicio</dt><dd><asp:Literal ID="litInicio" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Fecha de finalización</dt><dd><asp:Literal ID="litFin" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Período de vigencia</dt><dd><asp:Literal ID="litPeriodo" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Registrada por</dt><dd><asp:Literal ID="litResponsable" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Alta / última modificación</dt><dd><asp:Literal ID="litFechas" runat="server" Mode="Encode" /></dd></div>
                    </dl>

                    <h3>Vínculos comerciales</h3>
                    <asp:Panel ID="pnlSinVinculos" runat="server" Visible="false">
                        <p class="comercial-lista-vacia">La campaña todavía no tiene contactos, alianzas, contenido promocional ni orígenes asociados.</p>
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
                        <asp:HyperLink ID="lnkEditar" runat="server" CssClass="button button-primary" Text="Editar campaña" />
                        <asp:Button ID="btnDarDeBaja" runat="server" CssClass="button button-secondary" Text="Dar de baja campaña" CausesValidation="false"
                            OnClick="btnDarDeBaja_Click"
                            OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, 'La campaña se va a retirar de la gestión comercial activa, pero conserva su información histórica para consulta interna y análisis posterior. ¿Confirmás la baja?', { title: 'Dar de baja campaña', confirmText: 'Sí, dar de baja', cancelText: 'Volver' })) return false;" />
                        <a class="text-link" href="CampaniasComerciales.aspx">Volver al listado</a>
                    </div>
                </div>
            </asp:Panel>

            <%-- Pasos 8 a 26 (alta) y A6 (edición), con A10 a A17 --%>
            <asp:Panel ID="pnlFormulario" runat="server" Visible="false" DefaultButton="btnGuardar">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow"><asp:Literal ID="litFormularioEyebrow" runat="server" Text="Nueva campaña" /></span>
                        <h2><asp:Literal ID="litFormularioTitulo" runat="server" Text="Registrar campaña comercial" /></h2>
                        <p>Los campos con * son obligatorios.</p>
                    </div>

                    <asp:Panel ID="pnlSinCatalogos" runat="server" Visible="false" CssClass="comercial-aviso">
                        No se pudieron cargar los públicos, canales y estados definidos para las campañas, por eso no se puede guardar. Probá nuevamente en unos minutos.
                    </asp:Panel>

                    <div class="comercial-form-grid">
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtNombre.ClientID %>">Nombre identificatorio *</label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="150" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtDescripcion.ClientID %>">Descripción *</label>
                            <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" />
                            <p class="comercial-ayuda">Qué busca la campaña: difusión del servicio, captación de solicitantes, incorporación de gestores o posicionamiento.</p>
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <span class="form-label">Público objetivo * <small>(uno o más)</small></span>
                            <asp:CheckBoxList ID="cblPublico" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlCanal.ClientID %>">Canal de difusión *</label>
                            <asp:DropDownList ID="ddlCanal" runat="server" />
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
                            <label for="<%= txtFin.ClientID %>">Fecha de finalización *</label>
                            <asp:TextBox ID="txtFin" runat="server" TextMode="Date" />
                        </div>
                    </div>

                    <h3>Vínculos comerciales <small>(opcionales)</small></h3>
                    <p class="comercial-ayuda">Asociá la campaña con lo que permite seguir su impacto. Un contacto, una pieza promocional o un origen pertenecen a una sola campaña: si ya estaban en otra, pasan a esta.</p>
                    <div class="comercial-vinculos-grid">
                        <fieldset>
                            <legend>Contactos comerciales</legend>
                            <asp:CheckBoxList ID="cblContactos" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                            <asp:Literal ID="litSinContactos" runat="server" Visible="false" Text="<p class='comercial-lista-vacia'>No hay contactos comerciales cargados.</p>" />
                        </fieldset>
                        <fieldset>
                            <legend>Alianzas estratégicas</legend>
                            <asp:CheckBoxList ID="cblAlianzas" runat="server" RepeatLayout="UnorderedList" CssClass="comercial-opciones" />
                            <asp:Literal ID="litSinAlianzas" runat="server" Visible="false" Text="<p class='comercial-lista-vacia'>No hay alianzas disponibles para asociar.</p>" />
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
                        <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar campaña" CausesValidation="false" OnClick="btnGuardar_Click" />
                        <asp:HyperLink ID="lnkCancelar" runat="server" CssClass="button button-secondary" Text="Cancelar" NavigateUrl="CampaniasComerciales.aspx" />
                    </div>
                </div>
            </asp:Panel>

            <%-- Orígenes de usuario (canales de captación que usan las métricas de CU-004-004) --%>
            <asp:Panel ID="pnlOrigenes" runat="server" Visible="false">
                <div class="auth-card admin-list-card">
                    <div class="auth-card-header comercial-toolbar">
                        <div>
                            <span class="admin-card-eyebrow">Canales de captación</span>
                            <h2>Orígenes de usuario</h2>
                            <p>Cada cuenta nueva guarda por dónde llegó: por un enlace con <code>?origen=código</code> (por ejemplo el de una campaña) o por lo que la persona elige en "¿Cómo conociste StageUp?" al registrarse.</p>
                        </div>
                        <div class="comercial-toolbar-acciones">
                            <a class="button button-secondary" href="CampaniasComerciales.aspx">Volver a campañas</a>
                            <a class="button button-primary" href="CampaniasComerciales.aspx?vista=origenes&amp;origen=nuevo">Agregar origen</a>
                        </div>
                    </div>

                    <asp:Panel ID="pnlSinOrigenesListado" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No hay orígenes de usuario cargados</h3>
                    </asp:Panel>

                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptOrigenes" runat="server" OnItemCommand="rptOrigenes_ItemCommand">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Origen</th><th>Canal</th><th>Vinculado a</th><th>En el registro</th><th>Usuarios</th><th>Contactos</th><th>Estado</th><th></th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr class='<%# (bool)Eval("Activo") ? "" : "comercial-fila-baja" %>'>
                                    <td data-label="Origen"><strong><%#: Eval("NombreOrigen") %></strong><br /><small class="comercial-enlace"><%#: EnlaceOrigen(Eval("Codigo")) %></small></td>
                                    <td data-label="Canal"><%#: TextoCatalogo("CanalOrigen", Eval("CanalOrigen")) %></td>
                                    <td data-label="Vinculado a"><%#: TextoOGuion(Eval("NombreCampania") ?? Eval("NombreAlianza")) %></td>
                                    <td data-label="En el registro"><%#: (bool)Eval("VisibleEnRegistro") ? "Sí" : "No" %></td>
                                    <td data-label="Usuarios" class="comercial-num"><%#: Eval("CantidadUsuarios") %></td>
                                    <td data-label="Contactos" class="comercial-num"><%#: Eval("CantidadContactos") %></td>
                                    <td data-label="Estado"><span class='<%# (bool)Eval("Activo") ? "comercial-badge comercial-badge-activa" : "comercial-badge comercial-badge-baja" %>'><%#: (bool)Eval("Activo") ? "Activo" : "Dado de baja" %></span></td>
                                    <td>
                                        <a class="admin-action-link admin-action-link-primary" href='<%#: "CampaniasComerciales.aspx?vista=origenes&origen=" + Eval("IdOrigenUsuario") %>'>Editar</a>
                                        <asp:LinkButton runat="server" CssClass="admin-action-link" CommandName="CambiarActivo"
                                            CommandArgument='<%# Eval("IdOrigenUsuario") + "|" + ((bool)Eval("Activo") ? "0" : "1") %>'
                                            Text='<%# (bool)Eval("Activo") ? "Dar de baja" : "Reactivar" %>' CausesValidation="false" />
                                    </td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlFormOrigen" runat="server" Visible="false" DefaultButton="btnGuardarOrigen">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Origen de usuario</span>
                        <h2><asp:Literal ID="litTituloOrigen" runat="server" Text="Nuevo origen" /></h2>
                    </div>
                    <div class="comercial-form-grid">
                        <div class="form-field">
                            <label for="<%= txtNombreOrigen.ClientID %>">Nombre *</label>
                            <asp:TextBox ID="txtNombreOrigen" runat="server" MaxLength="150" placeholder="Ej: Campaña de primavera en Instagram" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCodigoOrigen.ClientID %>">Código del enlace</label>
                            <asp:TextBox ID="txtCodigoOrigen" runat="server" MaxLength="40" placeholder="Se arma solo a partir del nombre" />
                            <p class="comercial-ayuda">Minúsculas, números y guiones. Va en los enlaces: <span class="comercial-enlace">?origen=codigo</span></p>
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlCanalOrigen.ClientID %>">Canal de origen *</label>
                            <asp:DropDownList ID="ddlCanalOrigen" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlCampaniaOrigen.ClientID %>">Campaña comercial</label>
                            <asp:DropDownList ID="ddlCampaniaOrigen" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlAlianzaOrigen.ClientID %>">Alianza estratégica</label>
                            <asp:DropDownList ID="ddlAlianzaOrigen" runat="server" />
                        </div>
                        <div class="form-field">
                            <span class="form-label">Registro</span>
                            <asp:CheckBox ID="chkVisibleOrigen" runat="server" CssClass="comercial-check" Text="Ofrecerlo en &quot;¿Cómo conociste StageUp?&quot;" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtDescripcionOrigen.ClientID %>">Descripción</label>
                            <asp:TextBox ID="txtDescripcionOrigen" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                    </div>
                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardarOrigen" runat="server" CssClass="button button-primary" Text="Guardar origen" CausesValidation="false" OnClick="btnGuardarOrigen_Click" />
                        <a class="button button-secondary" href="CampaniasComerciales.aspx?vista=origenes">Cancelar</a>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
