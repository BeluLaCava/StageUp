<%@ Page Title="Disponibilidad del espacio | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DisponibilidadEspacio.aspx.cs" Inherits="StageUp.UI.DisponibilidadEspacio" %>

<asp:Content ID="DisponibilidadEspacioContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .da-barra { display: flex; flex-wrap: wrap; gap: 12px 18px; align-items: flex-end; justify-content: space-between; margin-bottom: 16px; }
        .da-barra .form-field { margin: 0; min-width: 240px; }
        .da-barra select { width: 100%; }
        .da-enlaces { display: flex; flex-wrap: wrap; gap: 14px; align-items: center; }
        .da-acciones { display: flex; flex-wrap: wrap; gap: 10px; margin: 0 0 16px; }
        .da-panel { margin: 0 0 16px; }
        .da-panel h2 { margin-top: 0; font-size: 1.15rem; }
        .da-campos { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 14px; }
        .da-campos > * { min-width: 0; }
        .da-campos input, .da-campos select, .da-ancho textarea { width: 100%; box-sizing: border-box; }
        .da-ancho { grid-column: 1 / -1; }
        .da-modo { display: flex; flex-wrap: wrap; gap: 6px 18px; margin: 0 0 12px; }
        .da-modo label, .da-dias label { font-weight: 500; }
        .da-dias { display: flex; flex-wrap: wrap; align-items: center; gap: 8px 4px; margin: 6px 0 14px; }
        .da-dias input, .da-modo input { margin: 0 4px 0 0; vertical-align: middle; }
        .da-dias label { display: inline; margin: 0 14px 0 0; cursor: pointer; }
        .da-modo label { display: inline; margin: 0 16px 0 0; }
        .da-check { display: flex; align-items: center; gap: 8px; align-self: end; padding-bottom: 10px; }
        .da-check span { display: inline-flex; align-items: center; gap: 8px; }
        .da-campos .da-check input { width: auto; margin: 0; }
        .da-check label { display: inline; margin: 0; font-weight: 600; }
        .da-ayuda { color: var(--color-text-muted); font-size: 0.88em; margin: 4px 0 0; }
        .da-detalle dl { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px 18px; margin: 0 0 12px; }
        .da-detalle dt { color: var(--color-text-muted); font-size: 0.82em; }
        .da-detalle dd { margin: 2px 0 0; font-weight: 600; }
        .da-confirmacion { margin-top: 12px; padding: 14px 16px; border-radius: 12px; border: 2px solid var(--color-primary); }
        .da-confirmacion p { margin: 0 0 12px; }

        .da-semana { display: flex; flex-wrap: wrap; gap: 8px 14px; align-items: center; justify-content: space-between; margin-bottom: 10px; }
        .da-semana strong { font-size: 1.02rem; }
        .da-calendario { display: grid; grid-template-columns: repeat(7, minmax(0, 1fr)); gap: 8px; }
        .da-dia { border: 1px solid var(--color-border); border-radius: 12px; padding: 8px; min-height: 120px; background: var(--color-surface); min-width: 0; }
        .da-dia-hoy { border: 2px solid var(--color-primary); }
        .da-dia h3 { margin: 0 0 6px; font-size: 0.86rem; font-family: var(--font-interface); }
        .da-dia h3 span { display: block; color: var(--color-text-muted); font-weight: 500; font-size: 0.8rem; }
        .da-dia-vacio { color: var(--color-text-muted); font-size: 0.8rem; }
        .da-item { display: block; margin: 0 0 5px; padding: 4px 6px; border-radius: 8px; font-size: 0.78rem; line-height: 1.3; border-left: 4px solid; overflow-wrap: anywhere; text-decoration: none; color: var(--color-text); }
        .da-item b { display: block; font-weight: 700; }
        .da-item-disponible { background: #e8f5ec; border-color: #2e7d4a; }
        .da-item-bloqueo { background: #fdecea; border-color: #b3261e; }
        .da-item-actividad { background: #f1ebf7; border-color: #6a4c93; }
        .da-item-reservaaceptada { background: #e7eef8; border-color: #2f5d9a; }
        .da-item-solicitudpendiente { background: #fff4e5; border-color: #c77700; }
        a.da-item:hover, a.da-item:focus-visible { text-decoration: underline; }
        .da-leyenda { display: flex; flex-wrap: wrap; gap: 6px 14px; margin: 10px 0 0; padding: 0; list-style: none; font-size: 0.82rem; color: var(--color-text-muted); }
        .da-leyenda li::before { content: ""; display: inline-block; width: 10px; height: 10px; border-radius: 3px; margin-right: 6px; vertical-align: -1px; }
        .da-leyenda .l-disponible::before { background: #2e7d4a; }
        .da-leyenda .l-bloqueo::before { background: #b3261e; }
        .da-leyenda .l-actividad::before { background: #6a4c93; }
        .da-leyenda .l-reserva::before { background: #2f5d9a; }
        .da-leyenda .l-pendiente::before { background: #c77700; }

        .da-listas { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; align-items: start; }
        .da-listas > * { min-width: 0; }
        .da-lista { list-style: none; margin: 0; padding: 0; }
        .da-lista li { display: flex; gap: 10px; justify-content: space-between; align-items: center; padding: 10px 0; border-bottom: 1px solid var(--color-border); }
        .da-lista li > div { min-width: 0; }
        .da-lista strong { display: block; }
        .da-lista span { display: block; color: var(--color-text-muted); font-size: 0.88em; overflow-wrap: anywhere; }
        .da-lista-seleccionada { background: var(--color-nude-light); }
        .da-vacio { color: var(--color-text-muted); margin: 0; }
        .button-danger { background: #b3261e; border-color: #b3261e; color: #fff; }
        .button-danger:hover, .button-danger:focus-visible { background: #8c1d18; border-color: #8c1d18; color: #fff; }

        @media (max-width: 900px) {
            .da-calendario { grid-template-columns: 1fr; }
            .da-dia { min-height: 0; }
            .da-listas { grid-template-columns: 1fr; }
        }
        @media (max-width: 560px) {
            .da-campos, .da-detalle dl { grid-template-columns: 1fr; }
            .da-barra .form-field { min-width: 0; width: 100%; }
        }
    </style>

    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Mis espacios</span>
            <h1>Disponibilidad del espacio</h1>
            <p><asp:Literal ID="litNombreEspacio" runat="server" Mode="Encode" /></p>
        </div>

        <div class="static-page-body">
            <nav class="gestor-subnav" aria-label="Navegación de gestor de espacios">
                <a href="~/MisEspacios.aspx" runat="server" class="active">Administrar espacios</a>
                <a href="~/SolicitudesRecibidas.aspx" runat="server">Solicitudes recibidas</a>
            </nav>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <%-- A1: el gestor no tiene espacios registrados. --%>
            <asp:Panel ID="pnlSinEspacios" runat="server" Visible="false" CssClass="auth-card">
                <h2>Todavía no tenés espacios</h2>
                <p>Para configurar disponibilidad primero tenés que registrar un espacio.</p>
                <asp:HyperLink ID="lnkAgregarEspacio" runat="server" CssClass="button button-primary" NavigateUrl="~/MisEspacios.aspx?nuevo=1" Text="Agregar espacio" />
            </asp:Panel>

            <asp:Panel ID="pnlNoDisponible" runat="server" Visible="false" CssClass="auth-card">
                <p><asp:Literal ID="litNoDisponible" runat="server" Mode="Encode" /></p>
                <a class="text-link" href="MisEspacios.aspx">Volver a Mis espacios</a>
            </asp:Panel>

            <asp:Panel ID="pnlContenido" runat="server" Visible="false">
                <div class="da-barra">
                    <div class="form-field">
                        <label for="<%= ddlEspacio.ClientID %>">Espacio</label>
                        <asp:DropDownList ID="ddlEspacio" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlEspacio_SelectedIndexChanged" />
                    </div>
                    <div class="da-enlaces">
                        <asp:HyperLink ID="lnkDetalleEspacio" runat="server" CssClass="text-link" Text="Ver detalle del espacio" />
                        <a class="text-link" href="MisEspacios.aspx">Volver a Mis espacios</a>
                    </div>
                </div>

                <div class="da-acciones">
                    <asp:Button ID="btnAgregar" runat="server" CssClass="button button-primary" Text="＋ Agregar disponibilidad" CausesValidation="false" OnClick="btnAgregar_Click" />
                    <asp:Button ID="btnBloquear" runat="server" CssClass="button button-secondary" Text="Bloquear horario" CausesValidation="false" OnClick="btnBloquear_Click" />
                </div>

                <%-- Pasos 10 a 13 y A6: agregar o editar disponibilidad. --%>
                <asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="auth-card da-panel" DefaultButton="btnGuardarDisponibilidad">
                    <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Agregar disponibilidad" /></h2>
                    <asp:RadioButtonList ID="rblModo" runat="server" RepeatLayout="Flow" RepeatDirection="Horizontal" CssClass="da-modo"
                        AutoPostBack="true" OnSelectedIndexChanged="rblModo_SelectedIndexChanged">
                        <asp:ListItem Value="Semanal" Selected="True">Todas las semanas</asp:ListItem>
                        <asp:ListItem Value="Fecha">Una fecha concreta (horario especial)</asp:ListItem>
                    </asp:RadioButtonList>

                    <asp:Panel ID="pnlDiasSemana" runat="server">
                        <span class="field-label">Días de la semana *</span>
                        <asp:CheckBoxList ID="cblDias" runat="server" RepeatLayout="Flow" RepeatDirection="Horizontal" CssClass="da-dias" />
                    </asp:Panel>
                    <asp:Panel ID="pnlDiaUnico" runat="server" Visible="false" CssClass="form-field">
                        <label for="<%= ddlDiaEdicion.ClientID %>">Día de la semana *</label>
                        <asp:DropDownList ID="ddlDiaEdicion" runat="server" />
                    </asp:Panel>
                    <asp:Panel ID="pnlFecha" runat="server" Visible="false" CssClass="form-field">
                        <label for="<%= txtFecha.ClientID %>">Fecha *</label>
                        <asp:TextBox ID="txtFecha" runat="server" TextMode="Date" />
                        <p class="da-ayuda">Ese día rige este horario en lugar del semanal.</p>
                    </asp:Panel>

                    <div class="da-campos">
                        <div class="form-field">
                            <label for="<%= txtDesde.ClientID %>">Horario de inicio *</label>
                            <asp:TextBox ID="txtDesde" runat="server" TextMode="Time" step="1800" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtHasta.ClientID %>">Horario de finalización *</label>
                            <asp:TextBox ID="txtHasta" runat="server" TextMode="Time" step="1800" />
                            <p class="da-ayuda">00:00 como fin indica el cierre del día. Horarios en punto o y media.</p>
                        </div>
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnGuardarDisponibilidad" runat="server" CssClass="button button-primary" Text="Guardar disponibilidad" CausesValidation="false" OnClick="btnGuardarDisponibilidad_Click" />
                        <asp:LinkButton ID="lnkCancelarFormulario" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarFormulario_Click">Cancelar</asp:LinkButton>
                    </div>
                </asp:Panel>

                <%-- A8: bloqueo manual de horario. --%>
                <asp:Panel ID="pnlBloqueo" runat="server" Visible="false" CssClass="auth-card da-panel" DefaultButton="btnGuardarBloqueo">
                    <h2>Bloquear horario</h2>
                    <p class="da-ayuda">El horario bloqueado no se va a poder elegir para nuevas reservas.</p>
                    <asp:RadioButtonList ID="rblModoBloqueo" runat="server" RepeatLayout="Flow" RepeatDirection="Horizontal" CssClass="da-modo"
                        AutoPostBack="true" OnSelectedIndexChanged="rblModoBloqueo_SelectedIndexChanged">
                        <asp:ListItem Value="Fecha" Selected="True">Un día concreto</asp:ListItem>
                        <asp:ListItem Value="Semanal">Todas las semanas</asp:ListItem>
                    </asp:RadioButtonList>
                    <div class="da-campos">
                        <asp:Panel ID="pnlFechaBloqueo" runat="server" CssClass="form-field">
                            <label for="<%= txtFechaBloqueo.ClientID %>">Día *</label>
                            <asp:TextBox ID="txtFechaBloqueo" runat="server" TextMode="Date" />
                        </asp:Panel>
                        <asp:Panel ID="pnlDiaBloqueo" runat="server" Visible="false" CssClass="form-field">
                            <label for="<%= ddlDiaBloqueo.ClientID %>">Día de la semana *</label>
                            <asp:DropDownList ID="ddlDiaBloqueo" runat="server" />
                        </asp:Panel>
                        <div class="form-field da-check">
                            <asp:CheckBox ID="chkDiaCompleto" runat="server" Text="Todo el día" AutoPostBack="true" OnCheckedChanged="chkDiaCompleto_CheckedChanged" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtDesdeBloqueo.ClientID %>">Horario de inicio *</label>
                            <asp:TextBox ID="txtDesdeBloqueo" runat="server" TextMode="Time" step="1800" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtHastaBloqueo.ClientID %>">Horario de finalización *</label>
                            <asp:TextBox ID="txtHastaBloqueo" runat="server" TextMode="Time" step="1800" />
                        </div>
                        <div class="form-field da-ancho">
                            <label for="<%= txtMotivoBloqueo.ClientID %>">Motivo del bloqueo *</label>
                            <asp:TextBox ID="txtMotivoBloqueo" runat="server" TextMode="MultiLine" Rows="2" MaxLength="300"
                                placeholder="Ej: mantenimiento del piso, evento privado, feriado." />
                        </div>
                    </div>
                    <div class="form-actions">
                        <asp:Button ID="btnGuardarBloqueo" runat="server" CssClass="button button-primary" Text="Guardar bloqueo" CausesValidation="false" OnClick="btnGuardarBloqueo_Click" />
                        <asp:LinkButton ID="lnkCancelarBloqueo" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarBloqueo_Click">Cancelar</asp:LinkButton>
                    </div>
                </asp:Panel>

                <%-- A6 / A10 pasos 1 y 2: detalle de la disponibilidad elegida. --%>
                <asp:Panel ID="pnlDetalle" runat="server" Visible="false" CssClass="auth-card da-panel da-detalle">
                    <h2>Disponibilidad seleccionada</h2>
                    <p><strong><asp:Literal ID="litDetalleCuando" runat="server" Mode="Encode" /></strong></p>
                    <dl>
                        <div><dt>Reservas aceptadas</dt><dd><asp:Literal ID="litDetalleAceptadas" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Solicitudes pendientes</dt><dd><asp:Literal ID="litDetallePendientes" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Actividades internas</dt><dd><asp:Literal ID="litDetalleActividades" runat="server" Mode="Encode" /></dd></div>
                    </dl>
                    <p class="da-ayuda"><asp:Literal ID="litDetalleNota" runat="server" Mode="Encode" /></p>
                    <div class="form-actions">
                        <asp:Button ID="btnEditarFranja" runat="server" CssClass="button button-primary" Text="Editar disponibilidad" CausesValidation="false" OnClick="btnEditarFranja_Click" />
                        <asp:Button ID="btnEliminarFranja" runat="server" CssClass="button button-secondary" Text="Eliminar disponibilidad" CausesValidation="false" OnClick="btnEliminarFranja_Click" />
                        <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCerrarDetalle_Click">Cerrar</asp:LinkButton>
                    </div>
                    <%-- A10 pasos 4 y 5. --%>
                    <asp:Panel ID="pnlConfirmarEliminar" runat="server" Visible="false" CssClass="da-confirmacion" role="alertdialog">
                        <p><strong>¿Eliminar esta disponibilidad?</strong> Ese horario deja de estar disponible para nuevas solicitudes de reserva.</p>
                        <asp:Button ID="btnConfirmarEliminar" runat="server" CssClass="button button-danger" Text="Sí, eliminar" CausesValidation="false" OnClick="btnConfirmarEliminar_Click" />
                        <asp:LinkButton ID="lnkCancelarEliminar" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarEliminar_Click">Cancelar</asp:LinkButton>
                    </asp:Panel>
                </asp:Panel>

                <%-- Paso 9: calendario de disponibilidad. --%>
                <div class="auth-card da-panel">
                    <div class="da-semana">
                        <strong><asp:Literal ID="litRangoSemana" runat="server" Mode="Encode" /></strong>
                        <div class="da-enlaces">
                            <asp:LinkButton ID="lnkSemanaAnterior" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkSemanaAnterior_Click">‹ Semana anterior</asp:LinkButton>
                            <asp:LinkButton ID="lnkSemanaActual" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkSemanaActual_Click">Esta semana</asp:LinkButton>
                            <asp:LinkButton ID="lnkSemanaSiguiente" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkSemanaSiguiente_Click">Semana siguiente ›</asp:LinkButton>
                        </div>
                    </div>
                    <div class="da-calendario">
                        <asp:Literal ID="litCalendario" runat="server" Mode="PassThrough" />
                    </div>
                    <ul class="da-leyenda">
                        <li class="l-disponible">Disponible</li>
                        <li class="l-bloqueo">Bloqueado</li>
                        <li class="l-actividad">Actividad interna</li>
                        <li class="l-reserva">Reserva aceptada</li>
                        <li class="l-pendiente">Solicitud pendiente</li>
                    </ul>
                </div>

                <div class="da-listas">
                    <div class="auth-card">
                        <h2>Días y franjas configurados</h2>
                        <asp:Literal ID="litSinDisponibilidad" runat="server" Visible="false" Text="&lt;p class=&quot;da-vacio&quot;&gt;Todavía no hay horarios disponibles. Usá «Agregar disponibilidad».&lt;/p&gt;" />
                        <ul class="da-lista">
                            <asp:Repeater ID="rptDisponibilidad" runat="server" OnItemCommand="rptDisponibilidad_ItemCommand">
                                <ItemTemplate>
                                    <li class='<%# EsSeleccionada(Container.DataItem) ? "da-lista-seleccionada" : string.Empty %>'>
                                        <div>
                                            <strong><%# Server.HtmlEncode(Cuando(Container.DataItem)) %></strong>
                                            <span><%# Server.HtmlEncode(Horario(Container.DataItem)) %></span>
                                        </div>
                                        <asp:LinkButton ID="lnkVer" runat="server" CssClass="button button-secondary button-small" CausesValidation="false"
                                            CommandName="Ver" CommandArgument='<%# Eval("IdFranjaEspacio") %>' Text="Ver" />
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                    <div class="auth-card">
                        <h2>Horarios bloqueados</h2>
                        <asp:Literal ID="litSinBloqueos" runat="server" Visible="false" Text="&lt;p class=&quot;da-vacio&quot;&gt;No hay bloqueos manuales. Los horarios de tus actividades internas se bloquean solos.&lt;/p&gt;" />
                        <ul class="da-lista">
                            <asp:Repeater ID="rptBloqueos" runat="server" OnItemCommand="rptBloqueos_ItemCommand">
                                <ItemTemplate>
                                    <li>
                                        <div>
                                            <strong><%# Server.HtmlEncode(Cuando(Container.DataItem)) %></strong>
                                            <span><%# Server.HtmlEncode(Horario(Container.DataItem)) %></span>
                                            <span><%# Server.HtmlEncode(Motivo(Container.DataItem)) %></span>
                                        </div>
                                        <asp:LinkButton ID="lnkQuitar" runat="server" CssClass="button button-secondary button-small" CausesValidation="false"
                                            CommandName="Quitar" CommandArgument='<%# Eval("IdFranjaEspacio") %>' Text="Quitar bloqueo"
                                            OnClientClick="return confirm('¿Quitar este bloqueo? El horario vuelve a estar disponible para reservas si está dentro de la disponibilidad.');" />
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
