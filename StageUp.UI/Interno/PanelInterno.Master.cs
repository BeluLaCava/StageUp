using System;
using System.Collections.Generic;
using StageUp.BE.Menu;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI.Interno
{
    public partial class PanelInterno : MasterPageMultidioma
    {
        private readonly BLL_PermisoInterno _bllPermiso = new BLL_PermisoInterno();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            InternalUserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoInternoActual();
            CargarMenu();
        }

        protected void lnkCerrarSesionInterna_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesionInterna();
            Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
        }

        private void CargarMenu()
        {
            int idRolInterno = GestorDeSesion.ObtenerIdRolInternoActual().Value;
            GrupoMenu raiz = _bllPermiso.ConstruirMenuParaRol(idRolInterno);
            List<ItemMenu> items = raiz.ObtenerItems();

            AgregarAccesoNewsletterSiFalta(items);

            rptMenu.DataSource = items;
            rptMenu.DataBind();
        }

        private static void AgregarAccesoNewsletterSiFalta(List<ItemMenu> items)
        {
            if (!DebeMostrarPrototipoNewsletter())
            {
                return;
            }

            foreach (ItemMenu item in items)
            {
                if (string.Equals(item.Url, "~/Interno/GestionNovedades.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            items.Add(new ItemMenu(
                "Novedades",
                "~/Interno/GestionNovedades.aspx",
                "Administrar novedades públicas y envíos de newsletter."));
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
