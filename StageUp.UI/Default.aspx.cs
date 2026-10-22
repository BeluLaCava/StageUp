using System;
using System.Web.UI;

namespace StageUp.UI
{
    public partial class _Default : Page
    {


        protected void Page_Load(object sender, EventArgs e)
        {
            pnlAvisoBaja.Visible = string.Equals(Request.QueryString["cuenta"], "baja", StringComparison.OrdinalIgnoreCase);
        }
    }
}
