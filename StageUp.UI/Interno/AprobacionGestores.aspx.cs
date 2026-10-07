using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-001-007: revisión de las solicitudes de habilitación como gestor. Se
    // ven todos los datos de la solicitud; para rechazar hay que escribir el
    // motivo, que el usuario ve en "Ofrecer espacio".
    public partial class AprobacionGestores : Page
    {
        private readonly BLL_SolicitudHabilitacionGestor _bllSolicitud = new BLL_SolicitudHabilitacionGestor();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!GestorDeSesion.TienePermisoInterno("APROBAR_GESTORES"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return;
            }

            if (!IsPostBack)
            {
                ddlEstado.Items.Add(new ListItem("Pendientes de revisión", SolicitudHabilitacionGestor.EstadoPendienteRevision));
                ddlEstado.Items.Add(new ListItem("Aprobadas", SolicitudHabilitacionGestor.EstadoAprobada));
                ddlEstado.Items.Add(new ListItem("Rechazadas", SolicitudHabilitacionGestor.EstadoRechazada));
                ddlEstado.Items.Add(new ListItem("Todas", string.Empty));
                CargarSolicitudes();
            }
        }

        protected void ddlEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlMensaje.Visible = false;
            CargarSolicitudes();
        }

        protected void rptSolicitudes_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            SolicitudHabilitacionGestor solicitud = (SolicitudHabilitacionGestor)e.Item.DataItem;
            bool pendiente = solicitud.Estado == SolicitudHabilitacionGestor.EstadoPendienteRevision;
            e.Item.FindControl("pnlResolucion").Visible = pendiente;
            e.Item.FindControl("pnlResuelta").Visible = !pendiente;
        }

        protected void rptSolicitudes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int idSolicitud = Convert.ToInt32(e.CommandArgument);
            int idUsuarioInterno = GestorDeSesion.ObtenerIdUsuarioInternoActual().Value;
            TextBox txtMotivo = (TextBox)e.Item.FindControl("txtMotivo");

            ResultadoOperacion resultado = e.CommandName == "Aprobar"
                ? _bllSolicitud.Aprobar(idSolicitud, idUsuarioInterno)
                : _bllSolicitud.Rechazar(idSolicitud, txtMotivo == null ? null : txtMotivo.Text, idUsuarioInterno);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                CargarSolicitudes();
            }
        }

        private void CargarSolicitudes()
        {
            List<SolicitudHabilitacionGestor> solicitudes = _bllSolicitud.Listar(ddlEstado.SelectedValue);
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
