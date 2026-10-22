<%@ Page Title="Preguntas recibidas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PreguntasRecibidas.aspx.cs" Inherits="StageUp.UI.PreguntasRecibidas" %>

<asp:Content ID="PreguntasRecibidasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page user-module-page requests-page">
        <div class="container">
            <header class="static-page-header requests-header">
                <div>
                    <span class="section-label">Panel del gestor</span>
                    <h1>Preguntas recibidas</h1>
                    <p>Respondé las consultas públicas que hacen sobre tus espacios. La pregunta y tu respuesta se ven en la ficha del espacio.</p>
                </div>
                <a class="button button-secondary" href="MisEspacios.aspx">Ver mis espacios</a>
            </header>

            <nav class="gestor-subnav requests-subnav" aria-label="Navegación de gestor de espacios">
                <a href="~/MisEspacios.aspx" runat="server">Administrar espacios</a>
                <a href="~/SolicitudesRecibidas.aspx" runat="server">Solicitudes recibidas</a>
                <a href="~/PreguntasRecibidas.aspx" runat="server" class="active">Preguntas recibidas</a>
            </nav>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="requests-filter preguntas-filtro">
                <div class="preguntas-filtro-intro">
                    <span class="preguntas-filtro-icon" aria-hidden="true">?</span>
                    <div>
                        <strong>Organizá las consultas</strong>
                        <small>Filtrá por estado o por espacio para encontrar rápido qué falta responder.</small>
                    </div>
                </div>
                <div class="preguntas-filtro-campos">
                    <div class="requests-filter-control">
                        <label for="<%= ddlEstado.ClientID %>">Mostrar</label>
                        <asp:DropDownList ID="ddlEstado" runat="server" CssClass="requests-filter-select" AutoPostBack="true" OnSelectedIndexChanged="Filtrar" />
                    </div>
                    <div class="requests-filter-control">
                        <label for="<%= ddlEspacio.ClientID %>">Espacio</label>
                        <asp:DropDownList ID="ddlEspacio" runat="server" CssClass="requests-filter-select" AutoPostBack="true" OnSelectedIndexChanged="Filtrar" />
                    </div>
                    <span class="requests-filter-count"><asp:Literal ID="litCantidad" runat="server" /></span>
                </div>
            </div>

            <asp:Panel ID="pnlVacio" runat="server" CssClass="empty-state compact-empty-state" Visible="false">
                <h3><asp:Literal ID="litVacioTitulo" runat="server" Mode="Encode" /></h3>
                <p><asp:Literal ID="litVacioTexto" runat="server" Mode="Encode" /></p>
            </asp:Panel>

            <div class="preguntas-lista">
                <asp:Repeater ID="rptPreguntas" runat="server" OnItemCommand="rptPreguntas_ItemCommand">
                    <ItemTemplate>
                        <article class='<%# "space-question preguntas-item preguntas-item-" + Eval("Estado") %>'>
                            <header class="preguntas-item-header">
                                <a class="text-link" href='<%#: ResolveUrl("~/Explorar/DetalleEspacio.aspx") + "?id=" + Eval("IdEspacioArtistico") + "&pregunta=" + Eval("IdPreguntaEspacio") %>'><%#: Eval("NombreEspacio") %></a>
                                <span class='<%# "preguntas-estado preguntas-estado-" + Eval("Estado") %>'><%#: TextoEstado(Eval("Estado")) %></span>
                            </header>
                            <p class="space-question-text"><%#: Eval("Pregunta") %></p>
                            <small class="space-question-meta">Preguntó <%#: Eval("NombreAutorPublico") %> el <%# Eval("FechaPregunta", "{0:dd/MM/yyyy HH:mm}") %></small>

                            <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("EstaRespondida")) %>'>
                                <div class="space-answer">
                                    <strong>Tu respuesta</strong>
                                    <p><%#: Eval("Respuesta") %></p>
                                    <small><%# Eval("FechaRespuesta", "{0:dd/MM/yyyy HH:mm}") %></small>
                                </div>
                            </asp:PlaceHolder>
                            <asp:PlaceHolder runat="server" Visible='<%# Convert.ToString(Eval("Estado")) == "Oculta" %>'>
                                <p class="space-answer-pending">Oculta el <%# Eval("FechaBaja", "{0:dd/MM/yyyy}") %>. Motivo: <%#: Eval("MotivoOcultamiento") %></p>
                            </asp:PlaceHolder>

                            <asp:Panel ID="pnlResponder" runat="server" CssClass="space-answer-form" Visible='<%# Convert.ToBoolean(Eval("EstaPendiente")) %>'>
                                <asp:TextBox ID="txtRespuesta" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" placeholder="Tu respuesta (se publica en el espacio)" aria-label="Respuesta" />
                                <asp:Button ID="btnResponder" runat="server" CssClass="button button-primary button-small" Text="Responder"
                                    CommandName="Responder" CommandArgument='<%# Eval("IdPreguntaEspacio") %>' CausesValidation="false" />
                            </asp:Panel>

                            <asp:Panel ID="pnlOcultar" runat="server" CssClass="space-question-hide" Visible='<%# Convert.ToInt32(Eval("IdPreguntaEspacio")) == PreguntaOcultando %>'>
                                <span>Motivo para ocultarla (lo ve quien preguntó)</span>
                                <asp:TextBox ID="txtMotivo" runat="server" MaxLength="300" placeholder="Ej: contenido ofensivo o datos de contacto" aria-label="Motivo para ocultar la pregunta" />
                                <asp:Button ID="btnConfirmarOcultar" runat="server" CssClass="button button-primary button-small" Text="Ocultar pregunta"
                                    CommandName="ConfirmarOcultar" CommandArgument='<%# Eval("IdPreguntaEspacio") %>' CausesValidation="false" />
                                <asp:LinkButton ID="lnkCancelarOcultar" runat="server" CssClass="text-link" CommandName="CancelarOcultar" CausesValidation="false">Cancelar</asp:LinkButton>
                            </asp:Panel>

                            <div class="space-question-actions">
                                <asp:LinkButton ID="lnkOcultar" runat="server" CssClass="text-link" CommandName="Ocultar" CommandArgument='<%# Eval("IdPreguntaEspacio") %>'
                                    Visible='<%# Convert.ToString(Eval("Estado")) != "Oculta" && Convert.ToInt32(Eval("IdPreguntaEspacio")) != PreguntaOcultando %>' CausesValidation="false">Ocultar</asp:LinkButton>
                            </div>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <style>
        .preguntas-filtro { display: grid; grid-template-columns: minmax(15rem, 1fr) auto; gap: 1rem 1.5rem; align-items: end; padding: 1rem 1.1rem; background: linear-gradient(135deg, rgba(255, 252, 250, 0.98), rgba(248, 235, 228, 0.78)); border: 1px solid rgba(122, 12, 32, 0.12); border-radius: 1rem; box-shadow: 0 0.65rem 1.8rem rgba(77, 11, 23, 0.045); }
        .preguntas-filtro-intro { display: flex; align-items: center; gap: 0.8rem; min-width: 0; }
        .preguntas-filtro-intro strong, .preguntas-filtro-intro small { display: block; }
        .preguntas-filtro-intro strong { color: var(--color-primary-dark); font-size: 0.98rem; }
        .preguntas-filtro-intro small { margin-top: 0.15rem; color: var(--color-text-muted); line-height: 1.35; }
        .preguntas-filtro-icon { display: grid; place-items: center; width: 2.5rem; height: 2.5rem; flex: 0 0 auto; color: #fff; background: var(--color-primary); border-radius: 0.8rem; font-family: var(--font-display); font-size: 1.3rem; box-shadow: 0 0.45rem 1rem rgba(109, 16, 33, 0.18); }
        .preguntas-filtro-campos { display: flex; align-items: flex-end; justify-content: flex-end; gap: 0.75rem; flex-wrap: wrap; }
        .preguntas-filtro .requests-filter-control { min-width: 10.5rem; }
        .preguntas-filtro .requests-filter-count { display: inline-flex; align-items: center; min-height: 2.65rem; padding: 0.55rem 0.85rem; color: var(--color-primary); background: #fff; border: 1px solid rgba(122, 12, 32, 0.14); border-radius: 999px; font-weight: 800; white-space: nowrap; }
        .preguntas-lista { display: grid; gap: 0.9rem; margin-top: 1rem; }
        .preguntas-item { position: relative; overflow: hidden; transition: transform 180ms ease, border-color 180ms ease, box-shadow 180ms ease; animation: pregunta-entrada 360ms ease both; }
        .preguntas-item::before { content: ""; position: absolute; inset: 0 auto 0 0; width: 0.24rem; background: #b7791f; }
        .preguntas-item-Respondida::before { background: #3c8058; }
        .preguntas-item-Oculta::before { background: #9a897f; }
        .preguntas-item:hover { transform: translateY(-0.16rem); border-color: rgba(122, 12, 32, 0.22); box-shadow: 0 0.9rem 2.1rem rgba(77, 11, 23, 0.08); }
        .preguntas-item:nth-child(2) { animation-delay: 45ms; }
        .preguntas-item:nth-child(3) { animation-delay: 90ms; }
        .preguntas-item:nth-child(4) { animation-delay: 135ms; }
        .preguntas-item-header { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 0.4rem; }
        .preguntas-estado { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 0.75rem; font-weight: 700; }
        .preguntas-estado-Pendiente { background: #fff2cf; color: #825100; }
        .preguntas-estado-Respondida { background: #e5f4e9; color: #286241; }
        .preguntas-estado-Oculta { background: #f1ebe7; color: var(--color-text-muted); }
        @keyframes pregunta-entrada { from { opacity: 0; transform: translateY(0.45rem); } to { opacity: 1; transform: translateY(0); } }
        @media (max-width: 62rem) {
            .preguntas-filtro { grid-template-columns: 1fr; align-items: stretch; }
            .preguntas-filtro-campos { justify-content: flex-start; }
        }
        @media (max-width: 38rem) {
            .preguntas-filtro-campos, .preguntas-filtro .requests-filter-control { width: 100%; }
            .preguntas-filtro .requests-filter-select { width: 100%; }
        }
        @media (prefers-reduced-motion: reduce) {
            .preguntas-item { animation: none; transition: none; }
        }
    </style>
</asp:Content>
