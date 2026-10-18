<%@ Page Title="Consulta de espacio | StageUp" Language="C#" MasterPageFile="~/Interno/PanelInterno.Master" AutoEventWireup="true" CodeBehind="DetalleEspacioInterno.aspx.cs" Inherits="StageUp.UI.Interno.DetalleEspacioInterno" %>

<asp:Content ID="DetalleEspacioInternoContent" ContentPlaceHolderID="MainContent" runat="server">
    <section class="static-page internal-page internal-management-page">
        <div class="static-page-header internal-page-hero">
            <span class="section-label">Búsqueda interna</span>
            <h1>Consulta de espacio</h1>
            <p>Datos del espacio encontrado en la búsqueda interna. Es una consulta de solo lectura: no publica ni modifica el espacio.</p>
        </div>

        <div class="static-page-body internal-admin-stack">
            <asp:Panel ID="pnlNoEncontrado" runat="server" Visible="false" CssClass="auth-card admin-list-card">
                <div class="form-message form-message-error">
                    <asp:Literal ID="litNoEncontrado" runat="server" Mode="Encode" />
                </div>
                <div class="form-actions">
                    <asp:HyperLink ID="lnkVolverNoEncontrado" runat="server" CssClass="button button-secondary" Text="Volver a la búsqueda" />
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlDetalle" runat="server" CssClass="auth-card admin-list-card">
                <div class="auth-card-header espacio-int-cabecera">
                    <div>
                        <span class="admin-card-eyebrow"><asp:Literal ID="litTipoEspacio" runat="server" Mode="Encode" /></span>
                        <h2><asp:Literal ID="litNombreEspacio" runat="server" Mode="Encode" /></h2>
                    </div>
                    <asp:Label ID="lblEstado" runat="server" CssClass="espacio-int-estado" />
                </div>

                <p class="espacio-int-aviso">
                    <asp:Literal ID="litAvisoEstado" runat="server" Mode="Encode" />
                </p>

                <h3 class="espacio-int-subtitulo">Datos generales</h3>
                <dl class="internal-detail-grid">
                    <div><dt>Gestor</dt><dd><asp:Literal ID="litGestor" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Ubicación</dt><dd><asp:Literal ID="litUbicacion" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Dirección</dt><dd><asp:Literal ID="litDireccion" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Capacidad máxima</dt><dd><asp:Literal ID="litCapacidad" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Precio por hora</dt><dd><asp:Literal ID="litPrecio" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Tipo de piso</dt><dd><asp:Literal ID="litPiso" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Superficie</dt><dd><asp:Literal ID="litSuperficie" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Altura</dt><dd><asp:Literal ID="litAltura" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Franjas de disponibilidad</dt><dd><asp:Literal ID="litFranjas" runat="server" Mode="Encode" /></dd></div>
                    <div class="internal-detail-wide"><dt>Equipamiento</dt><dd><asp:Literal ID="litEquipamiento" runat="server" Mode="Encode" /></dd></div>
                    <div class="internal-detail-wide"><dt>Descripción</dt><dd class="espacio-int-texto"><asp:Literal ID="litDescripcion" runat="server" Mode="Encode" /></dd></div>
                    <div class="internal-detail-wide"><dt>Condiciones de uso</dt><dd class="espacio-int-texto"><asp:Literal ID="litCondiciones" runat="server" Mode="Encode" /></dd></div>
                    <div class="internal-detail-wide"><dt>Reglas de uso</dt><dd class="espacio-int-texto"><asp:Literal ID="litReglas" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <h3 class="espacio-int-subtitulo">Fechas</h3>
                <dl class="internal-detail-grid">
                    <div><dt>Alta</dt><dd><asp:Literal ID="litFechaAlta" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Publicación</dt><dd><asp:Literal ID="litFechaPublicacion" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Última modificación</dt><dd><asp:Literal ID="litFechaModificacion" runat="server" Mode="Encode" /></dd></div>
                    <div><dt>Baja</dt><dd><asp:Literal ID="litFechaBaja" runat="server" Mode="Encode" /></dd></div>
                </dl>

                <div class="form-actions">
                    <asp:HyperLink ID="lnkVolver" runat="server" CssClass="button button-secondary" Text="Volver a la búsqueda" />
                    <asp:HyperLink ID="lnkFichaPublica" runat="server" CssClass="button button-primary" Text="Ver la ficha pública" Visible="false" />
                </div>
            </asp:Panel>
        </div>
    </section>

    <style>
        .internal-admin-stack > * { min-width: 0; }
        .espacio-int-cabecera { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap; }
        .espacio-int-subtitulo { margin: 1.3rem 0 0.6rem; font-size: 1rem; }
        .espacio-int-aviso { margin: 0.4rem 0 0.2rem; padding: 10px 14px; border-radius: 10px; background: #fff8ee; border: 1px solid #f1d7b5; color: #6b3d0c; font-size: 0.92rem; }
        .espacio-int-estado { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 0.82rem; font-weight: 700; background: #eee; white-space: nowrap; }
        .espacio-int-estado-publicado { background: #e6f5eb; color: #236240; }
        .espacio-int-estado-pausado, .espacio-int-estado-borrador, .espacio-int-estado-sinpublicar { background: #fff4e5; color: #8a4b0f; }
        .espacio-int-estado-baja { background: #fdecec; color: #8f1d1d; }
        .espacio-int-texto { font-weight: 400 !important; white-space: pre-line; }
        .internal-detail-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px 20px; margin: 0; }
        .internal-detail-grid > div { min-width: 0; }
        .internal-detail-grid dt { color: var(--color-text-muted); font-size: 13px; }
        .internal-detail-grid dd { margin: 2px 0 0; font-weight: 600; color: var(--color-text); overflow-wrap: anywhere; }
        .internal-detail-wide { grid-column: 1 / -1; }
        @media (max-width: 720px) {
            .internal-detail-grid { grid-template-columns: 1fr; }
        }
    </style>
</asp:Content>
