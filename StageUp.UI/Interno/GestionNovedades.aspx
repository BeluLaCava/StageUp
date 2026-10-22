<%@ Page Title="Novedades y newsletter | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionNovedades.aspx.cs" Inherits="StageUp.UI.Interno.GestionNovedades" %>

<asp:Content ID="GestionNovedadesContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page newsletter-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comunicación</span>
            <h1>Novedades y newsletter</h1>
            <p>Administrá noticias públicas de StageUp y prepará envíos por correo para los suscriptores del newsletter.</p>
            <div class="newsletter-admin-links">
                <a class="button button-secondary" href="GestionNewsletter.aspx">Suscriptores, categorías y envíos</a>
            </div>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <div class="newsletter-dashboard-strip" aria-label="Resumen de newsletters">
                <article>
                    <span>Borradores</span>
                    <strong><asp:Literal ID="litCantidadBorradores" runat="server" /></strong>
                </article>
                <article>
                    <span>Programadas</span>
                    <strong><asp:Literal ID="litCantidadProgramadas" runat="server" /></strong>
                </article>
                <article>
                    <span>Vigentes</span>
                    <strong><asp:Literal ID="litCantidadPublicadas" runat="server" /></strong>
                </article>
                <article>
                    <span>Último envío</span>
                    <strong><asp:Literal ID="litUltimoEnvio" runat="server" /></strong>
                </article>
            </div>

            <div class="newsletter-admin-layout">
                <asp:Panel ID="pnlFormularioNovedad" runat="server" CssClass="auth-card admin-editor-card newsletter-editor-card">
                    <div class="auth-card-header">
                        <span class="admin-card-eyebrow">Contenido</span>
                        <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva novedad" /></h2>
                        <p>Se publica en la página de novedades y, si lo tildás, también se envía por correo.</p>
                    </div>

                    <div class="form-field">
                        <label for="<%= txtTitulo.ClientID %>">Título *</label>
                        <asp:TextBox ID="txtTitulo" runat="server" TextMode="SingleLine" MaxLength="180"
                            placeholder="Ej: Nuevos espacios para crear esta temporada" />
                        <asp:RequiredFieldValidator ID="rfvTitulo" runat="server" ControlToValidate="txtTitulo"
                            ValidationGroup="Novedad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el título." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtResumen.ClientID %>">Resumen para listado y correo *</label>
                        <asp:TextBox ID="txtResumen" runat="server" TextMode="MultiLine" Rows="3" MaxLength="300"
                            placeholder="Un párrafo breve: es lo que se ve en el listado y en el cuerpo del mail." />
                        <asp:RequiredFieldValidator ID="rfvResumen" runat="server" ControlToValidate="txtResumen"
                            ValidationGroup="Novedad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el resumen." />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtContenido.ClientID %>">Contenido de la novedad *</label>
                        <asp:TextBox ID="txtContenido" runat="server" TextMode="MultiLine" Rows="8"
                            placeholder="El texto completo que se muestra en la página pública de novedades." />
                        <asp:RequiredFieldValidator ID="rfvContenido" runat="server" ControlToValidate="txtContenido"
                            ValidationGroup="Novedad" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el contenido." />
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlCategoria.ClientID %>">Categoría</label>
                        <asp:DropDownList ID="ddlCategoria" runat="server" CssClass="form-select" />
                        <small class="field-help">Las categorías se administran en <a href="GestionNewsletter.aspx#categorias">Suscriptores, categorías y envíos</a>.</small>
                    </div>

                    <div class="newsletter-form-grid novedad-vigencia-grid">
                        <div class="form-field">
                            <label for="<%= txtFechaPublicacion.ClientID %>">Publicar desde</label>
                            <asp:TextBox ID="txtFechaPublicacion" runat="server" TextMode="DateTimeLocal" />
                            <small class="field-help">Vacía: se publica en el momento. Con una fecha futura queda programada.</small>
                        </div>

                        <div class="form-field">
                            <label for="<%= txtFechaVencimiento.ClientID %>">Vence el (opcional)</label>
                            <asp:TextBox ID="txtFechaVencimiento" runat="server" TextMode="DateTimeLocal" />
                            <small class="field-help">Desde ese momento deja de mostrarse. Vacía: no vence.</small>
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="<%= fuImagen.ClientID %>">Imagen destacada</label>
                        <asp:Panel ID="pnlImagenActual" runat="server" Visible="false" CssClass="novedad-imagen-actual">
                            <asp:Image ID="imgImagenActual" runat="server" AlternateText="Imagen actual de la novedad" />
                            <div class="novedad-imagen-detalle">
                                <strong>Imagen actual</strong>
                                <small id="novedadImagenActualEstado">Se mantiene si no elegís otra.</small>
                                <label class="newsletter-check-option">
                                    <asp:CheckBox ID="chkQuitarImagen" runat="server" />
                                    <span>Quitar la imagen</span>
                                </label>
                            </div>
                        </asp:Panel>
                        <div id="novedadImagenNueva" class="novedad-imagen-actual novedad-imagen-nueva" hidden>
                            <img id="novedadImagenNuevaVista" alt="Vista previa de la imagen elegida" />
                            <div class="novedad-imagen-detalle">
                                <strong>Imagen elegida (sin guardar)</strong>
                                <small id="novedadImagenNuevaDetalle"></small>
                                <button type="button" id="novedadImagenNuevaQuitar" class="text-link novedad-imagen-quitar">Quitar la imagen elegida</button>
                            </div>
                        </div>
                        <p id="novedadImagenAviso" class="field-error-text" role="alert" hidden></p>
                        <asp:FileUpload ID="fuImagen" runat="server" accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp" />
                        <small class="field-help">JPG, PNG o WebP de hasta 3 MB. JPG y PNG se ajustan a 1600 px y se guardan como JPG; WebP se guarda tal cual, sin metadatos (algunos programas de correo no muestran WebP). Si elegís una nueva, reemplaza a la actual.</small>
                    </div>

                    <div class="newsletter-options">
                        <label class="newsletter-check-option">
                            <asp:CheckBox ID="chkEnviarMail" runat="server" />
                            <span>
                                <strong>Enviar también por mail al publicar</strong>
                                <small>Usa la plantilla StageUp y los destinatarios elegidos abajo. Se envía una vez por novedad; si algún correo falla, se reintentan solo esos.</small>
                            </span>
                        </label>
                    </div>

                    <div class="form-field">
                        <label for="<%= ddlDestinatarios.ClientID %>">Destinatarios</label>
                        <asp:DropDownList ID="ddlDestinatarios" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Todos los suscriptores confirmados" Value="Activos" Selected="True" />
                            <asp:ListItem Text="Suscriptores con cuenta de gestor" Value="Gestores" />
                            <asp:ListItem Text="Suscriptores con cuenta de solicitante" Value="Solicitantes" />
                            <asp:ListItem Text="Suscriptores sin cuenta" Value="Suscriptores" />
                        </asp:DropDownList>
                        <small class="field-help"><asp:Literal ID="litResumenSuscriptores" runat="server" Mode="Encode" /> Solo reciben el newsletter quienes confirmaron su suscripción, eligieron la categoría de la novedad (o todas) y no se dieron de baja; tener cuenta no suscribe a nadie. Cada correo lleva el enlace para elegir categorías o darse de baja.</small>
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnGuardarBorrador" runat="server" CssClass="button button-secondary" Text="Guardar como borrador"
                            ValidationGroup="Novedad" OnClick="btnGuardarBorrador_Click" />
                        <asp:Button ID="btnPublicar" runat="server" CssClass="button button-primary" Text="Guardar y publicar"
                            ValidationGroup="Novedad" OnClick="btnPublicar_Click"
                            OnClientClick="if (!confirmarPublicacionNovedad(this, event)) return false;" />
                        <asp:Button ID="btnEnviarPrueba" runat="server" CssClass="button button-secondary" Text="Enviar prueba"
                            ValidationGroup="Novedad" OnClick="btnEnviarPrueba_Click" />
                        <asp:LinkButton ID="lnkCancelarNovedad" runat="server" CssClass="text-link" Text="Cancelar edición"
                            CausesValidation="false" Visible="false" OnClick="lnkCancelarNovedad_Click" />
                    </div>
                </asp:Panel>

                <div class="newsletter-side-column">
                    <div class="auth-card admin-workspace-card newsletter-preview-card">
                        <div class="auth-card-header admin-workspace-header">
                            <div>
                                <span class="admin-card-eyebrow">Vista previa</span>
                                <h2>Mail newsletter</h2>
                            </div>
                            <span class="admin-workspace-hint">Plantilla StageUp</span>
                        </div>

                        <div class="newsletter-mail-preview" aria-label="Vista previa del email">
                            <div class="newsletter-mail-header">
                                <span class="newsletter-mail-logo" aria-hidden="true">S</span>
                                <div>
                                    <strong>StageUp</strong>
                                    <small><asp:Literal ID="litPreviewCategoria" runat="server" Text="Novedades StageUp" /></small>
                                </div>
                            </div>
                            <div class="newsletter-mail-body">
                                <h3><asp:Literal ID="litPreviewTitulo" runat="server" Text="Así se va a ver el correo" /></h3>
                                <p>Hola artista,</p>
                                <p><asp:Literal ID="litPreviewResumen" runat="server" Text="Completá el formulario y guardá para actualizar esta vista previa." /></p>
                                <a href="#" class="newsletter-mail-button">Ver novedades</a>
                                <small>Recibís este correo porque te suscribiste al newsletter de StageUp.</small>
                            </div>
                        </div>
                    </div>

                    <div class="auth-card admin-list-card newsletter-list-card">
                        <div class="auth-card-header">
                            <span class="admin-card-eyebrow">Historial</span>
                            <h2>Novedades recientes</h2>
                        </div>

                        <div class="form-field">
                            <label for="<%= ddlFiltroEstado.ClientID %>">Mostrar</label>
                            <asp:DropDownList ID="ddlFiltroEstado" runat="server" CssClass="form-select" AutoPostBack="true"
                                CausesValidation="false" OnSelectedIndexChanged="ddlFiltroEstado_SelectedIndexChanged">
                                <asp:ListItem Text="Todas (menos las dadas de baja)" Value="" />
                                <asp:ListItem Text="Borradores" Value="Borrador" />
                                <asp:ListItem Text="Programadas" Value="Programada" />
                                <asp:ListItem Text="Vigentes" Value="Vigente" />
                                <asp:ListItem Text="Vencidas" Value="Vencida" />
                                <asp:ListItem Text="Dadas de baja" Value="DadaDeBaja" />
                            </asp:DropDownList>
                        </div>

                        <asp:Panel ID="pnlSinNovedades" runat="server" CssClass="admin-empty-hint" Visible="false">
                            <asp:Literal ID="litSinNovedades" runat="server" Text="Todavía no cargaste ninguna novedad. Creá la primera desde el formulario." />
                        </asp:Panel>

                        <div class="newsletter-campaign-list">
                            <asp:Repeater ID="rptNovedades" runat="server" OnItemCommand="rptNovedades_ItemCommand">
                                <ItemTemplate>
                                    <article class='<%# ObtenerClaseFilaNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %>'>
                                        <div>
                                            <h3><%#: Eval("Titulo") %></h3>
                                            <p><%#: ObtenerTextoFechaNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %></p>
                                            <asp:Panel runat="server" CssClass='<%# ObtenerClaseEnvio(Container.DataItem) %>' Visible='<%# ObtenerEnvio(Container.DataItem) != null %>'>
                                                <%#: ObtenerTextoEnvio(Container.DataItem) %>
                                            </asp:Panel>
                                            <div class="space-row-actions">
                                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                                    CommandName="Editar" CommandArgument='<%# Eval("IdNovedad") %>' Text="Editar"
                                                    Visible='<%# PuedeEditar(Container.DataItem) %>' />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                    CommandName="Publicar" CommandArgument='<%# Eval("IdNovedad") %>' Text="Publicar"
                                                    Visible='<%# EsEstado(Container.DataItem, "Borrador") %>'
                                                    OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Publicar esta novedad? Si tiene una fecha de publicación futura, queda programada hasta ese momento.', { title: 'Publicar novedad', confirmText: 'Sí, publicar', cancelText: 'Volver' })) return false;" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                                    CommandName="VolverABorrador" CommandArgument='<%# Eval("IdNovedad") %>' Text="Volver a borrador"
                                                    Visible='<%# EsEstado(Container.DataItem, "Programada") || EsEstado(Container.DataItem, "Vigente") || EsEstado(Container.DataItem, "Vencida") %>'
                                                    OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Volver esta novedad a borrador? Deja de mostrarse en la página pública.')) return false;" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                    CommandName="EnviarNewsletter" CommandArgument='<%# Eval("IdNovedad") %>' Text="Enviar newsletter"
                                                    Visible='<%# EsEstado(Container.DataItem, "Vigente") && !Convert.ToBoolean(Eval("EnviadaPorCorreo")) && PuedeEnviarNewsletter(Container.DataItem) %>'
                                                    OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Enviar el newsletter de esta novedad a los destinatarios seleccionados arriba?')) return false;" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                    CommandName="ReintentarNewsletter" CommandArgument='<%# Eval("IdNovedad") %>' Text="Reintentar fallidos"
                                                    Visible='<%# EsEstado(Container.DataItem, "Vigente") && PuedeReintentar(Container.DataItem) %>'
                                                    OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Reintentar el envío solo a los destinatarios que no lo recibieron?')) return false;" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                                    CommandName="DarDeBaja" CommandArgument='<%# Eval("IdNovedad") %>' Text="Dar de baja"
                                                    Visible='<%# !EsEstado(Container.DataItem, "DadaDeBaja") %>'
                                                    OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Dar de baja esta novedad? Deja de mostrarse y queda en el historial (se puede restaurar como borrador).', { title: 'Dar de baja', confirmText: 'Sí, dar de baja', cancelText: 'Volver' })) return false;" />
                                                <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                                    CommandName="Restaurar" CommandArgument='<%# Eval("IdNovedad") %>' Text="Restaurar como borrador"
                                                    Visible='<%# EsEstado(Container.DataItem, "DadaDeBaja") %>' />
                                            </div>
                                        </div>
                                        <span class='<%# ObtenerClaseEstadoNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %>'>
                                            <%#: ObtenerTextoEstadoNovedad((StageUp.BE.Entidades.Novedad)Container.DataItem) %>
                                        </span>
                                    </article>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>
    <style>
        .novedad-imagen-actual { display: flex; align-items: center; gap: 12px; margin: 6px 0 10px; flex-wrap: wrap; }
        .novedad-imagen-actual[hidden] { display: none; }
        .novedad-imagen-actual img { width: 120px; height: 72px; object-fit: cover; border-radius: 8px; border: 1px solid var(--color-border); }
        .novedad-imagen-detalle { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
        .novedad-imagen-detalle small { color: var(--color-text-muted); overflow-wrap: anywhere; }
        .novedad-imagen-nueva { padding: 8px; border: 1px dashed var(--color-border); border-radius: 10px; }
        .novedad-imagen-quitar { align-self: flex-start; padding: 0; border: 0; background: none; cursor: pointer; font: inherit; }
        .novedad-imagen-reemplazada img { opacity: 0.4; }
        #novedadImagenAviso[hidden] { display: none; }
        .novedad-vigencia-grid input { width: 100%; min-width: 0; box-sizing: border-box; }
        .admin-status-badge-programada { background: #fff4e5; color: #8a4b08; }
        .admin-status-badge-vencida { background: #eef2f7; color: #33475b; }
        .newsletter-admin-links { margin-top: 0.9rem; }
        .newsletter-admin-links .button { background: #fff; color: var(--color-primary); border-color: #fff; }
        .newsletter-admin-links .button:hover, .newsletter-admin-links .button:focus-visible { background: var(--color-nude); color: var(--color-primary-dark); }
        .envio-resumen { margin: 4px 0 6px; font-size: 0.85rem; padding: 4px 10px; border-radius: 8px; display: inline-block; }
        .envio-resumen-ok { background: #e6f5eb; color: #236240; }
        .envio-resumen-alerta { background: #fff4e5; color: #8a4b0f; }
        .envio-resumen-error { background: #fdecec; color: #8f1d1d; }
        .envio-resumen-neutro { background: #eef2f7; color: #33475b; }
    </style>
    <script>


        (function () {
            var entrada = document.getElementById("<%= fuImagen.ClientID %>");
            var quitarActual = document.getElementById("<%= chkQuitarImagen.ClientID %>");
            var panelActual = document.getElementById("<%= pnlImagenActual.ClientID %>");
            var estadoActual = document.getElementById("novedadImagenActualEstado");
            var nueva = document.getElementById("novedadImagenNueva");
            var vista = document.getElementById("novedadImagenNuevaVista");
            var detalle = document.getElementById("novedadImagenNuevaDetalle");
            var quitarNueva = document.getElementById("novedadImagenNuevaQuitar");
            var aviso = document.getElementById("novedadImagenAviso");
            var maximo = <%= StageUp.UI.Infraestructura.ImagenSubida.MaximoBytes %>;
            var tipos = ["image/jpeg", "image/png", "image/webp"];
            var extension = /\.(jpe?g|png|webp)$/i;
            var urlVista = null;

            if (!entrada || !nueva) {
                return;
            }

            function liberar() {
                if (urlVista && window.URL) {
                    URL.revokeObjectURL(urlVista);
                }
                urlVista = null;
            }

            function avisar(texto) {
                aviso.textContent = texto || "";
                aviso.hidden = !texto;
            }

            function actualizarActual() {
                if (!panelActual) {
                    return;
                }
                var hayNueva = !nueva.hidden;
                var seQuita = quitarActual && quitarActual.checked;
                panelActual.classList.toggle("novedad-imagen-reemplazada", hayNueva || seQuita);
                if (estadoActual) {
                    estadoActual.textContent = hayNueva
                        ? "Se va a reemplazar por la imagen elegida al guardar."
                        : seQuita ? "Se va a quitar al guardar." : "Se mantiene si no elegís otra.";
                }
            }

            function limpiarSeleccion() {
                entrada.value = "";
                liberar();
                vista.removeAttribute("src");
                nueva.hidden = true;
                actualizarActual();
            }

            function tamanio(bytes) {
                return bytes >= 1048576
                    ? (bytes / 1048576).toFixed(1).replace(".", ",") + " MB"
                    : Math.max(1, Math.round(bytes / 1024)) + " KB";
            }

            entrada.addEventListener("change", function () {
                avisar("");
                var archivo = entrada.files && entrada.files[0];
                if (!archivo) {
                    limpiarSeleccion();
                    return;
                }

                if (tipos.indexOf((archivo.type || "").toLowerCase()) < 0 || !extension.test(archivo.name)) {
                    limpiarSeleccion();
                    avisar("La imagen tiene que ser JPG, PNG o WebP.");
                    return;
                }

                if (archivo.size > maximo) {
                    limpiarSeleccion();
                    avisar("La imagen no puede superar los 3 MB.");
                    return;
                }

                liberar();
                urlVista = URL.createObjectURL(archivo);
                vista.onerror = function () {
                    limpiarSeleccion();
                    avisar("No se pudo mostrar la imagen: el archivo parece dañado.");
                };
                vista.src = urlVista;
                detalle.textContent = archivo.name + " · " + tamanio(archivo.size);
                nueva.hidden = false;
                if (quitarActual) {
                    quitarActual.checked = false;
                }
                actualizarActual();
            });

            quitarNueva.addEventListener("click", function () {
                avisar("");
                limpiarSeleccion();
                entrada.focus();
            });

            if (quitarActual) {
                quitarActual.addEventListener("change", function () {
                    if (quitarActual.checked && !nueva.hidden) {
                        limpiarSeleccion();
                    }
                    actualizarActual();
                });
            }

            window.addEventListener("pagehide", liberar);
        })();

        function confirmarPublicacionNovedad(btn, event) {
            var enviarMail = document.getElementById("<%= chkEnviarMail.ClientID %>");
            var mensaje = enviarMail && enviarMail.checked
                ? "¿Publicar esta novedad y enviarla por mail? El envío se realizará a los destinatarios seleccionados."
                : "¿Guardar y publicar esta novedad? Va a quedar visible en la web.";

            return StageUpConfirm.confirmPostback(btn, event, mensaje, {
                title: "Publicar novedad",
                confirmText: "Sí, publicar",
                cancelText: "Volver",
                validationGroup: "Novedad"
            });
        }
    </script>
</asp:Content>
