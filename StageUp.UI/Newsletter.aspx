<%@ Page Title="Newsletter | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Newsletter.aspx.cs" Inherits="StageUp.UI.Newsletter" %>

<asp:Content ID="NewsletterContent" ContentPlaceHolderID="MainContent" runat="server">
    <style type="text/css">
        .nl-layout { display: grid; gap: 1.25rem; max-width: 46rem; }
        .nl-categorias { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0.55rem; margin: 0.35rem 0 0; padding: 0; list-style: none; }
        .nl-categorias li { display: flex; align-items: center; gap: 0.6rem; padding: 0.65rem 0.8rem; border: 1px solid var(--color-border, #e2d4cc); border-radius: 0.8rem; background: #fffaf7; }
        .nl-categorias li input { width: 1.1rem; height: 1.1rem; margin: 0; flex: 0 0 auto; accent-color: var(--color-primary, #6d1021); }
        .nl-categorias li label { flex: 1 1 auto; margin: 0; font-weight: 600; cursor: pointer; }
        .nl-layout input[type="email"], .nl-layout input[type="text"] { width: 100%; }
        .nl-acepto { display: flex; align-items: flex-start; gap: 0.55rem; margin: 0.75rem 0; }
        .nl-acepto label { font-weight: normal; }
        .nl-nota { color: var(--color-text-muted, #765f55); font-size: 0.9em; margin: 0.6rem 0 0; }
        .nl-acciones { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; margin-top: 1rem; }
        .nl-baja-destacada { padding: 1rem 1.1rem; border-radius: 1rem; border: 2px solid var(--color-primary, #6d1021); background: #fff7f4; }
        .nl-baja-destacada p { margin: 0 0 0.75rem; }
        @media (max-width: 40rem) { .nl-categorias { grid-template-columns: 1fr; } }
    </style>

    <section class="static-page user-module-page">
        <div class="static-page-header">
            <span class="section-label">Newsletter</span>
            <h1>Recibí las novedades de StageUp</h1>
            <p>Nuevos espacios, consejos y novedades de la comunidad artística, directo en tu correo. No hace falta tener cuenta: elegís qué categorías recibir y te das de baja cuando quieras.</p>
        </div>

        <div class="static-page-body nl-layout">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <asp:Panel ID="pnlSuscribir" runat="server" CssClass="auth-card" DefaultButton="btnSuscribir">
                <div class="auth-card-header"><h2>Suscribirme</h2></div>
                <div class="form-field">
                    <label for="<%= txtCorreo.ClientID %>">Correo electrónico *</label>
                    <asp:TextBox ID="txtCorreo" runat="server" TextMode="Email" MaxLength="254" autocomplete="email" placeholder="tu@correo.com" />
                </div>
                <div class="form-field">
                    <label for="<%= txtNombre.ClientID %>">Nombre (opcional)</label>
                    <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" autocomplete="given-name" />
                </div>
                <div class="form-field">
                    <span class="form-label">Qué querés recibir *</span>
                    <asp:CheckBoxList ID="cblCategorias" runat="server" RepeatLayout="UnorderedList" CssClass="nl-categorias" />
                </div>
                <div class="nl-acepto">
                    <asp:CheckBox ID="chkAcepto" runat="server" />
                    <label for="<%= chkAcepto.ClientID %>">Acepto recibir por correo las novedades de StageUp de las categorías elegidas. Puedo darme de baja en cualquier momento desde el enlace de cada correo.</label>
                </div>
                <div class="g-recaptcha" data-sitekey="<%= ClaveSitioRecaptcha %>"></div>
                <div class="nl-acciones">
                    <asp:Button ID="btnSuscribir" runat="server" CssClass="button button-primary" Text="Suscribirme" CausesValidation="false" OnClick="btnSuscribir_Click" />
                </div>
                <p class="nl-nota">Te vamos a enviar un correo para confirmar la suscripción (doble confirmación). Solo usamos tu correo para enviarte el newsletter.</p>
            </asp:Panel>

            <asp:Panel ID="pnlConfirmada" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header"><h2>Suscripción confirmada</h2></div>
                <p>Desde ahora vas a recibir las novedades de StageUp. En cada correo vas a encontrar un enlace para cambiar tus preferencias o darte de baja.</p>
                <div class="nl-acciones">
                    <asp:HyperLink ID="lnkPreferenciasConfirmada" runat="server" CssClass="button button-secondary" Text="Elegir categorías" />
                    <a class="text-link" href="Novedades.aspx">Ver las novedades</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlPreferencias" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header"><h2>Tus preferencias</h2></div>
                <p>Suscripción de <strong><asp:Literal ID="litCorreo" runat="server" Mode="Encode" /></strong></p>

                <asp:Panel ID="pnlActiva" runat="server">
                    <asp:Panel ID="pnlBajaDestacada" runat="server" Visible="false" CssClass="nl-baja-destacada">
                        <p><strong>¿Querés dejar de recibir el newsletter?</strong> Confirmá la baja con el botón.</p>
                        <asp:Button ID="btnBajaDestacada" runat="server" CssClass="button button-primary" Text="Sí, darme de baja" CausesValidation="false" OnClick="btnBaja_Click" />
                    </asp:Panel>
                    <div class="form-field">
                        <span class="form-label">Categorías que recibís</span>
                        <asp:CheckBoxList ID="cblPreferencias" runat="server" RepeatLayout="UnorderedList" CssClass="nl-categorias" />
                    </div>
                    <div class="nl-acciones">
                        <asp:Button ID="btnGuardarPreferencias" runat="server" CssClass="button button-primary" Text="Guardar preferencias" CausesValidation="false" OnClick="btnGuardarPreferencias_Click" />
                        <asp:Button ID="btnBaja" runat="server" CssClass="button button-secondary" Text="Darme de baja" CausesValidation="false" OnClick="btnBaja_Click"
                            OnClientClick="if (!StageUpConfirm.confirmPostback(this, event, '¿Dejar de recibir el newsletter de StageUp? Podés volver a suscribirte cuando quieras.', { title: 'Darme de baja', confirmText: 'Sí, darme de baja', cancelText: 'Volver' })) return false;" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlInactiva" runat="server" Visible="false">
                    <p>No estás recibiendo el newsletter.</p>
                    <div class="nl-acciones">
                        <asp:Button ID="btnReactivar" runat="server" CssClass="button button-primary" Text="Volver a suscribirme" CausesValidation="false" OnClick="btnReactivar_Click" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlPendiente" runat="server" Visible="false">
                    <p>Tu suscripción todavía no está confirmada. Buscá en tu correo el mensaje "Confirmá tu suscripción" o volvé a suscribirte para recibir uno nuevo.</p>
                    <div class="nl-acciones"><a class="button button-secondary" href="Newsletter.aspx">Volver a suscribirme</a></div>
                </asp:Panel>
            </asp:Panel>
        </div>
    </section>

    <asp:PlaceHolder ID="phScriptRecaptcha" runat="server">
        <script src="https://www.google.com/recaptcha/api.js?hl=<%= CodigoIdiomaRecaptcha %>" async defer></script>
    </asp:PlaceHolder>
</asp:Content>
