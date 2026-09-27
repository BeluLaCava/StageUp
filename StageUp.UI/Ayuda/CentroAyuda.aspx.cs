using System;
using System.Web.UI;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Ayuda
{
    public partial class CentroAyuda : Page
    {
        private readonly BLL_Faq _bllFaq = new BLL_Faq();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            rptFaq.DataSource = _bllFaq.ListarActivas();
            rptFaq.DataBind();

            // Ítem 6C: ya existe el módulo real de soporte (Soporte.aspx). Un
            // usuario ya logueado va directo ahí en vez de a IniciarSesion.aspx.
            if (GestorDeSesion.EstaAutenticado())
            {
                lnkContactarSoporte.NavigateUrl = "~/Soporte.aspx";
                lnkContactarSoporte.Text = "Ir a soporte";
            }
        }
    }
}
