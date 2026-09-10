<%@ Page Title="Acceso interno | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="IniciarSesionInterno.aspx.cs" Inherits="StageUp.UI.Interno.IniciarSesionInterno" %>

<asp:Content ID="LoginInternoContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="auth-page">
        <div class="auth-layout container-narrow">
            <div class="auth-intro">
                <span class="section-label">Acceso interno</span>
                <h1>Panel administrativo de StageUp.</h1>
                <p>Esta sección es exclusiva para el personal interno de StageUp.</p>
            </div>

            <asp:Panel ID="pnlLoginInterno" runat="server" CssClass="auth-card" role="form" aria-labelledby="login-interno-title">
                <div class="auth-card-header">
                    <h2 id="login-interno-title">Iniciar sesión interna</h2>
                    <p>Completá tus datos para continuar.</p>
                </div>

                <asp:Panel ID="pnlMensajeLoginInterno" runat="server" Visible="false" CssClass="form-message">
                    <asp:Literal ID="litMensajeLoginInterno" runat="server" />
                </asp:Panel>

                <div class="form-field">
                    <label for="<%= txtCorreoInterno.ClientID %>">Correo electrónico</label>
                    <asp:TextBox ID="txtCorreoInterno" runat="server" TextMode="Email" autocomplete="email" placeholder="nombre@stageup.test" MaxLength="300" />
                    <asp:RequiredFieldValidator ID="rfvCorreoInterno" runat="server" ControlToValidate="txtCorreoInterno"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu correo electrónico." ValidationGroup="LoginInterno" />
                </div>

                <div class="form-field">
                    <label for="<%= txtPasswordInterno.ClientID %>">Contraseña</label>
                    <asp:TextBox ID="txtPasswordInterno" runat="server" TextMode="Password" autocomplete="current-password" placeholder="Ingresá tu contraseña" />
                    <asp:RequiredFieldValidator ID="rfvPasswordInterno" runat="server" ControlToValidate="txtPasswordInterno"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu contraseña." ValidationGroup="LoginInterno" />
                </div>

                <div class="captcha-field" aria-labelledby="captcha-login-interno-title">
                    <div class="captcha-field-copy">
                        <strong id="captcha-login-interno-title" data-i18n="Captcha_Titulo">Verificación de seguridad</strong>
                        <span data-i18n="Captcha_Ayuda">Marcá la casilla para continuar.</span>
                    </div>
                    <div class="captcha-widget-frame">
                        <div class="g-recaptcha" data-sitekey="<%= ClaveSitioRecaptcha %>"></div>
                    </div>
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnIniciarSesionInterno" runat="server" CssClass="button button-primary button-full"
                        Text="Ingresar" ValidationGroup="LoginInterno" OnClick="btnIniciarSesionInterno_Click" />
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>

<asp:Content ID="LoginInternoScripts" ContentPlaceHolderID="PageScripts" runat="server">
    <script src="https://www.google.com/recaptcha/api.js?hl=<%= CodigoIdiomaRecaptcha %>" async defer></script>
</asp:Content>
