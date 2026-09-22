<%@ Page Title="Registros de actividad | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="RegistrosActividad.aspx.cs" Inherits="StageUp.UI.Interno.RegistrosActividad" %>

<asp:Content ID="RegistrosActividadContent" ContentPlaceHolderID="MainContent" runat="server">
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
                                    <p>Todos los filtros son opcionales; dejalos vacíos para ver el listado completo.</p>
                                </div>

                                <div class="filter-form-grid">
                                    <div class="form-field">
                                        <label for="<%= txtCorreoUsuario.ClientID %>">Correo del usuario</label>
                                        <asp:TextBox ID="txtCorreoUsuario" runat="server" TextMode="Email" placeholder="usuario@correo.com" />
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
                            <asp:Repeater ID="rptRegistros" runat="server">
                                <ItemTemplate>
                                    <tr class="activity-table-row">
                                        <td data-label="Operación" class="activity-col-operacion"><%#: Eval("TipoOperacion") %></td>
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

        </div>
    </section>
</asp:Content>
