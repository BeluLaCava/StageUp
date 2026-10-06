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
                CargarFiltrosHistorial();
                CargarMisReservas();
            }
        }

        protected void ddlFiltroHistorial_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarMisReservas();
        }

        private void CargarFiltrosHistorial()
        {
            ddlFiltroHistorial.Items.Clear();
            ddlFiltroHistorial.Items.Add(new ListItem("Todas", BLL_Reserva.FiltroTodas));
            ddlFiltroHistorial.Items.Add(new ListItem("Pendientes de revisión", BLL_Reserva.FiltroPendientes));
            ddlFiltroHistorial.Items.Add(new ListItem("Pendientes de pago", BLL_Reserva.FiltroPendientesDePago));
            ddlFiltroHistorial.Items.Add(new ListItem("Confirmadas / próximas", BLL_Reserva.FiltroProximas));
            ddlFiltroHistorial.Items.Add(new ListItem("Finalizadas", BLL_Reserva.FiltroFinalizadas));
            ddlFiltroHistorial.Items.Add(new ListItem("Pendientes de calificar", BLL_Reserva.FiltroSinCalificar));
            ddlFiltroHistorial.Items.Add(new ListItem("Rechazadas", BLL_Reserva.FiltroRechazadas));
            ddlFiltroHistorial.Items.Add(new ListItem("Canceladas", BLL_Reserva.FiltroCanceladas));
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
            lnkCancelar.Text = reserva.EstadoReserva == "Pendiente" ? "Cancelar solicitud" : "Cancelar reserva";
            if (puedeCancelar)
            {
                lnkCancelar.Attributes["onclick"] =
                    "return confirm('" + _bllReserva.ObtenerAvisoCancelacion(reserva).Replace("'", "\\'") + "');";
            }

            // Módulo de pagos: estado del pago y botón para pagar.
            HyperLink lnkPagar = (HyperLink)e.Item.FindControl("lnkPagar");
            bool esperaPago = BLL_Pago.EsperaPago(reserva);
            lnkPagar.Visible = esperaPago;
            lnkPagar.NavigateUrl = "~/Pagar.aspx?reserva=" + reserva.IdReserva.ToString(CultureInfo.InvariantCulture);

            Panel pnlEstadoPago = (Panel)e.Item.FindControl("pnlEstadoPago");
            Literal litEstadoPago = (Literal)e.Item.FindControl("litEstadoPago");
            string estadoPago = BLL_Pago.DescribirEstadoPago(reserva, false);
            pnlEstadoPago.Visible = !string.IsNullOrEmpty(estadoPago);
            pnlEstadoPago.CssClass = esperaPago ? "reserva-pago reserva-pago-pendiente" : "reserva-pago";
            litEstadoPago.Text = Server.HtmlEncode(estadoPago ?? string.Empty);

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

        private static string FormatearHora(int minutos)
        {
            return minutos == 1440
                ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        protected string ObtenerIconoSeguimiento(string titulo)
        {
            if (string.IsNullOrWhiteSpace(titulo))
            {
                return "•";
            }

            string normalizado = titulo.ToLowerInvariant();
            if (normalizado.Contains("solicitud"))
            {
                return "✉";
            }

            if (normalizado.Contains("pago"))
            {
                return "$";
            }

            if (normalizado.Contains("día") || normalizado.Contains("reserva finalizada"))
            {
                return "◷";
            }

            if (normalizado.Contains("calificación"))
            {
                return "★";
            }

            if (normalizado.Contains("rechazada") || normalizado.Contains("cancelada") || normalizado.Contains("vencido"))
            {
                return "!";
            }

            return "✓";
        }

        private void CargarMisReservas()
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            string filtro = ddlFiltroHistorial.SelectedValue;
            List<Reserva> reservas = _bllReserva.ListarMisReservas(idUsuarioExterno, filtro);

            litSinReservas.Text = string.IsNullOrEmpty(filtro) || filtro == BLL_Reserva.FiltroTodas
                ? "Todavía no solicitaste ninguna reserva. Podés explorar espacios publicados y solicitar una desde su detalle."
                : "No tenés reservas con ese estado.";
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
