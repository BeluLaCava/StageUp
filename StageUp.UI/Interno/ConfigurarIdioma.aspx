<%@ Page Title="Configurar idioma | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="ConfigurarIdioma.aspx.cs" Inherits="StageUp.UI.Interno.ConfigurarIdioma" %>

<asp:Content ID="ConfigurarIdiomaContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="translation-admin-page">
        <a class="translation-back-link" href="GestionIdiomas.aspx">← Volver a idiomas</a>

        <asp:Panel ID="pnlIdiomaNoEncontrado" runat="server" CssClass="language-empty" Visible="false">
            <h1>No encontramos el idioma</h1>
            <p>Puede que se haya dado de baja o que el enlace sea incorrecto.</p>
            <a class="button button-secondary" href="GestionIdiomas.aspx">Volver a gestión de idiomas</a>
        </asp:Panel>

        <asp:Panel ID="pnlConfiguracion" runat="server">
            <header class="translation-admin-header">
                <div>
                    <span class="section-label">Configuración de traducciones</span>
                    <div class="translation-title-line">
                        <h1><asp:Literal ID="litNombreIdioma" runat="server" /></h1>
                        <span class="language-code"><asp:Literal ID="litCodigoIdioma" runat="server" /></span>
                    </div>
                    <p>Completá cada etiqueta una sola vez. La misma clave se reutilizará en todos los lugares de la plataforma donde aparezca ese texto.</p>
                </div>
                <div class="translation-progress-card">
                    <strong><asp:Literal ID="litPorcentaje" runat="server" />%</strong>
                    <span>del idioma completado</span>
                    <div class="language-progress" role="progressbar" aria-valuemin="0" aria-valuemax="100">
                        <asp:Panel ID="pnlBarraProgreso" runat="server" />
                    </div>
                </div>
            </header>

            <div class="translation-stats" aria-label="Estado de las traducciones">
                <article><strong><asp:Literal ID="litTotalEtiquetas" runat="server" /></strong><span>Etiquetas totales</span></article>
                <article><strong><asp:Literal ID="litCompletadas" runat="server" /></strong><span>Completadas</span></article>
                <article><strong><asp:Literal ID="litPendientes" runat="server" /></strong><span>Pendientes</span></article>
            </div>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message language-admin-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <section class="translation-workspace">
                <div class="translation-toolbar">
                    <div class="form-field translation-search-field">
                        <label for="<%= txtBuscar.ClientID %>">Buscar etiqueta</label>
                        <asp:TextBox ID="txtBuscar" runat="server" placeholder="Clave, texto original o traducción..." />
                    </div>
                    <div class="form-field translation-module-field">
                        <label for="<%= ddlModulo.ClientID %>">Módulo</label>
                        <asp:DropDownList ID="ddlModulo" runat="server" />
                    </div>
                    <label class="translation-pending-filter">
                        <asp:CheckBox ID="chkSoloPendientes" runat="server" />
                        <span>Mostrar solo pendientes</span>
                    </label>
                    <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-secondary" Text="Aplicar filtros"
                        CausesValidation="false" OnClick="btnFiltrar_Click" />
                </div>

                <asp:Panel ID="pnlSinEtiquetas" runat="server" CssClass="language-empty translation-empty" Visible="false">
                    <h3>No hay etiquetas para mostrar</h3>
                    <p>Probá cambiando los filtros de búsqueda.</p>
                </asp:Panel>

                <div class="translation-list">
                    <asp:Repeater ID="rptTraducciones" runat="server" OnItemCommand="rptTraducciones_ItemCommand">
                        <ItemTemplate>
                            <article class='<%# ObtenerClaseFila((StageUp.BE.Entidades.Traduccion)Container.DataItem) %>'>
                                <div class="translation-source">
                                    <div class="translation-row-meta">
                                        <span><%#: Eval("Modulo") %></span>
                                        <code><%#: Eval("ClaveEtiqueta") %></code>
                                    </div>
                                    <small>Texto predeterminado</small>
                                    <p><%#: Eval("TextoPredeterminado") %></p>
                                </div>
                                <div class="translation-editor">
                                    <div class="translation-editor-heading">
                                        <label for='<%# ((System.Web.UI.WebControls.TextBox)Container.FindControl("txtTextoTraducido")).ClientID %>'>Traducción</label>
                                        <span class='<%# ObtenerClaseEstado((StageUp.BE.Entidades.Traduccion)Container.DataItem) %>'><%# ObtenerTextoEstado((StageUp.BE.Entidades.Traduccion)Container.DataItem) %></span>
                                    </div>
                                    <asp:TextBox ID="txtTextoTraducido" runat="server" TextMode="MultiLine" Rows="2" MaxLength="2000"
                                        Text='<%# Bind("TextoTraducido") %>' placeholder="Escribí la traducción para este idioma..." />
                                    <div class="translation-row-actions">
                                        <asp:LinkButton runat="server" CssClass="text-link language-danger-link" Text="Eliminar traducción"
                                            CausesValidation="false" CommandName="Eliminar" CommandArgument='<%# Eval("IdEtiquetaTraduccion") %>'
                                            Visible='<%# Convert.ToBoolean(Eval("TieneTraduccion")) %>'
                                            OnClientClick="return confirm('¿Querés eliminar esta traducción? Se utilizará el texto predeterminado como respaldo.');" />
                                        <asp:LinkButton runat="server" CssClass="button button-primary button-small" Text="Guardar traducción"
                                            CausesValidation="false" CommandName="Guardar" CommandArgument='<%# Eval("IdEtiquetaTraduccion") %>' />
                                    </div>
                                </div>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </section>
        </asp:Panel>
    </section>
</asp:Content>
