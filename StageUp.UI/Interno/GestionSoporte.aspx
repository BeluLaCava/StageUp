<%@ Page Title="Solicitudes de soporte | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionSoporte.aspx.cs" Inherits="StageUp.UI.Interno.GestionSoporte" %>

<asp:Content ID="GestionSoporteContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Administración</span>
            <h1><asp:Literal ID="litTitulo" runat="server" Text="Solicitudes de soporte" /></h1>
            <p>Analizá, respondé, asigná y cerrá las solicitudes de soporte que registran los usuarios de StageUp.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- Ítem 12: aviso de novedades cuando hay una respuesta sin guardar. --%>
            <div class="ticket-novedades-aviso" data-soporte-aviso hidden>
                <span>Hay novedades en las solicitudes de soporte.</span>
                <button type="button" class="button button-secondary button-small" data-soporte-recargar>Actualizar</button>
            </div>

            <%-- CU-003-003 pasos 3 a 8, A2, A3 y A4. --%>
            <asp:Panel ID="pnlListado" runat="server" Visible="false">
                <div class="soporte-resumen" aria-label="Resumen de solicitudes de soporte">
                    <a href="GestionSoporte.aspx"><span>Total</span><strong><asp:Literal ID="litTotal" runat="server" /></strong></a>
                    <a href="GestionSoporte.aspx?estado=Pendiente"><span>Pendientes</span><strong><asp:Literal ID="litPendientes" runat="server" /></strong></a>
                    <a href="GestionSoporte.aspx?estado=EnProceso"><span>En proceso</span><strong><asp:Literal ID="litEnProceso" runat="server" /></strong></a>
                    <a href="GestionSoporte.aspx?estado=Resuelta"><span>Resueltas</span><strong><asp:Literal ID="litResueltas" runat="server" /></strong></a>
                    <a href="GestionSoporte.aspx?estado=Cerrada"><span>Cerradas</span><strong><asp:Literal ID="litCerradas" runat="server" /></strong></a>
                    <a href="GestionSoporte.aspx?noLeidas=1" class="soporte-resumen-alerta"><span>Con mensajes sin leer</span><strong><asp:Literal ID="litSinLeer" runat="server" /></strong></a>
                    <a href="GestionSoporte.aspx?resp=sin"><span>Sin responsable</span><strong><asp:Literal ID="litSinAsignar" runat="server" /></strong></a>
                </div>

                <div class="auth-card admin-list-card">
                    <div class="auth-card-header internal-users-list-heading">
                        <div>
                            <span class="admin-card-eyebrow">Atención a usuarios</span>
                            <h2>Solicitudes registradas</h2>
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiltros" runat="server" CssClass="internal-filter-row" DefaultButton="btnAplicarFiltros" role="search">
                        <div class="form-field">
                            <label for="<%= ddlEstado.ClientID %>">Estado</label>
                            <asp:DropDownList ID="ddlEstado" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlFuncionalidad.ClientID %>">Funcionalidad vinculada</label>
                            <asp:DropDownList ID="ddlFuncionalidad" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= ddlResponsable.ClientID %>">Responsable</label>
                            <asp:DropDownList ID="ddlResponsable" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtDesde.ClientID %>">Registrada desde</label>
                            <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtHasta.ClientID %>">Registrada hasta</label>
                            <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtUsuario.ClientID %>">Usuario solicitante</label>
                            <asp:TextBox ID="txtUsuario" runat="server" MaxLength="100" placeholder="Nombre o correo" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCaso.ClientID %>">N.º de caso</label>
                            <asp:TextBox ID="txtCaso" runat="server" MaxLength="12" placeholder="Ej: SOP-000123" />
                        </div>
                        <div class="form-field soporte-filtro-check">
                            <asp:CheckBox ID="chkNoLeidas" runat="server" Text="Solo con mensajes sin leer" />
                        </div>
                        <div class="internal-filter-actions">
                            <asp:Button ID="btnAplicarFiltros" runat="server" CssClass="button button-secondary" Text="Aplicar filtros" CausesValidation="false" OnClick="btnAplicarFiltros_Click" />
                            <a class="text-link" href="GestionSoporte.aspx">Limpiar filtros</a>
                        </div>
                    </asp:Panel>

                    <%-- A2: no hay ninguna solicitud registrada. --%>
                    <asp:Panel ID="pnlSinSolicitudes" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No hay solicitudes de soporte disponibles para gestionar</h3>
                        <p>Las solicitudes aparecen acá cuando un usuario las registra desde Soporte, el centro de ayuda, el asistente o una reserva.</p>
                    </asp:Panel>
                    <%-- A4: hay solicitudes, pero ninguna cumple con los filtros. --%>
                    <asp:Panel ID="pnlSinResultados" runat="server" Visible="false" CssClass="empty-state compact-empty-state">
                        <h3>No se encontraron solicitudes para los criterios seleccionados</h3>
                        <p>Modificá los filtros o limpialos para volver al listado general.</p>
                    </asp:Panel>

                    <p class="internal-list-count"><asp:Literal ID="litCantidad" runat="server" Mode="Encode" /></p>

                    <div class="pagos-tabla-wrap">
                        <asp:Repeater ID="rptSolicitudes" runat="server">
                            <HeaderTemplate>
                                <table class="pagos-tabla">
                                    <thead><tr><th>Caso</th><th>Usuario solicitante</th><th>Registrada</th><th>Motivo</th><th>Funcionalidad</th><th>Estado</th><th>Responsable</th><th></th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr class='<%# TieneSinLeer(Container.DataItem) ? "soporte-fila-sinleer" : "" %>'>
                                    <td data-label="Caso"><strong class="historial-codigo"><%#: Eval("Codigo") %></strong>
                                        <asp:PlaceHolder runat="server" Visible='<%# TieneSinLeer(Container.DataItem) %>'><br /><span class="ticket-nuevo"><%#: TextoSinLeer(Container.DataItem) %></span></asp:PlaceHolder>
                                    </td>
                                    <td data-label="Usuario solicitante"><%#: Eval("NombreUsuarioExterno") %><br /><small><%#: Eval("CorreoUsuarioExterno") %></small></td>
                                    <td data-label="Registrada" class="pagos-num"><%#: Eval("FechaCreacion", "{0:dd/MM/yyyy}") %><br /><small><%#: Eval("FechaCreacion", "{0:HH:mm}") %> hs</small></td>
                                    <td data-label="Motivo" class="soporte-motivo"><%#: Eval("Motivo") %><br /><small>Última actividad: <%#: Eval("FechaUltimaActividad", "{0:dd/MM/yyyy HH:mm}") %></small></td>
                                    <td data-label="Funcionalidad"><%#: StageUp.BLL.BLL_Ticket.TextoFuncionalidad(Convert.ToString(Eval("FuncionalidadVinculada"))) %></td>
                                    <td data-label="Estado"><span class='<%# "ticket-badge " + StageUp.BLL.BLL_Ticket.ClaseEstado(Convert.ToString(Eval("Estado"))) %>'><%#: StageUp.BLL.BLL_Ticket.TextoEstado(Convert.ToString(Eval("Estado"))) %></span></td>
                                    <td data-label="Responsable"><%#: Eval("NombreUsuarioInternoAsignado") ?? "Sin asignar" %></td>
                                    <td><a class="admin-action-link admin-action-link-primary" href='<%#: "GestionSoporte.aspx?ver=" + Eval("IdTicket") %>'>Ver detalle</a></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>

                    <asp:Panel ID="pnlPaginado" runat="server" CssClass="soporte-paginado" Visible="false">
                        <asp:HyperLink ID="lnkAnterior" runat="server" CssClass="button button-secondary button-small" Text="Anterior" />
                        <span><asp:Literal ID="litPagina" runat="server" Mode="Encode" /></span>
                        <asp:HyperLink ID="lnkSiguiente" runat="server" CssClass="button button-secondary button-small" Text="Siguiente" />
                    </asp:Panel>
                </div>
            </asp:Panel>

            <%-- CU-003-003 pasos 9 a 26, A5 a A12. --%>
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="auth-card helpdesk-card helpdesk-detail-card">
                    <div class="auth-card-header helpdesk-detail-header">
                        <div>
                            <span class="ticket-codigo"><asp:Literal ID="litCodigo" runat="server" Mode="Encode" /></span>
                            <h2><asp:Literal ID="litMotivo" runat="server" Mode="Encode" /></h2>
                        </div>
                        <asp:Label ID="lblEstado" runat="server" CssClass="ticket-badge" />
                    </div>

                    <asp:Panel ID="pnlNovedades" runat="server" Visible="false" CssClass="ticket-novedades-aviso">
                        <asp:Literal ID="litNovedades" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <%-- A10 / A11: aviso según el estado. --%>
                    <asp:Panel ID="pnlEstadoAviso" runat="server" Visible="false" CssClass="ticket-estado-aviso">
                        <asp:Literal ID="litEstadoAviso" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <dl class="ticket-datos">
                        <div><dt>Usuario solicitante</dt><dd><asp:Literal ID="litUsuario" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Fecha de registro</dt><dd><asp:Literal ID="litFechaRegistro" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Funcionalidad vinculada</dt><dd><asp:Literal ID="litFuncionalidad" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Origen</dt><dd><asp:Literal ID="litOrigen" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Responsable</dt><dd><asp:Literal ID="litResponsable" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Última actividad</dt><dd><asp:Literal ID="litUltimaActividad" runat="server" Mode="Encode" /></dd></div>
                        <asp:PlaceHolder ID="phResolucion" runat="server" Visible="false">
                            <div><dt>Resuelta el</dt><dd><asp:Literal ID="litFechaResolucion" runat="server" Mode="Encode" /></dd></div>
                        </asp:PlaceHolder>
                        <asp:PlaceHolder ID="phCierre" runat="server" Visible="false">
                            <div><dt>Cerrada el</dt><dd><asp:Literal ID="litFechaCierre" runat="server" Mode="Encode" /></dd></div>
                        </asp:PlaceHolder>
                    </dl>

                    <asp:Panel ID="pnlReserva" runat="server" Visible="false" CssClass="ticket-estado-aviso">
                        <strong>Reserva relacionada #<asp:Literal ID="litIdReserva" runat="server" Mode="Encode" /></strong>:
                        <asp:Literal ID="litReserva" runat="server" Mode="Encode" />
                    </asp:Panel>

                    <div class="ticket-descripcion">
                        <h3>Descripción del inconveniente</h3>
                        <p><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></p>
                        <asp:PlaceHolder ID="phInformacion" runat="server" Visible="false">
                            <h3>Información adicional</h3>
                            <p><asp:Literal ID="litInformacion" runat="server" Mode="Encode" /></p>
                        </asp:PlaceHolder>
                    </div>

                    <h3 class="ticket-seccion-titulo">Conversación</h3>
                    <div class="ticket-hilo">
                        <asp:Panel ID="pnlSinMensajes" runat="server" Visible="false">
                            <p class="ticket-hilo-vacio">Todavía no hay respuestas registradas.</p>
                        </asp:Panel>
                        <asp:Repeater ID="rptMensajes" runat="server">
                            <ItemTemplate>
                                <div class='<%# "ticket-mensaje " + (Convert.ToBoolean(Eval("EsInterno")) ? "ticket-mensaje-usuario" : "ticket-mensaje-soporte") + (Convert.ToBoolean(Eval("EsNuevo")) ? " ticket-mensaje-nuevo" : "") %>'>
                                    <div class="ticket-mensaje-meta">
                                        <strong><%#: Eval("NombreAutor") %><%#: Convert.ToBoolean(Eval("EsInterno")) ? " (soporte)" : " (usuario)" %></strong>
                                        <span>
                                            <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("EsNuevo")) %>'><span class="ticket-nuevo">Sin leer</span></asp:PlaceHolder>
                                            <%#: Eval("FechaEnvio", "{0:dd/MM/yyyy HH:mm}") %>
                                        </span>
                                    </div>
                                    <p class="ticket-mensaje-texto"><%#: Eval("Mensaje") %></p>
                                    <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("EsInterno")) %>'>
                                        <span class="ticket-mensaje-leido"><%#: Convert.ToBoolean(Eval("LeidoPorLaOtraParte")) ? "Visto por el usuario" : "Todavía no lo vio el usuario" %></span>
                                    </asp:PlaceHolder>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <h3 class="ticket-seccion-titulo">Historial de la solicitud</h3>
                    <div class="pagos-tabla-wrap">
                        <asp:Repeater ID="rptActualizaciones" runat="server">
                            <HeaderTemplate>
                                <table class="pagos-tabla">
                                    <thead><tr><th>Fecha y hora</th><th>Responsable</th><th>Actualización</th><th>Estado anterior</th><th>Estado nuevo</th><th>Detalle</th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td class="pagos-num" data-label="Fecha y hora"><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %></td>
                                    <td data-label="Responsable"><%#: Eval("NombreResponsable") %><%#: Convert.ToBoolean(Eval("EsInterno")) ? "" : " (usuario)" %></td>
                                    <td data-label="Actualización"><%#: TextoTipoActualizacion(Eval("TipoActualizacion")) %></td>
                                    <td data-label="Estado anterior"><%#: StageUp.BLL.BLL_Ticket.TextoEstado(Convert.ToString(Eval("EstadoAnterior"))) ?? "" %></td>
                                    <td data-label="Estado nuevo"><%#: StageUp.BLL.BLL_Ticket.TextoEstado(Convert.ToString(Eval("EstadoNuevo"))) ?? "" %></td>
                                    <td data-label="Detalle"><%#: Eval("Detalle") %></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>

                    <%-- Ítem 12: asignación. --%>
                    <asp:Panel ID="pnlAsignacion" runat="server" CssClass="soporte-gestion-bloque">
                        <h3 class="ticket-seccion-titulo">Responsable</h3>
                        <div class="soporte-gestion-fila">
                            <asp:DropDownList ID="ddlAsignarA" runat="server" aria-label="Asignar a" />
                            <asp:Button ID="btnAsignar" runat="server" CssClass="button button-secondary" Text="Asignar" CausesValidation="false" OnClick="btnAsignar_Click" />
                            <asp:Button ID="btnTomar" runat="server" CssClass="button button-secondary" Text="Tomar solicitud" CausesValidation="false" OnClick="btnTomar_Click" />
                        </div>
                    </asp:Panel>

                    <%-- Pasos 12 a 17, A6 y A7. --%>
                    <asp:Panel ID="pnlResponder" runat="server" CssClass="helpdesk-reply soporte-gestion-bloque" DefaultButton="btnGuardarRespuesta">
                        <h3 class="ticket-seccion-titulo">Registrar respuesta</h3>
                        <div class="form-field">
                            <label for="<%= txtRespuesta.ClientID %>">Respuesta del equipo de soporte</label>
                            <asp:TextBox ID="txtRespuesta" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" CssClass="helpdesk-textarea"
                                placeholder="Lo que analizaste y la solución o el próximo paso para el usuario." data-soporte-borrador="1" />
                        </div>
                        <asp:CheckBox ID="chkMarcarResuelta" runat="server" Text="Marcar como resuelta al guardar" CssClass="soporte-check" />
                        <div class="form-actions">
                            <asp:Button ID="btnGuardarRespuesta" runat="server" CssClass="button button-primary" Text="Guardar respuesta" CausesValidation="false" OnClick="btnGuardarRespuesta_Click"
                                OnClientClick="if (!confirmarRespuestaSoporte(this, event)) return false;" />
                        </div>
                    </asp:Panel>

                    <%-- Pasos 18 a 23, A8 y A9. --%>
                    <asp:Panel ID="pnlCambiarEstado" runat="server" CssClass="soporte-gestion-bloque">
                        <h3 class="ticket-seccion-titulo">Cambiar estado</h3>
                        <p class="ticket-lista-item-meta"><asp:Literal ID="litTransiciones" runat="server" Mode="Encode" /></p>
                        <div class="soporte-gestion-fila">
                            <asp:DropDownList ID="ddlNuevoEstado" runat="server" aria-label="Nuevo estado" />
                            <asp:Button ID="btnCambiarEstado" runat="server" CssClass="button button-secondary" Text="Cambiar estado" CausesValidation="false" OnClick="btnCambiarEstado_Click" />
                            <asp:Button ID="btnCerrar" runat="server" CssClass="button button-secondary" Text="Cerrar solicitud" CausesValidation="false" Visible="false" OnClick="btnCerrar_Click" />
                        </div>
                    </asp:Panel>

                    <%-- A10: cerrada, solo consulta salvo permiso de reapertura. --%>
                    <asp:Panel ID="pnlReabrir" runat="server" CssClass="soporte-gestion-bloque" Visible="false">
                        <asp:Button ID="btnReabrir" runat="server" CssClass="button button-secondary" Text="Reabrir solicitud" CausesValidation="false" OnClick="btnReabrir_Click" />
                    </asp:Panel>

                    <%-- A8 paso 4 y A11 paso 4: confirmación del cambio de estado. --%>
                    <asp:Panel ID="pnlConfirmar" runat="server" CssClass="soporte-confirmar" Visible="false" role="alertdialog">
                        <p><asp:Literal ID="litConfirmar" runat="server" Mode="Encode" /></p>
                        <div class="soporte-gestion-fila">
                            <asp:Button ID="btnConfirmarEstado" runat="server" CssClass="button button-primary" Text="Confirmar" CausesValidation="false" OnClick="btnConfirmarEstado_Click" />
                            <asp:Button ID="btnCancelarEstado" runat="server" CssClass="button button-secondary" Text="Cancelar" CausesValidation="false" OnClick="btnCancelarEstado_Click" />
                        </div>
                    </asp:Panel>

                    <div class="form-actions">
                        <asp:HyperLink ID="lnkVolver" runat="server" CssClass="text-link" NavigateUrl="~/Interno/GestionSoporte.aspx" Text="Volver al listado" />
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlNovedadesPeriodicas" runat="server" Visible="false">
                <div data-soporte-novedades="<%: UrlNovedades %>" data-marca="<%: MarcaNovedades %>" data-intervalo="20000"></div>
            </asp:Panel>
        </div>
    </section>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .soporte-resumen { display: grid; grid-template-columns: repeat(7, minmax(0, 1fr)); gap: 10px; }
        .soporte-resumen a { display: block; border: 1px solid var(--color-border); border-radius: 14px; padding: 10px 12px; background: var(--color-surface); color: var(--color-text); text-decoration: none; min-width: 0; }
        .soporte-resumen a:hover { border-color: var(--color-primary); }
        .soporte-resumen span { display: block; color: var(--color-text-muted); font-size: 0.75rem; line-height: 1.3; }
        .soporte-resumen strong { font-size: 1.45rem; font-variant-numeric: tabular-nums; }
        .soporte-resumen .soporte-resumen-alerta strong { color: var(--color-primary); }
        .internal-filter-row { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 0.75rem; align-items: end; margin: 0.5rem 0 0.4rem; }
        .internal-filter-row .form-field { margin-bottom: 0; min-width: 0; }
        .internal-filter-row .form-field label { display: block; margin-bottom: 0.35rem; }
        .internal-filter-row .form-field input[type=text], .internal-filter-row .form-field input[type=date], .internal-filter-row .form-field select { width: 100%; min-height: 2.9rem; box-sizing: border-box; padding: 0.6rem 0.75rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; }
        .soporte-filtro-check { align-self: end; padding-bottom: 0.7rem; }
        .soporte-filtro-check label { display: inline !important; }
        .soporte-motivo { min-width: 13rem; }
        .soporte-filtro-check label, .soporte-check label { margin-left: 0.4rem; font-weight: 600; }
        .internal-filter-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; grid-column: 1 / -1; }
        .internal-list-count { margin: 0.6rem 0; color: var(--color-text-muted); font-size: 0.8rem; }
        .pagos-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .pagos-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .pagos-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .pagos-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; }
        .pagos-tabla small { color: var(--color-text-muted); }
        .pagos-tabla .admin-action-link { white-space: nowrap; }
        .historial-codigo { white-space: nowrap; }
        .pagos-num { font-variant-numeric: tabular-nums; white-space: nowrap; }
        .soporte-fila-sinleer td { background: #fdf6f3; }
        .soporte-fila-sinleer td:first-child { box-shadow: inset 3px 0 0 var(--color-primary); }
        .soporte-paginado { display: flex; align-items: center; justify-content: center; gap: 1rem; margin-top: 0.9rem; }
        .soporte-paginado a.aspNetDisabled { opacity: 0.45; pointer-events: none; }
        .soporte-gestion-bloque { margin-top: 1rem; padding-top: 0.9rem; border-top: 1px solid rgba(109, 16, 33, 0.1); }
        .soporte-gestion-bloque .ticket-seccion-titulo { margin-top: 0; }
        .soporte-gestion-fila { display: flex; align-items: center; gap: 0.6rem; flex-wrap: wrap; }
        .helpdesk-card .soporte-gestion-fila select { width: auto; min-width: 16rem; min-height: 2.6rem; padding: 0.5rem 0.75rem; border: 1px solid var(--color-border); border-radius: var(--radius-small); background: var(--color-surface); color: var(--color-text); font: inherit; max-width: 100%; }
        .soporte-check { display: inline-block; margin: 0 0 0.6rem; }
        @media (max-width: 480px) { .helpdesk-card .soporte-gestion-fila select { width: 100%; min-width: 0; } }
        .soporte-confirmar { margin-top: 1rem; padding: 0.9rem 1rem; background: #fdf1ee; border: 1px solid rgba(109, 16, 33, 0.3); border-radius: 0.8rem; }
        .soporte-confirmar p { margin: 0 0 0.6rem; font-weight: 650; color: var(--color-primary-dark); }
        @media (max-width: 1100px) {
            .soporte-resumen { grid-template-columns: repeat(4, minmax(0, 1fr)); }
        }
        @media (max-width: 760px) {
            .internal-filter-row { grid-template-columns: 1fr 1fr; }
            .soporte-resumen { grid-template-columns: repeat(2, minmax(0, 1fr)); }
        }
        @media (max-width: 480px) {
            .internal-filter-row { grid-template-columns: 1fr; }
        }
    </style>
    <script>
        function confirmarRespuestaSoporte(btn, event) {
            var marcarResuelta = document.getElementById("<%= chkMarcarResuelta.ClientID %>");
            if (!marcarResuelta || !marcarResuelta.checked) {
                return true;
            }

            return StageUpConfirm.confirmPostback(btn, event, "¿Guardar la respuesta y marcar esta solicitud como resuelta?", {
                title: "Resolver solicitud",
                confirmText: "Sí, guardar y resolver",
                cancelText: "Revisar"
            });
        }
    </script>
    <script src="<%= ResolveUrl("~/Scripts/soporte-novedades.js") %>?v=20261012" defer></script>
</asp:Content>
