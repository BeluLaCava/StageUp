using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class GestionNovedades : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            if (!TieneAccesoAdministrativo())
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
            }
        }

        private static bool TieneAccesoAdministrativo()
        {
            return GestorDeSesion.TienePermisoInterno("GESTIONAR_ROLES") ||
                GestorDeSesion.TienePermisoInterno("GESTIONAR_IDIOMAS") ||
                GestorDeSesion.TienePermisoInterno("GESTIONAR_USUARIOS_INTERNOS");
        }
    }
}
