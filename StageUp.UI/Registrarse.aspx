<%@ Page Title="Registrarse | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Registrarse.aspx.cs" Inherits="StageUp.UI.Registrarse" %>

<asp:Content ID="RegisterContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="auth-page auth-page-register">
        <div class="auth-layout container-narrow">
            <div class="auth-intro">
                <span class="section-label">Sumate a StageUp</span>
                <h1>Un punto de encuentro para artistas y espacios.</h1>
                <p>Creá tu cuenta para comenzar a formar parte de una comunidad artística más visible y organizada.</p>
            </div>

            <!-- Paso 1: datos de registro (CU-001-001, escenario principal pasos 5-8) -->
            <asp:Panel ID="pnlDatosRegistro" runat="server" CssClass="auth-card" role="form" aria-labelledby="register-title">
                <div class="auth-card-header">
                    <h2 id="register-title">Crear una cuenta</h2>
                    <p>Ingresá tus datos personales.</p>
                </div>

                <asp:Panel ID="pnlMensajeRegistro" runat="server" Visible="false" CssClass="form-message">
                    <asp:Literal ID="litMensajeRegistro" runat="server" />
                </asp:Panel>

                <div class="form-grid form-grid-two-columns">
                    <div class="form-field">
                        <label for="<%= txtNombre.ClientID %>">Nombre</label>
                        <asp:TextBox ID="txtNombre" runat="server" TextMode="SingleLine" autocomplete="given-name" placeholder="Tu nombre" MaxLength="200" />
                        <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu nombre." ValidationGroup="Registro" />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtApellido.ClientID %>">Apellido</label>
                        <asp:TextBox ID="txtApellido" runat="server" TextMode="SingleLine" autocomplete="family-name" placeholder="Tu apellido" MaxLength="200" />
                        <asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu apellido." ValidationGroup="Registro" />
                    </div>
                </div>

                <div class="form-field">
                    <label for="<%= txtEmail.ClientID %>">Correo electrónico</label>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" autocomplete="email" placeholder="nombre@correo.com" MaxLength="300" />
                    <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu correo electrónico." ValidationGroup="Registro" />
                    <asp:RegularExpressionValidator ID="revEmail" runat="server" ControlToValidate="txtEmail"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="El correo electrónico no tiene un formato válido."
                        ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ValidationGroup="Registro" />
                </div>

                <div class="form-grid form-grid-two-columns">
                    <div class="form-field">
                        <label for="<%= txtPassword.ClientID %>">Contraseña</label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="new-password" placeholder="Creá una contraseña" />
                        <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá una contraseña." ValidationGroup="Registro" />
                    </div>

                    <div class="form-field">
                        <label for="<%= txtConfirmarPassword.ClientID %>">Confirmar contraseña</label>
                        <asp:TextBox ID="txtConfirmarPassword" runat="server" TextMode="Password" autocomplete="new-password" placeholder="Repetí la contraseña" />
                        <asp:RequiredFieldValidator ID="rfvConfirmarPassword" runat="server" ControlToValidate="txtConfirmarPassword"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="Confirmá la contraseña." ValidationGroup="Registro" />
                        <asp:CompareValidator ID="cvPassword" runat="server" ControlToValidate="txtConfirmarPassword" ControlToCompare="txtPassword"
                            Display="Dynamic" CssClass="field-error-text" ErrorMessage="La contraseña y su confirmación no coinciden." ValidationGroup="Registro" />
                    </div>
                </div>

                <label class="checkbox-field" for="<%= chkAceptaTerminos.ClientID %>">
                    <asp:CheckBox ID="chkAceptaTerminos" runat="server" />
                    <span>
                        Acepto los <a class="text-link" href="TerminosCondiciones.aspx" target="_blank" rel="noopener">Términos y condiciones</a>.
                    </span>
                </label>
                <asp:RequiredFieldValidator ID="rfvAceptaTerminos" runat="server" ControlToValidate="chkAceptaTerminos"
                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="Debés aceptar los Términos y condiciones."
                    InitialValue="false" ValidationGroup="Registro" />

                <label class="checkbox-field" for="<%= chkAceptaPoliticaPrivacidad.ClientID %>">
                    <asp:CheckBox ID="chkAceptaPoliticaPrivacidad" runat="server" />
                    <span>
                        Acepto la <a class="text-link" href="PoliticaPrivacidad.aspx" target="_blank" rel="noopener">Política de privacidad</a>.
                    </span>
                </label>
                <asp:RequiredFieldValidator ID="rfvAceptaPoliticaPrivacidad" runat="server" ControlToValidate="chkAceptaPoliticaPrivacidad"
                    Display="Dynamic" CssClass="field-error-text" ErrorMessage="Debés aceptar la Política de privacidad."
                    InitialValue="false" ValidationGroup="Registro" />

                <asp:Button ID="btnRegistrarse" runat="server" CssClass="button button-primary button-full" Text="Registrarse"
                    ValidationGroup="Registro" OnClick="btnRegistrarse_Click" />

                <p class="auth-alternative">
                    ¿Ya tenés una cuenta?
                    <a class="text-link" href="IniciarSesion.aspx">Iniciá sesión</a>
                </p>
            </asp:Panel>

            <!-- Paso 2: activación por código (CU-001-001, escenario principal pasos 12-18) -->
            <asp:Panel ID="pnlActivacion" runat="server" Visible="false" CssClass="auth-card" role="form" aria-labelledby="activation-title">
                <div class="auth-card-header">
                    <h2 id="activation-title">Activá tu cuenta</h2>
                    <p>Te enviamos un código de activación a tu correo electrónico. Ingresalo para activar tu cuenta.</p>
                </div>

                <asp:Panel ID="pnlMensajeActivacion" runat="server" Visible="false" CssClass="form-message">
                    <asp:Literal ID="litMensajeActivacion" runat="server" />
                </asp:Panel>

                <div class="form-field">
                    <label for="<%= txtCodigoActivacion.ClientID %>">Código de activación</label>
                    <asp:TextBox ID="txtCodigoActivacion" runat="server" TextMode="SingleLine" MaxLength="40" placeholder="Ingresá el código recibido" />
                    <asp:RequiredFieldValidator ID="rfvCodigoActivacion" runat="server" ControlToValidate="txtCodigoActivacion"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el código de activación." ValidationGroup="Activacion" />
                </div>

                <asp:Button ID="btnActivarCuenta" runat="server" CssClass="button button-primary button-full" Text="Activar cuenta"
                    ValidationGroup="Activacion" OnClick="btnActivarCuenta_Click" />

                <p class="auth-alternative">
                    ¿No te llegó el código?
                    <asp:LinkButton ID="lnkReenviarCodigoActivacion" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Reenviar código" OnClick="lnkReenviarCodigoActivacion_Click" />
                </p>
            </asp:Panel>

            <!-- Paso 3: confirmación (CU-001-001, paso 18) -->
            <asp:Panel ID="pnlExitoRegistro" runat="server" Visible="false" CssClass="auth-card">
                <div class="auth-card-header">
                    <h2>¡Cuenta activada!</h2>
                    <p>Tu cuenta fue activada correctamente. Ya podés iniciar sesión.</p>
                </div>
                <a class="button button-primary button-full" href="IniciarSesion.aspx">Iniciar sesión</a>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
