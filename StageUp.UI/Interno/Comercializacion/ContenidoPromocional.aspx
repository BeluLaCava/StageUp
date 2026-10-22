<%@ Page Title="Contenido promocional | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="ContenidoPromocional.aspx.cs" Inherits="StageUp.UI.Interno.Comercializacion.ContenidoPromocionalPagina" %>
<%@ Register Src="~/Interno/Comercializacion/ComercializacionTabs.ascx" TagPrefix="su" TagName="Solapas" %>

<asp:Content ID="ContenidoContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comercialización</span>
            <h1><asp:Literal ID="litTitulo" runat="server" Text="Contenido promocional" /></h1>
            <p><asp:Literal ID="litBajada" runat="server" Text="Repositorio interno de piezas para difundir StageUp: imágenes, videos, presentaciones, textos y folletos, con su canal de uso, campaña y estado." /></p>
        </div>

        <su:Solapas ID="solapas" runat="server" />

        <div class="static-page-body internal-admin-stack comercial-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>


            <asp:Panel ID="pnlListado" runat="server" Visible="false">
                <div class="comercial-resumen" aria-label="Resumen de contenido promocional">
                    <a href="ContenidoPromocional.aspx?estado=Activo"><span>Activos</span><strong><asp:Literal ID="litActivos" runat="server" /></strong></a>
                    <a href="ContenidoPromocional.aspx?estado=EnRevision"><span>En revisión</span><strong><asp:Literal ID="litEnRevision" runat="server" /></strong></a>
                    <a href="ContenidoPromocional.aspx?estado=Desactualizado"><span>Desactualizados</span><strong><asp:Literal ID="litDesactualizados" runat="server" /></strong></a>
                    <a href="ContenidoPromocional.aspx?vista=Bajas"><span>Dados de baja</span><strong><asp:Literal ID="litBajas" runat="server" /></strong></a>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header comercial-toolbar">
                        <div>
                            <span class="admin-card-eyebrow">Repositorio comercial</span>
                            <h2>Piezas promocionales</h2>
                        </div>
                        <div class="comercial-toolbar-acciones">
                            <a class="button button-primary" href="ContenidoPromocional.aspx?nuevo=1">Agregar contenido</a>
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiltros" runat="server" CssClass="comercial-filtros" DefaultButton="btnAplicarFiltros" role="search">
                        <div class="form-field">
                            <label for="<%= ddlFiltroVista.ClientID %>">Mostrar</label>
                            <asp:DropDownList ID="ddlFiltroVista" runat="server">
                                <asp:ListItem Value="Vigentes" Text="Sin los dados de baja" />
                                <asp:ListItem Value="Bajas" Text="Dados de baja" />
                                <asp:ListItem Value="Todos" Text="Todos" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                            <asp:DropDownList ID="ddlFiltroEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroTipo.ClientID %>">Tipo de contenido</label>
                            <asp:DropDownList ID="ddlFiltroTipo" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroCanal.ClientID %>">Canal previsto de uso</label>
                            <asp:DropDownList ID="ddlFiltroCanal" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFiltroCampania.ClientID %>">Campaña asociada</label>
                            <asp:DropDownList ID="ddlFiltroCampania" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtFiltroTexto.ClientID %>">Título o descripción</label>
                            <asp:TextBox ID="txtFiltroTexto" runat="server" MaxLength="100" />
                        </div>
                        <div class="comercial-filtros-acciones">
                            <asp:Button ID="btnAplicarFiltros" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnAplicarFiltros_Click" />
                            <a class="text-link" href="ContenidoPromocional.aspx">Limpiar filtros</a>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlSinContenidos" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No hay contenidos promocionales cargados</h3>
                        <p>Registrá la primera pieza con "Agregar contenido".</p>
                    </asp:Panel>
                    <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No se encontraron contenidos para los criterios seleccionados</h3>
                        <p>Modificá los filtros o limpialos para volver al listado general.</p>
                    </asp:Panel>

                    <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                    <div class="comercial-tabla-wrap">
                        <asp:Repeater ID="rptContenidos" runat="server">
                            <HeaderTemplate>
                                <table class="comercial-tabla">
                                    <thead><tr><th>Título</th><th>Tipo</th><th>Canal previsto</th><th>Campaña</th><th>Estado</th><th>Fecha de carga</th><th></th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr class='<%# (bool)Eval("Activo") ? "" : "comercial-fila-baja" %>'>
                                    <td data-label="Título"><strong><%#: Eval("Titulo") %></strong><br /><small><%#: (bool)Eval("TieneArchivo") ? "Archivo: " + Eval("NombreArchivoOriginal") : "Enlace" %></small></td>
                                    <td data-label="Tipo"><%#: TextoCatalogo("TipoContenido", Eval("TipoContenido")) %></td>
                                    <td data-label="Canal previsto"><%#: TextoCatalogo("CanalUso", Eval("CanalPublicacion")) %></td>
                                    <td data-label="Campaña"><%#: TextoOGuion(Eval("NombreCampania")) %></td>
                                    <td data-label="Estado"><span class='<%# "comercial-badge " + StageUp.BLL.BLL_ContenidoPromocional.ClaseEstado(Convert.ToString(Eval("EstadoContenido"))) %>'><%#: StageUp.BLL.BLL_ContenidoPromocional.TextoEstado(Convert.ToString(Eval("EstadoContenido"))) %></span></td>
                                    <td data-label="Fecha de carga" class="comercial-num"><%#: Eval("FechaAlta", "{0:dd/MM/yyyy}") %></td>
                                    <td><a class="admin-action-link admin-action-link-primary" href='<%#: "ContenidoPromocional.aspx?ver=" + Eval("IdContenidoPromocional") %>'>Ver detalle</a></td>
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
                            <span class="admin-card-eyebrow">Detalle de contenido promocional</span>
                            <h2><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></h2>
                        </div>
                        <asp:Label ID="lblEstado" runat="server" CssClass="comercial-badge" />
                    </div>

                    <asp:Panel ID="pnlAviso" runat="server" Visible="false" CssClass="comercial-aviso">
                        <asp:Literal ID="litAviso" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <asp:Image ID="imgVista" runat="server" Visible="false" CssClass="comercial-vista-previa" AlternateText="Vista previa de la pieza" />

                    <dl class="comercial-datos">
                        <div class="comercial-dato-ancho"><dt>Descripción</dt><dd class="comercial-texto"><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Tipo de contenido</dt><dd><asp:Literal ID="litTipo" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Canal previsto de uso</dt><dd><asp:Literal ID="litCanal" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Campaña comercial</dt><dd><asp:HyperLink ID="lnkCampania" runat="server" CssClass="text-link" /><asp:Literal ID="litSinCampania" runat="server" Text="Sin campaña asociada" /></dd></div>
                        <div><dt>Alianza estratégica</dt><dd><asp:HyperLink ID="lnkAlianza" runat="server" CssClass="text-link" /><asp:Literal ID="litSinAlianza" runat="server" Text="—" /></dd></div>
                        <div><dt>Archivo</dt><dd><asp:HyperLink ID="lnkArchivo" runat="server" CssClass="text-link" /><asp:Literal ID="litSinArchivo" runat="server" Text="—" /></dd></div>
                        <div><dt>Enlace</dt><dd><asp:HyperLink ID="lnkEnlace" runat="server" CssClass="text-link" Target="_blank" /><asp:Literal ID="litSinEnlace" runat="server" Text="—" /></dd></div>
                        <div><dt>Cargado por</dt><dd><asp:Literal ID="litResponsable" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Fecha de carga</dt><dd><asp:Literal ID="litAlta" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Última actualización</dt><dd><asp:Literal ID="litActualizacion" runat="server" Mode="Encode" /></dd></div>
                    </dl>

                    <asp:Panel ID="pnlCambioEstado" runat="server" CssClass="comercial-filtros" DefaultButton="btnCambiarEstado">
                        <div class="form-field">
                            <label for="<%= ddlNuevoEstado.ClientID %>"><asp:Literal ID="litEtiquetaEstado" runat="server" Text="Cambiar estado" /></label>
                            <asp:DropDownList ID="ddlNuevoEstado" runat="server" />
                        </div>
                        <div class="form-field comercial-campo-boton">
                            <asp:Button ID="btnCambiarEstado" runat="server" CssClass="button button-secondary" Text="Actualizar estado" CausesValidation="false" OnClick="btnCambiarEstado_Click" />
                        </div>
                    </asp:Panel>

                    <div class="comercial-acciones">
                        <asp:HyperLink ID="lnkEditar" runat="server" CssClass="button button-primary" Text="Editar contenido" />
                        <asp:Button ID="btnDarDeBaja" runat="server" CssClass="button button-secondary" Text="Dar de baja contenido" CausesValidation="false"
                            OnClick="btnDarDeBaja_Click"
                            OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, 'El contenido se va a retirar de los materiales vigentes para uso comercial, pero conserva su información histórica en el repositorio. ¿Confirmás la baja?', { title: 'Dar de baja contenido', confirmText: 'Sí, dar de baja', cancelText: 'Volver' })) return false;" />
                        <a class="text-link" href="ContenidoPromocional.aspx">Volver al listado</a>
                    </div>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlFormulario" runat="server" Visible="false" DefaultButton="btnGuardar">
                <div class="auth-card admin-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow"><asp:Literal ID="litFormularioEyebrow" runat="server" Text="Nuevo contenido" /></span>
                        <h2><asp:Literal ID="litFormularioTitulo" runat="server" Text="Registrar pieza promocional" /></h2>
                        <p>Los campos con * son obligatorios. Cada pieza lleva un archivo, un enlace o los dos.</p>
                    </div>

                    <asp:Panel ID="pnlSinCatalogos" runat="server" Visible="false" CssClass="comercial-aviso">
                        No hay tipos de contenido o canales de uso definidos, por eso no se puede guardar. Consultalo con un administrador.
                    </asp:Panel>

                    <div class="comercial-form-grid">
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtTituloPieza.ClientID %>">Título *</label>
                            <asp:TextBox ID="txtTituloPieza" runat="server" MaxLength="200" />
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtDescripcion.ClientID %>">Descripción</label>
                            <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="3" MaxLength="2000" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlTipo.ClientID %>">Tipo de contenido *</label>
                            <asp:DropDownList ID="ddlTipo" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlCanal.ClientID %>">Canal previsto de uso *</label>
                            <asp:DropDownList ID="ddlCanal" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlCampania.ClientID %>">Campaña comercial</label>
                            <asp:DropDownList ID="ddlCampania" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlAlianza.ClientID %>">Alianza estratégica</label>
                            <asp:DropDownList ID="ddlAlianza" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlEstado.ClientID %>">Estado *</label>
                            <asp:DropDownList ID="ddlEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= fuArchivo.ClientID %>">Archivo</label>
                            <asp:FileUpload ID="fuArchivo" runat="server" accept=".jpg,.jpeg,.png,.gif,.pdf,.mp4,.docx,.pptx,.xlsx" />
                            <p class="comercial-ayuda">JPG, PNG, GIF, PDF, MP4, DOCX, PPTX o XLSX, hasta 3 MB. Para piezas más grandes usá un enlace.</p>
                            <asp:Panel ID="pnlArchivoActual" runat="server" Visible="false" CssClass="comercial-check">
                                <asp:CheckBox ID="chkQuitarArchivo" runat="server" />
                            </asp:Panel>
                        </div>
                        <div class="form-field comercial-campo-ancho">
                            <label for="<%= txtEnlace.ClientID %>">Enlace</label>
                            <asp:TextBox ID="txtEnlace" runat="server" MaxLength="1000" placeholder="https://…" />
                        </div>
                    </div>

                    <div class="comercial-acciones">
                        <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar contenido" CausesValidation="false" OnClick="btnGuardar_Click" />
                        <asp:HyperLink ID="lnkCancelar" runat="server" CssClass="button button-secondary" Text="Cancelar" NavigateUrl="ContenidoPromocional.aspx" />
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
