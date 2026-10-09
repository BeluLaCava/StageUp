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
    // Módulo de soporte/helpdesk (ítems 6C y 16 de la segunda entrega), del
    // lado del usuario externo. Dos vistas en una sola página, según query
    // string, mismo patrón que Encuestas.aspx:
    //   Soporte.aspx              -> nueva consulta + mis consultas
    //   Soporte.aspx?reserva={id} -> igual, con esa reserva ya elegida
    //                                (botón "Contactar soporte" de Mis reservas)
    //   Soporte.aspx?ver={id}     -> hilo de mensajes de una consulta puntual
    public partial class Soporte : Page
    {
        private readonly BLL_Ticket _bllTicket = new BLL_Ticket();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GestorDeSesion.EstaAutenticado())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            CargarCategorias();
            CargarReservas();

            int idVer;
            if (int.TryParse(Request.QueryString["ver"], out idVer))
            {
                CargarDetalle(idVer);
            }
            else
            {
                PreseleccionarReservaDesdeQueryString();
                CargarDashboard();
            }
        }

        protected void btnCrearTicket_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            int idReserva;
            int? idReservaAsociada = int.TryParse(ddlReservaAsociada.SelectedValue, out idReserva)
                ? idReserva
                : (int?)null;

            ResultadoOperacion<int> resultado = _bllTicket.CrearTicket(
                idUsuarioExterno, ddlCategoria.SelectedValue, txtAsunto.Text, txtMensajeInicial.Text, idReservaAsociada);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarDashboard();
                return;
            }

            Response.Redirect("~/Soporte.aspx?ver=" + resultado.Valor);
        }

        protected void btnEnviarMensaje_Click(object sender, EventArgs e)
        {
            int idTicket;
            if (!int.TryParse(Request.QueryString["ver"], out idTicket))
            {
                Response.Redirect("~/Soporte.aspx");
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado = _bllTicket.AgregarMensajeUsuario(idTicket, idUsuarioExterno, txtNuevoMensaje.Text);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
            }

            CargarDetalle(idTicket);
        }

        private void CargarCategorias()
        {
            ddlCategoria.DataSource = BLL_Ticket.Categorias;
            ddlCategoria.DataBind();
        }

        private void CargarReservas()
        {
            ddlReservaAsociada.Items.Clear();
            ddlReservaAsociada.Items.Add(new ListItem("Ninguna en particular", string.Empty));

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<Reserva> reservas = _bllTicket.ListarReservasParaAsociar(idUsuarioExterno);
            foreach (Reserva reserva in reservas)
            {
                ddlReservaAsociada.Items.Add(new ListItem(
                    DescribirReserva(reserva.NombreEspacio, reserva.FechaSolicitada, reserva.MinutoDesde, reserva.MinutoHasta, reserva.EstadoReserva),
                    reserva.IdReserva.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void PreseleccionarReservaDesdeQueryString()
        {
            string idReserva = Request.QueryString["reserva"];
            if (string.IsNullOrEmpty(idReserva) || ddlReservaAsociada.Items.FindByValue(idReserva) == null)
            {
                // Solo se preselecciona si es una reserva propia (la lista ya
                // viene filtrada por usuario); cualquier otro id se ignora.
                return;
            }

            ddlReservaAsociada.SelectedValue = idReserva;
            if (ddlCategoria.Items.FindByValue("Reserva") != null)
            {
                ddlCategoria.SelectedValue = "Reserva";
            }
        }

        private void CargarDashboard()
        {
            MostrarSolo(pnlDashboard);

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            List<Ticket> tickets = _bllTicket.ListarTicketsUsuario(idUsuarioExterno);
            pnlSinTickets.Visible = tickets.Count == 0;
            rptMisTickets.DataSource = tickets;
            rptMisTickets.DataBind();
        }

        private void CargarDetalle(int idTicket)
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion<TicketCompleto> resultado = _bllTicket.ObtenerDetalleParaUsuario(idTicket, idUsuarioExterno);

            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarDashboard();
                return;
            }

            MostrarSolo(pnlDetalle);

            Ticket ticket = resultado.Valor.Ticket;
            litAsuntoDetalle.Text = Server.HtmlEncode(ticket.Asunto);
            litCategoriaDetalle.Text = Server.HtmlEncode(ticket.Categoria);
            pnlReservaDetalle.Visible = ticket.TieneReservaAsociada;
            litReservaDetalle.Text = ticket.TieneReservaAsociada ? Server.HtmlEncode(DescribirReserva(ticket)) : string.Empty;
            litEstadoDetalle.Text = ObtenerTextoEstado(ticket.Estado);
            pnlEstadoDetalle.CssClass = "ticket-badge " + ObtenerClaseEstado(ticket.Estado);

            rptMensajes.DataSource = resultado.Valor.Mensajes;
            rptMensajes.DataBind();

            bool cerrado = ticket.Estado == "Cerrado";
            pnlResponder.Visible = !cerrado;
            pnlCerrado.Visible = cerrado;

            txtNuevoMensaje.Text = string.Empty;
        }

        private void MostrarSolo(Panel panelVisible)
        {
            pnlDashboard.Visible = ReferenceEquals(panelVisible, pnlDashboard);
            pnlDetalle.Visible = ReferenceEquals(panelVisible, pnlDetalle);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.CssClass = esError
                ? "form-message form-message-error"
                : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        protected static string DescribirReserva(Ticket ticket)
        {
            return DescribirReserva(ticket.NombreEspacioReserva, ticket.FechaReserva, ticket.MinutoDesdeReserva,
                ticket.MinutoHastaReserva, ticket.EstadoReserva);
        }

        private static string DescribirReserva(string nombreEspacio, DateTime? fecha, int? minutoDesde, int? minutoHasta, string estado)
        {
            string texto = string.IsNullOrEmpty(nombreEspacio) ? "Reserva" : nombreEspacio;
            if (fecha.HasValue)
            {
                texto += " · " + fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            if (minutoDesde.HasValue && minutoHasta.HasValue)
            {
                texto += " " + FormatearHora(minutoDesde.Value) + " a " + FormatearHora(minutoHasta.Value);
            }

            if (!string.IsNullOrEmpty(estado))
            {
                texto += " (" + estado + ")";
            }

            return texto;
        }

        private static string FormatearHora(int minutos)
        {
            return (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        protected static string ObtenerTextoEstado(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case "Abierto": return "Abierto";
                case "EnRevision": return "En revisión";
                case "Respondido": return "Respondido";
                case "Cerrado": return "Cerrado";
                default: return Convert.ToString(estado);
            }
        }

        protected static string ObtenerClaseEstado(object estado)
        {
            switch (Convert.ToString(estado))
            {
                case "Abierto": return "ticket-badge-abierto";
                case "EnRevision": return "ticket-badge-enrevision";
                case "Respondido": return "ticket-badge-respondido";
                case "Cerrado": return "ticket-badge-cerrado";
                default: return string.Empty;
            }
        }
    }
}
