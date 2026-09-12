<%@ Page Title="Mi perfil | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MiPerfil.aspx.cs" Inherits="StageUp.UI.MiPerfil" %>

<asp:Content ID="ProfileContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="profile-page">
        <div class="container-wide">
            <header class="profile-page-header">
                <div>
                    <span class="section-label">Tu cuenta</span>
                    <h1>Mi perfil</h1>
                    <p>Administrá tu información personal y la seguridad de tu cuenta.</p>
                </div>
            </header>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <div class="profile-layout">
                <aside class="profile-summary-card">
                    <div class="profile-avatar-shell">
                        <asp:Image ID="imgPerfil" runat="server" CssClass="profile-avatar-image" AlternateText="Foto de perfil" Visible="false" />
                        <asp:Panel ID="pnlIniciales" runat="server" CssClass="profile-avatar-initials">
                            <asp:Literal ID="litIniciales" runat="server" Mode="Encode" />
                        </asp:Panel>
                    </div>
                    <h2><asp:Literal ID="litNombreResumen" runat="server" Mode="Encode" /></h2>
                    <span class="profile-role"><asp:Literal ID="litPerfilUsuario" runat="server" Mode="Encode" /></span>
                    <p class="profile-summary-email"><asp:Literal ID="litCorreoResumen" runat="server" Mode="Encode" /></p>

                    <div class="profile-reputation" aria-label="Tu reputación como solicitante">
                        <span class="profile-card-eyebrow">Tu reputación</span>
                        <div class="profile-stars" aria-hidden="true"><asp:Literal ID="litEstrellasPerfil" runat="server" /></div>
                        <strong><asp:Literal ID="litResumenReputacionPerfil" runat="server" Mode="Encode" /></strong>
                        <p><asp:Literal ID="litAyudaReputacionPerfil" runat="server" Mode="Encode" /></p>
                    </div>
                </aside>

                <div class="profile-content-stack">
                    <section class="profile-card" aria-labelledby="personal-data-title">
                        <div class="profile-card-header">
                            <div>
                                <span class="profile-card-eyebrow">Información personal</span>
                                <h2 id="personal-data-title">Tus datos</h2>
                            </div>
                            <asp:LinkButton ID="lnkEditarDatos" runat="server" CssClass="button button-secondary button-small" CausesValidation="false" OnClick="lnkEditarDatos_Click">Editar perfil</asp:LinkButton>
                        </div>

                        <asp:Panel ID="pnlVistaDatos" runat="server">
                            <dl class="profile-data-list">
                                <div>
                                    <dt>Nombre</dt>
                                    <dd><asp:Literal ID="litNombre" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Apellido</dt>
                                    <dd><asp:Literal ID="litApellido" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Correo electrónico</dt>
                                    <dd><asp:Literal ID="litCorreo" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Teléfono</dt>
                                    <dd><asp:Literal ID="litTelefono" runat="server" Mode="Encode" /></dd>
                                </div>
                                <div>
                                    <dt>Miembro desde</dt>
                                    <dd><asp:Literal ID="litFechaAlta" runat="server" Mode="Encode" /></dd>
                                </div>
                            </dl>
                            <div class="profile-description">
                                <span class="profile-card-eyebrow">Sobre vos</span>
                                <p><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></p>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlEditarDatos" runat="server" Visible="false" CssClass="profile-edit-form">
                            <asp:Panel ID="pnlIntegracionPerfilPendiente" runat="server" CssClass="profile-integration-note">
                                La edición ya está preparada. Se habilitará cuando se integren los procedimientos de perfil en la base de datos.
                            </asp:Panel>

                            <div class="profile-photo-field">
                                <div>
                                    <label for="<%= fuFotoPerfil.ClientID %>">Foto de perfil</label>
                                    <p>Elegí una imagen JPG o PNG de hasta 5 MB.</p>
                                </div>
                                <asp:FileUpload ID="fuFotoPerfil" runat="server" accept=".jpg,.jpeg,.png,image/jpeg,image/png" />
                            </div>

                            <div class="form-grid form-grid-two-columns">
                                <div class="form-field">
                                    <label for="<%= txtNombre.ClientID %>">Nombre</label>
                                    <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" autocomplete="given-name" />
                                    <asp:RequiredFieldValidator ID="rfvNombre" runat="server" ControlToValidate="txtNombre" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu nombre." ValidationGroup="Perfil" />
                                </div>
                                <div class="form-field">
                                    <label for="<%= txtApellido.ClientID %>">Apellido</label>
                                    <asp:TextBox ID="txtApellido" runat="server" MaxLength="100" autocomplete="family-name" />
                                    <asp:RequiredFieldValidator ID="rfvApellido" runat="server" ControlToValidate="txtApellido" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu apellido." ValidationGroup="Perfil" />
                                </div>
                            </div>

                            <div class="form-field">
                                <label for="<%= txtCorreo.ClientID %>">Correo electrónico</label>
                                <asp:TextBox ID="txtCorreo" runat="server" TextMode="Email" MaxLength="300" autocomplete="email" />
                                <asp:RequiredFieldValidator ID="rfvCorreo" runat="server" ControlToValidate="txtCorreo" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu correo electrónico." ValidationGroup="Perfil" />
                                <asp:RegularExpressionValidator ID="revCorreo" runat="server" ControlToValidate="txtCorreo" Display="Dynamic" CssClass="field-error-text" ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Ingresá un correo electrónico válido." ValidationGroup="Perfil" />
                            </div>

                            <div class="form-field">
                                <label for="<%= txtTelefono.ClientID %>">Teléfono <span class="field-help">(opcional)</span></label>
                                <asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" autocomplete="tel" placeholder="Ej: +54 11 5555-5555" />
                                <asp:RegularExpressionValidator ID="revTelefono" runat="server" ControlToValidate="txtTelefono" Display="Dynamic" CssClass="field-error-text" ValidationExpression="^[0-9+()\-\s]{6,30}$" ErrorMessage="El teléfono ingresado no es válido." ValidationGroup="Perfil" />
                            </div>

                            <div class="form-field">
                                <label for="<%= txtDescripcion.ClientID %>">Descripción personal o profesional</label>
                                <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="5" MaxLength="1200" CssClass="profile-textarea" placeholder="Contá brevemente quién sos y qué hacés." />
                                <small class="field-help">Máximo 1200 caracteres.</small>
                            </div>

                            <div class="profile-form-actions">
                                <asp:Button ID="btnGuardarDatos" runat="server" CssClass="button button-primary" Text="Guardar cambios" ValidationGroup="Perfil" OnClick="btnGuardarDatos_Click" />
                                <asp:LinkButton ID="lnkCancelarEdicion" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarEdicion_Click">Cancelar</asp:LinkButton>
                            </div>
                        </asp:Panel>
                    </section>

                    <section class="profile-card" aria-labelledby="profile-reviews-title">
                        <div class="profile-card-header">
                            <div>
                                <span class="profile-card-eyebrow" data-i18n="Calificacion_Historial">Experiencias verificadas</span>
                                <h2 id="profile-reviews-title" data-i18n="Calificacion_MisCalificaciones">Mis calificaciones</h2>
                            </div>
                        </div>
                        <div class="profile-reviews-grid">
                            <section aria-labelledby="received-reviews-title">
                                <h3 id="received-reviews-title" data-i18n="Calificacion_Recibidas">Recibidas como solicitante</h3>
                                <asp:Panel ID="pnlSinCalificacionesRecibidas" runat="server" CssClass="profile-reviews-empty">
                                    Todavía no recibiste calificaciones. Aparecerán después de tus reservas finalizadas.
                                </asp:Panel>
                                <asp:Repeater ID="rptCalificacionesRecibidas" runat="server">
                                    <ItemTemplate>
                                        <article class="profile-review-item">
                                            <header><strong><%#: Eval("NombreEspacio") %></strong><span><%#: ObtenerEstrellas(Convert.ToInt32(Eval("Puntaje"))) %></span></header>
                                            <p><%#: Eval("Comentario") %></p>
                                            <small>Reserva del <%# Eval("FechaReserva", "{0:dd/MM/yyyy}") %> · Por <%#: Eval("NombreAutor") %></small>
                                        </article>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </section>
                            <section aria-labelledby="made-reviews-title">
                                <h3 id="made-reviews-title" data-i18n="Calificacion_Realizadas">Realizadas</h3>
                                <asp:Panel ID="pnlSinCalificacionesRealizadas" runat="server" CssClass="profile-reviews-empty">
                                    Todavía no realizaste calificaciones.
                                </asp:Panel>
                                <asp:Repeater ID="rptCalificacionesRealizadas" runat="server">
                                    <ItemTemplate>
                                        <article class="profile-review-item">
                                            <header><strong><%#: ObtenerDestinoCalificacion((StageUp.BE.Entidades.Calificacion)Container.DataItem) %></strong><span><%#: ObtenerEstrellas(Convert.ToInt32(Eval("Puntaje"))) %></span></header>
                                            <p><%#: Eval("Comentario") %></p>
                                            <small>Reserva del <%# Eval("FechaReserva", "{0:dd/MM/yyyy}") %></small>
                                        </article>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </section>
                        </div>
                    </section>

                    <section class="profile-card" aria-labelledby="security-title">
                        <div class="profile-card-header">
                            <div>
                                <span class="profile-card-eyebrow">Seguridad</span>
                                <h2 id="security-title">Contraseña</h2>
                            </div>
                            <asp:LinkButton ID="lnkCambiarPassword" runat="server" CssClass="button button-secondary button-small" CausesValidation="false" OnClick="lnkCambiarPassword_Click">Cambiar contraseña</asp:LinkButton>
                        </div>

                        <asp:Panel ID="pnlPasswordResumen" runat="server" CssClass="profile-password-summary">
                            <span class="profile-password-dots" aria-hidden="true">••••••••••••</span>
                            <p>Por seguridad, tu contraseña nunca se muestra.</p>
                        </asp:Panel>

                        <asp:Panel ID="pnlCambiarPassword" runat="server" Visible="false" CssClass="profile-password-form">
                            <p class="profile-form-intro">Para proteger tu cuenta, primero confirmá tu contraseña actual.</p>
                            <div class="form-field">
                                <label for="<%= txtPasswordActual.ClientID %>">Contraseña actual</label>
                                <asp:TextBox ID="txtPasswordActual" runat="server" TextMode="Password" autocomplete="current-password" />
                                <asp:RequiredFieldValidator ID="rfvPasswordActual" runat="server" ControlToValidate="txtPasswordActual" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá tu contraseña actual." ValidationGroup="CambiarPassword" />
                            </div>
                            <div class="form-grid form-grid-two-columns">
                                <div class="form-field">
                                    <label for="<%= txtNuevaPassword.ClientID %>">Nueva contraseña</label>
                                    <asp:TextBox ID="txtNuevaPassword" runat="server" TextMode="Password" autocomplete="new-password" />
                                    <asp:RequiredFieldValidator ID="rfvNuevaPassword" runat="server" ControlToValidate="txtNuevaPassword" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá la nueva contraseña." ValidationGroup="CambiarPassword" />
                                </div>
                                <div class="form-field">
                                    <label for="<%= txtConfirmarPassword.ClientID %>">Confirmar nueva contraseña</label>
                                    <asp:TextBox ID="txtConfirmarPassword" runat="server" TextMode="Password" autocomplete="new-password" />
                                    <asp:RequiredFieldValidator ID="rfvConfirmarPassword" runat="server" ControlToValidate="txtConfirmarPassword" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Confirmá la nueva contraseña." ValidationGroup="CambiarPassword" />
                                    <asp:CompareValidator ID="cvPassword" runat="server" ControlToValidate="txtConfirmarPassword" ControlToCompare="txtNuevaPassword" Display="Dynamic" CssClass="field-error-text" ErrorMessage="La nueva contraseña y su confirmación no coinciden." ValidationGroup="CambiarPassword" />
                                </div>
                            </div>
                            <p class="profile-password-help">Usá al menos 8 caracteres e incluí letras y números.</p>
                            <div class="profile-form-actions">
                                <asp:Button ID="btnCambiarPassword" runat="server" CssClass="button button-primary" Text="Actualizar contraseña" ValidationGroup="CambiarPassword" OnClick="btnCambiarPassword_Click" />
                                <asp:LinkButton ID="lnkCancelarPassword" runat="server" CssClass="text-link" CausesValidation="false" OnClick="lnkCancelarPassword_Click">Cancelar</asp:LinkButton>
                            </div>
                        </asp:Panel>
                    </section>
                </div>
            </div>
        </div>
    </section>
</asp:Content>
