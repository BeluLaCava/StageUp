<%@ Page Title="Mis espacios | StageUp" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="MisEspacios.aspx.cs" Inherits="StageUp.UI.MisEspacios" %>

<asp:Content ID="MisEspaciosContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page">
        <div class="static-page-header">
            <span class="section-label">Mis espacios</span>
            <h1>Administrá tus espacios artísticos</h1>
            <p>Acá podés dar de alta un espacio nuevo, editarlo, publicarlo o pausarlo, y dar de baja el que ya no ofrezcas.</p>
        </div>

        <div class="static-page-body">
            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" CssClass="form-message">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <!-- Alta / edición (CU-001-007, escenario principal) -->
            <asp:Panel ID="pnlFormularioEspacio" runat="server" CssClass="auth-card" role="form" aria-labelledby="form-espacio-title">
                <div class="auth-card-header">
                    <h2 id="form-espacio-title"><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo espacio" /></h2>
                    <p>Los campos con * son obligatorios. Podés publicar el espacio más adelante, no hace falta hacerlo ahora.</p>
                </div>

                <div class="form-field">
                    <label for="<%= txtNombreEspacio.ClientID %>">Nombre del espacio *</label>
                    <asp:TextBox ID="txtNombreEspacio" runat="server" TextMode="SingleLine" MaxLength="300" placeholder="Ej: Sala de ensayo Belgrano" />
                    <asp:RequiredFieldValidator ID="rfvNombreEspacio" runat="server" ControlToValidate="txtNombreEspacio"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el nombre del espacio." ValidationGroup="Espacio" />
                </div>

                <div class="form-field">
                    <label for="<%= txtTipoEspacio.ClientID %>">Tipo de espacio *</label>
                    <asp:TextBox ID="txtTipoEspacio" runat="server" TextMode="SingleLine" MaxLength="200" placeholder="Ej: Sala de ensayo, estudio de grabación, auditorio..." />
                    <asp:RequiredFieldValidator ID="rfvTipoEspacio" runat="server" ControlToValidate="txtTipoEspacio"
                        Display="Dynamic" CssClass="field-error-text" ErrorMessage="Ingresá el tipo de espacio." ValidationGroup="Espacio" />
                </div>

                <div class="form-field">
                    <label for="<%= txtDescripcion.ClientID %>">Descripción</label>
                    <asp:TextBox ID="txtDescripcion" runat="server" TextMode="MultiLine" Rows="4" MaxLength="2000" placeholder="Contá brevemente qué ofrece el espacio." />
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnGuardarEspacio" runat="server" CssClass="button button-primary" Text="Guardar espacio"
                        ValidationGroup="Espacio" OnClick="btnGuardarEspacio_Click" />
                    <asp:LinkButton ID="lnkCancelarEdicion" runat="server" CssClass="text-link" CausesValidation="false"
                        Text="Cancelar edición" Visible="false" OnClick="lnkCancelarEdicion_Click" />
                </div>
            </asp:Panel>

            <!-- Listado de espacios del gestor autenticado -->
            <div class="auth-card">
                <div class="auth-card-header">
                    <h2>Tus espacios</h2>
                </div>

                <asp:Literal ID="litSinEspacios" runat="server" Visible="false" Text="Todavía no cargaste ningún espacio." />

                <asp:Repeater ID="rptMisEspacios" runat="server" OnItemCommand="rptMisEspacios_ItemCommand" OnItemDataBound="rptMisEspacios_ItemDataBound">
                    <ItemTemplate>
                        <div class="space-row">
                            <div class="space-row-info">
                                <h3><%# Eval("NombreEspacio") %></h3>
                                <p><%# Eval("TipoEspacio") %> · Estado: <%# Eval("EstadoEspacio") %></p>
                                <p class="space-row-descripcion"><%# Eval("Descripcion") %></p>
                            </div>
                            <div class="space-row-actions">
                                <asp:LinkButton ID="lnkEditar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Editar" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Editar" />
                                <asp:LinkButton ID="lnkPublicar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Publicar" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Publicar" />
                                <asp:LinkButton ID="lnkPausar" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="Pausar" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Pausar" />
                                <asp:LinkButton ID="lnkDarDeBaja" runat="server" CssClass="text-link" CausesValidation="false"
                                    CommandName="BajaLogica" CommandArgument='<%# Eval("IdEspacioArtistico") %>' Text="Dar de baja"
                                    OnClientClick="return confirm('¿Seguro que querés dar de baja este espacio?');" />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>
</asp:Content>
