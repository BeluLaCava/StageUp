<%@ Page Title="Suscriptores, categorías y envíos | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionNewsletter.aspx.cs" Inherits="StageUp.UI.Interno.GestionNewsletter" %>

<asp:Content ID="GestionNewsletterContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page newsletter-admin-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Comunicación</span>
            <h1>Suscriptores, categorías y envíos</h1>
            <p>Administrá quiénes reciben el newsletter, las categorías de novedades y el resultado de cada envío.</p>
            <div class="newsletter-admin-links">
                <a class="button button-secondary" href="GestionNovedades.aspx">Volver a Novedades y newsletter</a>
            </div>
        </div>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
            <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
        </asp:Panel>

        <div class="static-page-body internal-admin-stack">
            <div class="newsletter-dashboard-strip" aria-label="Resumen de suscriptores">
                <article><span>Confirmados</span><strong><asp:Literal ID="litActivos" runat="server" /></strong></article>
                <article><span>Sin cuenta</span><strong><asp:Literal ID="litSinCuenta" runat="server" /></strong></article>
                <article><span>Sin confirmar</span><strong><asp:Literal ID="litPendientes" runat="server" /></strong></article>
                <article><span>Dados de baja</span><strong><asp:Literal ID="litBajas" runat="server" /></strong></article>
            </div>

            <%-- Suscriptores --%>
            <div class="auth-card admin-list-card" id="suscriptores">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Suscriptores</span>
                    <h2>Quiénes reciben el newsletter</h2>
                    <p>Solo reciben los envíos los suscriptores confirmados. Una persona con cuenta recibe el newsletter solo si se suscribió (desde el formulario o desde Mi perfil).</p>
                </div>

                <asp:Panel ID="pnlFiltros" runat="server" CssClass="nl-admin-filtros" DefaultButton="btnFiltrar">
                    <div class="form-field">
                        <label for="<%= txtBuscar.ClientID %>">Correo o nombre</label>
                        <asp:TextBox ID="txtBuscar" runat="server" MaxLength="254" />
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlEstado.ClientID %>">Estado</label>
                        <asp:DropDownList ID="ddlEstado" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Todos" Value="" />
                            <asp:ListItem Text="Confirmados" Value="Activo" />
                            <asp:ListItem Text="Sin confirmar" Value="Pendiente" />
                            <asp:ListItem Text="Dados de baja" Value="Baja" />
                        </asp:DropDownList>
                    </div>
                    <div class="form-field">
                        <label for="<%= ddlCuenta.ClientID %>">Cuenta</label>
                        <asp:DropDownList ID="ddlCuenta" runat="server" CssClass="form-select">
                            <asp:ListItem Text="Con y sin cuenta" Value="" />
                            <asp:ListItem Text="Con cuenta" Value="1" />
                            <asp:ListItem Text="Sin cuenta" Value="0" />
                        </asp:DropDownList>
                    </div>
                    <div class="nl-admin-filtros-acciones">
                        <asp:Button ID="btnFiltrar" runat="server" CssClass="button button-primary" Text="Filtrar" CausesValidation="false" OnClick="btnFiltrar_Click" />
                        <asp:LinkButton ID="lnkLimpiar" runat="server" CssClass="text-link" Text="Limpiar" CausesValidation="false" OnClick="lnkLimpiar_Click" />
                    </div>
                </asp:Panel>

                <p class="nl-admin-total"><asp:Literal ID="litTotalSuscriptores" runat="server" Mode="Encode" /></p>

                <div class="nl-tabla-wrap">
                    <asp:Repeater ID="rptSuscriptores" runat="server" OnItemCommand="rptSuscriptores_ItemCommand">
                        <HeaderTemplate>
                            <table class="nl-tabla">
                                <thead><tr><th>Correo</th><th>Estado</th><th>Cuenta</th><th>Categorías</th><th>Alta</th><th>Confirmación / baja</th><th>Acciones</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td data-label="Correo"><strong><%#: Eval("CorreoElectronico") %></strong><br /><small><%#: Eval("Nombre") ?? "" %></small></td>
                                <td data-label="Estado"><span class='<%# ClaseEstadoSuscriptor(Eval("Estado")) %>'><%#: EtiquetaEstadoSuscriptor(Eval("Estado")) %></span></td>
                                <td data-label="Cuenta"><%#: TextoCuenta(Container.DataItem) %></td>
                                <td data-label="Categorías"><%#: TextoCategorias(Container.DataItem) %></td>
                                <td data-label="Alta"><%# Convert.ToDateTime(Eval("FechaAlta")).ToString("dd/MM/yyyy") %></td>
                                <td data-label="Confirmación / baja"><%#: TextoFechas(Container.DataItem) %></td>
                                <td data-label="Acciones" class="nl-acciones-celda">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                        CommandName="Reenviar" CommandArgument='<%# Eval("IdSuscriptorNewsletter") %>' Text="Reenviar confirmación"
                                        Visible='<%# Convert.ToString(Eval("Estado")) == "Pendiente" %>' />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                        CommandName="Baja" CommandArgument='<%# Eval("IdSuscriptorNewsletter") %>' Text="Dar de baja"
                                        Visible='<%# Convert.ToString(Eval("Estado")) != "Baja" %>'
                                        OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Dar de baja a este suscriptor? Deja de recibir el newsletter; puede volver a suscribirse cuando quiera.', { title: 'Dar de baja', confirmText: 'Sí, dar de baja', cancelText: 'Volver' })) return false;" />
                                </td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
                <asp:Panel ID="pnlSinSuscriptores" runat="server" CssClass="admin-empty-hint" Visible="false">No hay suscriptores con esos filtros.</asp:Panel>
            </div>

            <%-- Categorías --%>
            <div class="auth-card admin-list-card" id="categorias">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Categorías</span>
                    <h2>Categorías de novedades</h2>
                    <p>Se usan al cargar una novedad y en las preferencias de los suscriptores. Una categoría desactivada deja de ofrecerse, pero las novedades que ya la tienen la conservan.</p>
                </div>

                <div class="nl-tabla-wrap">
                    <asp:Repeater ID="rptCategorias" runat="server" OnItemCommand="rptCategorias_ItemCommand">
                        <HeaderTemplate>
                            <table class="nl-tabla">
                                <thead><tr><th>Nombre</th><th>Código</th><th>Orden</th><th>Estado</th><th>Novedades</th><th>Suscriptores que la eligieron</th><th>Acciones</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td data-label="Nombre"><strong><%#: Eval("Nombre") %></strong></td>
                                <td data-label="Código"><code><%#: Eval("Codigo") %></code></td>
                                <td data-label="Orden"><%#: Eval("Orden") %></td>
                                <td data-label="Estado"><span class='<%# Convert.ToBoolean(Eval("Activa")) ? "admin-status-badge admin-status-badge-active" : "admin-status-badge admin-status-badge-inactive" %>'><%# Convert.ToBoolean(Eval("Activa")) ? "Activa" : "Desactivada" %></span></td>
                                <td data-label="Novedades"><%#: Eval("CantidadNovedades") %></td>
                                <td data-label="Suscriptores"><%#: Eval("CantidadSuscriptores") %></td>
                                <td data-label="Acciones" class="nl-acciones-celda">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                        CommandName="Editar" CommandArgument='<%# Eval("Codigo") %>' Text="Editar" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-danger" CausesValidation="false"
                                        CommandName="Desactivar" CommandArgument='<%# Eval("Codigo") %>' Text="Desactivar"
                                        Visible='<%# Convert.ToBoolean(Eval("Activa")) %>'
                                        OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Desactivar esta categoría? Deja de ofrecerse para nuevas novedades y en las preferencias del newsletter.', { title: 'Desactivar categoría', confirmText: 'Sí, desactivar', cancelText: 'Volver' })) return false;" />
                                    <asp:LinkButton runat="server" CssClass="admin-action-link admin-action-link-primary" CausesValidation="false"
                                        CommandName="Activar" CommandArgument='<%# Eval("Codigo") %>' Text="Activar"
                                        Visible='<%# !Convert.ToBoolean(Eval("Activa")) %>' />
                                </td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>

                <asp:Panel ID="pnlFormularioCategoria" runat="server" CssClass="nl-admin-form" DefaultButton="btnGuardarCategoria">
                    <h3><asp:Literal ID="litTituloCategoria" runat="server" Text="Nueva categoría" /></h3>
                    <div class="nl-admin-form-grid">
                        <div class="form-field">
                            <label for="<%= txtCategoriaNombre.ClientID %>">Nombre *</label>
                            <asp:TextBox ID="txtCategoriaNombre" runat="server" MaxLength="60" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCategoriaOrden.ClientID %>">Orden</label>
                            <asp:TextBox ID="txtCategoriaOrden" runat="server" TextMode="Number" min="0" max="9999" />
                        </div>
                    </div>
                    <small class="field-help">El código se genera con el nombre y no cambia aunque después se renombre la categoría.</small>
                    <div class="form-actions">
                        <asp:Button ID="btnGuardarCategoria" runat="server" CssClass="button button-primary" Text="Agregar categoría" CausesValidation="false" OnClick="btnGuardarCategoria_Click" />
                        <asp:LinkButton ID="lnkCancelarCategoria" runat="server" CssClass="text-link" Text="Cancelar edición" Visible="false" CausesValidation="false" OnClick="lnkCancelarCategoria_Click" />
                    </div>
                </asp:Panel>
            </div>

            <%-- Envíos --%>
            <div class="auth-card admin-list-card" id="envios">
                <div class="auth-card-header">
                    <span class="admin-card-eyebrow">Envíos</span>
                    <h2>Resultado de cada envío</h2>
                    <p>Completo: todos lo recibieron. Parcial: algunos correos fallaron y se pueden reintentar solo esos. Fallido: no llegó a nadie. Incierto: el correo pudo haber salido pero no se pudo registrar el resultado; no se reenvía solo y podés decidir reenviarlo. Omitido: se dio de baja antes del reintento. Un intento sin destinatarios no marca la novedad como enviada.</p>
                </div>

                <div class="nl-tabla-wrap">
                    <asp:Repeater ID="rptEnvios" runat="server" OnItemCommand="rptEnvios_ItemCommand">
                        <HeaderTemplate>
                            <table class="nl-tabla">
                                <thead><tr><th>Novedad</th><th>Fecha</th><th>Destinatarios</th><th>Estado</th><th>Recibieron</th><th>Fallidos</th><th>Inciertos</th><th>Omitidos</th><th>Acciones</th></tr></thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td data-label="Novedad"><strong><%#: Eval("TituloNovedad") %></strong><br /><small><%#: Eval("Responsable") %></small></td>
                                <td data-label="Fecha"><%# Convert.ToDateTime(Eval("FechaInicio")).ToString("dd/MM/yyyy HH:mm") %></td>
                                <td data-label="Destinatarios"><%#: StageUp.BLL.BLL_Novedad.ObtenerEtiquetaCriterio(Convert.ToString(Eval("Criterio"))) %><br /><small><%#: StageUp.BLL.BLL_CategoriaNovedad.ObtenerEtiqueta(Convert.ToString(Eval("Categoria"))) %></small></td>
                                <td data-label="Estado"><span class='<%# ClaseEstadoEnvio(Eval("Estado")) %>'><%#: StageUp.BLL.BLL_Novedad.ObtenerEtiquetaEstadoEnvio(Convert.ToString(Eval("Estado"))) %></span></td>
                                <td data-label="Recibieron"><%#: Eval("CantidadEnviados") %> de <%#: Eval("CantidadDestinatarios") %></td>
                                <td data-label="Fallidos"><%#: Eval("CantidadFallidos") %></td>
                                <td data-label="Inciertos"><%#: Eval("CantidadInciertos") %></td>
                                <td data-label="Omitidos"><%#: Eval("CantidadOmitidos") %></td>
                                <td data-label="Acciones" class="nl-acciones-celda">
                                    <asp:LinkButton runat="server" CssClass="admin-action-link" CausesValidation="false"
                                        CommandName="Ver" CommandArgument='<%# Eval("IdEnvioNewsletter") %>' Text="Ver destinatarios" />
                                </td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></tbody></table></FooterTemplate>
                    </asp:Repeater>
                </div>
                <asp:Panel ID="pnlSinEnvios" runat="server" CssClass="admin-empty-hint" Visible="false">Todavía no se envió ningún newsletter con el registro por destinatario.</asp:Panel>

                <asp:Panel ID="pnlDetalleEnvio" runat="server" Visible="false" CssClass="nl-admin-detalle">
                    <div class="nl-admin-detalle-cabecera">
                        <h3><asp:Literal ID="litDetalleEnvio" runat="server" Mode="Encode" /></h3>
                        <div class="nl-admin-detalle-acciones">
                            <asp:Button ID="btnReintentarEnvio" runat="server" CssClass="button button-primary" Text="Reintentar fallidos" CausesValidation="false" OnClick="btnReintentarEnvio_Click"
                                OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Reintentar el envío solo a los destinatarios que no lo recibieron?')) return false;" />
                            <asp:Button ID="btnReenviarInciertos" runat="server" CssClass="button button-secondary" Text="Reenviar a los de resultado incierto" CausesValidation="false" OnClick="btnReenviarInciertos_Click"
                                OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, 'Puede que algunos de estos destinatarios ya hayan recibido el correo y lo reciban dos veces. ¿Reenviarles igual?', { title: 'Resultado incierto', confirmText: 'Sí, reenviar', cancelText: 'Volver' })) return false;" />
                            <asp:LinkButton ID="lnkCerrarDetalle" runat="server" CssClass="text-link" Text="Cerrar" CausesValidation="false" OnClick="lnkCerrarDetalle_Click" />
                        </div>
                    </div>
                    <div class="nl-tabla-wrap">
                        <asp:Repeater ID="rptDestinatarios" runat="server">
                            <HeaderTemplate>
                                <table class="nl-tabla">
                                    <thead><tr><th>Correo</th><th>Estado</th><th>Intentos</th><th>Último intento</th></tr></thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Correo"><%#: Eval("CorreoElectronico") %></td>
                                    <td data-label="Estado"><span class='<%# ClaseEstadoDestinatario(Eval("Estado")) %>'><%#: EtiquetaEstadoDestinatario(Eval("Estado")) %></span></td>
                                    <td data-label="Intentos"><%#: Eval("Intentos") %></td>
                                    <td data-label="Último intento"><%#: Eval("FechaUltimoIntento") == null ? "—" : Convert.ToDateTime(Eval("FechaUltimoIntento")).ToString("dd/MM/yyyy HH:mm") %></td>
                                </tr>
                            </ItemTemplate>
                            <FooterTemplate></tbody></table></FooterTemplate>
                        </asp:Repeater>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </section>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .newsletter-admin-links { margin-top: 0.9rem; }
        .newsletter-admin-links .button { background: #fff; color: var(--color-primary); border-color: #fff; }
        .newsletter-admin-links .button:hover, .newsletter-admin-links .button:focus-visible { background: var(--color-nude); color: var(--color-primary-dark); }
        .nl-tabla td:first-child { min-width: 12rem; }
        .nl-admin-filtros { display: grid; grid-template-columns: minmax(0, 2fr) repeat(2, minmax(0, 1fr)) auto; gap: 0.75rem; align-items: end; }
        .nl-admin-filtros .form-field { margin-bottom: 0; }
        .nl-admin-filtros input { width: 100%; box-sizing: border-box; }
        .nl-admin-filtros-acciones { display: flex; align-items: center; gap: 0.75rem; padding-bottom: 0.2rem; }
        .nl-admin-total { margin: 0.9rem 0 0.4rem; color: var(--color-text-muted); font-size: 0.88rem; }
        .nl-tabla-wrap { overflow-x: auto; max-width: 100%; }
        .nl-tabla { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
        .nl-tabla th { text-align: left; color: var(--color-text-muted); font-weight: 600; padding: 8px; border-bottom: 1px solid var(--color-border); white-space: nowrap; }
        .nl-tabla td { padding: 8px; border-bottom: 1px solid var(--color-border); vertical-align: top; overflow-wrap: anywhere; }
        .nl-tabla small { color: var(--color-text-muted); }
        .nl-acciones-celda { white-space: nowrap; }
        .nl-acciones-celda .admin-action-link { display: inline-block; margin-right: 0.6rem; }
        .nl-admin-form { margin-top: 1.2rem; padding-top: 1rem; border-top: 1px solid var(--color-border); }
        .nl-admin-form h3, .nl-admin-detalle h3 { margin: 0 0 0.6rem; font-size: 1rem; }
        .nl-admin-form-grid { display: grid; grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); gap: 0.75rem; }
        .nl-admin-form-grid input { width: 100%; box-sizing: border-box; min-height: 2.9rem; }
        .nl-admin-detalle { margin-top: 1.2rem; padding-top: 1rem; border-top: 1px solid var(--color-border); }
        .nl-admin-detalle-cabecera { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 0.5rem; }
        .nl-admin-detalle-acciones { display: flex; align-items: center; gap: 0.75rem; }
        .nl-estado { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 0.8rem; font-weight: 700; white-space: nowrap; background: #eef2f7; color: #33475b; }
        .nl-estado-ok { background: #e6f5eb; color: #236240; }
        .nl-estado-alerta { background: #fff4e5; color: #8a4b0f; }
        .nl-estado-error { background: #fdecec; color: #8f1d1d; }
        @media (max-width: 760px) {
            .nl-admin-filtros, .nl-admin-form-grid { grid-template-columns: 1fr; }
            .newsletter-admin-page .newsletter-dashboard-strip { grid-template-columns: repeat(2, minmax(0, 1fr)); }
        }
    </style>
</asp:Content>
