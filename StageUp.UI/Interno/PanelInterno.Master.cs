using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class PanelInterno : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            InternalUserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoInternoActual();
        }

        protected void lnkCerrarSesionInterna_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesionInterna();
            Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
        }
    }
}
