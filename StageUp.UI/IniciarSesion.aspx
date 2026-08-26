<%@ Page Title="Iniciar sesión | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="IniciarSesion.aspx.cs" Inherits="StageUp.UI.IniciarSesion" %>

<asp:Content ID="LoginContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="auth-page">
        <div class="auth-layout container-narrow">
            <div class="auth-intro">
                <span class="section-label">Bienvenido a StageUp</span>
                <h1>Volvé a conectar con tu espacio artístico.</h1>
                <p>Ingresá para acceder, más adelante, a tus reservas, espacios y actividades.</p>
            </div>

            <div class="auth-card" role="form" aria-labelledby="login-title">
                <div class="auth-card-header">
                    <h2 id="login-title">Iniciar sesión</h2>
                    <p>Completá tus datos para continuar.</p>
                </div>

                <div class="form-field">
                    <label for="login-email">Correo electrónico</label>
                    <input id="login-email" type="email" autocomplete="email" placeholder="nombre@correo.com" />
                </div>

                <div class="form-field">
                    <div class="label-row">
                        <label for="login-password">Contraseña</label>
                        <span class="text-link text-link-disabled" aria-disabled="true" title="Disponible próximamente">¿Olvidaste tu contraseña?</span>
                    </div>
                    <input id="login-password" type="password" autocomplete="current-password" placeholder="Ingresá tu contraseña" />
                </div>

                <button class="button button-primary button-full" type="button">Iniciar sesión</button>

                <p class="auth-alternative">
                    ¿Todavía no tenés una cuenta?
                    <a class="text-link" href="Registrarse.aspx">Registrate</a>
                </p>
            </div>
        </div>
    </section>
</asp:Content>
