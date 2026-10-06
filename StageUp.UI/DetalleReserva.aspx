<%@ Page Title="Detalle de reserva | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetalleReserva.aspx.cs" Inherits="StageUp.UI.DetalleReserva" %>

<asp:Content ID="DetalleReservaContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .dr-encabezado { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; margin-top: 6px; }
        .dr-estado { display: inline-block; padding: 3px 12px; border-radius: 999px; font-size: 0.85em; font-weight: 700; border: 1px solid var(--color-border, #e2e2e2); }
        .dr-estado-pendiente { background: #fff4e5; border-color: #f5c27a; color: #8a4b08; }
        .dr-estado-aceptada { background: #e8f5ec; border-color: #9fd3ae; color: #1e6b37; }
        .dr-estado-finalizada { background: #eef2f7; border-color: #c4cfdd; color: #33475b; }
        .dr-estado-rechazada, .dr-estado-cancelada { background: #fdecea; border-color: #f0b4ae; color: #8c1d18; }
        .dr-grid { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); gap: 16px; align-items: start; }
        .dr-grid > * { min-width: 0; }
        .dr-grid .auth-card { margin: 0 0 16px; }
        .dr-datos { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 20px; margin: 0; }
        .dr-datos div { min-width: 0; }
        .dr-datos dt { color: var(--color-text-muted, #765f55); font-size: 0.82em; }
        .dr-datos dd { margin: 2px 0 0; font-weight: 600; overflow-wrap: anywhere; }
        .dr-persona + .dr-persona { margin-top: 14px; padding-top: 14px; border-top: 1px solid var(--color-border, #e2e2e2); }
        .dr-persona small { display: block; color: var(--color-text-muted, #765f55); font-size: 0.82em; }
        .dr-persona strong { display: block; }
        .dr-persona span { display: block; color: var(--color-text-muted, #765f55); font-size: 0.9em; overflow-wrap: anywhere; }
        .dr-texto { margin: 0 0 10px; }
        .dr-nota { color: var(--color-text-muted, #765f55); font-size: 0.9em; margin: 0; }
        .dr-politica-ahora { margin-top: 10px; padding: 10px 14px; border-radius: 10px; background: var(--color-nude-light, #f6eee8); }
        .dr-seguimiento { list-style: none; margin: 0; padding: 0; }
        .dr-seguimiento li { display: grid; grid-template-columns: 110px minmax(0, 1fr); gap: 10px; padding: 8px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); }
        .dr-seguimiento time { color: var(--color-text-muted, #765f55); font-size: 0.85em; }
        .dr-seguimiento span { display: block; color: var(--color-text-muted, #765f55); font-size: 0.9em; }
        .dr-paso-pendiente strong { color: var(--color-text-muted, #765f55); font-weight: 500; }
        .dr-paso-interrumpido strong { color: #8c1d18; }
        .dr-acciones { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; }
        .dr-confirmacion { margin-top: 14px; padding: 14px 16px; border-radius: 12px; border: 2px solid var(--color-primary, #6d1021); }
        .dr-confirmacion p { margin: 0 0 12px; }
        .dr-comentario textarea { width: 100%; box-sizing: border-box; }
        .button-danger { background: #b3261e; border-color: #b3261e; color: #fff; }
        .button-danger:hover, .button-danger:focus-visible { background: #8c1d18; border-color: #8c1d18; color: #fff; }
        @media (max-width: 800px) { .dr-grid { grid-template-columns: 1fr; } }
        @media (max-width: 520px) {
            .dr-datos { grid-template-columns: 1fr; }
            .dr-seguimiento li { grid-template-columns: 1fr; gap: 2px; }
            .dr-acciones { flex-direction: column; align-items: stretch; }
        }
    </style>

    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Reservas</span>
            <h1>Detalle de reserva</h1>
            <asp:Panel ID="pnlEncabezado" runat="server" CssClass="dr-encabezado">
                <span>Reserva N° <asp:Literal ID="litNumero" runat="server" Mode="Encode" /></span>
                <asp:Label ID="lblEstado" runat="server" CssClass="dr-estado" />
            </asp:Panel>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <asp:Panel ID="pnlNoEncontrada" runat="server" Visible="false" CssClass="auth-card">
                <p class="dr-texto"><asp:Literal ID="litNoEncontrada" runat="server" Mode="Encode" /></p>
                <a class="button button-secondary" href="MisReservas.aspx">Ir a Mis reservas</a>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <div class="dr-grid">
                    <div>
                        <div class="auth-card">
                            <div class="auth-card-header"><h2>La reserva</h2></div>
                            <dl class="dr-datos">
                                <div>
                                    <dt>Espacio</dt>
                                    <dd><asp:HyperLink ID="lnkEspacio" runat="server" /></dd>
                                </div>
                                <div>
                                    <dt>Tipo de espacio</dt>
                                    <dd><asp:Literal ID="litTipoEspacio" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Ubicación</dt>
                                    <dd><asp:Literal ID="litUbicacion" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Fecha</dt>
                                    <dd><asp:Literal ID="litFecha" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Franja horaria</dt>
                                    <dd><asp:Literal ID="litHorario" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Valor de la operación</dt>
                                    <dd><asp:Literal ID="litValor" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Pago</dt>
                                    <dd><asp:Literal ID="litPago" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Solicitada el</dt>
                                    <dd><asp:Literal ID="litFechaSolicitud" runat="server" Mode="Encode" /></dd>
                                </div>
                                <asp:PlaceHolder ID="phResolucion" runat="server">
                                    <div>
                                        <dt>Resuelta el</dt>
                                        <dd><asp:Literal ID="litFechaResolucion" runat="server" Mode="Encode" /></dd>
                                    </div>
                                </asp:PlaceHolder>
                                <asp:PlaceHolder ID="phCancelacion" runat="server">
                                    <div>
                                        <dt>Cancelada el</dt>
                                        <dd><asp:Literal ID="litFechaCancelacion" runat="server" Mode="Encode" /></dd>
                                    </div>
                                    <div>
                                        <dt>Cargo por cancelación</dt>
                                        <dd><asp:Literal ID="litCargoCancelacion" runat="server" Mode="Encode" /></dd>
                                    </div>
                                </asp:PlaceHolder>
                            </dl>
                        </div>

                        <asp:Panel ID="pnlComentarios" runat="server" CssClass="auth-card">
                            <div class="auth-card-header"><h2>Comentarios</h2></div>
                            <asp:Panel ID="pnlComentarioSolicitante" runat="server">
                                <p class="dr-nota">Del solicitante</p>
                                <p class="dr-texto"><asp:Literal ID="litComentarioSolicitante" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                            <asp:Panel ID="pnlComentarioResolucion" runat="server">
                                <p class="dr-nota">Del gestor</p>
                                <p class="dr-texto"><asp:Literal ID="litComentarioResolucion" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                        </asp:Panel>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Seguimiento</h2></div>
                            <ol class="dr-seguimiento">
                                <asp:Repeater ID="rptSeguimiento" runat="server">
                                    <ItemTemplate>
                                        <li class='<%# "dr-paso-" + Eval("Estado").ToString().ToLowerInvariant() %>'>
                                            <time><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %></time>
                                            <div>
                                                <strong><%#: Eval("Titulo") %></strong>
                                                <span><%#: Eval("Detalle") %></span>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ol>
                        </div>
                    </div>

                    <div>
                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Participantes</h2></div>
                            <div class="dr-persona">
                                <small>Solicitante</small>
                                <strong><asp:Literal ID="litSolicitante" runat="server" Mode="Encode" /></strong>
                                <asp:Label ID="lblCorreoSolicitante" runat="server" />
                            </div>
                            <div class="dr-persona">
                                <small>Gestor del espacio</small>
                                <strong><asp:Literal ID="litGestor" runat="server" Mode="Encode" /></strong>
                            </div>
                        </div>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Condiciones de uso</h2></div>
                            <dl class="dr-datos" style="grid-template-columns: 1fr;">
                                <div>
                                    <dt>Capacidad máxima</dt>
                                    <dd><asp:Literal ID="litCapacidad" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Tipo de piso</dt>
                                    <dd><asp:Literal ID="litTipoPiso" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Equipamiento y condiciones</dt>
                                    <dd><asp:Literal ID="litEquipamiento" runat="server" Mode="Encode" /></dd>
                                </div>
                            </dl>
                        </div>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Política de cancelación</h2></div>
                            <p class="dr-texto"><asp:Literal ID="litPolitica" runat="server" Mode="Encode" /></p>
                            <asp:Panel ID="pnlPoliticaAhora" runat="server" CssClass="dr-politica-ahora">
                                <asp:Literal ID="litPoliticaAhora" runat="server" Mode="Encode" />
                            </asp:Panel>
                        </div>

                        <div class="auth-card">
                            <div class="auth-card-header"><h2>Acciones</h2></div>
                            <asp:Panel ID="pnlAcciones" runat="server" CssClass="dr-acciones">
                                <asp:HyperLink ID="lnkPagar" runat="server" CssClass="button button-primary" Text="Pagar reserva" Visible="false" />
                                <asp:Button ID="btnCancelar" runat="server" CssClass="button button-secondary" CausesValidation="false" Visible="false" OnClick="btnCancelar_Click" />
                                <asp:Button ID="btnAceptar" runat="server" CssClass="button button-primary" Text="Aceptar solicitud" CausesValidation="false" Visible="false" OnClick="btnAceptar_Click" />
                                <asp:Button ID="btnRechazar" runat="server" CssClass="button button-secondary" Text="Rechazar solicitud" CausesValidation="false" Visible="false" OnClick="btnRechazar_Click" />
                                <asp:HyperLink ID="lnkCalificar" runat="server" CssClass="button button-secondary" Visible="false" />
                                <asp:HyperLink ID="lnkSoporte" runat="server" CssClass="text-link" Text="Contactar soporte" Visible="false" />
                            </asp:Panel>
                            <asp:Panel ID="pnlSinAcciones" runat="server" Visible="false">
                                <p class="dr-nota">No hay acciones disponibles para esta reserva en su estado actual.</p>
                            </asp:Panel>

                            <asp:Panel ID="pnlConfirmacion" runat="server" CssClass="dr-confirmacion" Visible="false" role="alertdialog" aria-labelledby="dr-confirmacion-texto">
                                <asp:HiddenField ID="hfAccion" runat="server" />
                                <p id="dr-confirmacion-texto"><strong><asp:Literal ID="litConfirmacion" runat="server" Mode="Encode" /></strong></p>
                                <asp:Panel ID="pnlComentarioGestor" runat="server" CssClass="form-field dr-comentario" Visible="false">
                                    <label for="<%= txtComentarioGestor.ClientID %>">Comentario para el solicitante (opcional)</label>
                                    <asp:TextBox ID="txtComentarioGestor" runat="server" TextMode="MultiLine" Rows="3" MaxLength="750" />
                                </asp:Panel>
                                <div class="dr-acciones">
                                    <asp:Button ID="btnConfirmar" runat="server" CssClass="button button-danger" CausesValidation="false"
                                        UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Procesando...';" OnClick="btnConfirmar_Click" />
                                    <asp:LinkButton ID="lnkVolver" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkVolver_Click">No, volver</asp:LinkButton>
                                </div>
                            </asp:Panel>
                        </div>

                        <p><asp:HyperLink ID="lnkVolverListado" runat="server" CssClass="text-link" /></p>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
