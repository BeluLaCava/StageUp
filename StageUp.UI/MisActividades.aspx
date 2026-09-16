<%@ Page Title="Mis actividades | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MisActividades.aspx.cs" Inherits="StageUp.UI.MisActividades" %>

<asp:Content ID="MisActividadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page managed-activities">
        <div class="static-page-header managed-activities-header">
            <span class="section-label">Programación interna</span>
            <h1>Mis actividades</h1>
            <p>Organizá las clases, talleres y ensayos fijos de tus espacios para reflejar su ocupación real.</p>
            <asp:LinkButton ID="lnkNuevaActividad" runat="server" CssClass="button button-primary managed-activities-add" CausesValidation="false" OnClick="lnkNuevaActividad_Click"><span aria-hidden="true">＋</span> Nueva actividad</asp:LinkButton>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlPendienteGestor" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>Tu solicitud está pendiente de aprobación</h2>
                    <p>Cuando un administrador habilite tu perfil de gestor vas a poder registrar actividades internas para tus espacios.</p>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlNoGestor" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>Esta sección es para gestores de espacios</h2>
                    <p>Las actividades internas se cargan sobre espacios propios. Primero solicitá la habilitación como gestor para publicar y administrar tus espacios.</p>
                </div>
                <div class="form-actions">
                    <asp:Button ID="btnSolicitarGestor" runat="server" CssClass="button button-primary" Text="Solicitar ser gestor de espacios"
                        CausesValidation="false" OnClick="btnSolicitarGestor_Click" />
                    <a id="lnkVerMisReservas" class="text-link" href="~/MisReservas.aspx" runat="server">Ver mis reservas</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlPanelGestor" runat="server" Visible="false">
                <asp:Panel ID="pnlSinEspacios" runat="server" Visible="false" CssClass="managed-activities-empty">
                    <span aria-hidden="true">◇</span>
                    <h2>Primero cargá un espacio</h2>
                    <p>Para crear actividades internas necesitás tener al menos un espacio propio activo o en borrador.</p>
                    <a class="button button-secondary" href="MisEspacios.aspx">Agregar espacio</a>
                </asp:Panel>

                <asp:Panel ID="pnlActividades" runat="server" Visible="false">
                    <div class="activities-module-tabs gestor-subnav" role="tablist" aria-label="Secciones de Mis actividades">
                        <button type="button" class="active" data-activities-tab-trigger="programacion" aria-selected="true">Mis actividades</button>
                        <button type="button" data-activities-tab-trigger="participantes" aria-selected="false">Participantes</button>
                    </div>

                    <div class="activities-tab-panel active" data-activities-tab-panel="programacion">
                        <div class="activities-summary" aria-label="Resumen de actividades internas">
                            <article>
                                <small>Actividades activas</small>
                                <strong><asp:Literal ID="litActividadesActivas" runat="server" /></strong>
                            </article>
                            <article>
                                <small>Horas bloqueadas por semana</small>
                                <strong><asp:Literal ID="litHorasBloqueadas" runat="server" /></strong>
                            </article>
                            <article>
                                <small>Espacios con programación</small>
                                <strong><asp:Literal ID="litEspaciosProgramados" runat="server" /></strong>
                            </article>
                        </div>

                        <section class="activities-board" aria-labelledby="activities-board-title">
                            <header class="activities-board-header">
                                <div>
                                    <h2 id="activities-board-title">Programación cargada</h2>
                                    <p>Vista preparada para conectar con la gestión real de actividades internas.</p>
                                </div>
                                <div class="activities-board-filter" aria-label="Filtros visuales">
                                    <span>Todo</span>
                                    <span>Esta semana</span>
                                    <span>Activas</span>
                                </div>
                            </header>

                            <div class="activities-table-wrap">
                                <table class="activities-table">
                                    <thead>
                                        <tr>
                                            <th scope="col">Actividad</th>
                                            <th scope="col">Espacio</th>
                                            <th scope="col">Días</th>
                                            <th scope="col">Horario</th>
                                            <th scope="col">Cupo</th>
                                            <th scope="col">Estado</th>
                                            <th scope="col">Acciones</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="rptActividades" runat="server">
                                            <ItemTemplate>
                                                <tr>
                                                    <td>
                                                        <strong><%#: Eval("Nombre") %></strong>
                                                        <span><%#: Eval("Tipo") %></span>
                                                    </td>
                                                    <td><%#: Eval("Espacio") %></td>
                                                    <td><%#: Eval("Dias") %></td>
                                                    <td><%#: Eval("Horario") %></td>
                                                    <td><%#: Eval("Cupo") %></td>
                                                    <td><span class='<%# Eval("ClaseEstado") %>'><%#: Eval("Estado") %></span></td>
                                                    <td class="activities-row-actions">
                                                        <a class="text-link" href="#" onclick="return false;">Ver</a>
                                                        <a class="text-link" href="#" onclick="return false;">Editar</a>
                                                        <a class="text-link text-link-danger" href="#" onclick="return false;">Dar de baja</a>
                                                    </td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </div>
                        </section>
                    </div>

                    <div class="activities-tab-panel" data-activities-tab-panel="participantes">
                        <div class="participants-heading">
                            <div>
                                <span class="section-label">Gestión interna</span>
                                <h2>Participantes</h2>
                                <p>Administrá las personas que forman parte de tus clases, talleres o ensayos fijos.</p>
                            </div>
                            <asp:LinkButton ID="lnkNuevoParticipante" runat="server" CssClass="button button-primary" CausesValidation="false" OnClick="lnkNuevoParticipante_Click"><span aria-hidden="true">＋</span> Nuevo participante</asp:LinkButton>
                        </div>

                        <div class="activities-summary participants-summary" aria-label="Resumen de participantes">
                            <article>
                                <small>Participantes activos</small>
                                <strong><asp:Literal ID="litParticipantesActivos" runat="server" /></strong>
                            </article>
                            <article>
                                <small>Asociados a actividades</small>
                                <strong><asp:Literal ID="litParticipantesAsignados" runat="server" /></strong>
                            </article>
                            <article>
                                <small>Sin actividad asignada</small>
                                <strong><asp:Literal ID="litParticipantesSinAsignar" runat="server" /></strong>
                            </article>
                        </div>

                        <section class="activities-board participants-board" aria-labelledby="participants-board-title">
                            <header class="activities-board-header">
                                <div>
                                    <h2 id="participants-board-title">Listado de participantes</h2>
                                    <p>Vista preparada para alta, modificación, baja lógica y asociación a actividades.</p>
                                </div>
                                <div class="participants-search-preview">
                                    <span aria-hidden="true">⌕</span>
                                    <input type="search" placeholder="Buscar participante" aria-label="Buscar participante" />
                                </div>
                            </header>

                            <div class="activities-table-wrap">
                                <table class="activities-table participants-table">
                                    <thead>
                                        <tr>
                                            <th scope="col">Participante</th>
                                            <th scope="col">DNI</th>
                                            <th scope="col">Actividad asociada</th>
                                            <th scope="col">Estado</th>
                                            <th scope="col">Acciones</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="rptParticipantes" runat="server">
                                            <ItemTemplate>
                                                <tr>
                                                    <td>
                                                        <strong><%#: Eval("NombreCompleto") %></strong>
                                                        <span><%#: Eval("Iniciales") %></span>
                                                    </td>
                                                    <td><%#: Eval("Dni") %></td>
                                                    <td><%#: Eval("Actividades") %></td>
                                                    <td><span class='<%# Eval("ClaseEstado") %>'><%#: Eval("Estado") %></span></td>
                                                    <td class="activities-row-actions">
                                                        <a class="text-link" href="#" onclick="return false;">Ver</a>
                                                        <a class="text-link" href="#" onclick="return false;">Editar</a>
                                                        <a class="text-link text-link-danger" href="#" onclick="return false;">Dar de baja</a>
                                                    </td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </div>
                        </section>
                    </div>
                </asp:Panel>
            </asp:Panel>

            <asp:Panel ID="pnlFormularioActividad" runat="server" Visible="false" CssClass="activity-dialog-layer">
                <dialog class="activity-editor" open aria-labelledby="activity-form-title">
                    <asp:LinkButton ID="lnkCerrarActividad" runat="server" CssClass="activity-editor-close" CausesValidation="false" OnClick="lnkCerrarActividad_Click" aria-label="Cerrar">×</asp:LinkButton>
                    <div class="auth-card-header">
                        <h2 id="activity-form-title">Nueva actividad interna</h2>
                        <p>Cargá la programación estable del espacio. Estos horarios se usarán luego para bloquear disponibilidad frente a reservas externas.</p>
                    </div>

                    <div class="activity-form-grid">
                        <div class="form-field activity-field-wide">
                            <label for="<%= txtNombreActividad.ClientID %>">Nombre de la actividad *</label>
                            <asp:TextBox ID="txtNombreActividad" runat="server" MaxLength="200" placeholder="Ej: Ballet principiantes" />
                            <asp:RequiredFieldValidator ID="rfvNombreActividad" runat="server" ControlToValidate="txtNombreActividad"
                                ValidationGroup="Actividad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre de la actividad." />
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlEspacio.ClientID %>">Espacio asociado *</label>
                            <asp:DropDownList ID="ddlEspacio" runat="server" />
                            <asp:RequiredFieldValidator ID="rfvEspacio" runat="server" ControlToValidate="ddlEspacio" InitialValue=""
                                ValidationGroup="Actividad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Seleccioná un espacio." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtTipoActividad.ClientID %>">Tipo de actividad</label>
                            <asp:TextBox ID="txtTipoActividad" runat="server" MaxLength="120" placeholder="Clase, taller, ensayo..." />
                        </div>

                        <div class="form-field activity-field-wide activity-schedule-box">
                            <span class="activity-schedule-title">Programación *</span>
                            <p>Definí si la actividad ocupa el espacio todas las semanas, una vez al mes o en una fecha puntual.</p>
                            <asp:HiddenField ID="hdnProgramacionActividad" runat="server" ClientIDMode="Static" Value="" />

                            <div class="activity-schedule-grid">
                                <div class="form-field">
                                    <label for="activity-repeat-mode">¿Cuándo se repite?</label>
                                    <select id="activity-repeat-mode">
                                        <option value="weekly">Todas las semanas</option>
                                        <option value="monthly">Una vez al mes</option>
                                        <option value="date">Una fecha concreta</option>
                                    </select>
                                </div>
                                <div class="form-field" data-activity-date-wrap hidden>
                                    <label for="activity-date">Fecha</label>
                                    <input id="activity-date" type="date" />
                                </div>
                                <div class="form-field" data-activity-monthly-wrap hidden>
                                    <label for="activity-month-week">Semana del mes</label>
                                    <select id="activity-month-week">
                                        <option value="first">Primera</option>
                                        <option value="second">Segunda</option>
                                        <option value="third">Tercera</option>
                                        <option value="fourth">Cuarta</option>
                                        <option value="last">Última</option>
                                    </select>
                                </div>
                                <div class="form-field" data-activity-monthly-wrap hidden>
                                    <label for="activity-month-day">Día</label>
                                    <select id="activity-month-day">
                                        <option value="Lun">Lunes</option>
                                        <option value="Mar">Martes</option>
                                        <option value="Mié">Miércoles</option>
                                        <option value="Jue">Jueves</option>
                                        <option value="Vie">Viernes</option>
                                        <option value="Sáb">Sábado</option>
                                        <option value="Dom">Domingo</option>
                                    </select>
                                </div>
                            </div>

                            <div data-activity-days-wrap>
                                <label>Días de la semana</label>
                                <asp:CheckBoxList ID="cblDias" runat="server" RepeatLayout="Flow" CssClass="activity-days">
                                    <asp:ListItem Value="Lun">Lun</asp:ListItem>
                                    <asp:ListItem Value="Mar">Mar</asp:ListItem>
                                    <asp:ListItem Value="Mié">Mié</asp:ListItem>
                                    <asp:ListItem Value="Jue">Jue</asp:ListItem>
                                    <asp:ListItem Value="Vie">Vie</asp:ListItem>
                                    <asp:ListItem Value="Sáb">Sáb</asp:ListItem>
                                    <asp:ListItem Value="Dom">Dom</asp:ListItem>
                                </asp:CheckBoxList>
                            </div>

                            <div class="activity-schedule-times">
                                <div class="form-field">
                                    <label for="<%= txtHoraInicio.ClientID %>">Horario de inicio *</label>
                                    <asp:TextBox ID="txtHoraInicio" runat="server" TextMode="Time" />
                                    <asp:RequiredFieldValidator ID="rfvHoraInicio" runat="server" ControlToValidate="txtHoraInicio"
                                        ValidationGroup="Actividad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el horario de inicio." />
                                </div>

                                <div class="form-field">
                                    <label for="<%= txtHoraFin.ClientID %>">Horario de fin *</label>
                                    <asp:TextBox ID="txtHoraFin" runat="server" TextMode="Time" />
                                    <asp:RequiredFieldValidator ID="rfvHoraFin" runat="server" ControlToValidate="txtHoraFin"
                                        ValidationGroup="Actividad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el horario de fin." />
                                </div>
                            </div>
                            <small id="activity-schedule-preview">Se bloqueará el espacio en los días seleccionados y el rango horario indicado.</small>
                        </div>

                        <div class="form-field">
                            <label for="<%= txtCupoMaximo.ClientID %>">Cupo máximo *</label>
                            <asp:TextBox ID="txtCupoMaximo" runat="server" TextMode="Number" min="1" max="10000" step="1" placeholder="Ej: 18" />
                            <asp:RequiredFieldValidator ID="rfvCupoMaximo" runat="server" ControlToValidate="txtCupoMaximo"
                                ValidationGroup="Actividad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el cupo máximo." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtParticipantes.ClientID %>">Participantes estimados</label>
                            <asp:TextBox ID="txtParticipantes" runat="server" TextMode="Number" min="0" max="10000" step="1" placeholder="Opcional" />
                        </div>

                        <div class="form-field activity-field-wide">
                            <label for="participant-search">Participantes asociados</label>
                            <div class="participant-picker" data-participant-picker>
                                <input id="participant-search" type="search" autocomplete="off" placeholder="Buscar por nombre, apellido o DNI" data-participant-search />
                                <div class="participant-suggestions" data-participant-suggestions aria-label="Sugerencias de participantes">
                                    <button type="button" data-participant-id="101" data-participant-name="Juana López">Juana López <span>DNI 42111222</span></button>
                                    <button type="button" data-participant-id="102" data-participant-name="Lucía Fernández">Lucía Fernández <span>DNI 39888777</span></button>
                                    <button type="button" data-participant-id="103" data-participant-name="Martín Álvarez">Martín Álvarez <span>DNI 40555111</span></button>
                                    <button type="button" data-participant-id="104" data-participant-name="Camila Ruiz">Camila Ruiz <span>DNI 44777222</span></button>
                                </div>
                                <div class="participant-chip-list" data-participant-chips aria-live="polite"></div>
                                <asp:HiddenField ID="hdnParticipantesActividad" runat="server" ClientIDMode="Static" Value="" />
                            </div>
                            <small>El buscador simula la selección múltiple. Después se podrá conectar al listado real de participantes del gestor.</small>
                        </div>

                        <div class="form-field activity-field-wide">
                            <label for="<%= txtDescripcionActividad.ClientID %>">Notas internas</label>
                            <asp:TextBox ID="txtDescripcionActividad" runat="server" TextMode="MultiLine" Rows="4" MaxLength="1000" placeholder="Ej: Grupo regular de la escuela. Mantener salón libre 15 minutos antes para preparación." />
                        </div>
                    </div>

                    <div class="activity-editor-note">
                        <strong>Impacto esperado:</strong> al conectar el back, esta actividad bloqueará automáticamente ese espacio en los días y horarios indicados.
                    </div>

                    <div class="form-actions activity-editor-actions">
                        <asp:Button ID="btnGuardarActividad" runat="server" CssClass="button button-primary" ValidationGroup="Actividad"
                            Text="Guardar actividad" OnClick="btnGuardarActividad_Click" />
                        <asp:LinkButton ID="lnkCancelarActividad" runat="server" CssClass="text-link" CausesValidation="false"
                            Text="Cancelar" OnClick="lnkCerrarActividad_Click" />
                    </div>
                </dialog>
            </asp:Panel>

            <asp:Panel ID="pnlFormularioParticipante" runat="server" Visible="false" CssClass="activity-dialog-layer">
                <dialog class="activity-editor participant-editor" open aria-labelledby="participant-form-title">
                    <asp:LinkButton ID="lnkCerrarParticipante" runat="server" CssClass="activity-editor-close" CausesValidation="false" OnClick="lnkCerrarParticipante_Click" aria-label="Cerrar">×</asp:LinkButton>
                    <div class="auth-card-header">
                        <h2 id="participant-form-title">Nuevo participante</h2>
                        <p>Registrá a una persona para asociarla luego a una o más actividades internas.</p>
                    </div>

                    <div class="activity-form-grid">
                        <div class="form-field">
                            <label for="<%= txtNombreParticipante.ClientID %>">Nombre *</label>
                            <asp:TextBox ID="txtNombreParticipante" runat="server" MaxLength="120" placeholder="Ej: Juana" />
                            <asp:RequiredFieldValidator ID="rfvNombreParticipante" runat="server" ControlToValidate="txtNombreParticipante"
                                ValidationGroup="Participante" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtApellidoParticipante.ClientID %>">Apellido *</label>
                            <asp:TextBox ID="txtApellidoParticipante" runat="server" MaxLength="120" placeholder="Ej: López" />
                            <asp:RequiredFieldValidator ID="rfvApellidoParticipante" runat="server" ControlToValidate="txtApellidoParticipante"
                                ValidationGroup="Participante" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el apellido." />
                        </div>

                        <div class="form-field">
                            <label for="<%= txtDniParticipante.ClientID %>">DNI *</label>
                            <asp:TextBox ID="txtDniParticipante" runat="server" MaxLength="20" placeholder="Ej: 42111222" />
                            <asp:RequiredFieldValidator ID="rfvDniParticipante" runat="server" ControlToValidate="txtDniParticipante"
                                ValidationGroup="Participante" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el DNI." />
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlActividadParticipante.ClientID %>">Asociar a actividad</label>
                            <asp:DropDownList ID="ddlActividadParticipante" runat="server">
                                <asp:ListItem Value="">Sin asociar por ahora</asp:ListItem>
                                <asp:ListItem Value="Ballet principiantes">Ballet principiantes</asp:ListItem>
                                <asp:ListItem Value="Ensayo compañía estable">Ensayo compañía estable</asp:ListItem>
                                <asp:ListItem Value="Taller de montaje escénico">Taller de montaje escénico</asp:ListItem>
                            </asp:DropDownList>
                        </div>

                        <div class="form-field activity-field-wide">
                            <label for="<%= txtNotasParticipante.ClientID %>">Notas internas</label>
                            <asp:TextBox ID="txtNotasParticipante" runat="server" TextMode="MultiLine" Rows="4" MaxLength="800" placeholder="Datos útiles para la gestión interna del espacio." />
                        </div>
                    </div>

                    <div class="activity-editor-note">
                        <strong>Alcance UI:</strong> el DNI y el cupo se validarán al conectar la lógica de participantes.
                    </div>

                    <div class="form-actions activity-editor-actions">
                        <asp:Button ID="btnGuardarParticipante" runat="server" CssClass="button button-primary" ValidationGroup="Participante"
                            Text="Guardar participante" OnClick="btnGuardarParticipante_Click" />
                        <asp:LinkButton ID="lnkCancelarParticipante" runat="server" CssClass="text-link" CausesValidation="false"
                            Text="Cancelar" OnClick="lnkCerrarParticipante_Click" />
                    </div>
                </dialog>
            </asp:Panel>
        </div>
    </section>
</asp:Content>

<asp:Content ID="MisActividadesStyles" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .managed-activities { max-width: 1400px; margin: 0 auto; padding-left: 28px; padding-right: 28px; }
        .managed-activities-header { position: relative; padding-right: 230px; }
        .managed-activities-add { position: absolute; right: 0; top: 28px; gap: 10px; }
        .managed-activities-add span { font-size: 24px; line-height: 1; }
        .activities-module-tabs { gap: 0.5rem; margin-bottom: 1.75rem; }
        .activities-module-tabs button { display: inline-flex; align-items: center; min-height: 46px; padding: 0.85rem 0.25rem; margin-right: 1.75rem; color: var(--color-text-muted); background: transparent; border: 0; border-bottom: 3px solid transparent; font: inherit; font-weight: 600; cursor: pointer; }
        .activities-module-tabs button:hover { color: var(--color-primary); }
        .activities-module-tabs button.active { color: var(--color-primary); border-bottom-color: var(--color-primary); }
        .activities-tab-panel { display: none; }
        .activities-tab-panel.active { display: block; }
        .activities-summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 18px; margin-bottom: 22px; }
        .activities-summary article { padding: 18px 20px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 18px; box-shadow: 0 8px 24px rgba(77,11,23,.05); }
        .activities-summary small { display: block; color: var(--color-text-muted); font-weight: 700; }
        .activities-summary strong { display: block; margin-top: 6px; color: var(--color-primary); font-family: var(--font-display); font-size: 32px; line-height: 1; }
        .activities-board { overflow: hidden; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 22px; box-shadow: 0 8px 24px rgba(77,11,23,.05); }
        .activities-board-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; padding: 24px; border-bottom: 1px solid var(--color-border); }
        .activities-board-header h2 { margin: 0; color: var(--color-primary); font-size: 28px; }
        .activities-board-header p { margin: 6px 0 0; color: var(--color-text-muted); }
        .activities-board-filter { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; }
        .activities-board-filter span { padding: 8px 12px; color: var(--color-primary); background: var(--color-nude-light); border: 1px solid rgba(109,16,33,.16); border-radius: 999px; font-size: 13px; font-weight: 700; }
        .activities-table-wrap { overflow-x: auto; }
        .activities-table { width: 100%; border-collapse: collapse; min-width: 880px; }
        .activities-table th { padding: 14px 18px; color: var(--color-brown-dark); background: rgba(243,223,209,.36); font-size: 13px; text-align: left; }
        .activities-table td { padding: 18px; border-top: 1px solid var(--color-border); color: var(--color-text-muted); vertical-align: top; }
        .activities-table td strong { display: block; color: var(--color-primary); font-size: 17px; }
        .activities-table td span { display: block; margin-top: 4px; }
        .activities-status { display: inline-flex !important; width: fit-content; margin: 0 !important; padding: 5px 10px; border-radius: 999px; font-size: 12px; font-weight: 800; }
        .activities-status-active { color: #286345; background: #e8f4ec; }
        .activities-status-draft { color: #835c20; background: #fff1d7; }
        .activities-row-actions { white-space: nowrap; }
        .activities-row-actions .text-link { margin-right: 14px; }
        .text-link-danger { color: #a32132; }
        .participants-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 22px; margin-bottom: 22px; padding: 24px; background: rgba(255,252,250,.78); border: 1px solid var(--color-border); border-radius: 22px; }
        .participants-heading h2 { margin: 4px 0 0; color: var(--color-primary); font-size: 30px; }
        .participants-heading p { margin: 8px 0 0; color: var(--color-text-muted); }
        .participants-heading .button { flex: 0 0 auto; gap: 8px; }
        .participants-summary { margin-bottom: 22px; }
        .participants-search-preview { min-width: min(100%, 280px); display: flex; align-items: center; gap: 8px; padding: 8px 12px; background: var(--color-nude-light); border: 1px solid var(--color-border); border-radius: 999px; }
        .participants-search-preview span { color: var(--color-primary); font-size: 18px; }
        .participants-search-preview input { width: 100%; min-width: 0; border: 0; outline: 0; background: transparent; color: var(--color-text); font: inherit; }
        .participants-table { min-width: 760px; }
        .managed-activities-empty { padding: 58px 24px; text-align: center; border: 1px dashed var(--color-border); border-radius: 20px; background: var(--color-surface); }
        .managed-activities-empty span { color: var(--color-primary); font-size: 44px; }
        .managed-activities-empty h2 { margin: 10px 0 8px; color: var(--color-primary); }
        .activity-dialog-layer { position: fixed; inset: 0; z-index: 200; display: grid; place-items: center; padding: 24px; background: rgba(35,18,23,.65); backdrop-filter: blur(4px); }
        .activity-editor { position: relative; display: block; width: min(920px, calc(100vw - 32px)); max-height: calc(100vh - 48px); overflow: auto; margin: 0; border: 0; border-radius: 24px; padding: 32px; color: var(--color-text); background: var(--color-surface); box-shadow: 0 24px 100px rgba(0,0,0,.25); }
        .activity-editor-close { position: absolute; top: 16px; right: 18px; border: 0; background: transparent; color: var(--color-primary); font-size: 30px; cursor: pointer; width: 40px; height: 40px; }
        .activity-form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 20px; }
        .activity-field-wide { grid-column: 1 / -1; }
        .activity-editor textarea { display: block; width: 100%; min-height: 110px; resize: vertical; padding: 14px 16px; border: 1px solid var(--color-border); border-radius: 12px; background: var(--color-nude-light); color: var(--color-text); font: inherit; line-height: 1.6; box-sizing: border-box; }
        .activity-editor input:not([type=checkbox]):not([type=submit]):not([type=hidden]), .activity-editor select { width: 100%; padding: 12px 14px; border-radius: 10px; border: 1px solid var(--color-border); background: var(--color-surface); color: var(--color-text); font: inherit; box-sizing: border-box; }
        .activity-editor input:focus, .activity-editor textarea:focus, .activity-editor select:focus { outline: 2px solid var(--color-primary-soft); outline-offset: 2px; }
        .activity-schedule-box { margin-top: 4px; padding: 18px; background: var(--color-nude-light); border: 1px solid var(--color-border); border-radius: 18px; }
        .activity-schedule-title { display: block; margin-bottom: 6px; color: var(--color-primary); font-weight: 800; }
        .activity-schedule-box p { margin: 0 0 14px; color: var(--color-text-muted); font-size: 14px; line-height: 1.5; }
        .activity-schedule-grid, .activity-schedule-times { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 18px; }
        .activity-schedule-box [hidden] { display: none !important; }
        .activity-days { display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 12px; }
        .activity-days label { display: inline-flex; align-items: center; gap: 6px; margin: 0 6px 6px 0; padding: 9px 11px; background: var(--color-nude-light); border: 1px solid var(--color-border); border-radius: 999px; }
        .activity-days br { display: none; }
        .activity-days input { accent-color: var(--color-primary); }
        .participant-picker { position: relative; display: grid; gap: 10px; }
        .participant-picker > input { width: 100%; }
        .participant-suggestions { display: none; max-height: 170px; overflow: auto; padding: 8px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 14px; box-shadow: 0 12px 28px rgba(77,11,23,.09); }
        .participant-picker.is-open .participant-suggestions { display: grid; gap: 6px; }
        .participant-suggestions button { display: flex; align-items: center; justify-content: space-between; gap: 14px; width: 100%; padding: 10px 12px; color: var(--color-primary); background: var(--color-nude-light); border: 1px solid transparent; border-radius: 10px; text-align: left; font: inherit; font-weight: 800; cursor: pointer; }
        .participant-suggestions button:hover, .participant-suggestions button:focus-visible { border-color: rgba(109,16,33,.3); }
        .participant-suggestions button[hidden] { display: none; }
        .participant-suggestions span { color: var(--color-text-muted); font-size: 12px; font-weight: 600; }
        .participant-chip-list { display: flex; flex-wrap: wrap; gap: 8px; min-height: 34px; }
        .participant-chip { display: inline-flex; align-items: center; gap: 8px; padding: 7px 10px; color: var(--color-primary); background: var(--color-nude-light); border: 1px solid rgba(109,16,33,.18); border-radius: 999px; font-size: 13px; font-weight: 800; }
        .participant-chip button { width: 20px; height: 20px; display: grid; place-items: center; padding: 0; color: var(--color-primary); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 50%; cursor: pointer; }
        .activity-editor-note { margin-top: 8px; padding: 14px 16px; color: var(--color-text-muted); background: var(--color-nude-light); border-left: 3px solid var(--color-primary); border-radius: 12px; line-height: 1.5; }
        .activity-editor-actions { justify-content: flex-end; margin-top: 22px; padding-top: 20px; border-top: 1px solid var(--color-border); }
        body:has(.activity-dialog-layer) { overflow: hidden; }
        @media (max-width: 800px) {
            .managed-activities-header { padding-right: 0; }
            .managed-activities-add { position: static; margin-top: 20px; }
            .activities-module-tabs { width: 100%; }
            .activities-module-tabs button { flex: 1; }
            .activity-schedule-grid, .activity-schedule-times { grid-template-columns: 1fr; }
            .activities-summary { grid-template-columns: 1fr; }
            .activities-board-header { display: grid; }
            .activities-board-filter { justify-content: flex-start; }
            .participants-heading { display: grid; }
            .participants-heading .button { width: 100%; }
            .activity-form-grid { grid-template-columns: 1fr; }
            .activity-field-wide { grid-column: auto; }
            .activity-editor { padding: 24px; }
        }
    </style>
</asp:Content>

<asp:Content ID="MisActividadesScripts" ContentPlaceHolderID="PageScripts" runat="server">
    <script>
        (function () {
            var tabButtons = document.querySelectorAll('[data-activities-tab-trigger]');
            var tabPanels = document.querySelectorAll('[data-activities-tab-panel]');
            for (var i = 0; i < tabButtons.length; i++) {
                tabButtons[i].addEventListener('click', function () {
                    var target = this.getAttribute('data-activities-tab-trigger');
                    for (var b = 0; b < tabButtons.length; b++) {
                        var isActiveButton = tabButtons[b] === this;
                        tabButtons[b].classList.toggle('active', isActiveButton);
                        tabButtons[b].setAttribute('aria-selected', isActiveButton ? 'true' : 'false');
                    }
                    for (var p = 0; p < tabPanels.length; p++) {
                        tabPanels[p].classList.toggle('active', tabPanels[p].getAttribute('data-activities-tab-panel') === target);
                    }
                });
            }

            var picker = document.querySelector('[data-participant-picker]');
            var repeatMode = document.getElementById('activity-repeat-mode');
            var scheduleHidden = document.getElementById('hdnProgramacionActividad');
            var schedulePreview = document.getElementById('activity-schedule-preview');
            var dayWrap = document.querySelector('[data-activity-days-wrap]');
            var dateWrap = document.querySelector('[data-activity-date-wrap]');
            var monthlyWraps = document.querySelectorAll('[data-activity-monthly-wrap]');
            var dayChecks = document.querySelectorAll('.activity-days input[type="checkbox"]');
            var dateInput = document.getElementById('activity-date');
            var monthWeek = document.getElementById('activity-month-week');
            var monthDay = document.getElementById('activity-month-day');
            var hourFrom = document.getElementById('<%= txtHoraInicio.ClientID %>');
            var hourTo = document.getElementById('<%= txtHoraFin.ClientID %>');

            function selectedDaysText() {
                var days = [];
                for (var i = 0; i < dayChecks.length; i++) {
                    if (dayChecks[i].checked) {
                        days.push(dayChecks[i].value);
                    }
                }
                return days;
            }

            function updateActivitySchedule() {
                if (!repeatMode) {
                    return;
                }

                var mode = repeatMode.value;
                if (dayWrap) {
                    dayWrap.hidden = mode !== 'weekly';
                }
                if (dateWrap) {
                    dateWrap.hidden = mode !== 'date';
                }
                for (var i = 0; i < monthlyWraps.length; i++) {
                    monthlyWraps[i].hidden = mode !== 'monthly';
                }

                var days = selectedDaysText();
                var data = {
                    modo: mode,
                    dias: days,
                    fecha: dateInput ? dateInput.value : '',
                    semanaDelMes: monthWeek ? monthWeek.value : '',
                    diaDelMes: monthDay ? monthDay.value : '',
                    desde: hourFrom ? hourFrom.value : '',
                    hasta: hourTo ? hourTo.value : ''
                };

                if (scheduleHidden) {
                    scheduleHidden.value = JSON.stringify(data);
                }

                if (!schedulePreview) {
                    return;
                }

                if (mode === 'weekly') {
                    schedulePreview.textContent = days.length
                        ? 'Se bloqueará todas las semanas: ' + days.join(', ') + '.'
                        : 'Seleccioná uno o más días semanales para bloquear el espacio.';
                } else if (mode === 'monthly') {
                    schedulePreview.textContent = 'Se bloqueará una vez al mes: ' + monthWeek.options[monthWeek.selectedIndex].text.toLowerCase() + ' semana, día ' + monthDay.options[monthDay.selectedIndex].text.toLowerCase() + '.';
                } else {
                    schedulePreview.textContent = dateInput && dateInput.value
                        ? 'Se bloqueará únicamente la fecha seleccionada.'
                        : 'Seleccioná la fecha puntual de la actividad.';
                }

                if (data.desde && data.hasta) {
                    schedulePreview.textContent += ' Horario: ' + data.desde + ' a ' + data.hasta + '.';
                }
            }

            if (repeatMode) {
                repeatMode.addEventListener('change', updateActivitySchedule);
                if (dateInput) { dateInput.addEventListener('change', updateActivitySchedule); }
                if (monthWeek) { monthWeek.addEventListener('change', updateActivitySchedule); }
                if (monthDay) { monthDay.addEventListener('change', updateActivitySchedule); }
                if (hourFrom) { hourFrom.addEventListener('input', updateActivitySchedule); }
                if (hourTo) { hourTo.addEventListener('input', updateActivitySchedule); }
                for (var d = 0; d < dayChecks.length; d++) {
                    dayChecks[d].addEventListener('change', updateActivitySchedule);
                }
                updateActivitySchedule();
            }

            if (!picker) {
                return;
            }

            var search = picker.querySelector('[data-participant-search]');
            var suggestions = picker.querySelector('[data-participant-suggestions]');
            var options = suggestions ? suggestions.querySelectorAll('button[data-participant-id]') : [];
            var chips = picker.querySelector('[data-participant-chips]');
            var hidden = document.getElementById('hdnParticipantesActividad');
            var selected = [];

            function normalize(value) {
                return (value || '').toLowerCase();
            }

            function updateHidden() {
                if (hidden) {
                    hidden.value = selected.join(',');
                }
            }

            function addParticipant(id, name) {
                if (selected.indexOf(id) !== -1) {
                    return;
                }

                selected.push(id);
                var chip = document.createElement('span');
                chip.className = 'participant-chip';
                chip.setAttribute('data-participant-chip', id);
                chip.appendChild(document.createTextNode(name));

                var removeButton = document.createElement('button');
                removeButton.type = 'button';
                removeButton.setAttribute('aria-label', 'Quitar ' + name);
                removeButton.textContent = '×';
                removeButton.addEventListener('click', function () {
                    selected = selected.filter(function (selectedId) { return selectedId !== id; });
                    chip.remove();
                    updateHidden();
                });

                chip.appendChild(removeButton);
                chips.appendChild(chip);
                search.value = '';
                picker.classList.remove('is-open');
                updateHidden();
            }

            search.addEventListener('focus', function () {
                picker.classList.add('is-open');
            });

            search.addEventListener('input', function () {
                var query = normalize(search.value);
                picker.classList.add('is-open');
                for (var i = 0; i < options.length; i++) {
                    options[i].hidden = normalize(options[i].textContent).indexOf(query) === -1;
                }
            });

            for (var i = 0; i < options.length; i++) {
                options[i].addEventListener('click', function () {
                    addParticipant(this.getAttribute('data-participant-id'), this.getAttribute('data-participant-name'));
                });
            }

            document.addEventListener('click', function (event) {
                if (!picker.contains(event.target)) {
                    picker.classList.remove('is-open');
                }
            });
        })();
    </script>
</asp:Content>
