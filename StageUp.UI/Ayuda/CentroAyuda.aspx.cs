using System;
using System.Web.UI;
using StageUp.BLL;

namespace StageUp.UI.Ayuda
{
    public partial class CentroAyuda : Page
    {
        private readonly BLL_Faq _bllFaq = new BLL_Faq();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                rptFaq.DataSource = _bllFaq.ListarActivas();
                rptFaq.DataBind();
            }
        }
    }
}
