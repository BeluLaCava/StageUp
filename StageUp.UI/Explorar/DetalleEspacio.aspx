<%@ Page Title="Detalle del espacio | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetalleEspacio.aspx.cs" Inherits="StageUp.UI.Explorar.DetalleEspacio" %>

<asp:Content ID="SpaceDetailContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="space-detail-page">
        <div class="space-detail-container">
            <a class="back-to-results" href="ResultadosBusqueda.aspx"><span aria-hidden="true">←</span> Volver a resultados</a>

            <asp:Panel ID="pnlEspacioNoEncontrado" runat="server" CssClass="space-detail-empty" role="status">
                <div class="space-detail-empty-visual" aria-hidden="true"><span></span></div>
                <span class="section-label">Detalle del espacio</span>
                <h1><asp:Literal ID="litTituloNoEncontrado" runat="server" Text="No hay un espacio seleccionado" /></h1>
                <p><asp:Literal ID="litDescripcionNoEncontrado" runat="server" Text="Elegí un espacio desde los resultados para consultar su información." /></p>
                <div class="space-detail-empty-actions">
                    <a class="button button-primary" href="ResultadosBusqueda.aspx">Explorar espacios</a>
                    <a class="button button-secondary" href="../IniciarSesion.aspx">Iniciar sesión para reservar</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlDetalleEspacio" runat="server" CssClass="space-detail" Visible="false">
                <asp:Panel ID="pnlGaleria" runat="server" CssClass="space-gallery" data-space-gallery="true">
                    <div class="space-gallery-stage">
                        <asp:Image ID="imgGaleriaPrincipal" runat="server" CssClass="space-gallery-main-image" data-gallery-main="true" />
                        <button class="space-gallery-control space-gallery-previous" type="button" aria-label="Ver fotografía anterior" data-gallery-previous>‹</button>
                        <button class="space-gallery-control space-gallery-next" type="button" aria-label="Ver fotografía siguiente" data-gallery-next>›</button>
                        <span class="space-gallery-counter" data-gallery-counter></span>
                        <button class="space-gallery-expand" type="button" data-gallery-expand><span aria-hidden="true">⛶</span> Ver en grande</button>
                        <span class="space-gallery-label"><asp:Literal ID="litEtiquetaGaleria" runat="server" /></span>
                    </div>
                    <div class="space-gallery-thumbnails" aria-label="Fotografías del espacio">
                        <asp:Repeater ID="rptGaleriaFotos" runat="server">
                            <ItemTemplate>
                                <button type="button" class="space-gallery-thumbnail" data-gallery-thumbnail
                                    data-src='<%# ResolveUrl(Container.DataItem.ToString()) %>'
                                    aria-label='<%# "Ver fotografía " + (Container.ItemIndex + 1) %>'>
                                    <asp:Image ID="imgMiniatura" runat="server" ImageUrl='<%# Container.DataItem.ToString() %>' AlternateText="" />
                                </button>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlGaleriaVacia" runat="server" CssClass="space-gallery-empty">
                    <span aria-hidden="true">◇</span>
                    <strong>Este espacio todavía no tiene fotografías</strong>
                    <small>La información disponible se muestra a continuación.</small>
                </asp:Panel>

                <div class="space-detail-layout">
                    <div class="space-detail-main">
                        <header class="space-detail-heading">
                            <p class="space-detail-location"><asp:Literal ID="litUbicacionPrincipal" runat="server" /></p>
                            <h1><asp:Literal ID="litNombreEspacio" runat="server" /></h1>
                            <div class="space-detail-summary">
                                <span><asp:Literal ID="litTipoEspacio" runat="server" /></span>
                                <asp:Panel ID="pnlResumenCapacidad" runat="server" CssClass="space-detail-pill"><asp:Literal ID="litResumenCapacidad" runat="server" /></asp:Panel>
                                <asp:Panel ID="pnlResumenPrecio" runat="server" CssClass="space-detail-pill"><asp:Literal ID="litResumenPrecio" runat="server" /></asp:Panel>
                            </div>
                        </header>

                        <section class="space-host-card" aria-labelledby="space-host-title">
                            <div class="space-host-avatar" aria-hidden="true"><asp:Literal ID="litInicialesGestor" runat="server" /></div>
                            <div>
                                <span class="eyebrow">Gestionado por</span>
                                <h2 id="space-host-title"><asp:Literal ID="litNombreGestor" runat="server" /></h2>
                                <div class="space-host-rating"><span aria-hidden="true">☆☆☆☆☆</span> <asp:Literal ID="litReputacionGestor" runat="server" /></div>
                            </div>
                            <span class="space-host-badge">Gestor verificado</span>
                        </section>

                        <section class="space-detail-section" aria-labelledby="space-description-title">
                            <span class="eyebrow">Conocé el lugar</span>
                            <h2 id="space-description-title">Sobre el espacio</h2>
                            <div class="space-detail-description"><asp:Literal ID="litDescripcion" runat="server" /></div>
                        </section>

                        <asp:Panel ID="pnlFicha" runat="server">
                            <section class="space-detail-section" aria-labelledby="space-info-title">
                                <span class="eyebrow">Información esencial</span>
                                <h2 id="space-info-title">Todo lo que necesitás saber</h2>
                                <div class="space-information-grid">
                                    <article><span class="space-information-icon" aria-hidden="true">⌖</span><small>Ubicación</small><strong><asp:Literal ID="litDireccion" runat="server" /></strong></article>
                                    <article><span class="space-information-icon" aria-hidden="true">♙</span><small>Capacidad</small><strong><asp:Literal ID="litCapacidad" runat="server" /></strong></article>
                                    <article><span class="space-information-icon" aria-hidden="true">▦</span><small>Tipo de piso</small><strong><asp:Literal ID="litTipoPiso" runat="server" /></strong></article>
                                    <article><span class="space-information-icon" aria-hidden="true">$</span><small>Precio</small><strong><asp:Literal ID="litPrecioHora" runat="server" /></strong></article>
                                </div>
                            </section>

                            <section class="space-detail-section" aria-labelledby="space-features-title">
                                <span class="eyebrow">Incluido en el espacio</span>
                                <h2 id="space-features-title">Características y equipamiento</h2>
                                <asp:Panel ID="pnlCaracteristicas" runat="server" CssClass="space-feature-grid">
                                    <asp:Repeater ID="rptEquipamiento" runat="server"><ItemTemplate><span><span aria-hidden="true">✓</span> <%#: Container.DataItem %></span></ItemTemplate></asp:Repeater>
                                </asp:Panel>
                                <asp:Panel ID="pnlSinCaracteristicas" runat="server" CssClass="space-inline-empty">El gestor todavía no detalló las características incluidas.</asp:Panel>
                                <asp:Panel ID="pnlDetalleEquipamiento" runat="server" CssClass="space-equipment-detail">
                                    <strong>Detalle del equipamiento</strong>
                                    <p><asp:Literal ID="litDetalleEquipamiento" runat="server" /></p>
                                </asp:Panel>
                            </section>

                            <section class="space-detail-section" aria-labelledby="space-availability-title">
                                <span class="eyebrow">Horarios habituales</span>
                                <h2 id="space-availability-title">Disponibilidad informada</h2>
                                <p class="space-detail-intro">Elegí una fecha en el planificador para ver únicamente los horarios que el gestor indicó como disponibles.</p>
                                <asp:Panel ID="pnlDisponibilidad" runat="server" CssClass="space-availability-list">
                                    <asp:Repeater ID="rptDisponibilidad" runat="server"><ItemTemplate><div><span aria-hidden="true">◷</span><%#: Container.DataItem %></div></ItemTemplate></asp:Repeater>
                                </asp:Panel>
                                <asp:Panel ID="pnlSinDisponibilidad" runat="server" CssClass="space-inline-empty">La disponibilidad todavía debe confirmarse con el gestor.</asp:Panel>
                            </section>
                        </asp:Panel>

                        <asp:Panel ID="pnlSinFicha" runat="server" CssClass="space-detail-section space-profile-pending">
                            <span class="eyebrow">Ficha en preparación</span>
                            <h2>Próximamente vas a ver más información</h2>
                            <p>El gestor todavía no cargó ubicación, capacidad, precio, disponibilidad y equipamiento para este espacio.</p>
                        </asp:Panel>

                        <section class="space-detail-section" aria-labelledby="space-reviews-title">
                            <span class="eyebrow">Experiencias verificadas</span>
                            <h2 id="space-reviews-title">Reseñas del espacio</h2>
                            <asp:Panel ID="pnlResumenResenas" runat="server" CssClass="space-reviews-summary">
                                <span class="space-reviews-score"><asp:Literal ID="litPromedioResenas" runat="server" /></span>
                                <div>
                                    <span class="space-reviews-stars" aria-hidden="true"><asp:Literal ID="litEstrellasResenas" runat="server" /></span>
                                    <strong><asp:Literal ID="litCantidadResenas" runat="server" /></strong>
                                    <p data-i18n="Calificacion_SoloReservasVerificadas">Todas las opiniones corresponden a reservas finalizadas.</p>
                                </div>
                            </asp:Panel>
                            <asp:Repeater ID="rptResenasEspacio" runat="server">
                                <ItemTemplate>
                                    <article class="space-review-card">
                                        <header>
                                            <div class="space-review-avatar" aria-hidden="true"><%#: ObtenerInicialesResena(Eval("NombreAutor") as string) %></div>
                                            <div><strong><%#: Eval("NombreAutor") %></strong><span>Reserva del <%# Eval("FechaReserva", "{0:dd/MM/yyyy}") %></span></div>
                                            <span class="space-review-rating" aria-label='<%# Eval("Puntaje") + " de 5 estrellas" %>'><%#: ObtenerEstrellas(Convert.ToInt32(Eval("Puntaje"))) %></span>
                                        </header>
                                        <p><%#: Eval("Comentario") %></p>
                                        <small>Publicada el <%# Eval("FechaAlta", "{0:dd/MM/yyyy}") %></small>
                                    </article>
                                </ItemTemplate>
                            </asp:Repeater>
                            <asp:Panel ID="pnlSinResenas" runat="server" CssClass="space-reviews-empty">
                                <span class="space-reviews-stars" aria-hidden="true">☆☆☆☆☆</span>
                                <strong>Todavía no hay reseñas</strong>
                                <p>Cuando finalice la primera reserva, la persona que utilizó el espacio podrá compartir su experiencia.</p>
                            </asp:Panel>
                        </section>
                    </div>

                    <aside class="reservation-card" aria-label="Solicitud de reserva">
                        <span class="reservation-card-label">Planificá tu reserva</span>
                        <div class="reservation-card-price"><asp:Literal ID="litPrecioReserva" runat="server" /></div>
                        <p>Seleccioná una fecha, el horario y la duración. El gestor deberá aceptar tu solicitud.</p>

                        <asp:Panel ID="pnlReservarMensaje" runat="server" Visible="false" CssClass="form-message">
                            <asp:Literal ID="litReservarMensaje" runat="server" />
                        </asp:Panel>

                        <asp:HiddenField ID="hdnDisponibilidadDetalle" runat="server" ClientIDMode="Static" Value="[]" />
                        <asp:HiddenField ID="hdnPrecioHoraDetalle" runat="server" ClientIDMode="Static" />
                        <asp:HiddenField ID="hdnMonedaDetalle" runat="server" ClientIDMode="Static" Value="ARS" />
                        <asp:HiddenField ID="hdnMinutoDesdeReserva" runat="server" ClientIDMode="Static" />
                        <asp:HiddenField ID="hdnDuracionReserva" runat="server" ClientIDMode="Static" />

                        <asp:Panel ID="pnlPlanificadorDisponible" runat="server" CssClass="reservation-planner" data-reservation-planner="true">
                            <div class="form-field">
                                <label for="txtFechaReserva">Fecha *</label>
                                <asp:TextBox ID="txtFechaReserva" runat="server" ClientIDMode="Static" TextMode="Date" />
                                <asp:RequiredFieldValidator ID="rfvFechaReserva" runat="server" ControlToValidate="txtFechaReserva"
                                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="Elegí una fecha." ValidationGroup="Reserva" />
                            </div>
                            <div class="reservation-time-grid">
                                <div class="form-field"><label for="reservation-start">Hora de inicio *</label><select id="reservation-start"><option value="">Elegí una fecha</option></select></div>
                                <div class="form-field"><label for="reservation-duration">Duración *</label><select id="reservation-duration"><option value="">Elegí un horario</option></select></div>
                            </div>
                            <p id="reservation-feedback" class="reservation-feedback" role="status">Seleccioná una fecha para consultar sus horarios.</p>
                            <div class="reservation-estimate" aria-live="polite">
                                <span>Importe estimado</span>
                                <strong id="reservation-total">—</strong>
                                <small id="reservation-summary">Se calcula según la duración elegida.</small>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlPlanificadorNoDisponible" runat="server" CssClass="reservation-unavailable">El gestor todavía no configuró horarios y precio. La solicitud se habilitará cuando complete esa información.</asp:Panel>

                        <asp:Panel ID="pnlReservarInvitado" runat="server">
                            <a class="button button-primary button-full" href="../IniciarSesion.aspx">Iniciar sesión para solicitar</a>
                            <small>Necesitás una cuenta activa para enviar una solicitud.</small>
                        </asp:Panel>

                        <asp:Panel ID="pnlReservarPropio" runat="server" Visible="false">
                            <div class="reservation-owner-message"><strong>Este es tu espacio</strong><p>Podés administrarlo desde <a href="../MisEspacios.aspx">Mis espacios</a>.</p></div>
                        </asp:Panel>

                        <asp:Panel ID="pnlReservarFormulario" runat="server" Visible="false">
                            <div class="form-field reservation-comment-field">
                                <label for="<%= txtComentarioReserva.ClientID %>">Mensaje para el gestor (opcional)</label>
                                <asp:TextBox ID="txtComentarioReserva" runat="server" TextMode="MultiLine" Rows="3" MaxLength="750"
                                    placeholder="Contale para qué actividad necesitás el espacio." />
                            </div>
                            <asp:Button ID="btnSolicitarReserva" runat="server" ClientIDMode="Static" CssClass="button button-primary button-full"
                                Text="Solicitar reserva" ValidationGroup="Reserva" OnClick="btnSolicitarReserva_Click"
                                OnClientClick="return window.StageUpDetalleReserva.validar();" />
                            <small>La franja y el importe se incluirán en el detalle que recibe el gestor.</small>
                        </asp:Panel>

                        <div class="reservation-safety"><span aria-hidden="true">✓</span><span>No se confirma automáticamente: el gestor debe aceptar la solicitud.</span></div>
                    </aside>
                </div>
            </asp:Panel>
        </div>
    </section>

    <dialog id="space-gallery-lightbox" class="space-gallery-lightbox" aria-label="Fotografía ampliada">
        <button type="button" class="space-lightbox-close" aria-label="Cerrar fotografía" data-gallery-close>×</button>
        <img data-gallery-lightbox-image alt="Fotografía ampliada del espacio" />
    </dialog>
</asp:Content>

<asp:Content ID="SpaceDetailScripts" ContentPlaceHolderID="PageScripts" runat="server">
    <script src="<%= ResolveUrl("~/Scripts/detalle-espacio.js") %>?v=20260909-1"></script>
</asp:Content>
