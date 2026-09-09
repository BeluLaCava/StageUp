using System;
using System.Web.UI;
using StageUp.BE.Menu;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class PanelInterno : MasterPage
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            InternalUserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoInternoActual();
            CargarMenu();
        }

        protected void lnkCerrarSesionInterna_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesionInterna();
            Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
        }

        private void CargarMenu()
        {
            int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            GrupoMenu raiz = _bllPermiso.ConstruirMenuParaRol(idRolInterno);

            rptMenu.DataSource = raiz.ObtenerItems();
            rptMenu.DataBind();
        }
    }
}
