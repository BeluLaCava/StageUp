<%@ Page Title="Gestión de idiomas | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="GestionIdiomas.aspx.cs" Inherits="StageUp.UI.Interno.GestionIdiomas" %>

<asp:Content ID="GestionIdiomasContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="language-admin-page">
        <header class="language-admin-header">
            <div>
                <span class="section-label">Administración</span>
                <h1>Gestión de idiomas</h1>
                <p>Administrá los idiomas disponibles y completá las traducciones que después utilizará toda la plataforma.</p>
            </div>
            <div class="language-admin-summary" aria-label="Resumen de idiomas">
                <strong><asp:Literal ID="litCantidadIdiomas" runat="server" /></strong>
                <span>idiomas activos</span>
            </div>
        </header>

        <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message language-admin-message">
            <asp:Literal ID="litMensaje" runat="server" />
        </asp:Panel>

        <div class="language-admin-layout">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="language-form-card">
                <div class="language-card-heading">
                    <span class="language-card-kicker">Configuración general</span>
                    <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo idioma" /></h2>
                    <p>Usá un código de cultura estándar para que fechas y números se adapten correctamente.</p>
                </div>

                <div class="form-field">
                    <label for="<%= txtNombreIdioma.ClientID %>">Nombre del idioma *</label>
                    <asp:TextBox ID="txtNombreIdioma" runat="server" MaxLength="100" placeholder="Ej: Inglés" />
                    <asp:RequiredFieldValidator ID="rfvNombreIdioma" runat="server" ControlToValidate="txtNombreIdioma"
                        ValidationGroup="Idioma" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre del idioma." />
                </div>

                <div class="form-field">
                    <label for="<%= txtCodigoIdioma.ClientID %>">Código de cultura *</label>
                    <asp:TextBox ID="txtCodigoIdioma" runat="server" MaxLength="20" placeholder="Ej: en-US" />
                    <small class="field-help">Ejemplos: es-AR, en-US, pt-BR, fr-FR.</small>
                    <asp:RequiredFieldValidator ID="rfvCodigoIdioma" runat="server" ControlToValidate="txtCodigoIdioma"
                        ValidationGroup="Idioma" Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el código de cultura." />
                </div>

                <label class="language-default-option">
                    <asp:CheckBox ID="chkPredeterminado" runat="server" />
                    <span>
                        <strong>Usar como idioma predeterminado</strong>
                        <small>Será el idioma inicial cuando una persona todavía no haya elegido otro.</small>
                    </span>
                </label>

                <div class="form-actions language-form-actions">
                    <asp:Button ID="btnGuardarIdioma" runat="server" CssClass="button button-primary" Text="Crear idioma"
                        ValidationGroup="Idioma" OnClick="btnGuardarIdioma_Click" />
                    <asp:LinkButton ID="lnkCancelarEdicion" runat="server" CssClass="text-link" Text="Cancelar edición"
                        CausesValidation="false" Visible="false" OnClick="lnkCancelarEdicion_Click" />
                </div>
            </asp:Panel>

            <section class="language-list-section" aria-labelledby="tituloIdiomasDisponibles">
                <div class="language-list-heading">
                    <div>
                        <span class="language-card-kicker">Catálogo</span>
                        <h2 id="tituloIdiomasDisponibles">Idiomas disponibles</h2>
                    </div>
                    <p>El progreso indica cuántas etiquetas ya tienen traducción.</p>
                </div>

                <asp:Panel ID="pnlSinIdiomas" runat="server" CssClass="language-empty" Visible="false">
                    <h3>Todavía no hay idiomas activos</h3>
                    <p>Creá el primer idioma desde el formulario.</p>
                </asp:Panel>

                <div class="language-list">
                    <asp:Repeater ID="rptIdiomas" runat="server" OnItemCommand="rptIdiomas_ItemCommand">
                        <ItemTemplate>
                            <article class="language-row">
                                <div class="language-row-main">
                                    <div class="language-symbol" aria-hidden="true"><%#: ObtenerInicialIdioma(Eval("NombreIdioma") as string) %></div>
                                    <div>
                                        <div class="language-title-line">
                                            <h3><%#: Eval("NombreIdioma") %></h3>
                                            <span class="language-code"><%#: Eval("CodigoIdioma") %></span>
                                            <asp:Panel ID="pnlPredeterminado" runat="server" CssClass="language-default-badge"
                                                Visible='<%# Convert.ToBoolean(Eval("EsPredeterminado")) %>'>Predeterminado</asp:Panel>
                                        </div>
                                        <div class="language-progress-line">
                                            <div class="language-progress" role="progressbar" aria-label="Progreso de traducción"
                                                aria-valuemin="0" aria-valuemax="100" aria-valuenow='<%# Eval("PorcentajeTraducido") %>'>
                                                <span style='<%# "width:" + Eval("PorcentajeTraducido") + "%" %>'></span>
                                            </div>
                                            <span><%# Eval("CantidadTraducciones") %> de <%# Eval("CantidadEtiquetas") %> etiquetas · <%# Eval("PorcentajeTraducido") %>%</span>
                                        </div>
                                    </div>
                                </div>
                                <div class="language-row-actions">
                                    <asp:LinkButton runat="server" CssClass="button button-primary button-small" CausesValidation="false"
                                        CommandName="Configurar" CommandArgument='<%# Eval("IdIdioma") %>' Text="Configurar traducciones" />
                                    <asp:LinkButton runat="server" CssClass="text-link" CausesValidation="false"
                                        CommandName="Editar" CommandArgument='<%# Eval("IdIdioma") %>' Text="Editar" />
                                    <asp:LinkButton runat="server" CssClass="text-link language-danger-link" CausesValidation="false"
                                        CommandName="Baja" CommandArgument='<%# Eval("IdIdioma") %>' Text="Dar de baja"
                                        Visible='<%# !Convert.ToBoolean(Eval("EsPredeterminado")) %>'
                                        OnClientClick="return confirm('¿Seguro que querés dar de baja este idioma? Dejará de aparecer en el selector público.');" />
                                </div>
                            </article>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </section>
        </div>
    </section>
</asp:Content>
