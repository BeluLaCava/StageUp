<%@ Page Title="Iniciar sesión | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="IniciarSesion.aspx.cs" Inherits="StageUp.UI.IniciarSesion" %>

<asp:Content ID="LoginContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="auth-page">
        <div class="auth-layout container-narrow">
            <div class="auth-intro">
                <span class="section-label">Bienvenido a StageUp</span>
                <h1>Volvé a conectar con tu espacio artístico.</h1>
                <p>Ingresá para acceder, más adelante, a tus reservas, espacios y actividades.</p>
            </div>

            <!-- Inicio de sesión -->
            <asp:Panel ID="pnlLogin" runat="server" CssClass="auth-card" role="form" aria-labelledby="login-title">
                <div class="auth-card-header">
                    <h2 id="login-title">Iniciar sesión</h2>
                    <p>Completá tus datos para continuar.</p>
                </div>

                <asp:Panel ID="pnlMensajeLogin" runat="server" Visible="false" CssClass="form-message">
                    <asp:Literal ID="litMensajeLogin" runat="server" />
                </asp:Panel>

                <div class="form-field">
                    <label for="<%= txtLoginEmail.ClientID %>">Correo electrónico</label>
                    <asp:TextBox ID="txtLoginEmail" runat="server" TextMode="Email" autocomplete="email" placeholder="nombre@correo.com" MaxLength="300" />
                    <asp:RequiredFieldValidator ID="rfvLoginEmail" runat="server" ControlToValidate="txtLoginEmail"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu correo electrónico." ValidationGroup="Login" />
                </div>

                <div class="form-field">
                    <div class="label-row">
                        <label for="<%= txtLoginPassword.ClientID %>">Contraseña</label>
                        <asp:LinkButton ID="lnkOlvideContrasena" runat="server" CssClass="text-link" CausesValidation="false"
                            Text="¿Olvidaste tu contraseña?" OnClick="lnkOlvideContrasena_Click" />
                    </div>
                    <asp:TextBox ID="txtLoginPassword" runat="server" TextMode="Password" autocomplete="current-password" placeholder="Ingresá tu contraseña" />
                    <asp:RequiredFieldValidator ID="rfvLoginPassword" runat="server" ControlToValidate="txtLoginPassword"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu contraseña." ValidationGroup="Login" />
                </div>

                <asp:Button ID="btnIniciarSesion" runat="server" CssClass="button button-primary button-full" Text="Iniciar sesión"
                    ValidationGroup="Login" OnClick="btnIniciarSesion_Click" />

                <p class="auth-alternative">
                    ¿Todavía no tenés una cuenta?
                    <a class="text-link" href="Registrarse.aspx">Registrate</a>
                </p>
            </asp:Panel>

            <!-- Recuperación paso 1: solicitar código (CU-001-002 pasos 7-14) -->
            <asp:Panel ID="pnlRecuperarSolicitar" runat="server" Visible="false" CssClass="auth-card" role="form" aria-labelledby="recover-title">
                <div class="auth-card-header">
                    <h2 id="recover-title">Recuperar contraseña</h2>
                    <p>Ingresá el correo electrónico asociado a tu cuenta y te enviamos un código de recuperación.</p>
                </div>

                <asp:Panel ID="pnlMensajeRecuperarSolicitar" runat="server" Visible="false" CssClass="form-message">
                    <asp:Literal ID="litMensajeRecuperarSolicitar" runat="server" />
                </asp:Panel>

                <div class="form-field">
                    <label for="<%= txtRecuperarEmail.ClientID %>">Correo electrónico</label>
                    <asp:TextBox ID="txtRecuperarEmail" runat="server" TextMode="Email" autocomplete="email" placeholder="nombre@correo.com" MaxLength="300" />
                    <asp:RequiredFieldValidator ID="rfvRecuperarEmail" runat="server" ControlToValidate="txtRecuperarEmail"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu correo electrónico." ValidationGroup="RecuperarSolicitar" />
                </div>

                <asp:Button ID="btnEnviarCodigoRecuperacion" runat="server" CssClass="button button-primary button-full" Text="Enviar código"
                    ValidationGroup="RecuperarSolicitar" OnClick="btnEnviarCodigoRecuperacion_Click" />

                <p class="auth-alternative">
                    <asp:LinkButton ID="lnkVolverALoginDesdeSolicitar" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Volver a iniciar sesión" OnClick="lnkVolverALogin_Click" />
                </p>
            </asp:Panel>

            <!-- Recuperación paso 2: código + nueva contraseña (CU-001-002 pasos 15-21) -->
            <asp:Panel ID="pnlRecuperarActualizar" runat="server" Visible="false" CssClass="auth-card" role="form" aria-labelledby="recover-update-title">
                <div class="auth-card-header">
                    <h2 id="recover-update-title">Ingresá el código y tu nueva contraseña</h2>
                    <p>Te enviamos un código a tu correo electrónico. Tiene una vigencia de 15 minutos.</p>
                </div>

                <asp:Panel ID="pnlMensajeRecuperarActualizar" runat="server" Visible="false" CssClass="form-message">
                    <asp:Literal ID="litMensajeRecuperarActualizar" runat="server" />
                </asp:Panel>

                <div class="form-field">
                    <label for="<%= txtCodigoRecuperacion.ClientID %>">Código de recuperación</label>
                    <asp:TextBox ID="txtCodigoRecuperacion" runat="server" TextMode="SingleLine" MaxLength="40" placeholder="Ingresá el código recibido" />
                    <asp:RequiredFieldValidator ID="rfvCodigoRecuperacion" runat="server" ControlToValidate="txtCodigoRecuperacion"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el código de recuperación." ValidationGroup="RecuperarActualizar" />
                </div>

                <div class="form-grid form-grid-two-columns">
                    <div class="form-field">
                        <label for="<%= txtNuevaPassword.ClientID %>">Nueva contraseña</label>
                        <asp:TextBox ID="txtNuevaPassword" runat="server" TextMode="Password" autocomplete="new-password" placeholder="Nueva contraseña" />
                        <asp:RequiredFieldValidator ID="rfvNuevaPassword" runat="server" ControlToValidate="txtNuevaPassword"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la nueva contraseña." ValidationGroup="RecuperarActualizar" />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtConfirmarNuevaPassword.ClientID %>">Confirmar nueva contraseña</label>
                        <asp:TextBox ID="txtConfirmarNuevaPassword" runat="server" TextMode="Password" autocomplete="new-password" placeholder="Repetí la nueva contraseña" />
                        <asp:RequiredFieldValidator ID="rfvConfirmarNuevaPassword" runat="server" ControlToValidate="txtConfirmarNuevaPassword"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Confirmá la nueva contraseña." ValidationGroup="RecuperarActualizar" />
                        <asp:CompareValidator ID="cvNuevaPassword" runat="server" ControlToValidate="txtConfirmarNuevaPassword" ControlToCompare="txtNuevaPassword"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="La nueva contraseña y su confirmación no coinciden." ValidationGroup="RecuperarActualizar" />
                    </div>
                </div>

                <asp:Button ID="btnActualizarPassword" runat="server" CssClass="button button-primary button-full" Text="Actualizar contraseña"
                    ValidationGroup="RecuperarActualizar" OnClick="btnActualizarPassword_Click" />

                <p class="auth-alternative">
                    ¿No te llegó el código?
                    <asp:LinkButton ID="lnkReenviarCodigoRecuperacion" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Reenviar código" OnClick="lnkReenviarCodigoRecuperacion_Click" />
                </p>
            </asp:Panel>

            <!-- Recuperación paso 3: confirmación (CU-001-002 paso 21) -->
            <asp:Panel ID="pnlRecuperarExito" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>Contraseña actualizada</h2>
                    <p>Tu contraseña fue actualizada correctamente. Ya podés iniciar sesión con tus nuevas credenciales.</p>
                </div>
                <asp:LinkButton ID="lnkVolverALoginDesdeExito" runat="server" CssClass="button button-primary button-full" CausesValidation="false"
                    Text="Iniciar sesión" OnClick="lnkVolverALogin_Click" />
            </asp:Panel>
        </div>
    </section>
</asp:Content>
