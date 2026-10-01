<%@ Page Title="Registros de actividad | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="RegistrosActividad.aspx.cs" Inherits="StageUp.UI.Interno.RegistrosActividad" %>

<asp:Content ID="RegistrosActividadContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .bitacora-detalle { display: grid; grid-template-columns: max-content minmax(0, 1fr); gap: 6px 16px; margin: 0 0 12px; }
        .bitacora-detalle dt { color: var(--color-text-muted, #765f55); font-size: 0.9em; }
        .bitacora-detalle dd { margin: 0; overflow-wrap: anywhere; }
        .bitacora-detalle-descripcion { white-space: pre-wrap; }
        .bitacora-ver { font-size: 0.82em; }
        @media (max-width: 600px) { .bitacora-detalle { grid-template-columns: 1fr; } }
    </style>
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Registros de actividad</span>
            <h1>Bitácora del sistema</h1>
            <p>Consultá las acciones registradas en StageUp: registros, activaciones, inicios de sesión, y altas/modificaciones/bajas de espacios artísticos.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="auth-card admin-workspace-card activity-workspace">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                    <span class="admin-card-eyebrow" data-i18n="AdminUI_Historial">Historial</span>
                    <h2>Registros</h2>
                    </div>
                    <div class="activity-workspace-tools">
                        <span class="admin-workspace-hint" data-i18n="AdminUI_ActividadReciente">Actividad ordenada desde la más reciente.</span>
                        <details class="activity-filter-details">
                            <summary aria-label="Abrir filtros de bitácora" title="Filtrar registros">
                                <span class="activity-filter-icon" aria-hidden="true"></span>
                            </summary>

                            <asp:Panel ID="pnlFiltros" runat="server" CssClass="auth-card admin-filter-card activity-filter-card" role="search" aria-labelledby="filtros-bitacora-title">
                                <div class="auth-card-header">
                                    <span class="admin-card-eyebrow" data-i18n="AdminUI_BusquedaAvanzada">Búsqueda avanzada</span>
                                    <h2 id="filtros-bitacora-title">Filtros</h2>
                                    <p>Todos los filtros son opcionales. Se muestran los registros más recientes que cumplan los filtros (hasta 500).</p>
                                </div>

                                <div class="filter-form-grid">
                                    <div class="form-field">
                                        <label for="<%= txtResponsable.ClientID %>">Usuario responsable (nombre o correo)</label>
                                        <asp:TextBox ID="txtResponsable" runat="server" MaxLength="150" placeholder="Nombre, apellido o correo" />
                                    </div>
                                    <div class="form-field">
                                        <label for="<%= ddlTipoResponsable.ClientID %>">Tipo de usuario</label>
                                        <asp:DropDownList ID="ddlTipoResponsable" runat="server">
                                            <asp:ListItem Value="">Todos</asp:ListItem>
                                            <asp:ListItem Value="Interno">Usuarios internos</asp:ListItem>
                                            <asp:ListItem Value="Externo">Usuarios externos</asp:ListItem>
                                            <asp:ListItem Value="Sistema">Sistema (procesos automáticos)</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                    <div class="form-field">
                                        <label for="<%= ddlTipoOperacion.ClientID %>">Tipo de operación</label>
                                        <asp:DropDownList ID="ddlTipoOperacion" runat="server" />
                                    </div>
                                    <div class="form-field">
                                        <label for="<%= txtFechaDesde.ClientID %>">Fecha desde</label>
                                        <asp:TextBox ID="txtFechaDesde" runat="server" TextMode="Date" />
                                    </div>
                                    <div class="form-field">
                                        <label for="<%= txtFechaHasta.ClientID %>">Fecha hasta</label>
                                        <asp:TextBox ID="txtFechaHasta" runat="server" TextMode="Date" />
                                    </div>
                                    <div class="form-field form-field-wide">
                                        <label for="<%= ddlTipoEntidad.ClientID %>">Entidad afectada</label>
                                        <asp:DropDownList ID="ddlTipoEntidad" runat="server" />
                                    </div>
                                </div>

                                <div class="form-actions">
                                    <asp:Button ID="btnBuscar" runat="server" CssClass="button button-primary" Text="Aplicar filtros"
                                        CausesValidation="false" OnClick="btnBuscar_Click" />
                                    <asp:LinkButton ID="lnkLimpiarFiltros" runat="server" CssClass="text-link" CausesValidation="false"
                                        Text="Limpiar filtros" OnClick="lnkLimpiarFiltros_Click" />
                                </div>
                            </asp:Panel>
                        </details>
                    </div>
                </div>

                <p class="admin-workspace-hint"><asp:Literal ID="litCantidad" runat="server" /></p>

                <asp:Panel ID="pnlDetalle" runat="server" CssClass="auth-card admin-editor-card" Visible="false">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Detalle del registro</span>
                        <h2>Registro N° <asp:Literal ID="litDetalleId" runat="server" /></h2>
                    </div>
                    <dl class="bitacora-detalle">
                        <dt>Operación</dt><dd><asp:Literal ID="litDetalleOperacion" runat="server" /></dd>
                        <dt>Entidad afectada</dt><dd><asp:Literal ID="litDetalleEntidad" runat="server" /></dd>
                        <dt>Id afectado</dt><dd><asp:Literal ID="litDetalleIdAfectado" runat="server" /></dd>
                        <dt>Responsable</dt><dd><asp:Literal ID="litDetalleResponsable" runat="server" /></dd>
                        <dt>Fecha y hora</dt><dd><asp:Literal ID="litDetalleFecha" runat="server" /></dd>
                        <dt>Origen</dt><dd><asp:Literal ID="litDetalleOrigen" runat="server" /></dd>
                        <dt>Descripción</dt><dd class="bitacora-detalle-descripcion"><asp:Literal ID="litDetalleDescripcion" runat="server" /></dd>
                    </dl>
                    <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Cerrar detalle" OnClick="lnkCerrarDetalle_Click" />
                </asp:Panel>

                <div class="activity-table-wrap">
                    <table class="activity-table" aria-label="Registros de actividad">
                        <thead>
                            <tr>
                                <th scope="col">Operación</th>
                                <th scope="col">Entidad</th>
                                <th scope="col">Responsable</th>
                                <th scope="col">Fecha</th>
                                <th scope="col">Descripción</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptRegistros" runat="server" OnItemCommand="rptRegistros_ItemCommand">
                                <ItemTemplate>
                                    <tr class="activity-table-row">
                                        <td data-label="Operación" class="activity-col-operacion">
                                            <%#: Eval("TipoOperacion") %>
                                            <br /><asp:LinkButton runat="server" CssClass="text-link bitacora-ver" CausesValidation="false"
                                                CommandName="Ver" CommandArgument='<%# Eval("IdRegistroActividad") %>' Text="Ver detalle" />
                                        </td>
                                        <td data-label="Entidad" class="activity-col-entidad"><%#: Eval("TipoEntidadAfectada") %></td>
                                        <td data-label="Responsable" class="activity-col-responsable"><%#: ObtenerNombreMostrado(Eval("NombreResponsable") as string) %></td>
                                        <td data-label="Fecha" class="activity-col-fecha"><%#: Eval("FechaOperacion", "{0:dd/MM/yyyy HH:mm}") %></td>
                                        <td data-label="Descripción" class="activity-col-descripcion" title='<%# Eval("DescripcionOperacion") %>'><%#: Eval("DescripcionOperacion") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>

                <asp:Panel ID="pnlSinResultados" runat="server" CssClass="empty-state" Visible="false">
                    <h3><asp:Literal ID="litTituloSinResultados" runat="server" /></h3>
                    <p><asp:Literal ID="litDescripcionSinResultados" runat="server" /></p>
                </asp:Panel>
            </div>

            <div class="auth-card admin-workspace-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Exportación</span>
                        <h2>Exportar registros cifrados</h2>
                    </div>
                </div>
                <p>Genera un archivo XML con los registros que ves arriba (según los filtros aplicados), protegido con cifrado híbrido (AES + RSA). Para volver a leerlo hace falta la clave privada configurada en <a href="HerramientasSeguridad.aspx">Herramientas de seguridad</a>.</p>
                <asp:Button ID="btnExportar" runat="server" CssClass="button button-secondary" Text="Exportar filtro actual a XML cifrado"
                    CausesValidation="false" OnClick="btnExportar_Click" />
            </div>

        </div>
    </section>
</asp:Content>
