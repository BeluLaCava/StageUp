using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    /// <summary>
    /// Historial de reservas del usuario autenticado, sin importar su perfil: para un
    /// gestor son las reservas que hizo sobre espacios de otros, y para un solicitante
    /// (como Ana, que todavía no es gestora) es directamente su "Mis espacios" hasta que
    /// solicite y le aprueben la habilitación como gestora.
    /// </summary>
    public partial class MisReservas : Page
    {
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarMisReservas();
            }
        }

        protected void rptMisReservas_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Cancelar")
            {
                return;
            }

            int idReserva = Convert.ToInt32(e.CommandArgument);
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            ResultadoOperacion resultado = _bllReserva.Cancelar(idReserva, idUsuarioExterno);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarMisReservas();
        }

        protected void rptMisReservas_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var reserva = (Reserva)e.Item.DataItem;

            var lnkCancelar = (LinkButton)e.Item.FindControl("lnkCancelar");
            lnkCancelar.Visible = reserva.EstadoReserva == "Pendiente";

            var litComentarioResolucion = (Literal)e.Item.FindControl("litComentarioResolucion");
            if (litComentarioResolucion != null && !string.IsNullOrEmpty(reserva.ComentarioResolucion))
            {
                litComentarioResolucion.Text = "Respuesta del gestor: " + Server.HtmlEncode(reserva.ComentarioResolucion);
            }
        }

        private void CargarMisReservas()
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<Reserva> reservas = _bllReserva.ListarMisReservas(idUsuarioExterno);

            litSinReservas.Visible = reservas.Count == 0;
            rptMisReservas.DataSource = reservas;
            rptMisReservas.DataBind();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }
    }
}
