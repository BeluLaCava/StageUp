<%@ Page Title="Aprobación de gestores | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="AprobacionGestores.aspx.cs" Inherits="StageUp.UI.Interno.AprobacionGestores" %>

<asp:Content ID="AprobacionGestoresContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .ag-filtro { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
        .ag-solicitud { border: 1px solid var(--color-border, #e2e2e2); border-radius: 14px; padding: 14px 16px; margin: 0 0 14px; }
        .ag-cabecera { display: flex; justify-content: space-between; gap: 10px; flex-wrap: wrap; align-items: baseline; }
        .ag-cabecera h3 { margin: 0; }
        .ag-estado { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 0.8em; font-weight: 700; border: 1px solid var(--color-border, #e2e2e2); }
        .ag-estado-pendienterevision { background: #fff4e5; border-color: #f5c27a; color: #8a4b08; }
        .ag-estado-aprobada { background: #e8f5ec; border-color: #9fd3ae; color: #1e6b37; }
        .ag-estado-rechazada { background: #fdecea; border-color: #f0b4ae; color: #8c1d18; }
        .ag-datos { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px 18px; margin: 12px 0; }
        .ag-datos div { min-width: 0; }
        .ag-datos dt { color: var(--color-text-muted, #765f55); font-size: 0.8em; }
        .ag-datos dd { margin: 2px 0 0; overflow-wrap: anywhere; }
        .ag-descripcion { margin: 0 0 12px; padding: 10px 12px; border-radius: 10px; background: var(--color-nude-light, #f6eee8); }
        .ag-resolucion { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 10px; align-items: end; }
        .ag-resolucion textarea { width: 100%; box-sizing: border-box; }
        .ag-botones { display: flex; gap: 8px; flex-wrap: wrap; }
        .ag-nota { color: var(--color-text-muted, #765f55); font-size: 0.9em; margin: 0; }
        @media (max-width: 800px) { .ag-datos { grid-template-columns: repeat(2, minmax(0, 1fr)); } .ag-resolucion { grid-template-columns: 1fr; } }
        @media (max-width: 520px) { .ag-datos { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Aprobación de gestores</span>
            <h1>Solicitudes de habilitación como gestor</h1>
            <p>Usuarios externos que pidieron poder publicar y administrar sus propios espacios artísticos (CU-001-007).</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="auth-card admin-workspace-card manager-requests-card">
                <div class="auth-card-header admin-workspace-header">
                    <div>
                        <span class="admin-card-eyebrow">Revisión</span>
                        <h2>Solicitudes</h2>
                    </div>
                    <div class="ag-filtro">
                        <label for="<%= ddlEstado.ClientID %>">Mostrar</label>
                        <asp:DropDownList ID="ddlEstado" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlEstado_SelectedIndexChanged" />
                    </div>
                </div>

                <asp:Repeater ID="rptSolicitudes" runat="server" OnItemCommand="rptSolicitudes_ItemCommand" OnItemDataBound="rptSolicitudes_ItemDataBound">
                    <ItemTemplate>
                        <article class="ag-solicitud">
                            <div class="ag-cabecera">
                                <h3><%#: Eval("NombreEspacio") %> <small>(<%#: Eval("TipoEspacio") %>)</small></h3>
                                <span class='<%# "ag-estado ag-estado-" + Eval("Estado").ToString().ToLowerInvariant() %>'><%#: StageUp.BLL.BLL_SolicitudHabilitacionGestor.NombreEstado(Eval("Estado") as string) %></span>
                            </div>
                            <p class="ag-nota">Solicitud N° <%#: Eval("IdSolicitud") %> · enviada el <%#: Eval("FechaSolicitud", "{0:dd/MM/yyyy HH:mm}") %> por <%#: Eval("NombreUsuario") %> (<%#: Eval("CorreoUsuario") %>)</p>
                            <dl class="ag-datos">
                                <div><dt>Responsable</dt><dd><%#: Eval("NombreResponsable") %></dd></div>
                                <div><dt>DNI / CUIT del responsable</dt><dd><%#: Eval("DocumentoResponsable") %></dd></div>
                                <div><dt>Contacto</dt><dd><%#: Eval("TelefonoContacto") %> · <%#: Eval("CorreoContacto") %></dd></div>
                                <div><dt>Condición fiscal</dt><dd><%#: StageUp.BLL.BLL_SolicitudHabilitacionGestor.NombreCondicionFiscal(Eval("CondicionFiscal") as string) %></dd></div>
                                <div><dt>Razón social / CUIT</dt><dd><%#: (Eval("RazonSocial") ?? "-") + " / " + (Eval("Cuit") ?? "-") %></dd></div>
                                <div><dt>Ubicación</dt><dd><%#: Eval("Ciudad") %>, <%#: Eval("Provincia") %></dd></div>
                            </dl>
                            <p class="ag-descripcion"><%#: Eval("DescripcionPropuesta") %></p>

                            <asp:Panel ID="pnlResolucion" runat="server" CssClass="ag-resolucion">
                                <div class="form-field">
                                    <label for='<%# Container.FindControl("txtMotivo").ClientID %>'>Motivo (obligatorio solo para rechazar; lo ve el usuario)</label>
                                    <asp:TextBox ID="txtMotivo" runat="server" TextMode="MultiLine" Rows="2" MaxLength="500" />
                                </div>
                                <div class="ag-botones">
                                    <asp:Button ID="btnAprobar" runat="server" CssClass="button button-primary button-small" Text="Aprobar"
                                        CommandName="Aprobar" CommandArgument='<%# Eval("IdSolicitud") %>' CausesValidation="false"
                                        OnClientClick="return confirm('¿Aprobar la solicitud? La cuenta va a poder publicar espacios.');" />
                                    <asp:Button ID="btnRechazar" runat="server" CssClass="button button-ghost button-small" Text="Rechazar"
                                        CommandName="Rechazar" CommandArgument='<%# Eval("IdSolicitud") %>' CausesValidation="false"
                                        OnClientClick="return confirm('¿Rechazar la solicitud con el motivo escrito?');" />
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlResuelta" runat="server" Visible="false">
                                <p class="ag-nota">
                                    <%#: Eval("Estado").ToString() == "Aprobada" ? "Aprobada" : "Rechazada" %> el <%#: Eval("FechaRevision", "{0:dd/MM/yyyy HH:mm}") %>
                                    por <%#: Eval("NombreRevisor") ?? "-" %><%#: string.IsNullOrEmpty(Eval("MotivoRechazo") as string) ? "" : ". Motivo: " + Eval("MotivoRechazo") %>
                                </p>
                            </asp:Panel>
                        </article>
                    </ItemTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlSinSolicitudes" runat="server" CssClass="empty-state" Visible="false">
                    <h3>No hay solicitudes para mostrar</h3>
                    <p>A medida que los usuarios externos pidan habilitarse como gestores, van a aparecer acá.</p>
                </asp:Panel>
            </div>
        </div>
    </section>
</asp:Content>
