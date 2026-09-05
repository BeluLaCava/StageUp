using System;
using System.Web.UI;
using StageUp.Seguridad;

namespace StageUp.UI
{
    /// <summary>
    /// Code-behind del layout general. Muestra los accesos de "Iniciar
    /// sesión"/"Registrarse" cuando no hay sesión iniciada, o el resumen
    /// del usuario + "Cerrar sesión" + el sidebar de "Mi StageUp" cuando
    /// sí la hay. El resto de los accesos del sidebar (Mis reservas, Mi
    /// perfil, Mis actividades, Panel interno) siguen deshabilitados
    /// porque esas funcionalidades todavía no existen.
    /// </summary>
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
