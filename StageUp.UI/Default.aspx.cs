using System;
using System.Web.UI;

namespace StageUp.UI
{
    public partial class _Default : Page
    {
        // CU-001-003 A8 (pasos 11 y 13): después de la baja lógica, Mi perfil
        // cierra la sesión y redirige acá con ?cuenta=baja.
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlAvisoBaja.Visible = string.Equals(Request.QueryString["cuenta"], "baja", StringComparison.OrdinalIgnoreCase);
        }
    }
}
