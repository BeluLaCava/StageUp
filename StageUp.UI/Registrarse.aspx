<%@ Page Title="Registrarse | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Registrarse.aspx.cs" Inherits="StageUp.UI.Registrarse" %>

<asp:Content ID="RegisterContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="auth-page auth-page-register">
        <div class="auth-layout container-narrow">
            <div class="auth-intro">
                <span class="section-label">Sumate a StageUp</span>
                <h1>Un punto de encuentro para artistas y espacios.</h1>
                <p>Creá tu cuenta para comenzar a formar parte de una comunidad artística más visible y organizada.</p>
            </div>

            <div class="auth-card" role="form" aria-labelledby="register-title">
                <div class="auth-card-header">
                    <h2 id="register-title">Crear una cuenta</h2>
                    <p>Ingresá tus datos personales.</p>
                </div>

                <div class="form-grid form-grid-two-columns">
                    <div class="form-field">
                        <label for="register-name">Nombre</label>
                        <input id="register-name" type="text" autocomplete="given-name" placeholder="Tu nombre" />
                    </div>

                    <div class="form-field">
                        <label for="register-lastname">Apellido</label>
                        <input id="register-lastname" type="text" autocomplete="family-name" placeholder="Tu apellido" />
                    </div>
                </div>

                <div class="form-field">
                    <label for="register-email">Correo electrónico</label>
                    <input id="register-email" type="email" autocomplete="email" placeholder="nombre@correo.com" />
                </div>

                <div class="form-grid form-grid-two-columns">
                    <div class="form-field">
                        <label for="register-password">Contraseña</label>
                        <input id="register-password" type="password" autocomplete="new-password" placeholder="Creá una contraseña" />
                    </div>

                    <div class="form-field">
                        <label for="register-password-confirmation">Confirmar contraseña</label>
                        <input id="register-password-confirmation" type="password" autocomplete="new-password" placeholder="Repetí la contraseña" />
                    </div>
                </div>

                <label class="checkbox-field" for="accept-legal">
                    <input id="accept-legal" type="checkbox" />
                    <span>
                        Acepto los <span class="text-link text-link-disabled" aria-disabled="true">Términos y condiciones</span>
                        y la <span class="text-link text-link-disabled" aria-disabled="true">Política de privacidad</span>.
                    </span>
                </label>

                <button class="button button-primary button-full" type="button">Registrarse</button>

                <p class="auth-alternative">
                    ¿Ya tenés una cuenta?
                    <a class="text-link" href="IniciarSesion.aspx">Iniciá sesión</a>
                </p>
            </div>
        </div>
    </section>
</asp:Content>
