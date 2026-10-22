<%@ Page Title="Contenido de ayuda | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionFaq.aspx.cs" Inherits="StageUp.UI.Interno.GestionFaq" %>

<asp:Content ID="GestionFaqContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Centro de ayuda</span>
            <h1>Contenido de ayuda</h1>
            <p>Preguntas frecuentes por categoría. En el centro de ayuda se muestra solo una lista breve de preguntas básicas, sin filtros; las categorías organizan el contenido internamente. Las consultas específicas las resuelve el asistente, que tiene su propio contenido y no usa estas preguntas.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="ayuda-resumen" aria-label="Resumen del contenido de ayuda">
                <div><span>Publicados</span><strong><asp:Literal ID="litCantidadActivos" runat="server" /></strong></div>
                <div><span>Borradores</span><strong><asp:Literal ID="litCantidadBorradores" runat="server" /></strong></div>
                <div><span>Desactivados</span><strong><asp:Literal ID="litCantidadInactivos" runat="server" /></strong></div>
                <div><span>Básicas publicadas</span><strong><asp:Literal ID="litCantidadBasicas" runat="server" /></strong></div>
            </div>


            <asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="auth-card" DefaultButton="btnGuardar">
                <span class="admin-card-eyebrow">Contenido</span>
                <h2><asp:Literal ID="litTituloFormulario" runat="server" Mode="Encode" /></h2>
                <asp:HiddenField ID="hfIdFaq" runat="server" />
                <asp:HiddenField ID="hfConsultaOrigen" runat="server" />
                <asp:Panel ID="pnlSinCategorias" runat="server" Visible="false" CssClass="form-message form-message-error">
                    No hay categorías temáticas disponibles: el contenido solo puede guardarse como borrador hasta que haya una categoría válida.
                </asp:Panel>
                <div class="form-field">
                    <label for="<%= txtPregunta.ClientID %>">Pregunta o título *</label>
                    <asp:TextBox ID="txtPregunta" runat="server" MaxLength="300" placeholder="Ej: ¿Cómo cancelo una reserva?" />
                </div>
                <div class="form-field">
                    <label for="<%= txtRespuesta.ClientID %>">Respuesta *</label>
                    <asp:TextBox ID="txtRespuesta" runat="server" TextMode="MultiLine" Rows="6" MaxLength="4000" placeholder="Explicá con lenguaje claro cómo resolver la consulta." />
                </div>
                <div class="ayuda-form-fila">
                    <div class="form-field">
                        <label for="<%= ddlCategoria.ClientID %>">Categoría temática *</label>
                        <asp:DropDownList ID="ddlCategoria" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtOrden.ClientID %>">Orden dentro de la categoría</label>
                        <asp:TextBox ID="txtOrden" runat="server" TextMode="Number" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlEstado.ClientID %>">Estado</label>
                        <asp:DropDownList ID="ddlEstado" runat="server" />
                    </div>
                </div>
                <div class="form-field">
                    <label for="<%= txtPalabrasClave.ClientID %>">Palabras clave (opcional, ayudan a encontrarla en la búsqueda)</label>
                    <asp:TextBox ID="txtPalabrasClave" runat="server" MaxLength="300" placeholder="Ej: cancelar cancelación devolución cargo" />
                </div>
                <div class="ayuda-check">
                    <asp:CheckBox ID="chkBasica" runat="server" />
                    <label for="<%= chkBasica.ClientID %>">Mostrarla entre las preguntas básicas del centro de ayuda (conviene que sea una lista breve; lo específico lo resuelve el asistente)</label>
                </div>

                <asp:Panel ID="pnlConfirmarGuardar" runat="server" Visible="false" CssClass="ayuda-confirmar">
                    <p><strong>¿Guardar y publicar este contenido?</strong> Va a quedar visible en el centro de ayuda para todos los usuarios.</p>
                    <asp:Button ID="btnConfirmarGuardar" runat="server" CssClass="button button-primary button-small" Text="Sí, guardar y publicar" CausesValidation="false" OnClick="btnConfirmarGuardar_Click" />
                    <asp:Button ID="btnCancelarGuardar" runat="server" CssClass="button button-secondary button-small" Text="Volver al formulario" CausesValidation="false" OnClick="btnCancelarGuardar_Click" />
                </asp:Panel>
                <div class="form-actions">
                    <asp:Button ID="btnGuardar" runat="server" CssClass="button button-primary" Text="Guardar contenido" CausesValidation="false" OnClick="btnGuardar_Click" />
                    <asp:Button ID="btnCancelarFormulario" runat="server" CssClass="button button-secondary" Text="Cancelar" CausesValidation="false" OnClick="btnCancelarFormulario_Click" />
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="auth-card">
                <div class="ayuda-detalle-cabecera">
                    <div>
                        <span class="admin-card-eyebrow">Detalle de contenido de ayuda</span>
                        <h2><asp:Literal ID="litDetallePregunta" runat="server" Mode="Encode" /></h2>
                    </div>
                    <asp:Label ID="lblDetalleEstado" runat="server" />
                </div>
                <asp:Panel ID="pnlAvisoEstado" runat="server" Visible="false" CssClass="ayuda-aviso">
                    <asp:Literal ID="litAvisoEstado" runat="server" Mode="Encode" />
                </asp:Panel>
                <p class="ayuda-detalle-respuesta"><asp:Literal ID="litDetalleRespuesta" runat="server" Mode="Encode" /></p>
                <dl class="internal-detail-grid">
                    <div><dt>Categoría</dt><dd><asp:Literal ID="litDetalleCategoria" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Centro de ayuda</dt><dd><asp:Literal ID="litDetalleBasica" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Orden</dt><dd><asp:Literal ID="litDetalleOrden" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Palabras clave</dt><dd><asp:Literal ID="litDetallePalabras" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Alta</dt><dd><asp:Literal ID="litDetalleAlta" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Última actualización</dt><dd><asp:Literal ID="litDetalleActualizacion" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <asp:Panel ID="pnlConfirmarPublicar" runat="server" Visible="false" CssClass="ayuda-confirmar">
                    <p><strong>¿Publicar este contenido?</strong> Va a quedar visible en el centro de ayuda para todos los usuarios.</p>
                    <asp:Button ID="btnConfirmarPublicar" runat="server" CssClass="button button-primary button-small" Text="Sí, publicar" CausesValidation="false" OnClick="btnConfirmarPublicar_Click" />
                    <asp:Button ID="btnCancelarPublicar" runat="server" CssClass="button button-secondary button-small" Text="No" CausesValidation="false" OnClick="btnCancelarPublicar_Click" />
                </asp:Panel>
                <asp:Panel ID="pnlConfirmarDesactivar" runat="server" Visible="false" CssClass="ayuda-confirmar">
                    <p><strong>¿Desactivar este contenido?</strong> Deja de verse en el centro de ayuda<asp:Literal ID="litConfirmarBasica" runat="server" Text=" y sale de las preguntas básicas" />. Se conserva su historial.</p>
                    <asp:Button ID="btnConfirmarDesactivar" runat="server" CssClass="button button-primary button-small" Text="Sí, desactivar" CausesValidation="false" OnClick="btnConfirmarDesactivar_Click" />
                    <asp:Button ID="btnCancelarDesactivar" runat="server" CssClass="button button-secondary button-small" Text="No" CausesValidation="false" OnClick="btnCancelarDesactivar_Click" />
                </asp:Panel>
                <div class="form-actions">
                    <asp:Button ID="btnEditar" runat="server" CssClass="button button-secondary" Text="Editar contenido" CausesValidation="false" OnClick="btnEditar_Click" />
                    <asp:Button ID="btnPublicar" runat="server" CssClass="button button-primary" Text="Publicar contenido" CausesValidation="false" OnClick="btnPublicar_Click" />
                    <asp:Button ID="btnDesactivar" runat="server" CssClass="button button-secondary" Text="Desactivar contenido" CausesValidation="false" OnClick="btnDesactivar_Click" />
                    <asp:LinkButton ID="lnkVolverListado" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkVolverListado_Click">Volver al listado</asp:LinkButton>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlListado" runat="server" CssClass="auth-card admin-list-card">
                <div class="ayuda-listado-cabecera">
                    <div>
                        <span class="admin-card-eyebrow">Directorio</span>
                        <h2>Contenido registrado</h2>
                    </div>
                    <asp:Button ID="btnAgregar" runat="server" CssClass="button button-primary" Text="Agregar contenido" CausesValidation="false" OnClick="btnAgregar_Click" />
                </div>
                <asp:Panel ID="pnlFiltros" runat="server" CssClass="internal-filter-row" DefaultButton="btnFiltrar" role="search">
                    <div class="form-field">
                        <label for="<%= ddlFiltroEstado.ClientID %>">Estado</label>
                        <asp:DropDownList ID="ddlFiltroEstado" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlFiltroCategoria.ClientID %>">Categoría</label>
                        <asp:DropDownList ID="ddlFiltroCategoria" runat="server" />
                    </div>
                    <div class="form-field">
                        <label for="<%= txtFiltroTexto.ClientID %>">Buscar</label>
                        <asp:TextBox ID="txtFiltroTexto" runat="server" MaxLength="100" placeholder="Pregunta, respuesta o palabra clave" />
                    </div>
                    <div class="internal-filter-actions">
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnFiltrar_Click" />
                        <asp:LinkButton ID="lnkLimpiar" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkLimpiar_Click">Limpiar filtros</asp:LinkButton>
                    </div>
                </asp:Panel>


                <asp:Panel ID="pnlSinContenido" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                    <h3><asp:Literal ID="litSinContenido" runat="server" Mode="Encode" /></h3>
                    <p>Usá "Agregar contenido" para cargar una pregunta frecuente.</p>
                </asp:Panel>

                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptFaq" runat="server" OnItemCommand="rptFaq_ItemCommand">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Pregunta o título</th><th>Categoría</th><th>Estado</th><th>Básica</th><th>Última actualización</th><th></th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><strong><%# Server.HtmlEncode(Convert.ToString(Eval("Pregunta"))) %></strong></td>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("NombreCategoria") ?? "Sin categoría")) %></td>
                                <td><span class="faq-estado faq-estado-<%# Eval("Estado") %>"><%# Server.HtmlEncode(StageUp.BLL.BLL_Faq.DescribirEstado(Convert.ToString(Eval("Estado")))) %></span></td>
                                <td><%# Convert.ToBoolean(Eval("EsBasica")) ? "Sí" : "No" %></td>
                                <td><%# Server.HtmlEncode(Fecha(Container.DataItem)) %></td>
                                <td><asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false" CommandName="Ver" CommandArgument='<%# Eval("IdFaq") %>' Text="Ver detalle" /></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>


            <asp:Panel ID="pnlConsultas" runat="server" CssClass="auth-card">
                <span class="admin-card-eyebrow">Consultas recurrentes</span>
                <h2>Consultas del asistente sin respuesta</h2>
                <p class="ayuda-nota">Lo que la gente escribió en el asistente y no encontró respuesta. Creá un contenido a partir de una consulta o descartala.</p>
                <asp:Panel ID="pnlSinConsultas" runat="server" CssClass="ayuda-nota">No hay consultas pendientes.</asp:Panel>
                <div class="pagos-tabla-wrap">
                    <asp:Repeater ID="rptConsultas" runat="server" OnItemCommand="rptConsultas_ItemCommand">
                        <HeaderTemplate>
                            <table class="pagos-tabla">
                                <thead><tr><th>Consulta</th><th>Veces</th><th>Última</th><th></th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td><%# Server.HtmlEncode(Convert.ToString(Eval("Texto"))) %></td>
                                <td><%# Eval("Cantidad") %></td>
                                <td><%# Convert.ToDateTime(Eval("UltimaFecha")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td class="ayuda-acciones">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false" CommandName="Crear" CommandArgument='<%# Eval("TextoNormalizado") + "|" + Eval("Texto") %>' Text="Crear contenido" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false" CommandName="Descartar" CommandArgument='<%# Eval("TextoNormalizado") %>' Text="Descartar" />
                                </td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>
        </div>
    </section>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .ayuda-resumen { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
        .ayuda-resumen > div { border: 1px solid var(--color-border); border-radius: 14px; padding: 12px 14px; background: var(--color-surface); }
        .ayuda-resumen span { display: block; color: var(--color-text-muted); font-size: 0.8rem; }
        .ayuda-resumen strong { font-size: 1.5rem; }
        .ayuda-listado-cabecera, .ayuda-detalle-cabecera { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap; }
        .ayuda-form-fila { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 0 12px; }
        .ayuda-form-fila > *, .internal-filter-row > * { min-width: 0; }
        .ayuda-form-fila select, .ayuda-form-fila input, .internal-filter-row input, .internal-filter-row select { width: 100%; box-sizing: border-box; }
        .ayuda-check { display: flex; gap: 8px; align-items: center; margin: 4px 0 10px; }
        .ayuda-check input { width: auto; margin: 0; }
        .ayuda-check label { margin: 0; }
        .ayuda-aviso { padding: 10px 14px; border-radius: 10px; background: #fff4e5; color: #78350f; margin: 6px 0 10px; }
        .ayuda-confirmar { padding: 10px 14px; border-radius: 10px; background: #fdecec; color: #6b1717; margin: 10px 0; }
        .ayuda-confirmar p { margin: 0 0 8px; }
        .ayuda-detalle-respuesta { white-space: pre-line; line-height: 1.55; }
        .ayuda-nota { color: var(--color-text-muted); font-size: 0.9rem; }
        .ayuda-acciones { white-space: nowrap; }
        .ayuda-acciones a + a { margin-left: 12px; }
        .internal-filter-row { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.75rem; align-items: end; margin: 0.5rem 0 0.8rem; }
        .internal-filter-row .form-field { margin-bottom: 0; }
        .internal-filter-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; grid-column: 1 / -1; }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 10px 0; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; overflow-wrap: anywhere; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .faq-estado { display: inline-block; padding: 2px 9px; border-radius: 999px; font-size: 0.78rem; font-weight: 700; white-space: nowrap; }
        .faq-estado-Activo { background: #e6f5eb; color: #236240; }
        .faq-estado-Borrador { background: #fff4e5; color: #8a4b0f; }
        .faq-estado-Inactivo { background: #eef0fb; color: #33407a; }
        @media (max-width: 760px) {
            .ayuda-resumen { grid-template-columns: repeat(2, minmax(0, 1fr)); }
            .ayuda-form-fila, .internal-filter-row, .internal-detail-grid { grid-template-columns: 1fr; }
        }
    </style>
</asp:Content>
