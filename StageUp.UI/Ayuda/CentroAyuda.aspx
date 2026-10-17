<%@ Page Title="Centro de ayuda | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CentroAyuda.aspx.cs" Inherits="StageUp.UI.Ayuda.CentroAyuda" %>

<asp:Content ID="HelpContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="help-page">
        <header class="help-hero">
            <div class="container-narrow">
                <span class="section-label">Centro de ayuda</span>
                <h1>Empecemos por una respuesta simple</h1>
                <p>Recorré la ayuda paso a paso: consultá las preguntas frecuentes, probá el asistente y, si todavía lo necesitás, contactá a soporte.</p>
                <nav class="help-journey" aria-label="Recorrido de ayuda">
                    <span class="help-journey-item is-current"><strong>1</strong>Preguntas frecuentes</span>
                    <span class="help-journey-line" aria-hidden="true"></span>
                    <span class="help-journey-item"><strong>2</strong>Asistente</span>
                    <span class="help-journey-line" aria-hidden="true"></span>
                    <span class="help-journey-item"><strong>3</strong>Soporte</span>
                </nav>
            </div>
        </header>

        <div class="help-content container-narrow">
            <asp:Panel ID="pnlNoDisponible" runat="server" Visible="false" CssClass="form-message form-message-error" role="status">
                <asp:Literal ID="litNoDisponible" runat="server" Mode="Encode" />
            </asp:Panel>

            <section class="help-section faq-section" aria-labelledby="faq-title">
                <div class="faq-introduction">
                    <div><span class="eyebrow">Primera opción</span><h2 id="faq-title">Preguntas frecuentes</h2></div>
                    <p>Reunimos respuestas esenciales para ayudarte a comprender las funciones principales de StageUp.</p>
                </div>

                <%-- CU-003-001 pasos 4 y 5: filtro opcional por tema (sin elegir, se ven todas). --%>
                <nav class="ayuda-categorias" aria-label="Filtrar por tema">
                    <asp:HyperLink ID="lnkTodas" runat="server" NavigateUrl="~/Ayuda/CentroAyuda.aspx#faq-title" Text="Todas" />
                    <asp:Repeater ID="rptCategorias" runat="server">
                        <ItemTemplate>
                            <a class='<%# Convert.ToInt32(Eval("IdCategoriaAyuda")) == CategoriaElegida ? "is-current" : "" %>'
                               href='<%# ResolveUrl("~/Ayuda/CentroAyuda.aspx") + "?tema=" + Eval("IdCategoriaAyuda") + "#faq-title" %>'
                               aria-current='<%# Convert.ToInt32(Eval("IdCategoriaAyuda")) == CategoriaElegida ? "true" : "false" %>'><%# Server.HtmlEncode(Convert.ToString(Eval("Nombre"))) %></a>
                        </ItemTemplate>
                    </asp:Repeater>
                </nav>

                <%-- A2: tema sin preguntas activas. --%>
                <asp:Panel ID="pnlCategoriaVacia" runat="server" Visible="false" CssClass="ayuda-vacia">
                    <p><strong>Todavía no hay preguntas para <asp:Literal ID="litCategoriaVacia" runat="server" Mode="Encode" />.</strong></p>
                    <p>Elegí otro tema o consultá al asistente.</p>
                </asp:Panel>

                <div class="faq-list">
                    <asp:Repeater ID="rptFaq" runat="server">
                        <ItemTemplate>
                            <article class="faq-item">
                                <h3>
                                    <button type="button" aria-expanded="false" aria-controls='faq-answer-<%# Eval("IdFaq") %>' data-accordion-trigger>
                                        <span class="faq-question"><span class="faq-number"><%# (Container.ItemIndex + 1).ToString("00") %></span><%# Server.HtmlEncode(Convert.ToString(Eval("Pregunta"))) %></span>
                                        <span class="faq-toggle" aria-hidden="true">+</span>
                                    </button>
                                </h3>
                                <div class="faq-answer" id='faq-answer-<%# Eval("IdFaq") %>' hidden>
                                    <p><%# Server.HtmlEncode(Convert.ToString(Eval("Respuesta"))) %></p>
                                </div>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </section>

            <section class="help-next-step" aria-labelledby="assistant-invitation-title">
                <div class="help-next-icon" aria-hidden="true"><span></span></div>
                <div class="help-next-copy"><span class="eyebrow">Segunda opción</span><h2 id="assistant-invitation-title">¿No encontraste tu respuesta?</h2><p>El asistente de StageUp te guía paso a paso con opciones: elegí el tema, después lo que necesitás, y te lleva a la solución o a la pantalla que corresponde.</p></div>
                <button class="button button-primary" type="button" data-assistant-open>Consultar al asistente</button>
            </section>

            <section class="support-path" aria-labelledby="support-title">
                <div class="support-path-marker"><span aria-hidden="true">3</span></div>
                <div><span class="eyebrow">Última instancia</span><h2 id="support-title">Contactar a soporte</h2><p>Si la asistencia automatizada no resuelve tu consulta, podrás registrar una solicitud. Esta acción requiere una cuenta activa y una sesión iniciada.</p></div>
                <div class="support-path-actions">
                    <asp:HyperLink ID="lnkContactarSoporte" runat="server" CssClass="button button-secondary" NavigateUrl="~/IniciarSesion.aspx" Text="Iniciar sesión para contactar a soporte" />
                    <asp:HyperLink ID="lnkMisSolicitudes" runat="server" CssClass="text-link" NavigateUrl="~/Soporte.aspx" Text="Mis solicitudes de soporte" Visible="false" />
                </div>
            </section>
        </div>
    </section>

    <div class="assistant-backdrop" data-assistant-backdrop hidden></div>
    <section class="assistant-modal" data-assistant-modal hidden role="dialog" aria-modal="true" aria-labelledby="assistant-dialog-title"
        data-asistente-url="<%= ResolveUrl("~/Ayuda/AsistenteAyuda.ashx") %>" data-soporte-url="<%= UrlSoporte %>" data-usuario-nombre="<%: NombreUsuario %>">
        <header class="assistant-modal-header">
            <div class="assistant-identity"><span class="assistant-avatar" aria-hidden="true">S</span><div><strong id="assistant-dialog-title">Asistente StageUp</strong><span><span class="asistente-en-linea" aria-hidden="true"></span>En línea · Respuestas automáticas, no es atención humana</span></div></div>
            <button class="icon-button" type="button" aria-label="Cerrar asistente" data-assistant-close>×</button>
        </header>
        <div class="assistant-conversation asistente-chat" data-asistente-chat aria-live="polite" aria-label="Conversación con el asistente"></div>
        <footer class="assistant-modal-footer">
            <div class="assistant-input-preview asistente-form" data-asistente-form role="search">
                <input type="text" maxlength="200" placeholder="Escribí tu consulta o el número de una opción" aria-label="Consulta al asistente" autocomplete="off" data-asistente-texto />
                <button class="button button-primary button-small" type="button" data-asistente-enviar>Enviar</button>
            </div>
            <span>Escribí "menú" para volver al inicio o "salir" para terminar. Si no se resuelve, podés registrar una solicitud de soporte.</span>
        </footer>
    </section>

    <style>
        .ayuda-categorias { display: flex; flex-wrap: wrap; gap: 6px; margin: 0 0 14px; }
        .ayuda-categorias a { display: inline-flex; align-items: center; padding: 4px 11px; border: 1px solid var(--color-border); border-radius: 999px; color: var(--color-text-muted); text-decoration: none; font-size: 0.82rem; background: var(--color-surface); }
        .ayuda-categorias a:hover, .ayuda-categorias a.is-current { border-color: var(--color-primary); color: var(--color-primary); font-weight: 700; }
        .ayuda-vacia { padding: 14px 16px; border: 1px dashed var(--color-border); border-radius: 12px; }
        .ayuda-vacia p { margin: 0 0 4px; }
        .asistente-chat { display: flex; flex-direction: column; gap: 8px; max-height: min(30rem, 60vh); overflow-y: auto; padding: 14px 12px; background: #f4ece6; background-image: radial-gradient(rgba(109, 16, 33, 0.05) 1px, transparent 1px); background-size: 14px 14px; }
        .asistente-dia { align-self: center; padding: 3px 10px; border-radius: 8px; background: rgba(255, 255, 255, 0.85); color: var(--color-text-muted); font-size: 0.75rem; box-shadow: 0 1px 1px rgba(0, 0, 0, 0.06); }
        .asistente-burbuja { position: relative; display: flex; flex-direction: column; gap: 3px; padding: 7px 10px 5px; border-radius: 10px; max-width: 85%; line-height: 1.42; font-size: 0.92rem; overflow-wrap: anywhere; box-shadow: 0 1px 1px rgba(0, 0, 0, 0.08); }
        .asistente-burbuja-bot { background: #fff; color: var(--color-text, #2b1a1d); align-self: flex-start; border-top-left-radius: 2px; }
        .asistente-burbuja-usuario { background: #f3dfe3; color: var(--color-text, #2b1a1d); align-self: flex-end; border-top-right-radius: 2px; }
        .asistente-burbuja-con-botones { min-width: min(17rem, 85%); padding-bottom: 0; }
        .asistente-titulo { font-weight: 700; }
        .asistente-texto { white-space: pre-line; }
        .asistente-nota { font-style: italic; color: var(--color-text-muted); }
        .asistente-descripcion { color: var(--color-text-muted); font-size: 0.85rem; }
        .asistente-hora { align-self: flex-end; display: inline-flex; gap: 4px; font-size: 0.7rem; color: var(--color-text-muted); line-height: 1; }
        .asistente-leido { color: #3d8fd1; letter-spacing: -2px; }
        .asistente-cita { display: flex; flex-direction: column; padding: 5px 8px; margin-bottom: 2px; border-left: 4px solid var(--color-primary, #6d1021); border-radius: 6px; background: rgba(109, 16, 33, 0.07); font-size: 0.8rem; }
        .asistente-cita strong { color: var(--color-primary, #6d1021); }
        .asistente-cita span { color: var(--color-text-muted); display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
        .asistente-respuestas { display: flex; flex-direction: column; margin: 6px -10px 0; }
        .asistente-respuesta, .asistente-boton-lista { display: flex; align-items: center; justify-content: center; gap: 8px; width: 100%; padding: 9px 12px; border: 0; border-top: 1px solid rgba(0, 0, 0, 0.08); background: transparent; color: var(--color-primary, #6d1021); font: inherit; font-size: 0.9rem; text-align: center; text-decoration: none; cursor: pointer; }
        .asistente-boton-lista { margin: 6px -10px 0; width: calc(100% + 20px); font-weight: 600; }
        .asistente-respuesta:hover, .asistente-boton-lista:hover { background: rgba(109, 16, 33, 0.05); }
        .asistente-respuesta:focus-visible, .asistente-boton-lista:focus-visible, .asistente-hoja-opcion:focus-visible { outline: 2px solid var(--color-primary, #6d1021); outline-offset: -2px; }
        .asistente-respuesta-destacada { font-weight: 700; }
        .asistente-respuesta-enlace span:last-child::after { content: " \2197"; }
        .asistente-respuesta:disabled, .asistente-boton-lista:disabled, .asistente-respuesta.is-disabled { color: var(--color-text-muted); cursor: default; background: transparent; pointer-events: none; opacity: 0.7; }
        .asistente-numero { display: inline-grid; place-items: center; min-width: 1.35rem; height: 1.35rem; border-radius: 50%; background: rgba(109, 16, 33, 0.08); color: var(--color-primary, #6d1021); font-size: 0.72rem; font-weight: 700; }
        .asistente-icono-lista { width: 14px; height: 10px; border-top: 2px solid currentColor; border-bottom: 2px solid currentColor; position: relative; }
        .asistente-icono-lista::after { content: ""; position: absolute; left: 0; right: 0; top: 2px; border-top: 2px solid currentColor; }
        .asistente-escribiendo { align-self: flex-start; display: inline-flex; gap: 4px; padding: 10px 12px; border-radius: 10px; border-top-left-radius: 2px; background: #fff; box-shadow: 0 1px 1px rgba(0, 0, 0, 0.08); }
        .asistente-escribiendo span { width: 6px; height: 6px; border-radius: 50%; background: var(--color-text-muted); animation: asistente-puntos 1s infinite ease-in-out; }
        .asistente-escribiendo span:nth-child(2) { animation-delay: 0.15s; }
        .asistente-escribiendo span:nth-child(3) { animation-delay: 0.3s; }
        @keyframes asistente-puntos { 0%, 80%, 100% { opacity: 0.3; transform: translateY(0); } 40% { opacity: 1; transform: translateY(-3px); } }
        @media (prefers-reduced-motion: reduce) { .asistente-escribiendo span { animation: none; } }
        .asistente-en-linea { display: inline-block; width: 8px; height: 8px; margin-right: 5px; border-radius: 50%; background: #2e9d5b; vertical-align: middle; }
        .asistente-hoja-fondo { position: absolute; inset: 0; background: rgba(0, 0, 0, 0.35); opacity: 0; transition: opacity 0.2s ease; z-index: 5; }
        .asistente-hoja-fondo.is-open { opacity: 1; }
        .asistente-hoja { position: absolute; left: 0; right: 0; bottom: 0; max-height: 78%; display: flex; flex-direction: column; background: var(--color-surface, #fff); border-radius: 16px 16px 0 0; box-shadow: 0 -6px 24px rgba(0, 0, 0, 0.18); transform: translateY(100%); transition: transform 0.22s ease; z-index: 6; }
        .asistente-hoja.is-open { transform: translateY(0); }
        @media (prefers-reduced-motion: reduce) { .asistente-hoja, .asistente-hoja-fondo { transition: none; } }
        .asistente-hoja-cabecera { position: relative; padding: 16px 48px 12px; text-align: center; border-bottom: 1px solid var(--color-border); }
        .asistente-hoja-cerrar { position: absolute; right: 10px; top: 8px; width: 36px; height: 36px; border: 0; background: transparent; font-size: 1.5rem; line-height: 1; cursor: pointer; color: var(--color-text); }
        .asistente-hoja-lista { overflow-y: auto; }
        .asistente-hoja-opcion { display: flex; align-items: flex-start; gap: 10px; width: 100%; padding: 12px 18px; border: 0; border-bottom: 1px solid var(--color-border); background: transparent; font: inherit; text-align: left; color: var(--color-text); text-decoration: none; cursor: pointer; }
        .asistente-hoja-opcion:hover { background: rgba(109, 16, 33, 0.04); }
        .asistente-hoja-opcion .asistente-numero { margin-top: 1px; }
        .asistente-hoja-texto { display: flex; flex-direction: column; gap: 2px; }
        .asistente-hoja-texto strong { font-weight: 600; }
        .asistente-hoja-texto > span { color: var(--color-text-muted); font-size: 0.85rem; }
        .asistente-hoja-pie { margin: 0; padding: 10px 16px 14px; text-align: center; font-size: 0.78rem; color: var(--color-text-muted); }
        .support-path-actions { display: flex; flex-direction: column; align-items: flex-start; gap: 0.6rem; }
        .asistente-form input:disabled { cursor: not-allowed; }
    </style>
    <script src="<%= ResolveUrl("~/Scripts/asistente-ayuda.js") %>?v=20261017-2" defer></script>
</asp:Content>
