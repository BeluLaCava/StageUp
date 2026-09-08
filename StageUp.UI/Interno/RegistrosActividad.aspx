<%@ Page Title="Registros de actividad | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RegistrosActividad.aspx.cs" Inherits="StageUp.UI.Interno.RegistrosActividad" %>

<asp:Content ID="RegistrosActividadContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Registros de actividad</span>
            <h1>Bitácora del sistema</h1>
            <p>Consultá las acciones registradas en StageUp: registros, activaciones, inicios de sesión, y altas/modificaciones/bajas de espacios artísticos.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <!-- Filtros de consulta (CU-001-013, camino alternativo A3: opcionales, no obligatorios) -->
            <asp:Panel ID="pnlFiltros" runat="server" CssClass="auth-card" role="search" aria-labelledby="filtros-bitacora-title">
                <div class="auth-card-header">
                    <h2 id="filtros-bitacora-title">Filtros</h2>
                    <p>Todos los filtros son opcionales — dejalos vacíos para ver el listado completo.</p>
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

            <!-- Listado de registros -->
            <div class="auth-card">
                <div class="auth-card-header">
                    <h2>Registros</h2>
                </div>

                <asp:Repeater ID="rptRegistros" runat="server">
                    <ItemTemplate>
                        <div class="space-row">
                            <div class="space-row-info">
                                <h3><%# Eval("TipoOperacion") %> · <%# Eval("TipoEntidadAfectada") %></h3>
                                <p>
                                    <%# ObtenerNombreMostrado(Eval("NombreResponsable") as string) %>
                                    · <%# Eval("FechaOperacion", "{0:dd/MM/yyyy HH:mm}") %>
                                </p>
                                <p class="space-row-descripcion"><%# Eval("DescripcionOperacion") %></p>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlSinResultados" runat="server" CssClass="empty-state" Visible="false">
                    <h3><asp:Literal ID="litTituloSinResultados" runat="server" /></h3>
                    <p><asp:Literal ID="litDescripcionSinResultados" runat="server" /></p>
                </asp:Panel>
            </div>
        </div>
    </section>
</asp:Content>
