<%@ Page Title="Encuestas | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Encuestas.aspx.cs" Inherits="StageUp.UI.Encuestas" %>

<asp:Content ID="EncuestasContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .encuesta-resultado-pregunta { margin-bottom: 20px; }
        .encuesta-resultado-pregunta h3 { margin: 0 0 8px 0; }
        .encuesta-resultado-opcion { margin-bottom: 10px; }
        .encuesta-resultado-opcion-etiqueta { display: flex; justify-content: space-between; font-size: 0.9em; margin-bottom: 4px; }
        .encuesta-resultado-barra-track { background: var(--color-border, #e2e2e2); border-radius: 6px; height: 14px; overflow: hidden; }
        .encuesta-resultado-barra-fill { background: var(--color-primary, #4f46e5); height: 100%; border-radius: 6px; }
        .encuesta-resultado-total { color: var(--color-text-muted, #6b7280); margin-bottom: 14px; }
        .encuesta-pregunta-item { margin-bottom: 18px; }
        .encuesta-pregunta-item h3 { margin: 0 0 8px 0; }
        .encuesta-opciones-responder { display: flex; flex-direction: column; gap: 6px; }
        .encuesta-opcion-radio { display: flex; align-items: center; gap: 8px; font-weight: normal; }
        .encuesta-lista-item { display: flex; justify-content: space-between; align-items: center; padding: 10px 0; border-bottom: 1px solid var(--color-border, #e2e2e2); }
        .encuesta-lista-item span.encuesta-vencimiento { color: var(--color-text-muted, #6b7280); font-size: 0.85em; }
    </style>

    <section class="static-page public-news-page">
        <header class="public-news-hero container-wide">
            <div>
                <span class="section-label">Comunidad</span>
                <h1>Encuestas</h1>
                <p>Respondé las encuestas activas de StageUp y mirá los resultados actualizados al instante.</p>
            </div>
        </header>

        <div class="public-news-layout container-wide">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlDashboard" runat="server" Visible="false">
                <div class="auth-card">
                    <div class="auth-card-header">
                        <h2>Pendientes por responder</h2>
                    </div>
                    <asp:Panel ID="pnlSinPendientes" runat="server" CssClass="admin-empty-hint" Visible="false">
                        No tenés encuestas pendientes por ahora.
                    </asp:Panel>
                    <asp:Repeater ID="rptPendientes" runat="server">
                        <ItemTemplate>
                            <div class="encuesta-lista-item">
                                <div>
                                    <strong><%#: Eval("Titulo") %></strong><br />
                                    <span class="encuesta-vencimiento">Vence el <%#: Eval("FechaVencimiento", "{0:dd/MM/yyyy HH:mm}") %></span>
                                </div>
                                <a class="button button-primary button-small" href='<%#: "Encuestas.aspx?responder=" + Eval("IdEncuesta") %>'>Responder</a>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <div class="auth-card" style="margin-top: 20px;">
                    <div class="auth-card-header">
                        <h2>Resultados disponibles</h2>
                    </div>
                    <asp:Panel ID="pnlSinDisponibles" runat="server" CssClass="admin-empty-hint" Visible="false">
                        Todavía no hay resultados disponibles para vos.
                    </asp:Panel>
                    <asp:Repeater ID="rptDisponibles" runat="server">
                        <ItemTemplate>
                            <div class="encuesta-lista-item">
                                <div>
                                    <strong><%#: Eval("Titulo") %></strong>
                                </div>
                                <a class="button button-secondary button-small" href='<%#: "Encuestas.aspx?ver=" + Eval("IdEncuesta") %>'>Ver resultados</a>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlResponder" runat="server" Visible="false">
                <div class="auth-card">
                    <div class="auth-card-header">
                        <h2><asp:Literal ID="litTituloResponder" runat="server" /></h2>
                        <p><asp:Literal ID="litDescripcionResponder" runat="server" /></p>
                    </div>

                    <asp:Repeater ID="rptPreguntasResponder" runat="server" OnItemDataBound="rptPreguntasResponder_ItemDataBound">
                        <ItemTemplate>
                            <div class="encuesta-pregunta-item">
                                <h3><%#: Eval("Texto") %></h3>
                                <div class="encuesta-opciones-responder">
                                    <asp:Repeater ID="rptOpcionesResponder" runat="server" DataSource='<%# Eval("Opciones") %>'>
                                        <ItemTemplate>
                                            <label class="encuesta-opcion-radio">
                                                <input type="radio" name='<%# "pregunta_" + Eval("IdPreguntaEncuesta") %>' value='<%# Eval("IdOpcionPregunta") %>' />
                                                <span><%#: Eval("Texto") %></span>
                                            </label>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                    <div class="form-actions">
                        <asp:Button ID="btnEnviarRespuestas" runat="server" CssClass="button button-primary" Text="Enviar respuestas"
                            CausesValidation="false" OnClick="btnEnviarRespuestas_Click" />
                        <a class="text-link" href="Encuestas.aspx">Volver</a>
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlVerResultados" runat="server" Visible="false">
                <div class="auth-card">
                    <div class="auth-card-header">
                        <h2><asp:Literal ID="litTituloResultados" runat="server" /></h2>
                        <p><asp:Literal ID="litDescripcionResultados" runat="server" /></p>
                    </div>
                    <p class="encuesta-resultado-total"><asp:Literal ID="litTotalRespuestas" runat="server" /></p>
                    <asp:Repeater ID="rptResultados" runat="server" OnItemDataBound="rptResultados_ItemDataBound">
                        <ItemTemplate>
                            <div class="encuesta-resultado-pregunta">
                                <h3><%#: Eval("TextoPregunta") %></h3>
                                <asp:Repeater ID="rptOpcionesResultado" runat="server" DataSource='<%# Eval("Opciones") %>'>
                                    <ItemTemplate>
                                        <div class="encuesta-resultado-opcion">
                                            <div class="encuesta-resultado-opcion-etiqueta">
                                                <span><%#: Eval("TextoOpcion") %></span>
                                                <span><%#: Eval("CantidadRespuestas") %> (<%#: Eval("Porcentaje") %>%)</span>
                                            </div>
                                            <div class="encuesta-resultado-barra-track">
                                                <div class="encuesta-resultado-barra-fill" style='<%# "width:" + Eval("Porcentaje").ToString().Replace(",", ".") + "%;" %>'></div>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <div class="form-actions">
                        <a class="text-link" href="Encuestas.aspx">Volver</a>
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
