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

            bool esPendiente = reserva.EstadoReserva == "Pendiente";
            lnkAceptar.Visible = esPendiente;
            lnkRechazar.Visible = esPendiente;
            pnlAcciones.Visible = esPendiente;
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
                partes.Add("Miembro desde " + reserva.SolicitanteDesde.Value.ToString("MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-AR")));
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

        private bool EsGestorEspacios()
        {
            return GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
        }
    }
}
