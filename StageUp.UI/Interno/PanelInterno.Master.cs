using System;
using System.Collections.Generic;
using System.Globalization;
using StageUp.BE.Menu;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{
    public partial class PanelInterno : MasterPageMultidioma
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();
        private int _ticketsAbiertos;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            InternalUserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoInternoActual();
            CargarMenu();
        }

        protected void lnkCerrarSesionInterna_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesionInterna();
            Response.Redirect("~/IniciarSesion.aspx");
        }

        protected string ObtenerIndicadorPendientes(object url)
        {
            string direccion = Convert.ToString(url);
            if (_ticketsAbiertos <= 0
                || direccion.IndexOf("GestionSoporte.aspx", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return string.Empty;
            }

            string cantidad = _ticketsAbiertos.ToString(CultureInfo.InvariantCulture);
            return " <span class=\"internal-nav-badge\" title=\"Tickets abiertos esperando respuesta\" "
                + "style=\"display:inline-block;min-width:20px;padding:1px 7px;margin-left:6px;border-radius:999px;"
                + "background:#7a0c20;color:#fff;font-size:0.75em;font-weight:bold;text-align:center;\">"
                + cantidad + "</span>";
        }

        private void CargarMenu()
        {
            int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            GrupoMenu raiz = _bllPermiso.ConstruirMenuParaRol(idRolInterno);
            List<ItemMenu> items = raiz.ObtenerItems();

            // Aviso al equipo interno de soporte (corrección de María sobre el
            // helpdesk): contador de tickets en estado Abierto al lado de
            // "Gestión de soporte", solo para quien tiene ese permiso.
            _ticketsAbiertos = GestorDeSesion.TienePermisoInterno("GESTIONAR_SOPORTE")
                ? new BLL_Ticket().ContarTicketsAbiertos()
                : 0;

            rptMenu.DataSource = items;
            rptMenu.DataBind();
        }
    }
}
