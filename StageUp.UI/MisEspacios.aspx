<%@ Page Title="Mis espacios | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MisEspacios.aspx.cs" Inherits="StageUp.UI.MisEspacios" %>

<asp:Content ID="MisEspaciosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page managed-spaces">
        <div class="static-page-header managed-spaces-header">
            <span class="section-label">Tu espacio creativo</span>
            <h1>Mis espacios</h1>
            <p>Un lugar para administrar los espacios donde el arte sucede.</p>
            <asp:LinkButton ID="lnkNuevoEspacio" runat="server" CssClass="button button-primary managed-spaces-add" CausesValidation="false" OnClick="lnkNuevoEspacio_Click"><span aria-hidden="true">＋</span> Agregar espacio</asp:LinkButton>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlPendienteGestor" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>Tu solicitud está pendiente de aprobación</h2>
                    <p>Pediste habilitarte como gestor de espacios. En cuanto un administrador de StageUp la revise vas a poder publicar y administrar tus propios espacios acá.</p>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlNoGestor" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>Todavía no sos gestor de espacios</h2>
                    <p>Esta sección es para administrar espacios artísticos propios. Si querés publicar y ofrecer un espacio, primero tenés que solicitar la habilitación como gestor.</p>
                </div>
                <div class="form-actions">
                    <asp:Button ID="btnSolicitarGestor" runat="server" CssClass="button button-primary" Text="Solicitar ser gestor de espacios"
                        CausesValidation="false" OnClick="btnSolicitarGestor_Click" />
                    <a id="lnkVerMisReservas" class="text-link" href="~/MisReservas.aspx" runat="server">Ver mis reservas</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlPanelGestor" runat="server" Visible="false">

            <nav class="gestor-subnav" aria-label="Navegación de gestor de espacios">
                <a href="~/MisEspacios.aspx" runat="server" class="active">Administrar espacios</a>
                <a href="~/SolicitudesRecibidas.aspx" runat="server">Solicitudes recibidas<asp:Literal ID="litBadgeSolicitudes" runat="server" /></a>
            </nav>

            <div class="managed-spaces-list">

                <asp:Literal ID="litSinEspacios" runat="server" Visible="false" Text="&lt;div class=&quot;managed-spaces-empty&quot;&gt;&lt;h2&gt;Aún no tienes espacios cargados&lt;/h2&gt;&lt;p&gt;Agregá tu primer espacio y preparalo para compartirlo.&lt;/p&gt;&lt;/div&gt;" />

                <div class="managed-spaces-grid">
                <asp:Repeater ID="rptMisEspacios" runat="server" OnItemCommand="rptMisEspacios_ItemCommand" OnItemDataBound="rptMisEspacios_ItemDataBound">
                    <ItemTemplate>
                        <article class="managed-space-card">
                            <div class="managed-space-media">
                                <asp:Image ID="imgEspacio" runat="server" ImageUrl='<%# ObtenerFoto((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %>' Visible='<%# !string.IsNullOrEmpty(ObtenerFoto((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem)) %>' AlternateText='<%# Eval("NombreEspacio") %>' CssClass="managed-space-photo" />
                                <span class="managed-space-photo-label"><%#: EtiquetaFoto((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></span>
                            </div>
                            <div class="managed-space-body">
                                <h3><%#: Eval("NombreEspacio") %></h3>
                                <p><%#: Eval("TipoEspacio") %> · Estado: <%#: Eval("EstadoEspacio") %></p>
                                <p class="managed-space-details"><%#: ResumenFicha((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></p>
                                <p class="managed-space-reputation"><%#: ObtenerReputacion((StageUp.BE.Entidades.EspacioArtistico)Container.DataItem) %></p>
                                <p class="managed-space-description"><%#: Eval("Descripcion") %></p>
                            </div>
                            <div class="managed-space-actions">
                                <a class="text-link" href='<%# "Explorar/DetalleEspacio.aspx?id=" + Eval("IdEspacioArtistico") + "#space-reviews-title" %>' data-i18n="Calificacion_VerResenas">Ver reseñas</a>
                                <asp:LinkButton ID="lnkEditar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Editar" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Editar" />
                                <asp:LinkButton ID="lnkPublicar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Publicar" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Publicar" />
                                <asp:LinkButton ID="lnkPausar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Pausar" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Pausar" />
                                <asp:LinkButton ID="lnkDarDeBaja" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="BajaLogica" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Dar de baja"
                                    OnClientClick="return confirm('¿Seguro que querés dar de baja este espacio?');" />
                            </div>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
                </div>
            </div>

            <asp:Panel ID="pnlFormularioEspacio" runat="server" Visible="false" DefaultButton="btnGuardarEspacio">
                <dialog id="space-editor" class="space-editor" aria-labelledby="form-espacio-title">
                <div class="space-editor-content">
                <button type="button" class="space-editor-close" aria-label="Cerrar formulario sin guardar" onclick="document.getElementById('lnkCancelarEdicion').click();">×</button>
                <div class="auth-card-header">
                    <h2 id="form-espacio-title"><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo espacio" /></h2>
                    <p>Dale identidad a tu espacio y contá qué lo hace especial. Los campos con * son obligatorios.</p>
                    <p class="space-form-notice" <%= FichaCompletaActiva ? "hidden" : "" %>>Fotos, ubicación, precio, capacidad y horarios están preparados, pero su guardado todavía no está habilitado. Por ahora podés guardar nombre, tipo y descripción.</p>
                </div>

                <asp:Panel ID="pnlFormularioMensaje" runat="server" Visible="false" CssClass="form-message form-message-error" role="alert"><asp:Literal ID="litFormularioMensaje" runat="server" Mode="Encode" /></asp:Panel>
                <div class="form-field">
                    <label for="<%= txtNombreEspacio.ClientID %>">Nombre del espacio *</label>
                    <asp:TextBox ID="txtNombreEspacio" runat="server" TextMode="SingleLine" MaxLength="300" placeholder="Ej: Sala de ensayo Belgrano" />
                    <asp:RequiredFieldValidator ID="rfvNombreEspacio" runat="server" ControlToValidate="txtNombreEspacio"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre del espacio." ValidationGroup="Espacio" />
                </div>

                <div class="form-field">
                    <label for="<%= txtTipoEspacio.ClientID %>">Tipo de espacio *</label>
                    <asp:TextBox ID="txtTipoEspacio" runat="server" TextMode="SingleLine" MaxLength="200" placeholder="Ej: Teatro, salón de danza, estudio..." list="tipos-espacio" />
                    <asp:RequiredFieldValidator ID="rfvTipoEspacio" runat="server" ControlToValidate="txtTipoEspacio"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el tipo de espacio." ValidationGroup="Espacio" />
                </div>

                <div class="form-field">
                    <datalist id="tipos-espacio"><option value="Teatro"></option><option value="Salón de danza"></option><option value="Estudio"></option><option value="Sala de ensayo"></option><option value="Espacio multifunción"></option></datalist>
                    <label for="<%= txtDescripcion.ClientID %>">Descripción</label>
                    <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" placeholder="Contá brevemente qué ofrece el espacio." />
                </div>

                <fieldset class="space-extra-fields" <%= FichaCompletaActiva ? "" : "disabled" %>>
                    <legend>La ficha de tu espacio</legend>
                    <section class="space-form-section">
                        <h3>01 · Fotos del espacio</h3>
                        <p>Subí una o varias fotos horizontales y bien iluminadas. La primera de la lista se usa como portada en el catálogo y el detalle.</p>
                        <asp:HiddenField ID="hdnFotosActuales" runat="server" ClientIDMode="Static" Value="[]" />
                        <ul id="photo-gallery" class="space-photo-gallery" aria-label="Fotos cargadas de este espacio" data-app-root='<%= ResolveUrl("~/") %>'></ul>
                        <p id="photo-gallery-vacia" class="space-photo-gallery-vacia">Todavía no cargaste ninguna foto.</p>
                        <div class="space-photo-upload">
                            <label for="<%= archivoFoto.ClientID %>">Agregar fotos</label>
                            <asp:FileUpload ID="archivoFoto" runat="server" ClientIDMode="Static" accept="image/jpeg,image/png" AllowMultiple="true" />
                            <small>JPG o PNG, hasta 3 MB cada una, hasta 8 fotos en total.</small>
                            <span id="photo-error" role="alert"></span>
                        </div>
                    </section>
                    <section class="space-form-section">
                        <h3>02 · Ubicación y capacidad</h3>
                        <div class="space-fields-grid">
                            <div class="form-field"><label for="<%= txtProvincia.ClientID %>">Provincia / región *</label><asp:TextBox ID="txtProvincia" runat="server" MaxLength="100" placeholder="Ej: Buenos Aires" /></div>
                            <div class="form-field"><label for="<%= txtCiudad.ClientID %>">Ciudad / localidad *</label><asp:TextBox ID="txtCiudad" runat="server" MaxLength="150" placeholder="Ej: La Plata" /></div>
                            <div class="form-field space-field-wide"><label for="<%= txtDireccion.ClientID %>">Dirección *</label><asp:TextBox ID="txtDireccion" runat="server" MaxLength="300" placeholder="Calle, número y piso o unidad" /></div>
                            <div class="form-field"><label for="<%= txtCapacidad.ClientID %>">Capacidad máxima *</label><asp:TextBox ID="txtCapacidad" runat="server" TextMode="Number" min="1" max="100000" step="1" placeholder="Cantidad de personas" /><small>Indicá cuántas personas pueden utilizar el espacio.</small></div>
                            <div class="form-field"><label for="<%= txtTipoPiso.ClientID %>">Tipo de piso</label><asp:TextBox ID="txtTipoPiso" runat="server" MaxLength="100" list="tipos-piso" placeholder="Ej: Madera" /><datalist id="tipos-piso"><option value="Madera"></option><option value="Vinílico"></option><option value="Cerámica"></option><option value="Cemento"></option><option value="Alfombra"></option></datalist></div>
                        </div>
                    </section>
                    <section class="space-form-section">
                        <h3>03 · Precio por hora</h3>
                        <div class="space-fields-grid">
                            <div class="form-field"><label for="<%= ddlMoneda.ClientID %>">Moneda</label><asp:DropDownList ID="ddlMoneda" runat="server" ClientIDMode="Static"><asp:ListItem Value="ARS">Pesos argentinos (ARS)</asp:ListItem><asp:ListItem Value="USD">Dólares (USD)</asp:ListItem></asp:DropDownList></div>
                            <div class="form-field"><label for="<%= txtPrecioHora.ClientID %>">Valor por hora *</label><asp:TextBox ID="txtPrecioHora" runat="server" ClientIDMode="Static" inputmode="decimal" MaxLength="12" placeholder="Ej: 10000,00" /></div>
                        </div>
                        <div class="space-price-example" id="price-example" aria-live="polite">El importe se calcula según la duración: precio por hora × minutos ÷ 60.</div>
                    </section>
                    <section class="space-form-section">
                        <h3>04 · Disponibilidad</h3>
                        <p>Agregá uno o varios horarios. Las excepciones reemplazan el horario habitual de esa fecha. Las reservas y actividades ocupadas se controlarán por separado.</p>
                        <asp:HiddenField ID="hdnDisponibilidad" runat="server" ClientIDMode="Static" Value="[]" />
                        <div class="space-fields-grid">
                            <div class="form-field"><label for="schedule-mode">¿Cuándo se repite?</label><select id="schedule-mode"><option value="weekly">Todas las semanas</option><option value="date">Una fecha concreta</option><option value="closed">Cerrar una fecha completa</option></select></div>
                            <div class="form-field" id="schedule-date-wrap" hidden><label for="schedule-date">Fecha</label><input id="schedule-date" type="date" /></div>
                        </div>
                        <div id="schedule-days" class="space-days" role="group" aria-label="Días de la semana">
                            <label><input type="checkbox" value="1" /> Lun</label><label><input type="checkbox" value="2" /> Mar</label><label><input type="checkbox" value="3" /> Mié</label><label><input type="checkbox" value="4" /> Jue</label><label><input type="checkbox" value="5" /> Vie</label><label><input type="checkbox" value="6" /> Sáb</label><label><input type="checkbox" value="7" /> Dom</label>
                        </div>
                        <div class="space-schedule-times" id="schedule-times">
                            <div class="form-field"><label for="schedule-from">Desde</label><input id="schedule-from" type="time" step="1800" value="09:00" /></div>
                            <div class="form-field"><label for="schedule-to">Hasta</label><input id="schedule-to" type="time" step="1800" value="18:00" /><small>00:00 como fin indica el cierre del día.</small></div>
                        </div>
                        <button type="button" class="button button-secondary button-small" id="schedule-add">＋ Agregar horario</button>
                        <p id="schedule-error" role="alert"></p>
                        <ul id="schedule-list" class="space-schedule-list"></ul>
                        <small>Para horarios que cruzan medianoche, cargá dos franjas en días consecutivos.</small>
                    </section>
                    <section class="space-form-section">
                        <h3>05 · Características y equipamiento</h3>
                        <p>Seleccioná lo que está incluido en el uso del espacio.</p>
                        <asp:CheckBoxList ID="cblEquipamiento" runat="server" RepeatLayout="Flow" CssClass="space-equipment">
                            <asp:ListItem Value="ESPEJOS">Espejos</asp:ListItem><asp:ListItem Value="SONIDO">Sonido</asp:ListItem><asp:ListItem Value="INSTRUMENTOS">Instrumentos</asp:ListItem><asp:ListItem Value="EQUIPAMIENTO">Equipamiento</asp:ListItem><asp:ListItem Value="ESCENARIO">Escenario</asp:ListItem><asp:ListItem Value="ILUMINACION">Iluminación</asp:ListItem>
                        </asp:CheckBoxList>
                        <div class="form-field"><label for="<%= txtEquipamientoDetalle.ClientID %>">Detalles del equipamiento</label><asp:TextBox ID="txtEquipamientoDetalle" runat="server" TextMode="MultiLine" Rows="3" MaxLength="1000" placeholder="Ej: piano vertical, dos micrófonos, consola y luces regulables." /></div>
                    </section>
                </fieldset>
                <div class="form-actions">
                    <asp:Button ID="btnGuardarEspacio" runat="server" CssClass="button button-primary" Text="Guardar espacio"
                        ValidationGroup="Espacio" OnClick="btnGuardarEspacio_Click" />
                    <asp:LinkButton ID="lnkCancelarEdicion" ClientIDMode="Static" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Cancelar" OnClick="lnkCancelarEdicion_Click" />
                </div>
                </div>
                </dialog>
            </asp:Panel>

            </asp:Panel>
        </div>
    </section>
</asp:Content>


<asp:Content ID="MisEspaciosStyles" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .managed-spaces { max-width: 1400px; margin: 0 auto; padding-left: 28px; padding-right: 28px; }
        .managed-spaces-header { position: relative; padding-right: 230px; }
        .managed-spaces-add { position: absolute; right: 0; top: 28px; gap: 10px; }
        .managed-spaces-add span { font-size: 24px; line-height: 1; }
        .managed-spaces-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(270px, 1fr)); gap: 24px; }
        .managed-space-card { min-width: 0; max-width: 480px; overflow: hidden; display: flex; flex-direction: column; border: 1px solid var(--color-border); border-radius: 20px; background: var(--color-surface); box-shadow: 0 8px 24px rgba(77,11,23,.05); }
        .managed-space-media { position: relative; height: 180px; display: grid; place-items: center; background: radial-gradient(ellipse at 50% 100%, #e6c8b6 0%, transparent 55%), linear-gradient(120deg, #4d0b17, #896650); overflow: hidden; }
        .managed-space-media::before, .managed-space-media::after { content: ""; position: absolute; width: 30%; height: 115%; top: -20%; background: repeating-linear-gradient(90deg, #6d1021 0, #8d3444 18px, #4d0b17 36px); border-radius: 0 0 60% 20%; }
        .managed-space-media::before { left: 0; transform: rotate(10deg); }
        .managed-space-media::after { right: 0; transform: rotate(-10deg); }
        .managed-space-stage { color: #f3dfd1; font-size: 75px; }
        .managed-space-photo-label { position: absolute; bottom: 12px; left: 16px; color: #fff; background: rgba(0,0,0,.4); border-radius: 20px; padding: 4px 10px; font-size: 11px; z-index: 1; }
        .managed-space-body { padding: 22px 22px 8px; flex: 1; overflow-wrap: anywhere; }
        .managed-space-body h3 { margin: 0 0 10px; color: var(--color-primary); font-size: 24px; }
        .managed-space-body p { color: var(--color-text-muted); font-size: 14px; }
        .managed-space-description { display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical; overflow: hidden; }
        .managed-space-actions { display: flex; flex-wrap: wrap; gap: 12px 20px; margin: 0 22px; padding: 18px 0; border-top: 1px solid var(--color-border); font-size: 14px; }
        .managed-spaces-empty { padding: 70px 24px; text-align: center; border: 1px dashed var(--color-border); border-radius: 20px; background: var(--color-surface); }
        .space-editor { width: min(960px, calc(100vw - 32px)); max-height: calc(100vh - 48px); border: 0; border-radius: 24px; padding: 0; color: var(--color-text); background: var(--color-surface); box-shadow: 0 24px 100px rgba(0,0,0,.25); }
        .space-editor::backdrop { background: rgba(35,18,23,.65); backdrop-filter: blur(4px); }
        .space-editor [hidden] { display: none !important; }
        .space-editor-content { padding: 36px; position: relative; }
        .space-editor-close { position: absolute; top: 16px; right: 18px; border: 0; background: transparent; color: var(--color-primary); font-size: 30px; cursor: pointer; width: 40px; height: 40px; }
        .space-editor h2 { padding-right: 35px; }
        .space-editor .auth-card-header { margin-bottom: 26px; }
        .space-editor .form-actions { padding-top: 20px; border-top: 1px solid var(--color-border); justify-content: flex-end; }
        .space-editor textarea { display: block; width: 100%; min-height: 130px; resize: none; padding: 14px 16px; border: 1px solid var(--color-border); border-radius: 12px; background: var(--color-nude-light); color: var(--color-text); font: inherit; line-height: 1.6; box-sizing: border-box; }
        .space-editor input:not([type=checkbox]):not([type=submit]):not([type=hidden]), .space-editor select { width: 100%; padding: 12px 14px; border-radius: 10px; border: 1px solid var(--color-border); background: var(--color-surface); color: var(--color-text); font: inherit; box-sizing: border-box; }
        .space-editor input:focus, .space-editor textarea:focus, .space-editor select:focus { outline: 2px solid var(--color-primary-soft); outline-offset: 2px; }
        .space-editor .form-field { margin-bottom: 16px; }
        .space-editor label { display: block; font-size: 14px; font-weight: 600; margin-bottom: 8px; }
        .space-editor small { display: block; font-size: 12px; color: var(--color-text-muted); margin-top: 8px; line-height: 1.5; }
        .space-extra-fields { border: 0; padding: 0; margin: 0; min-width: 0; }
        .space-extra-fields legend { font-size: 22px; color: var(--color-primary); padding: 20px 0 0; }
        .space-form-section { border-top: 1px solid var(--color-border); margin-top: 24px; padding-top: 24px; }
        .space-form-section h3 { color: var(--color-primary); margin: 0 0 12px; }
        .space-form-section p { color: var(--color-text-muted); font-size: 14px; }
        .space-fields-grid, .space-schedule-times { display: grid; grid-template-columns: 1fr 1fr; gap: 0 20px; }
        .space-field-wide { grid-column: 1 / -1; }
        .space-days { display: flex; flex-wrap: wrap; gap: 10px; margin: 12px 0 20px; }
        .space-days label { padding: 10px; border: 1px solid var(--color-border); border-radius: 10px; background: var(--color-nude-light); }
        .space-days input, .space-equipment input { accent-color: var(--color-primary); }
        .space-equipment { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; margin-bottom: 20px; }
        .space-equipment label { margin: 0 16px 0 0; }
        .space-equipment br { display: none; }
        .space-schedule-list { padding: 0; list-style: none; }
        .space-schedule-list li { display: flex; align-items: center; justify-content: space-between; padding: 12px; margin-bottom: 8px; background: var(--color-nude-light); border-radius: 10px; gap: 12px; }
        .space-schedule-list button { border: 0; background: none; color: var(--color-primary); cursor: pointer; }
        .space-price-example, .space-form-notice { padding: 16px; border-radius: 12px; background: var(--color-nude-light); font-size: 14px; line-height: 1.6; }
        .space-form-notice { border-left: 3px solid var(--color-primary); }
        .space-photo-upload { display: grid; grid-template-columns: 180px 1fr; align-items: center; gap: 20px; }
        .space-photo-preview { width: 180px; height: 125px; object-fit: cover; border-radius: 12px; background: var(--color-nude); }
        .space-photo-preview[src=""], .space-photo-preview:not([src]) { display: none; }
        #photo-error, #schedule-error { color: #a32132; }
        .managed-space-photo { position: absolute; width: 100%; height: 100%; object-fit: cover; z-index: 1; }
        .managed-space-photo-label { z-index: 2; }
        .managed-space-details { font-weight: 600; }
        .space-extra-fields:disabled input, .space-extra-fields:disabled select, .space-extra-fields:disabled textarea { background: #f4efeb; }
        @media (max-width: 600px) { .space-fields-grid, .space-schedule-times, .space-photo-upload { grid-template-columns: 1fr; } .space-field-wide { grid-column: auto; } }

        body:has(.space-editor[open]) { overflow: hidden; }
        @media (max-width: 700px) {
            .managed-spaces-header { padding-right: 0; }
            .managed-spaces-add { position: static; margin-top: 20px; }
            .managed-spaces-grid { grid-template-columns: 1fr; }
            .managed-space-card { max-width: none; }
            .space-editor-content { padding: 24px; }
        }
    </style>
</asp:Content>
<asp:Content ID="MisEspaciosScripts" ContentPlaceHolderID="PageScripts" runat="server">
    <script src="<%= ResolveUrl("~/Scripts/espacios-ficha.js") %>?v=20260909-1"></script>
    <script>
        (function () {
            var dialog = document.getElementById("space-editor");
            if (!dialog) return;
            dialog.showModal();
            dialog.addEventListener("cancel", function (event) {
                event.preventDefault();
                document.getElementById("lnkCancelarEdicion").click();
            });
        }());
    </script>
</asp:Content>
