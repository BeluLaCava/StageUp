using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class PanelAdministrador : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            litNombreAdministrador.Text = Server.HtmlEncode(GestorDeSesion.ObtenerNombreCompletoInternoActual());
        }
    }
}
