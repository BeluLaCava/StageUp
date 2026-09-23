using System;
using System.Collections.Generic;
using System.Web.UI;
using StageUp.BE.Menu;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class PanelAdministrador : Page
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            litNombreAdministrador.Text = Server.HtmlEncode(GestorDeSesion.ObtenerNombreCompletoInternoActual());

            if (!IsPostBack)
            {
                int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
                GrupoMenu menu = _bllPermiso.ConstruirMenuParaRol(idRolInterno);
                List<ItemMenu> accesos = menu.ObtenerItems();

                pnlSinAccesos.Visible = accesos.Count == 0;
                rptAccesos.DataSource = accesos;
                rptAccesos.DataBind();
            }
        }
    }
}
