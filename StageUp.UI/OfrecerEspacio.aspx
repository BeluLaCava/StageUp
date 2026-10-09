<%@ Page Title="Ofrecer espacio | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="OfrecerEspacio.aspx.cs" Inherits="StageUp.UI.OfrecerEspacio" %>

<asp:Content ID="OfrecerEspacioContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page user-module-page offer-space-page">
        <div class="static-page-header">
            <span class="section-label">Gestores de espacios</span>
            <h1>Ofrecer espacio</h1>
            <p>Para publicar espacios artísticos en StageUp, primero pedí la habilitación como gestor. Un administrador revisa cada solicitud.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message" role="status">
                <asp:Literal ID="litMensaje" runat="server" Mode="Encode" />
            </asp:Panel>

            <asp:Panel ID="pnlEstado" runat="server" Visible="false" CssClass="auth-card offer-space-card offer-space-status-card">
                <div class="auth-card-header">
                    <h2>Tu solicitud de habilitación</h2>
                </div>
                <p>
                    Estado: <asp:Label ID="lblEstado" runat="server" CssClass="oe-estado" />
                    · enviada el <asp:Literal ID="litFechaSolicitud" runat="server" Mode="Encode" />
                    <asp:Literal ID="litFechaRevision" runat="server" Mode="Encode" />
                </p>
                <p><asp:Literal ID="litExplicacionEstado" runat="server" Mode="Encode" /></p>

                <asp:Panel ID="pnlMotivoRechazo" runat="server" CssClass="oe-motivo" Visible="false">
                    <strong>Motivo del rechazo</strong>
                    <p class="oe-motivo-text"><asp:Literal ID="litMotivoRechazo" runat="server" Mode="Encode" /></p>
                </asp:Panel>

                <dl class="oe-datos">
                    <div><dt>Espacio o entidad</dt><dd><asp:Literal ID="litResumenEspacio" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Ubicación</dt><dd><asp:Literal ID="litResumenUbicacion" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Responsable</dt><dd><asp:Literal ID="litResumenResponsable" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Contacto</dt><dd><asp:Literal ID="litResumenContacto" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <div class="form-actions">
                    <asp:Button ID="btnNuevaSolicitud" runat="server" CssClass="button button-primary" Text="Enviar una nueva solicitud"
                        CausesValidation="false" Visible="false" OnClick="btnNuevaSolicitud_Click" />
                    <asp:HyperLink ID="lnkMisEspacios" runat="server" CssClass="button button-primary" NavigateUrl="~/MisEspacios.aspx" Text="Ir a Mis espacios" Visible="false" />
                    <a class="text-link" href="Explorar/ResultadosBusqueda.aspx">Seguir explorando espacios</a>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlFormulario" runat="server" Visible="false" CssClass="auth-card offer-space-card offer-space-form-card" DefaultButton="btnEnviar">
                <div class="auth-card-header">
                    <h2>Solicitud de habilitación como gestor</h2>
                    <p>Completá los datos para que podamos revisar tu solicitud. Los campos con * son obligatorios.</p>
                </div>

                <fieldset class="oe-grupo">
                    <legend>Responsable</legend>
                    <div class="oe-campos">
                        <div class="form-field">
                            <label for="<%= txtNombreResponsable.ClientID %>">Nombre y apellido *</label>
                            <asp:TextBox ID="txtNombreResponsable" runat="server" MaxLength="150" autocomplete="name" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtDocumento.ClientID %>">DNI o CUIT *</label>
                            <asp:TextBox ID="txtDocumento" runat="server" MaxLength="20" inputmode="numeric" />
                        </div>
                    </div>
                </fieldset>

                <fieldset class="oe-grupo">
                    <legend>Contacto</legend>
                    <div class="oe-campos">
                        <div class="form-field">
                            <label for="<%= txtTelefono.ClientID %>">Teléfono *</label>
                            <asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" autocomplete="tel" placeholder="Ej: +54 11 5555-5555" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCorreoContacto.ClientID %>">Correo electrónico *</label>
                            <asp:TextBox ID="txtCorreoContacto" runat="server" MaxLength="300" TextMode="Email" autocomplete="email" />
                        </div>
                    </div>
                </fieldset>

                <fieldset class="oe-grupo">
                    <legend>Datos administrativos</legend>
                    <p class="oe-ayuda">El CUIT es obligatorio si sos monotributista, responsable inscripto o exento.</p>
                    <div class="oe-campos">
                        <div class="form-field">
                            <label for="<%= ddlCondicionFiscal.ClientID %>">Condición fiscal *</label>
                            <asp:DropDownList ID="ddlCondicionFiscal" runat="server" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCuit.ClientID %>">CUIT</label>
                            <asp:TextBox ID="txtCuit" runat="server" MaxLength="13" placeholder="20-12345678-9" />
                        </div>
                        <div class="form-field oe-ancho">
                            <label for="<%= txtRazonSocial.ClientID %>">Razón social (si corresponde)</label>
                            <asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="150" />
                        </div>
                    </div>
                </fieldset>

                <fieldset class="oe-grupo">
                    <legend>El espacio o la entidad</legend>
                    <div class="oe-campos">
                        <div class="form-field">
                            <label for="<%= txtNombreEspacio.ClientID %>">Nombre *</label>
                            <asp:TextBox ID="txtNombreEspacio" runat="server" MaxLength="150" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtTipoEspacio.ClientID %>">Tipo de espacio *</label>
                            <asp:TextBox ID="txtTipoEspacio" runat="server" MaxLength="100" placeholder="Teatro, sala de ensayo, estudio..." />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtProvincia.ClientID %>">Provincia *</label>
                            <asp:TextBox ID="txtProvincia" runat="server" MaxLength="100" />
                        </div>
                        <div class="form-field">
                            <label for="<%= txtCiudad.ClientID %>">Ciudad *</label>
                            <asp:TextBox ID="txtCiudad" runat="server" MaxLength="150" />
                        </div>
                        <div class="form-field oe-ancho">
                            <label for="<%= txtDescripcion.ClientID %>">Contanos qué querés ofrecer *</label>
                            <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="4" MaxLength="1000"
                                placeholder="Qué tipo de actividades se pueden hacer, capacidad aproximada, equipamiento, desde cuándo funciona..." />
                        </div>
                    </div>
                </fieldset>

                <div class="form-actions">
                    <asp:Button ID="btnEnviar" runat="server" CssClass="button button-primary" Text="Enviar solicitud" CausesValidation="false"
                        UseSubmitBehavior="false" OnClientClick="this.disabled=true;this.value='Enviando...';" OnClick="btnEnviar_Click" />
                    <asp:LinkButton ID="lnkCancelarFormulario" runat="server" CssClass="text-link" CausesValidation="false" Visible="false" OnClick="lnkCancelarFormulario_Click">Cancelar</asp:LinkButton>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>
