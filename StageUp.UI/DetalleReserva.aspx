<%@ Page Title="Detalle de reserva | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DetalleReserva.aspx.cs" Inherits="StageUp.UI.DetalleReserva" %>

<asp:Content ID="DetalleReservaContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .dr-page { --dr-wine: var(--color-primary, #6d1021); --dr-rose: #a84d60; --dr-blush: #f7e9e6; --dr-cream: #fffaf7; --dr-ink: #4b302b; }
        .dr-page .static-page-header.dr-page-header { display: grid; grid-template-columns: minmax(18rem, 1fr) minmax(20rem, auto); gap: 1.25rem; align-items: center; padding-block: clamp(1rem, 2vw, 1.45rem); }
        .dr-heading-copy { min-width: 0; }
        .dr-page-header h1 { margin: .2rem 0 .35rem; font-family: var(--font-display); font-size: clamp(2.2rem, 4vw, 3.35rem); font-weight: 500; line-height: 1; color: var(--dr-wine); }
        .dr-encabezado { display: flex; gap: .65rem; align-items: center; flex-wrap: wrap; color: var(--color-text-muted, #765f55); }
        .dr-estado { display: inline-flex; align-items: center; min-height: 2rem; padding: .25rem .85rem; border-radius: 999px; font-size: .86rem; font-weight: 800; border: 1px solid var(--color-border, #e2e2e2); }
        .dr-estado-pendiente { background: #fff4e5; border-color: #f5c27a; color: #8a4b08; }
        .dr-estado-aceptada { background: #e8f5ec; border-color: #9fd3ae; color: #1e6b37; }
        .dr-estado-finalizada { background: #eef2f7; border-color: #c4cfdd; color: #33475b; }
        .dr-estado-rechazada, .dr-estado-cancelada { background: #fdecea; border-color: #f0b4ae; color: #8c1d18; }
        .dr-header-tools, .dr-acciones { display: flex; flex-wrap: wrap; justify-content: flex-end; align-items: center; gap: .55rem; }
        .dr-action-chip { display: inline-flex; align-items: center; justify-content: center; gap: .38rem; min-height: 2.5rem; padding: .55rem .78rem; border: 1px solid rgba(109, 16, 33, .24); border-radius: 999px; background: rgba(255, 255, 255, .76); color: var(--dr-wine); font: inherit; font-size: .92rem; font-weight: 800; line-height: 1; text-decoration: none; cursor: pointer; box-shadow: 0 .45rem 1rem rgba(88, 36, 27, .06); transition: transform .2s ease, color .2s ease, background .2s ease, box-shadow .2s ease; }
        .dr-action-chip:hover, .dr-action-chip:focus-visible { transform: translateY(-2px); background: #fff; color: #8d1730; box-shadow: 0 .65rem 1.35rem rgba(88, 36, 27, .12); text-decoration: none; }
        .dr-action-chip::before { display: inline-grid; place-items: center; width: 1.45rem; height: 1.45rem; border-radius: 50%; background: var(--dr-blush); font-size: .85rem; }
        .dr-action-back::before { content: "←"; }
        .dr-chat-open::before { content: "✉"; }
        .dr-action-pay::before { content: "$"; }
        .dr-action-receipt::before { content: "▤"; }
        .dr-action-cancel::before { content: "×"; }
        .dr-action-accept::before { content: "✓"; }
        .dr-action-reject::before { content: "!"; }
        .dr-action-review::before { content: "★"; }
        .dr-action-support::before { content: "?"; }
        .dr-action-accept, .dr-action-pay { background: var(--dr-wine); color: #fff; border-color: var(--dr-wine); }
        .dr-action-accept::before, .dr-action-pay::before { background: rgba(255,255,255,.16); }
        .dr-action-accept:hover, .dr-action-pay:hover, .dr-action-accept:focus-visible, .dr-action-pay:focus-visible { background: #85172c; color: #fff; }
        .dr-page .static-page-body { padding-top: 1.15rem; }
        .dr-grid { display: grid; grid-template-columns: minmax(0, 1.75fr) minmax(18rem, .8fr); gap: 1rem; align-items: start; }
        .dr-grid > * { min-width: 0; }
        .dr-grid .auth-card, .dr-tracking-card { margin: 0 0 1rem; }
        .dr-card { position: relative; overflow: hidden; border: 1px solid rgba(109, 16, 33, .13); border-radius: 1.35rem; background: linear-gradient(145deg, rgba(255,255,255,.97), rgba(252,244,240,.9)); box-shadow: 0 .85rem 2rem rgba(85, 42, 31, .07); }
        .dr-card::before { content: ""; position: absolute; inset: 0 auto 0 0; width: .28rem; background: linear-gradient(180deg, var(--dr-wine), #d59a87); opacity: .88; }
        .dr-card .auth-card-header { margin-bottom: 1rem; }
        .dr-card .auth-card-header h2 { display: flex; align-items: center; gap: .65rem; margin: 0; font-family: var(--font-display); font-size: clamp(1.55rem, 2vw, 1.9rem); font-weight: 500; color: var(--dr-wine); }
        .dr-card .auth-card-header h2::before { display: inline-grid; place-items: center; flex: 0 0 2.15rem; width: 2.15rem; height: 2.15rem; border-radius: .75rem; background: var(--dr-blush); font-family: Arial, sans-serif; font-size: 1rem; }
        .dr-reserva-card .auth-card-header h2::before { content: "▦"; }
        .dr-comentarios-card .auth-card-header h2::before { content: "“"; }
        .dr-participantes-card .auth-card-header h2::before { content: "◎"; }
        .dr-condiciones-card .auth-card-header h2::before { content: "✓"; }
        .dr-politica-card .auth-card-header h2::before { content: "i"; }
        .dr-tracking-card .auth-card-header h2::before { content: "↗"; }
        .dr-datos { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .9rem 1.2rem; margin: 0; }
        .dr-datos-unacolumna { grid-template-columns: 1fr; }
        .dr-datos div { min-width: 0; padding: .85rem .9rem; border-radius: .85rem; background: rgba(255,255,255,.72); }
        .dr-datos dt { color: var(--color-text-muted, #765f55); font-size: .78rem; letter-spacing: .025em; text-transform: uppercase; }
        .dr-datos dd { margin: .2rem 0 0; font-weight: 700; overflow-wrap: anywhere; color: var(--dr-ink); }
        .dr-persona { padding: .85rem .9rem; border-radius: .9rem; background: rgba(255,255,255,.72); }
        .dr-persona + .dr-persona { margin-top: .65rem; }
        .dr-persona small { display: block; color: var(--color-text-muted, #765f55); font-size: .78rem; text-transform: uppercase; letter-spacing: .035em; }
        .dr-persona strong { display: block; margin-top: .15rem; color: var(--dr-ink); }
        .dr-persona span { display: block; color: var(--color-text-muted, #765f55); font-size: .9rem; overflow-wrap: anywhere; }
        .dr-texto { margin: 0 0 .7rem; line-height: 1.6; }
        .dr-nota { color: var(--color-text-muted, #765f55); font-size: .9rem; margin: 0; }
        .dr-comentario-bloque { padding: .9rem 1rem; border-radius: .9rem; background: rgba(255,255,255,.72); }
        .dr-comentario-bloque + .dr-comentario-bloque { margin-top: .65rem; }
        .dr-politica-ahora { margin-top: .75rem; padding: .8rem 1rem; border-radius: .85rem; background: var(--color-nude-light, #f6eee8); color: var(--dr-wine); font-weight: 650; }
        .dr-confirmacion { margin: 0 0 1rem; padding: 1rem 1.15rem; border-radius: 1rem; border: 2px solid var(--dr-wine); background: #fffaf7; box-shadow: 0 .75rem 1.8rem rgba(109,16,33,.1); }
        .dr-confirmacion p { margin: 0 0 .75rem; }
        .dr-comentario textarea { width: 100%; box-sizing: border-box; }
        .button-danger { background: #b3261e; border-color: #b3261e; color: #fff; }
        .button-danger:hover, .button-danger:focus-visible { background: #8c1d18; border-color: #8c1d18; color: #fff; }
        .dr-tracking-card { padding: 1rem; }
        .dr-tracking-card .auth-card-header { margin-bottom: .55rem; }
        .dr-tracking-card .auth-card-header h2 { font-size: 1.55rem; }
        .dr-page .booking-progress.dr-booking-progress { width: 100%; max-width: none; margin: 0; }
        .dr-page .dr-tracking-card .booking-progress { padding: 1rem; }
        .dr-page .dr-tracking-card .booking-progress-icon { width: 2.55rem; height: 2.55rem; }
        .dr-page .dr-tracking-card .booking-progress-title { font-size: .85rem; }
        .dr-page .dr-tracking-card .booking-progress-date { font-size: .72rem; }
        .dr-page .booking-progress-details { margin-top: .85rem; }
        .dr-chat-dialog { width: min(31rem, calc(100vw - 1.5rem)); max-width: none; max-height: min(80vh, 45rem); margin: auto 1rem 1rem auto; padding: 0; overflow: hidden; border: 1px solid rgba(109,16,33,.18); border-radius: 1.35rem; background: #fffaf7; color: var(--dr-ink); box-shadow: 0 1.5rem 4rem rgba(55, 10, 19, .28); }
        .dr-chat-dialog[open] { display: grid; grid-template-rows: auto minmax(0, 1fr) auto; animation: dr-chat-in .24s ease-out both; }
        .dr-chat-dialog::backdrop { background: rgba(45, 15, 19, .28); backdrop-filter: blur(2px); }
        .dr-chat-dialog-header { display: flex; align-items: center; gap: .75rem; padding: .95rem 1rem; background: linear-gradient(135deg, #641022, #8f2941); color: #fff; }
        .dr-chat-dialog-icon { display: grid; place-items: center; width: 2.25rem; height: 2.25rem; border-radius: 50%; background: rgba(255,255,255,.16); }
        .dr-chat-dialog-title { min-width: 0; flex: 1; }
        .dr-chat-dialog-title strong { display: block; }
        .dr-chat-header-actions { position: relative; display: flex; align-items: center; gap: .45rem; }
        .dr-chat-info-button, .dr-chat-close { display: grid; place-items: center; width: 2.15rem; height: 2.15rem; padding: 0; border: 0; border-radius: 50%; background: rgba(255,255,255,.13); color: #fff; font-size: 1rem; font-weight: 800; cursor: pointer; transition: background .18s ease, transform .18s ease; }
        .dr-chat-close { font-size: 1.45rem; }
        .dr-chat-info-button:hover, .dr-chat-close:hover, .dr-chat-info-button:focus-visible, .dr-chat-close:focus-visible { background: rgba(255,255,255,.24); transform: translateY(-1px); }
        .dr-chat-info { position: absolute; z-index: 2; top: calc(100% + .65rem); right: 2.55rem; width: min(20rem, calc(100vw - 3rem)); padding: .75rem .85rem; border: 1px solid rgba(109,16,33,.13); border-radius: .85rem; background: #fffaf7; color: var(--dr-ink); font-size: .8rem; line-height: 1.45; box-shadow: 0 .8rem 2rem rgba(55,10,19,.2); transform-origin: top right; animation: dr-info-in .16s ease-out both; }
        .dr-chat-info[hidden] { display: none; }
        .dr-chat-content { min-height: 0; overflow: hidden; padding: .8rem 1rem 0; }
        .dr-chat { display: flex; flex-direction: column; gap: .6rem; height: min(52vh, 26rem); overflow-y: auto; padding: .75rem; border-radius: 1rem; background: linear-gradient(180deg, #f8efeb, #fffaf7); scroll-behavior: smooth; }
        .dr-chat-mensaje { display: flex; flex-direction: column; gap: .2rem; max-width: 82%; padding: .65rem .8rem; border-radius: 1rem 1rem 1rem .3rem; background: #fff; box-shadow: 0 .25rem .8rem rgba(55, 24, 20, .08); animation: dr-bubble-in .24s ease-out both; }
        .dr-chat-ajeno { align-self: flex-start; }
        .dr-chat-propio { align-self: flex-end; border: 1px solid rgba(109,16,33,.12); border-radius: 1rem 1rem .3rem 1rem; background: linear-gradient(135deg, #f8e8eb, #efd3da); color: #4b302b; }
        .dr-chat-nuevo { outline: 2px solid #cf8fa0; }
        .dr-chat-autor { font-size: .76rem; font-weight: 800; color: var(--dr-wine); }
        .dr-chat-propio .dr-chat-autor { color: #75152a; }
        .dr-chat-propio .dr-chat-texto { color: #4b302b; }
        .dr-chat-propio .dr-chat-meta { color: #795f62; }
        .dr-chat-texto { margin: 0; white-space: pre-line; overflow-wrap: anywhere; line-height: 1.42; }
        .dr-chat-meta { align-self: flex-end; font-size: .7rem; color: var(--color-text-muted, #765f55); }
        .dr-chat-vacio { margin: auto; color: var(--color-text-muted, #765f55); text-align: center; padding: 1rem .5rem; }
        .dr-chat-vacio p { margin: 0; }
        .dr-chat-footer { display: flex; align-items: end; gap: .55rem; padding: .75rem 1rem; background: #fff; border-top: 1px solid rgba(109,16,33,.09); }
        .dr-chat-form textarea { flex: 1; width: auto; min-height: 2.65rem; max-height: 7rem; box-sizing: border-box; resize: none; border-radius: 1.35rem; padding: .68rem .95rem; line-height: 1.25; }
        .dr-chat-send { flex: 0 0 2.65rem; width: 2.65rem; height: 2.65rem; min-width: 0; padding: 0; border-radius: 50%; font-size: 1.15rem; box-shadow: 0 .45rem 1rem rgba(109,16,33,.18); }
        .dr-chat-closed { padding: .8rem 1rem 1rem; background: #fff; }
        @keyframes dr-chat-in { from { opacity: 0; transform: translateY(1rem) scale(.97); } to { opacity: 1; transform: none; } }
        @keyframes dr-bubble-in { from { opacity: 0; transform: translateY(.35rem); } to { opacity: 1; transform: none; } }
        @keyframes dr-info-in { from { opacity: 0; transform: translateY(-.25rem) scale(.97); } to { opacity: 1; transform: none; } }
        @media (max-width: 70rem) {
            .dr-page .static-page-header.dr-page-header { grid-template-columns: 1fr; }
            .dr-header-tools, .dr-acciones { justify-content: flex-start; }
            .dr-grid { grid-template-columns: 1fr; }
            .dr-aside { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1rem; }
            .dr-aside .auth-card { height: 100%; }
        }
        @media (max-width: 52rem) { .dr-aside { grid-template-columns: 1fr; } }
        @media (max-width: 38rem) {
            .dr-datos { grid-template-columns: 1fr; }
            .dr-header-tools, .dr-acciones { align-items: stretch; }
            .dr-action-chip { flex: 1 1 auto; }
            .dr-chat-dialog { margin: auto; width: calc(100vw - 1rem); max-height: calc(100vh - 1rem); }
        }
        @media (prefers-reduced-motion: reduce) { .dr-action-chip, .dr-chat-dialog[open], .dr-chat-mensaje { animation: none; transition: none; } }
    </style>

    <section class="static-page user-module-page dr-page">
        <asp:Panel ID="pnlEncabezado" runat="server" CssClass="static-page-header dr-page-header">
            <div class="dr-heading-copy">
                <span class="section-label">Reservas</span>
                <h1>Detalle de reserva</h1>
                <div class="dr-encabezado">
                    <span>Reserva N° <asp:Literal ID="litNumero" runat="server" Mode="Encode" /></span>
                    <asp:Label ID="lblEstado" runat="server" CssClass="dr-estado" />
                </div>
            </div>
            <div class="dr-header-tools">
                <asp:HyperLink ID="lnkVolverListado" runat="server" CssClass="dr-action-chip dr-action-back" />
                <button type="button" class="dr-action-chip dr-chat-open" data-chat-open>Mensajes</button>
                <asp:Panel ID="pnlAcciones" runat="server" CssClass="dr-acciones">
                    <asp:HyperLink ID="lnkPagar" runat="server" CssClass="dr-action-chip dr-action-pay" Text="Pagar reserva" Visible="false" />
                    <asp:HyperLink ID="lnkVerPago" runat="server" CssClass="dr-action-chip dr-action-receipt" Text="Ver pago y factura" Visible="false" />
                    <asp:LinkButton ID="btnCancelar" runat="server" CssClass="dr-action-chip dr-action-cancel" CausesValidation="false" Visible="false" OnClick="btnCancelar_Click" />
                    <asp:LinkButton ID="btnAceptar" runat="server" CssClass="dr-action-chip dr-action-accept" Text="Aceptar solicitud" CausesValidation="false" Visible="false" OnClick="btnAceptar_Click" />
                    <asp:LinkButton ID="btnRechazar" runat="server" CssClass="dr-action-chip dr-action-reject" Text="Rechazar solicitud" CausesValidation="false" Visible="false" OnClick="btnRechazar_Click" />
                    <asp:HyperLink ID="lnkCalificar" runat="server" CssClass="dr-action-chip dr-action-review" Visible="false" />
                    <asp:HyperLink ID="lnkSoporte" runat="server" CssClass="dr-action-chip dr-action-support" Text="Contactar soporte" Visible="false" />
                </asp:Panel>
                <asp:Panel ID="pnlSinAcciones" runat="server" Visible="false"><span class="dr-nota">No hay acciones disponibles en este estado.</span></asp:Panel>
            </div>
        </asp:Panel>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status"><asp:Literal ID="litMensaje" runat="server" Mode="Encode" /></asp:Panel>
            <asp:Panel ID="pnlNoEncontrada" runat="server" Visible="false" CssClass="auth-card">
                <p class="dr-texto"><asp:Literal ID="litNoEncontrada" runat="server" Mode="Encode" /></p>
                <a class="button button-secondary" href="MisReservas.aspx">Ir a Mis reservas</a>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <asp:Panel ID="pnlConfirmacion" runat="server" CssClass="dr-confirmacion" Visible="false" role="alertdialog" aria-labelledby="dr-confirmacion-texto">
                    <asp:HiddenField ID="hfAccion" runat="server" />
                    <p id="dr-confirmacion-texto"><strong><asp:Literal ID="litConfirmacion" runat="server" Mode="Encode" /></strong></p>
                    <asp:Panel ID="pnlComentarioGestor" runat="server" CssClass="form-field dr-comentario" Visible="false">
                        <label for="<%= txtComentarioGestor.ClientID %>">Comentario para el solicitante (opcional)</label>
                        <asp:TextBox ID="txtComentarioGestor" runat="server" TextMode="MultiLine" Rows="3" MaxLength="750" />
                    </asp:Panel>
                    <div class="dr-acciones">
                        <asp:Button ID="btnConfirmar" runat="server" CssClass="button button-danger" CausesValidation="false" UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Procesando...';" OnClick="btnConfirmar_Click" />
                        <asp:LinkButton ID="lnkVolver" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkVolver_Click">No, volver</asp:LinkButton>
                    </div>
                </asp:Panel>

                <div class="dr-grid">
                    <div>
                        <div class="auth-card dr-card dr-reserva-card">
                            <div class="auth-card-header"><h2>La reserva</h2></div>
                            <dl class="dr-datos">
                                <div><dt>Espacio</dt><dd><asp:HyperLink ID="lnkEspacio" runat="server" /></dd></div>
                                <div><dt>Tipo de espacio</dt><dd><asp:Literal ID="litTipoEspacio" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Ubicación</dt><dd><asp:Literal ID="litUbicacion" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Fecha</dt><dd><asp:Literal ID="litFecha" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Franja horaria</dt><dd><asp:Literal ID="litHorario" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Valor de la operación</dt><dd><asp:Literal ID="litValor" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Pago</dt><dd><asp:Literal ID="litPago" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Solicitada el</dt><dd><asp:Literal ID="litFechaSolicitud" runat="server" Mode="Encode" /></dd></div>
                                <asp:PlaceHolder ID="phResolucion" runat="server"><div><dt>Resuelta el</dt><dd><asp:Literal ID="litFechaResolucion" runat="server" Mode="Encode" /></dd></div></asp:PlaceHolder>
                                <asp:PlaceHolder ID="phCancelacion" runat="server">
                                    <div><dt>Cancelada el</dt><dd><asp:Literal ID="litFechaCancelacion" runat="server" Mode="Encode" /></dd></div>
                                    <div><dt>Cargo por cancelación</dt><dd><asp:Literal ID="litCargoCancelacion" runat="server" Mode="Encode" /></dd></div>
                                </asp:PlaceHolder>
                            </dl>
                        </div>
                        <asp:Panel ID="pnlComentarios" runat="server" CssClass="auth-card dr-card dr-comentarios-card">
                            <div class="auth-card-header"><h2>Comentarios</h2></div>
                            <asp:Panel ID="pnlComentarioSolicitante" runat="server" CssClass="dr-comentario-bloque">
                                <p class="dr-nota">Del solicitante</p><p class="dr-texto"><asp:Literal ID="litComentarioSolicitante" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                            <asp:Panel ID="pnlComentarioResolucion" runat="server" CssClass="dr-comentario-bloque">
                                <p class="dr-nota">Del gestor</p><p class="dr-texto"><asp:Literal ID="litComentarioResolucion" runat="server" Mode="Encode" /></p>
                            </asp:Panel>
                        </asp:Panel>

                        <div class="auth-card dr-card dr-tracking-card">
                            <div class="auth-card-header"><h2>Seguimiento</h2></div>
                            <section class="booking-progress dr-booking-progress" aria-label="Seguimiento de la reserva">
                                <ol class="booking-progress-steps">
                                    <asp:Repeater ID="rptSeguimientoResumen" runat="server">
                                        <ItemTemplate>
                                            <li class='<%# "booking-progress-step booking-progress-step-" + Eval("Estado").ToString().ToLowerInvariant() %>'>
                                                <span class="booking-progress-icon"><%#: ObtenerIconoSeguimiento(Eval("Titulo") as string) %></span>
                                                <span class="booking-progress-title"><%#: Eval("Titulo") %></span>
                                                <span class="booking-progress-date"><%#: Eval("Fecha", "{0:dd/MM HH:mm}") %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ol>
                                <details class="booking-progress-details">
                                    <summary>Ver detalle del seguimiento</summary>
                                    <ol>
                                        <asp:Repeater ID="rptSeguimiento" runat="server">
                                            <ItemTemplate>
                                                <li class='<%# "booking-progress-detail booking-progress-detail-" + Eval("Estado").ToString().ToLowerInvariant() %>'>
                                                    <time><%#: Eval("Fecha", "{0:dd/MM/yyyy HH:mm}") %></time>
                                                    <div><strong><%#: Eval("Titulo") %></strong><span><%#: Eval("Detalle") %></span></div>
                                                </li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ol>
                                </details>
                            </section>
                        </div>
                    </div>

                    <div class="dr-aside">
                        <div class="auth-card dr-card dr-participantes-card">
                            <div class="auth-card-header"><h2>Participantes</h2></div>
                            <div class="dr-persona"><small>Solicitante</small><strong><asp:Literal ID="litSolicitante" runat="server" Mode="Encode" /></strong><asp:Label ID="lblCorreoSolicitante" runat="server" /></div>
                            <div class="dr-persona"><small>Gestor del espacio</small><strong><asp:Literal ID="litGestor" runat="server" Mode="Encode" /></strong></div>
                        </div>
                        <div class="auth-card dr-card dr-condiciones-card">
                            <div class="auth-card-header"><h2>Condiciones de uso</h2></div>
                            <dl class="dr-datos dr-datos-unacolumna">
                                <div><dt>Capacidad máxima</dt><dd><asp:Literal ID="litCapacidad" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Tipo de piso</dt><dd><asp:Literal ID="litTipoPiso" runat="server" Mode="Encode" /></dd></div>
                                <div><dt>Equipamiento y condiciones</dt><dd><asp:Literal ID="litEquipamiento" runat="server" Mode="Encode" /></dd></div>
                            </dl>
                        </div>
                        <div class="auth-card dr-card dr-politica-card">
                            <div class="auth-card-header"><h2>Política de cancelación</h2></div>
                            <p class="dr-texto"><asp:Literal ID="litPolitica" runat="server" Mode="Encode" /></p>
                            <asp:Panel ID="pnlPoliticaAhora" runat="server" CssClass="dr-politica-ahora"><asp:Literal ID="litPoliticaAhora" runat="server" Mode="Encode" /></asp:Panel>
                        </div>
                    </div>
                </div>

                <dialog id="dr-chat-dialog" class="dr-chat-dialog" aria-labelledby="dr-chat-title">
                    <div class="dr-chat-dialog-header">
                        <span class="dr-chat-dialog-icon">✉</span>
                        <div class="dr-chat-dialog-title"><strong id="dr-chat-title">Mensajes</strong></div>
                        <div class="dr-chat-header-actions">
                            <button type="button" class="dr-chat-info-button" data-chat-info aria-label="Información de la conversación" aria-expanded="false">i</button>
                            <div class="dr-chat-info" data-chat-info-panel hidden><asp:Literal ID="litMensajesAyuda" runat="server" Mode="Encode" /></div>
                            <button type="button" class="dr-chat-close" data-chat-close aria-label="Cerrar mensajes">×</button>
                        </div>
                    </div>
                    <div class="dr-chat-content">
                        <asp:Panel ID="pnlMensajeChat" runat="server" Visible="false" CssClass="form-message" role="status"><asp:Literal ID="litMensajeChat" runat="server" Mode="Encode" /></asp:Panel>
                        <div id="dr-chat" class="dr-chat" tabindex="0" role="log" aria-label="Conversación de la reserva">
                            <asp:Repeater ID="rptMensajes" runat="server">
                                <ItemTemplate>
                                    <div class='<%# ClaseMensaje(Container.DataItem) %>'>
                                        <span class="dr-chat-autor"><%#: AutorMensaje(Container.DataItem) %></span>
                                        <p class="dr-chat-texto"><%#: Eval("Texto") %></p>
                                        <span class="dr-chat-meta"><%#: Eval("FechaEnvio", "{0:dd/MM/yyyy HH:mm}") %><%#: EstadoLectura(Container.DataItem) %></span>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                            <asp:Panel ID="pnlSinMensajes" runat="server" CssClass="dr-chat-vacio"><p>Todavía no hay mensajes. Podés escribir para coordinar detalles de la reserva, como el horario de llegada, el equipamiento o el acceso al espacio.</p></asp:Panel>
                        </div>
                    </div>
                    <asp:Panel ID="pnlEscribirMensaje" runat="server" CssClass="dr-chat-footer dr-chat-form">
                        <label class="sr-only" for="<%= txtMensaje.ClientID %>">Mensaje</label>
                        <asp:TextBox ID="txtMensaje" runat="server" TextMode="MultiLine" Rows="1" MaxLength="1000" placeholder="Escribí tu mensaje" />
                        <asp:Button ID="btnEnviarMensaje" runat="server" CssClass="button button-primary dr-chat-send" Text="➤" ToolTip="Enviar mensaje" aria-label="Enviar mensaje" CausesValidation="false" UseSubmitBehavior="false" OnClientClick="this.disabled=true;" OnClick="btnEnviarMensaje_Click" />
                    </asp:Panel>
                    <asp:Panel ID="pnlMensajesCerrados" runat="server" Visible="false" CssClass="dr-chat-closed"><p class="dr-nota"><asp:Literal ID="litMensajesCerrados" runat="server" Mode="Encode" /></p></asp:Panel>
                </dialog>
            </asp:Panel>
        </div>
    </section>
    <script>
        (function () {
            var dialogo = document.getElementById("dr-chat-dialog");
            var chat = document.getElementById("dr-chat");
            var abrir = function () {
                if (!dialogo) return;
                if (typeof dialogo.showModal === "function") { if (!dialogo.open) dialogo.showModal(); }
                else dialogo.setAttribute("open", "open");
                if (chat) chat.scrollTop = chat.scrollHeight;
            };
            var cerrar = function () {
                if (!dialogo) return;
                if (typeof dialogo.close === "function" && dialogo.open) dialogo.close();
                else dialogo.removeAttribute("open");
            };
            document.querySelectorAll("[data-chat-open]").forEach(function (boton) { boton.addEventListener("click", abrir); });
            document.querySelectorAll("[data-chat-close]").forEach(function (boton) { boton.addEventListener("click", cerrar); });
            var botonInfo = document.querySelector("[data-chat-info]");
            var panelInfo = document.querySelector("[data-chat-info-panel]");
            if (botonInfo && panelInfo) {
                botonInfo.addEventListener("click", function () {
                    var mostrar = panelInfo.hasAttribute("hidden");
                    panelInfo.toggleAttribute("hidden", !mostrar);
                    botonInfo.setAttribute("aria-expanded", mostrar ? "true" : "false");
                });
            }
            if (dialogo) dialogo.addEventListener("click", function (evento) {
                if (evento.target === dialogo) cerrar();
                else if (panelInfo && botonInfo && !panelInfo.hasAttribute("hidden") && !panelInfo.contains(evento.target) && evento.target !== botonInfo) {
                    panelInfo.setAttribute("hidden", "hidden");
                    botonInfo.setAttribute("aria-expanded", "false");
                }
            });
            if (/[?&](ver=mensajes|enviado=1)/.test(window.location.search)) abrir();
            if (chat) chat.scrollTop = chat.scrollHeight;
            var campo = document.getElementById("<%= txtMensaje.ClientID %>");
            var enviar = document.getElementById("<%= btnEnviarMensaje.ClientID %>");
            if (campo) {
                campo.addEventListener("keydown", function (evento) {
                    if (evento.key === "Enter" && !evento.shiftKey && !evento.isComposing && enviar && !enviar.disabled) {
                        evento.preventDefault();
                        if (campo.value.trim()) enviar.click();
                    }
                });
            }
        }());
    </script>
</asp:Content>
