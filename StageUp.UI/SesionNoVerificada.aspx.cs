using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // Aviso cuando la base se restauró y no se pudo comprobar la cuenta de la
    // sesión (Global.asax suspende la sesión en ese pedido y trae acá a las
    // páginas). "Volver a intentar" vuelve a la página pedida, que hace una
    // verificación nueva.
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
