using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool autenticado = GestorDeSesion.EstaAutenticado();

            PublicActions.Visible = !autenticado;
            AuthenticatedTools.Visible = autenticado;
            AuthenticatedSidebar.Visible = autenticado;

            if (autenticado)
            {
                UserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoActual();
            }
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesion();
            Response.Redirect("~/Default.aspx");
        }
    }
}
