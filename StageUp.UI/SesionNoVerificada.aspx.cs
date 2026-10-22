using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI
{




    public partial class SesionNoVerificada : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();

            string volver = Request.QueryString["volver"];
            if (EsDireccionLocal(volver))
            {
                lnkReintentar.NavigateUrl = volver;
            }
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesionInterna();
            GestorDeSesion.CerrarSesion();
            Response.Redirect("~/Default.aspx");
        }

        private static bool EsDireccionLocal(string url)
        {
            return !string.IsNullOrEmpty(url) && url.Length <= 2000 && url[0] == '/' &&
                (url.Length == 1 || (url[1] != '/' && url[1] != '\\')) &&
                url.IndexOf("SesionNoVerificada.aspx", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
