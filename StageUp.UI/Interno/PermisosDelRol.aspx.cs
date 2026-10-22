using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{



    public partial class PermisosDelRol : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_ROLES"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            int idRolInterno;
            Response.Redirect(int.TryParse(Request.QueryString["idRol"], out idRolInterno)
                ? "~/Interno/GestionRoles.aspx?editar=" + idRolInterno
                : "~/Interno/GestionRoles.aspx");
        }
    }
}
