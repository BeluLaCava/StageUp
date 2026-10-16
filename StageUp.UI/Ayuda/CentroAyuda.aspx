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
        data-asistente-url="<%= ResolveUrl("~/Ayuda/AsistenteAyuda.ashx") %>" data-soporte-url="<%= UrlSoporte %>">
        <header class="assistant-modal-header">
            <div class="assistant-identity"><span class="assistant-avatar" aria-hidden="true">S</span><div><strong id="assistant-dialog-title">Asistente StageUp</strong><span>Respuestas automáticas · No es atención humana</span></div></div>
            <button class="icon-button" type="button" aria-label="Cerrar asistente" data-assistant-close>×</button>
        </header>
        <div class="assistant-conversation asistente-chat" data-asistente-chat aria-live="polite" aria-label="Conversación con el asistente"></div>
        <footer class="assistant-modal-footer">
            <div class="assistant-input-preview asistente-form" data-asistente-form role="search">
                <input type="text" maxlength="200" placeholder="Número de opción o tu consulta" aria-label="Consulta al asistente" autocomplete="off" data-asistente-texto />
                <button class="button button-primary button-small" type="button" data-asistente-enviar>Enviar</button>
            </div>
            <span>Elegí una opción o escribí su número. También podés escribir tu consulta. Si no se resuelve, podés registrar una solicitud de soporte.</span>
        </footer>
    </section>

    <style>
        .ayuda-categorias { display: flex; flex-wrap: wrap; gap: 6px; margin: 0 0 14px; }
        .ayuda-categorias a { display: inline-flex; align-items: center; padding: 4px 11px; border: 1px solid var(--color-border); border-radius: 999px; color: var(--color-text-muted); text-decoration: none; font-size: 0.82rem; background: var(--color-surface); }
        .ayuda-categorias a:hover, .ayuda-categorias a.is-current { border-color: var(--color-primary); color: var(--color-primary); font-weight: 700; }
        .ayuda-vacia { padding: 14px 16px; border: 1px dashed var(--color-border); border-radius: 12px; }
        .ayuda-vacia p { margin: 0 0 4px; }
        .asistente-chat { display: flex; flex-direction: column; gap: 10px; max-height: min(26rem, 55vh); overflow-y: auto; }
        .asistente-burbuja { padding: 9px 12px; border-radius: 14px; max-width: 88%; line-height: 1.45; font-size: 0.92rem; overflow-wrap: anywhere; }
        .asistente-burbuja-bot { background: var(--color-nude-light, #f7efe9); align-self: flex-start; border-bottom-left-radius: 4px; }
        .asistente-burbuja-usuario { background: var(--color-primary, #6d1021); color: #fff; align-self: flex-end; border-bottom-right-radius: 4px; }
        .asistente-burbuja strong { display: block; margin-bottom: 4px; }
        .asistente-opciones { display: flex; flex-wrap: wrap; gap: 6px; align-self: flex-start; }
        .asistente-opciones button, .asistente-opciones a { padding: 5px 11px; border: 1px solid rgba(109, 16, 33, 0.35); border-radius: 999px; background: var(--color-surface); color: var(--color-primary); font: inherit; font-size: 0.84rem; cursor: pointer; text-decoration: none; }
        .asistente-opciones button:hover, .asistente-opciones a:hover { background: var(--color-primary); color: #fff; }
        .support-path-actions { display: flex; flex-direction: column; align-items: flex-start; gap: 0.6rem; }
        .asistente-opciones-numeradas { flex-direction: column; align-items: stretch; width: min(100%, 26rem); }
        .asistente-opciones-numeradas button, .asistente-opciones-numeradas a { border-radius: 10px; text-align: left; }
        .asistente-opciones button:disabled { opacity: 0.55; cursor: default; background: var(--color-surface); color: var(--color-primary); }
        .asistente-opciones .asistente-soporte { background: var(--color-primary); color: #fff; font-weight: 700; }
        .asistente-form input:disabled { cursor: not-allowed; }
    </style>
    <script src="<%= ResolveUrl("~/Scripts/asistente-ayuda.js") %>?v=20261016" defer></script>
</asp:Content>
