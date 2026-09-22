using System;
using System.Collections.Generic;
using System.Web.UI;
using StageUp.BE.Menu;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class PanelAdministrador : Page
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            litNombreAdministrador.Text = Server.HtmlEncode(GestorDeSesion.ObtenerNombreCompletoInternoActual());

            if (!IsPostBack)
            {
                int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
                GrupoMenu menu = _bllPermiso.ConstruirMenuParaRol(idRolInterno);
                List<ItemMenu> accesos = menu.ObtenerItems();
                AgregarAccesoNewsletterSiFalta(accesos);
                AgregarAccesoFaqSiFalta(accesos);

                pnlSinAccesos.Visible = accesos.Count == 0;
                rptAccesos.DataSource = accesos;
                rptAccesos.DataBind();
            }
        }

        private static void AgregarAccesoNewsletterSiFalta(List<ItemMenu> accesos)
        {
            if (!DebeMostrarPrototipoNewsletter())
            {
                return;
            }

            foreach (ItemMenu acceso in accesos)
            {
                if (string.Equals(acceso.Url, "~/Interno/GestionNovedades.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            accesos.Add(new ItemMenu(
                "Novedades y newsletter",
                "~/Interno/GestionNovedades.aspx",
                "Crear noticias públicas y preparar envíos por correo a usuarios de StageUp."));
        }

        private static void AgregarAccesoFaqSiFalta(List<ItemMenu> accesos)
        {
            if (!DebeMostrarPrototipoAdministrativo())
            {
                return;
            }

            foreach (ItemMenu acceso in accesos)
            {
                if (string.Equals(acceso.Url, "~/Interno/GestionFaq.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            accesos.Add(new ItemMenu(
                "Preguntas frecuentes",
                "~/Interno/GestionFaq.aspx",
                "Crear y mantener respuestas visibles en el centro de ayuda."));
        }

        private static bool DebeMostrarPrototipoNewsletter()
        {
            return DebeMostrarPrototipoAdministrativo();
        }

        private static bool DebeMostrarPrototipoAdministrativo()
        {
            return GestorDeSesion.TienePermisoInterno("GESTIONAR_ROLES") ||
                GestorDeSesion.TienePermisoInterno("GESTIONAR_IDIOMAS") ||
                GestorDeSesion.TienePermisoInterno("GESTIONAR_USUARIOS_INTERNOS");
        }
    }
}
