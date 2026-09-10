using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    public partial class SolicitudesRecibidas : Page
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

            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            if (!IsPostBack)
            {
                CargarSolicitudesRecibidas();
            }
        }

        protected void rptSolicitudes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!EsGestorEspacios())
            {
                Response.Redirect("~/MisEspacios.aspx");
                return;
            }

            int idReserva = Convert.ToInt32(e.CommandArgument);
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado;

            switch (e.CommandName)
            {
                case "CalificarSolicitante":
                    IdReservaCalificando = idReserva;
                    ddlPuntajeSolicitante.SelectedIndex = 0;
                    txtComentarioCalificacionSolicitante.Text = string.Empty;
                    pnlCalificarSolicitante.Visible = true;
                    return;

                case "Aceptar":
                    resultado = _bllReserva.Aceptar(idReserva, idUsuarioGestor, null);
                    break;

                case "Rechazar":
                    resultado = _bllReserva.Rechazar(idReserva, idUsuarioGestor, null);
                    break;

                default:
                    return;
            }

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarSolicitudesRecibidas();
        }

        protected void rptSolicitudes_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var reserva = (Reserva)e.Item.DataItem;
            var lnkAceptar = (LinkButton)e.Item.FindControl("lnkAceptar");
            var lnkRechazar = (LinkButton)e.Item.FindControl("lnkRechazar");
            var pnlAcciones = (Panel)e.Item.FindControl("pnlAcciones");
            var pnlAccionesCalificacion = (Panel)e.Item.FindControl("pnlAccionesCalificacion");
            var lnkCalificarSolicitante = (LinkButton)e.Item.FindControl("lnkCalificarSolicitante");
            var pnlCalificacionRealizada = (Panel)e.Item.FindControl("pnlCalificacionSolicitanteRealizada");

            bool esPendiente = reserva.EstadoReserva == "Pendiente";
            lnkAceptar.Visible = esPendiente;
            lnkRechazar.Visible = esPendiente;
            pnlAcciones.Visible = esPendiente;

            bool finalizada = reserva.EstadoReserva == EstadoReserva.Finalizada.ToString();
            pnlAccionesCalificacion.Visible = finalizada;
            lnkCalificarSolicitante.Visible = finalizada && !reserva.CalificacionSolicitanteRealizada;
            pnlCalificacionRealizada.Visible = finalizada && reserva.CalificacionSolicitanteRealizada;
        }

        protected void lnkCerrarCalificacionSolicitante_Click(object sender, EventArgs e)
        {
            CerrarCalificacion();
        }

        protected void btnEnviarCalificacionSolicitante_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid || !IdReservaCalificando.HasValue)
            {
                return;
            }

            int puntaje;
            if (!int.TryParse(ddlPuntajeSolicitante.SelectedValue, out puntaje))
            {
                MostrarMensaje("Elegí una calificación entre 1 y 5 estrellas.", true);
                return;
            }

            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<int> resultado = _bllCalificacion.CalificarSolicitante(
                IdReservaCalificando.Value,
                idUsuarioGestor,
                puntaje,
                txtComentarioCalificacionSolicitante.Text);

            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            if (resultado.Exitoso)
            {
                CerrarCalificacion();
                CargarSolicitudesRecibidas();
            }
        }

        private void CerrarCalificacion()
        {
            IdReservaCalificando = null;
            pnlCalificarSolicitante.Visible = false;
            ddlPuntajeSolicitante.SelectedIndex = 0;
            txtComentarioCalificacionSolicitante.Text = string.Empty;
        }

        private void CargarSolicitudesRecibidas()
        {
            int idUsuarioGestor = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<Reserva> solicitudes = _bllReserva.ListarSolicitudesRecibidas(idUsuarioGestor);

            int cantidadPendientes = 0;
            foreach (Reserva solicitud in solicitudes)
            {
                if (solicitud.EstadoReserva == EstadoReserva.Pendiente.ToString())
                {
                    cantidadPendientes++;
                }
            }

            litCantidadPendientes.Text = cantidadPendientes.ToString(CultureInfo.InvariantCulture);
            litCantidadResueltas.Text = (solicitudes.Count - cantidadPendientes).ToString(CultureInfo.InvariantCulture);
            litCantidadTotal.Text = solicitudes.Count.ToString(CultureInfo.InvariantCulture);
            pnlSinSolicitudes.Visible = solicitudes.Count == 0;
            rptSolicitudes.DataSource = solicitudes;
            rptSolicitudes.DataBind();
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = HttpUtility.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        protected string ObtenerClaseSolicitud(Reserva reserva)
        {
            if (reserva == null)
            {
                return "request-card";
            }

            switch (reserva.EstadoReserva)
            {
                case "Pendiente":
                    return "request-card request-card-pending";
                case "Aceptada":
                    return "request-card request-card-accepted";
                case "Finalizada":
                    return "request-card request-card-finished";
                case "Rechazada":
                case "Cancelada":
                    return "request-card request-card-rejected";
                default:
                    return "request-card";
            }
        }

        protected string ObtenerClaseEstado(string estado)
        {
            switch (estado)
            {
                case "Pendiente":
                    return "request-status request-status-pending";
                case "Aceptada":
                    return "request-status request-status-accepted";
                case "Finalizada":
                    return "request-status request-status-finished";
                case "Rechazada":
                case "Cancelada":
                    return "request-status request-status-rejected";
                default:
                    return "request-status";
            }
        }

        protected string ObtenerIniciales(Reserva reserva)
        {
            string nombre = reserva == null ? null : reserva.NombreSolicitante;
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return "SU";
            }

            string[] partes = nombre.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var iniciales = new StringBuilder();
            iniciales.Append(char.ToUpper(partes[0][0], CultureInfo.CurrentCulture));
            if (partes.Length > 1)
            {
                iniciales.Append(char.ToUpper(partes[partes.Length - 1][0], CultureInfo.CurrentCulture));
            }

            return iniciales.ToString();
        }

        protected string ObtenerActividadSolicitante(Reserva reserva)
        {
            if (reserva == null)
            {
                return "Todavía no hay actividad para mostrar.";
            }

            var partes = new List<string>();
            if (reserva.SolicitanteDesde.HasValue)
            {
                partes.Add("Miembro desde " + reserva.SolicitanteDesde.Value.ToString("Y", CultureInfo.CurrentCulture));
            }

            int cantidad = reserva.CantidadReservasAceptadasSolicitante;
            if (cantidad == 0)
            {
                partes.Add("Sin reservas aceptadas anteriores");
            }
            else if (cantidad == 1)
            {
                partes.Add("1 reserva aceptada anteriormente");
            }
            else
            {
                partes.Add(cantidad.ToString(CultureInfo.InvariantCulture) + " reservas aceptadas anteriormente");
            }

            return string.Join(" · ", partes);
        }

        protected string ObtenerEstrellasSolicitante(Reserva reserva)
        {
            if (reserva == null || reserva.CantidadCalificacionesSolicitante == 0)
            {
                return "☆☆☆☆☆";
            }

            int llenas = Math.Max(0, Math.Min(5, (int)Math.Round(reserva.PromedioCalificacionSolicitante)));
            return new string('★', llenas) + new string('☆', 5 - llenas);
        }

        protected string ObtenerResumenReputacionSolicitante(Reserva reserva)
        {
            if (reserva == null || reserva.CantidadCalificacionesSolicitante == 0)
            {
                return "Sin calificaciones todavía";
            }

            int cantidad = reserva.CantidadCalificacionesSolicitante;
            return reserva.PromedioCalificacionSolicitante.ToString("0.0", CultureInfo.CurrentCulture) + " de 5 · " +
                cantidad + (cantidad == 1 ? " calificación" : " calificaciones");
        }

        protected string ObtenerEstrellas(int puntaje)
        {
            int valor = Math.Max(0, Math.Min(5, puntaje));
            return new string('★', valor) + new string('☆', 5 - valor);
        }

        protected string ObtenerHorarioTexto(Reserva reserva)
        {
            if (reserva == null || !reserva.MinutoDesde.HasValue || !reserva.MinutoHasta.HasValue)
            {
                return string.Empty;
            }

            string horario = FormatearHora(reserva.MinutoDesde.Value) + " a " + FormatearHora(reserva.MinutoHasta.Value);
            if (!reserva.ImporteEstimado.HasValue)
            {
                return horario;
            }

            return horario + " · Importe estimado: " +
                reserva.ImporteEstimado.Value.ToString("0.##", CultureInfo.CurrentCulture) + " " + (reserva.Moneda ?? "ARS");
        }

        private static string FormatearHora(int minutos)
        {
            return minutos == 1440
                ? "24:00"
                : (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private bool EsGestorEspacios()
        {
            return GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
        }
    }
}
