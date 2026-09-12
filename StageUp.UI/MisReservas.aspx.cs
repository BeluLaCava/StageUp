using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class MisReservas : Page
    {
        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();
        private readonly BLL_Calificacion _bllCalificacion = new BLL_Calificacion();

        private int? IdReservaCalificando
        {
            get { return ViewState["IdReservaCalificando"] as int?; }
            set { ViewState["IdReservaCalificando"] = value; }
        }

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
            int idReserva = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "CalificarEspacio")
            {
                IdReservaCalificando = idReserva;
                ddlPuntajeEspacio.SelectedIndex = 0;
                txtComentarioCalificacionEspacio.Text = string.Empty;
                pnlCalificarEspacio.Visible = true;
                return;
            }

            if (e.CommandName != "Cancelar")
            {
                return;
            }

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

            Reserva reserva = (Reserva)e.Item.DataItem;

            LinkButton lnkCancelar = (LinkButton)e.Item.FindControl("lnkCancelar");
            LinkButton lnkCalificarEspacio = (LinkButton)e.Item.FindControl("lnkCalificarEspacio");
            Panel pnlCalificacionRealizada = (Panel)e.Item.FindControl("pnlCalificacionEspacioRealizada");
            bool puedeCancelar = reserva.EstadoReserva == "Pendiente" || reserva.EstadoReserva == "Aceptada";
            lnkCancelar.Visible = puedeCancelar;
            if (puedeCancelar)
            {
                lnkCancelar.Attributes["onclick"] =
                    "return confirm('" + ObtenerMensajeConfirmacionCancelacion(reserva).Replace("'", "\\'") + "');";
            }

            bool finalizada = reserva.EstadoReserva == "Finalizada";
            lnkCalificarEspacio.Visible = finalizada && !reserva.CalificacionEspacioRealizada;
            pnlCalificacionRealizada.Visible = finalizada && reserva.CalificacionEspacioRealizada;

            Literal litComentarioResolucion = (Literal)e.Item.FindControl("litComentarioResolucion");
            if (litComentarioResolucion != null && !string.IsNullOrEmpty(reserva.ComentarioResolucion))
            {
                litComentarioResolucion.Text = "Respuesta del gestor: " + Server.HtmlEncode(reserva.ComentarioResolucion);
            }

            Literal litHorarioImporte = (Literal)e.Item.FindControl("litHorarioImporte");
            if (litHorarioImporte != null && reserva.MinutoDesde.HasValue && reserva.MinutoHasta.HasValue)
            {
                string horario = "Horario: " + FormatearHora(reserva.MinutoDesde.Value) + " a " + FormatearHora(reserva.MinutoHasta.Value);
                if (reserva.ImporteEstimado.HasValue)
                {
                    horario += " · Importe estimado: " +
                        reserva.ImporteEstimado.Value.ToString("0.##", CultureInfo.CurrentCulture) + " " + (reserva.Moneda ?? "ARS");
                }

                litHorarioImporte.Text = Server.HtmlEncode(horario);
            }
        }

        protected void lnkCerrarCalificacionEspacio_Click(object sender, EventArgs e)
        {
            CerrarCalificacion();
        }

        protected void btnEnviarCalificacionEspacio_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !IdReservaCalificando.HasValue)
            {
                return;
            }

            int puntaje;
            if (!int.TryParse(ddlPuntajeEspacio.SelectedValue, out puntaje))
            {
                MostrarMensaje("Elegí una calificación entre 1 y 5 estrellas.", true);
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<int> resultado = _bllCalificacion.CalificarEspacio(
                IdReservaCalificando.Value,
                idUsuarioExterno,
                puntaje,
                txtComentarioCalificacionEspacio.Text);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                CerrarCalificacion();
                CargarMisReservas();
            }
        }

        private void CerrarCalificacion()
        {
            IdReservaCalificando = null;
            pnlCalificarEspacio.Visible = false;
            ddlPuntajeEspacio.SelectedIndex = 0;
            txtComentarioCalificacionEspacio.Text = string.Empty;
        }

        private static string ObtenerMensajeConfirmacionCancelacion(Reserva reserva)
        {
            const string mensajeBase = "¿Seguro que querés cancelar esta solicitud de reserva?";
            if (reserva.EstadoReserva != "Aceptada" || !reserva.MinutoDesde.HasValue || !reserva.ImporteEstimado.HasValue)
            {
                return mensajeBase;
            }

            DateTime momentoReservado = reserva.FechaSolicitada.Date.AddMinutes(reserva.MinutoDesde.Value);
            double horasRestantes = (momentoReservado - DateTime.Now).TotalHours;
            if (horasRestantes >= 24)
            {
                return mensajeBase;
            }

            decimal comision = decimal.Round(reserva.ImporteEstimado.Value * 0.10m, 2);
            return "Esta reserva ya está aceptada y faltan menos de 24hs para el horario reservado. " +
                "Si la cancelás ahora se te va a aplicar una comisión de cancelación de " +
                comision.ToString("0.##", CultureInfo.CurrentCulture) + " " + (reserva.Moneda ?? "ARS") + ". ¿Querés continuar?";
        }

        private static string FormatearHora(int minutos)
        {
            return minutos == 1440
                ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
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
