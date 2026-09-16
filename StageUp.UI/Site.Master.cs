using System;
using StageUp.BE.Enumerados;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    public partial class SiteMaster : MasterPageMultidioma
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool autenticado = GestorDeSesion.EstaAutenticado();

            PublicActions.Visible = !autenticado;
            AuthenticatedTools.Visible = autenticado;
            AuthenticatedSidebar.Visible = autenticado;

            if (autenticado)
            {
                UserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoActual();

                // "Mis actividades" es exclusivo de gestores de espacios: a diferencia
                // de "Mis espacios" (que se muestra siempre y es la propia pantalla la
                // que le explica al solicitante que todavía no es gestor), acá el pedido
                // puntual fue que el botón ni aparezca para el perfil ExternoSolicitante.
                bool esGestorEspacios = GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
                MisActividadesLink.Visible = esGestorEspacios;
                NotificationActivitiesLink.Visible = esGestorEspacios;
            }
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesion();
            Response.Redirect("~/Default.aspx");
        }

    }
}
