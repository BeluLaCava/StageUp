using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    public partial class AprobacionGestores : Page
    {
        private readonly BLL_UsuarioExterno _bllUsuarioExterno = new BLL_UsuarioExterno();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/Interno/IniciarSesionInterno.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("APROBAR_GESTORES"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarSolicitudes();
            }
        }

        protected void rptSolicitudes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idUsuarioExterno = Convert.ToInt32(e.CommandArgument);
            int idUsuarioInternoResponsable = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;

            ResultadoOperacion resultado = e.CommandName == "Aprobar"
                ? _bllUsuarioExterno.AprobarHabilitacionComoGestor(idUsuarioExterno, idUsuarioInternoResponsable)
                : _bllUsuarioExterno.RechazarHabilitacionComoGestor(idUsuarioExterno, idUsuarioInternoResponsable);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarSolicitudes();
        }

        private void CargarSolicitudes()
        {
            List<UsuarioExterno> solicitudes = _bllUsuarioExterno.ListarPendientesHabilitacionGestor();

            rptSolicitudes.DataSource = solicitudes;
            rptSolicitudes.DataBind();

            pnlSinSolicitudes.Visible = solicitudes.Count == 0;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
