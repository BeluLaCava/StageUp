<%@ Page Title="Contactos comerciales | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="ContactosComerciales.aspx.cs" Inherits="StageUp.UI.Interno.Comercializacion.ContactosComerciales" %>
<%@ Register Src="~/Interno/Comercializacion/ComercializacionTabs.ascx" TagPrefix="su" TagName="Solapas" %>

<asp:Content ID="ContactosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comercialización</span>
            <h1><asp:Literal ID="litTitulo" runat="server" Text="Contactos comerciales" /></h1>
            <p><asp:Literal ID="litBajada" runat="server" Text="Personas, instituciones y referentes vinculados a la captación y expansión de StageUp, con el seguimiento de cada uno. Un contacto no es una cuenta de StageUp." /></p>
        </div>

        <su:Solapas ID="solapas" runat="server" />

        <div class="static-page-body internal-admin-stack comercial-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
                <asp:HyperLink ID="lnkDuplicado" runat="server" Visible="false" CssClass="text-link" Text="Ver el contacto existente" />
            </asp:Panel>

            <%-- CU-004-002 pasos 2 a 8, A2, A3 y A4 --%>
            <asp:Panel ID="pnlListado" runat="server" Visible="false">
                <div class="comercial-resumen" aria-label="Resumen de contactos por estado">
                    <a href="ContactosComerciales.aspx"><span>Total</span><strong><asp:Literal ID="litTotal" runat="server" /></strong></a>
                    <a href="ContactosComerciales.aspx?estado=Pendiente"><span>Pendientes</span><strong><asp:Literal ID="litPendientes" runat="server" /></strong></a>
                    <a href="ContactosComerciales.aspx?estado=Contactado"><span>Contactados</span><strong><asp:Literal ID="litContactados" runat="server" /></strong></a>
                    <a href="ContactosComerciales.aspx?estado=Interesado"><span>Interesados</span><strong><asp:Literal ID="litInteresados" runat="server" /></strong></a>
                    <a href="ContactosComerciales.aspx?estado=Rechazado"><span>Rechazados</span><strong><asp:Literal ID="litRechazados" runat="server" /></strong></a>
                    <a href="ContactosComerciales.aspx?estado=Incorporado"><span>Incorporados</span><strong><asp:Literal ID="litIncorporados" runat="server" /></strong></a>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header comercial-toolbar">
                        <div>
                            <span class="admin-card-eyebrow">Gestión comercial</span>
                            <h2>Contactos registrados</h2>
                        </div>
                        <div class="comercial-toolbar-acciones">
                            <a class="button button-primary" href="ContactosComerciales.aspx?nuevo=1">Agregar contacto</a>
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiltros" runat="server" CssClass="comercial-filtros" DefaultButton="btnAplicarFiltros" role="search">
                        <div class="form-field">
                            <label for="<%= ddlFiltroEstado.ClientID %>">Estado comercial</label>
                            <asp:DropDownList ID="ddlFiltroEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroTipo.ClientID %>">Tipo de entidad</label>
                            <asp:DropDownList ID="ddlFiltroTipo" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroOrigen.ClientID %>">Origen del contacto</label>
                            <asp:DropDownList ID="ddlFiltroOrigen" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroCampania.ClientID %>">Campaña asociada</label>
                            <asp:DropDownList ID="ddlFiltroCampania" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFiltroTexto.ClientID %>">Nombre, entidad o correo</label>
                            <asp:TextBox ID="txtFiltroTexto" runat="server" MaxLength="100" />
                        </div>
                        <div class="comercial-filtros-acciones">
                            <asp:Button ID="btnAplicarFiltros" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnAplicarFiltros_Click" />
                            <a class="text-link" href="ContactosComerciales.aspx">Limpiar filtros</a>
                        </div>
                    </asp:Panel>

                    <%-- A2 --%>
                    <asp:Panel ID="pnlSinContactos" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No hay contactos comerciales cargados</h3>
                        <p>Registrá el primero con "Agregar contacto".</p>
                    </asp:Panel>
                    <%-- A4 --%>
                    <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No se encontraron contactos para los criterios seleccionados</h3>
                        <p>Modificá los filtros o limpialos para volver al listado general.</p>
                    </asp:Panel>

                    <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptContactos" runat="server">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Contacto</th><th>Tipo de entidad</th><th>Medio de contacto</th><th>Estado comercial</th><th>Última actualización</th><th></th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Contacto"><strong><%#: Eval("NombreCompleto") %></strong><asp:PlaceHolder runat="server" Visible='<%# Eval("EntidadRepresentada") != null %>'><br /><small><%#: Eval("EntidadRepresentada") %></small></asp:PlaceHolder></td>
                                    <td data-label="Tipo de entidad"><%#: TextoCatalogo("TipoEntidad", Eval("TipoEntidad")) %></td>
                                    <td data-label="Medio de contacto"><%#: TextoCatalogo("MedioContacto", Eval("MedioContacto")) %><br /><small><%#: DatoPrincipal(Container.DataItem) %></small></td>
                                    <td data-label="Estado comercial"><span class='<%# "comercial-badge " + StageUp.BLL.BLL_ContactoComercial.ClaseEstado(Convert.ToString(Eval("EstadoContacto"))) %>'><%#: StageUp.BLL.BLL_ContactoComercial.TextoEstado(Convert.ToString(Eval("EstadoContacto"))) %></span></td>
                                    <td data-label="Última actualización" class="comercial-num"><%#: Eval("FechaUltimaModificacion", "{0:dd/MM/yyyy}") %><br /><small><%#: Eval("FechaUltimaModificacion", "{0:HH:mm}") %> hs</small></td>
                                    <td><a class="admin-action-link admin-action-link-primary" href='<%#: "ContactosComerciales.aspx?ver=" + Eval("IdContactoComercial") %>'>Ver detalle</a></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </asp:Panel>

            <%-- A5 y A9 --%>
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header comercial-detalle-cabecera">
                        <div>
                            <span class="admin-card-eyebrow">Detalle de contacto comercial</span>
                            <h2><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></h2>
                        </div>
                        <asp:Label ID="lblEstado" runat="server" CssClass="comercial-badge" />
                    </div>

                    <asp:Panel ID="pnlAvisoIncorporado" runat="server" Visible="false" CssClass="comercial-aviso">
                        <asp:Literal ID="litAvisoIncorporado" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <dl class="comercial-datos">
                        <div><dt>Tipo de entidad</dt><dd><asp:Literal ID="litTipo" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Entidad que representa</dt><dd><asp:Literal ID="litEntidad" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Medio de contacto</dt><dd><asp:Literal ID="litMedio" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Correo electrónico</dt><dd><asp:Literal ID="litCorreo" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Teléfono</dt><dd><asp:Literal ID="litTelefono" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Otro dato de contacto</dt><dd><asp:Literal ID="litOtroDato" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Origen del contacto</dt><dd><asp:Literal ID="litOrigen" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Campaña comercial</dt><dd><asp:HyperLink ID="lnkCampania" runat="server" CssClass="text-link" /><asp:Literal ID="litSinCampania" runat="server" Text="—" /></dd></div>
                        <div><dt>Alianza estratégica</dt><dd><asp:Literal ID="litAlianza" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Cuenta en StageUp</dt><dd><asp:Literal ID="litCuenta" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Fecha de alta</dt><dd><asp:Literal ID="litAlta" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Última actualización</dt><dd><asp:Literal ID="litActualizacion" runat="server" Mode="Encode" /></dd></div>
                        <div class="comercial-dato-ancho"><dt>Observaciones</dt><dd class="comercial-texto"><asp:Literal ID="litObservaciones" runat="server" Mode="Encode" /></dd></div>
                    </dl>

                    <div class="comercial-acciones">
                        <asp:HyperLink ID="lnkEditar" runat="server" CssClass="button button-primary" Text="Editar contacto" />
                        <asp:HyperLink ID="lnkSeguimiento" runat="server" CssClass="button button-secondary" Text="Registrar seguimiento" />
                        <a class="text-link" href="ContactosComerciales.aspx">Volver al listado</a>
                    </div>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Seguimiento</span>
                        <h2>Interacciones y cambios de estado</h2>
                    </div>
                    <asp:Panel ID="pnlSinSeguimiento" runat="server" Visible="false">
                        <p class="comercial-lista-vacia">Todavía no hay seguimientos registrados para este contacto.</p>
                    </asp:Panel>
                    <asp:Repeater ID="rptSeguimientos" runat="server">
                        <HeaderTemplate><ul class="comercial-historial"></HeaderTemplate>
                        <ItemTemplate>
                            <li>
                                <strong><%#: TituloSeguimiento(Container.DataItem) %></strong>
                                <div class="comercial-historial-meta"><%#: MetaSeguimiento(Container.DataItem) %></div>
                                <p class="comercial-texto"><%#: Eval("Descripcion") %></p>
                                <%# DetalleSeguimiento(Container.DataItem) %>
                            </li>
                        </ItemTemplate>
                        <FooterTemplate></ul></FooterTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>

            <%-- Pasos 9 a 30 (alta) y A6, A8, A9, A10 a A12 (edición), con A13 a A19 --%>
            <asp:Panel ID="pnlFormulario" runat="server" Visible="false" DefaultButton="btnGuardar">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow"><asp:Literal ID="litFormularioEyebrow" runat="server" Text="Nuevo contacto" /></span>
                        <h2><asp:Literal ID="litFormularioTitulo" runat="server" Text="Registrar contacto comercial" /></h2>
                        <p>Los campos con * son obligatorios. Según el medio de contacto, completá el correo, el teléfono u otro dato con el que se pueda llegar a la persona.</p>
                    </div>

                    <asp:Panel ID="pnlSinCatalogos" runat="server" Visible="false" CssClass="comercial-aviso">
                        No se pudieron cargar los tipos de entidad, medios y orígenes definidos para los contactos, por eso no se puede guardar. Probá nuevamente en unos minutos.
                    </asp:Panel>

                    <div class="comercial-form-grid">
                        <div class="form-field">
                            <label for="<%= txtNombre.ClientID %>">Nombre del contacto *</label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="150" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtApellido.ClientID %>">Apellido</label>
                            <asp:TextBox ID="txtApellido" runat="server" MaxLength="150" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlTipo.ClientID %>">Tipo de entidad *</label>
                            <asp:DropDownList ID="ddlTipo" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtEntidad.ClientID %>">Entidad que representa</label>
                            <asp:TextBox ID="txtEntidad" runat="server" MaxLength="200" placeholder="Ej: Centro Cultural Sur" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlMedio.ClientID %>">Medio de contacto *</label>
                            <asp:DropDownList ID="ddlMedio" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCorreo.ClientID %>">Correo electrónico</label>
                            <asp:TextBox ID="txtCorreo" runat="server" TextMode="Email" MaxLength="254" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtTelefono.ClientID %>">Teléfono</label>
                            <asp:TextBox ID="txtTelefono" runat="server" MaxLength="40" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtOtroDato.ClientID %>">Otro dato de contacto</label>
                            <asp:TextBox ID="txtOtroDato" runat="server" MaxLength="300" placeholder="Usuario de una red social o dirección web" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlOrigen.ClientID %>">Origen del contacto *</label>
                            <asp:DropDownList ID="ddlOrigen" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlEstado.ClientID %>">Estado comercial *</label>
                            <asp:DropDownList ID="ddlEstado" runat="server" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtObservaciones.ClientID %>"><asp:Literal ID="litEtiquetaObservaciones" runat="server" Text="Observaciones iniciales *" /></label>
                            <asp:TextBox ID="txtObservaciones" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
                        </div>
                        <asp:Panel ID="pnlObservacionCambio" runat="server" Visible="false" CssClass="form-field comercial-campo-ancho">
                            <label for="<%= txtObservacionCambio.ClientID %>">Si cambiás el estado, contá por qué</label>
                            <asp:TextBox ID="txtObservacionCambio" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000" />
                            <p class="comercial-ayuda">Queda en el seguimiento del contacto junto con el estado anterior y el nuevo.</p>
                        </asp:Panel>
                    </div>

                    <h3>Vinculación comercial <small>(opcional)</small></h3>
                    <div class="comercial-form-grid">
                        <div class="form-field">
                            <label for="<%= ddlCampania.ClientID %>">Campaña comercial</label>
                            <asp:DropDownList ID="ddlCampania" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlAlianza.ClientID %>">Alianza estratégica</label>
                            <asp:DropDownList ID="ddlAlianza" runat="server" />
                        </div>
                    </div>

                    <%-- A9 --%>
                    <fieldset class="comercial-fieldset">
                        <legend>Cuenta en StageUp <small>(cuando el contacto está incorporado a la plataforma)</small></legend>
                        <p class="comercial-ayuda">El contacto queda como antecedente comercial: no reemplaza la cuenta. Si la cuenta no tenía origen registrado, toma el origen de este contacto.</p>
                        <asp:Panel ID="pnlCuentaElegida" runat="server" Visible="false" CssClass="comercial-aviso">
                            <asp:Literal ID="litCuentaElegida" runat="server" Mode="Encode" />
                            <asp:LinkButton ID="lnkQuitarCuenta" runat="server" CssClass="text-link" Text="Quitar" CausesValidation="false" OnClick="lnkQuitarCuenta_Click" />
                        </asp:Panel>
                        <asp:Panel ID="pnlBuscarCuenta" runat="server" CssClass="comercial-form-grid" DefaultButton="btnBuscarCuenta">
                            <div class="form-field">
                                <label for="<%= txtCorreoCuenta.ClientID %>">Correo de la cuenta</label>
                                <asp:TextBox ID="txtCorreoCuenta" runat="server" TextMode="Email" MaxLength="254" />
                            </div>
                            <div class="form-field comercial-campo-boton">
                                <asp:Button ID="btnBuscarCuenta" runat="server" CssClass="button button-secondary" Text="Buscar cuenta" CausesValidation="false" OnClick="btnBuscarCuenta_Click" />
                            </div>
                        </asp:Panel>
                    </fieldset>

                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar contacto" CausesValidation="false" OnClick="btnGuardar_Click" />
                        <asp:HyperLink ID="lnkCancelar" runat="server" CssClass="button button-secondary" Text="Cancelar" NavigateUrl="ContactosComerciales.aspx" />
                    </div>
                </div>
            </asp:Panel>

            <%-- A7 y A8 --%>
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
                            <label for="<%= txtRespuesta.ClientID %>">Respuesta recibida</label>
                            <asp:TextBox ID="txtRespuesta" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtInteres.ClientID %>">Interés manifestado</label>
                            <asp:TextBox ID="txtInteres" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtAccionPendiente.ClientID %>">Acción pendiente</label>
                            <asp:TextBox ID="txtAccionPendiente" runat="server" MaxLength="1000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFechaAccion.ClientID %>">Fecha de la acción pendiente</label>
                            <asp:TextBox ID="txtFechaAccion" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlEstadoResultante.ClientID %>">Estado comercial resultante</label>
                            <asp:DropDownList ID="ddlEstadoResultante" runat="server" />
                        </div>
                    </div>
                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardarSeguimiento" runat="server" CssClass="button button-primary" Text="Guardar seguimiento" CausesValidation="false" OnClick="btnGuardarSeguimiento_Click" />
                        <asp:HyperLink ID="lnkCancelarSeguimiento" runat="server" CssClass="button button-secondary" Text="Cancelar" />
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
