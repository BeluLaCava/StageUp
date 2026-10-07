using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // Panel interno del módulo de soporte/helpdesk (ítems 6C y 16 de la
    // segunda entrega). Listado con filtros + detalle con hilo de mensajes,
    // mismo patrón de query string que GestionEncuestas.aspx:
    //   GestionSoporte.aspx          -> listado de tickets, con filtros
    //   GestionSoporte.aspx?ver={id} -> detalle: hilo, responder, estado
    public partial class GestionSoporte : Page
    {
        private readonly BLL_Ticket _bllTicket = new BLL_Ticket();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (IsPostBack)
            {
                return;
            }

            CargarFiltros();

            int idVer;
            if (int.TryParse(Request.QueryString["ver"], out idVer))
            {
                CargarDetalle(idVer);
            }
            else
            {
                CargarListado();
            }
        }

        protected void btnFiltrar_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            CargarListado();
        }

        protected void btnTomarTicket_Click(object sender, EventArgs e)
        {
            EjecutarAccionSobreTicket(idTicket =>
                _bllTicket.Asignarme(idTicket, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value));
        }

        protected void btnCambiarEstado_Click(object sender, EventArgs e)
        {
            EjecutarAccionSobreTicket(idTicket =>
                _bllTicket.CambiarEstado(idTicket, ddlNuevoEstado.SelectedValue, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value));
        }

        protected void btnResponder_Click(object sender, EventArgs e)
        {
            EjecutarAccionSobreTicket(idTicket =>
                _bllTicket.AgregarMensajeInterno(idTicket, GestorDeSesion.ObtenerIdUsuarioInternoActual().Value, txtRespuestaInterna.Text));
        }

        private void EjecutarAccionSobreTicket(Func<int, ResultadoOperacion> accion)
        {
            if (!TieneAcceso())
            {
                return;
            }

            int idTicket;
            if (!int.TryParse(Request.QueryString["ver"], out idTicket))
            {
                Response.Redirect("~/Interno/GestionSoporte.aspx");
                return;
            }

            ResultadoOperacion resultado = accion(idTicket);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarDetalle(idTicket);
        }

        private void CargarFiltros()
        {
            ddlFiltroEstado.Items.Clear();
            ddlFiltroEstado.Items.Add(new ListItem("Todos", string.Empty));
            ddlFiltroEstado.Items.Add(new ListItem("Abierto", "Abierto"));
            ddlFiltroEstado.Items.Add(new ListItem("En revisión", "EnRevision"));
            ddlFiltroEstado.Items.Add(new ListItem("Respondido", "Respondido"));
            ddlFiltroEstado.Items.Add(new ListItem("Cerrado", "Cerrado"));

            ddlFiltroCategoria.Items.Clear();
            ddlFiltroCategoria.Items.Add(new ListItem("Todas", string.Empty));
            foreach (string categoria in BLL_Ticket.Categorias)
            {
                ddlFiltroCategoria.Items.Add(new ListItem(categoria, categoria));
            }

            ddlNuevoEstado.Items.Clear();
            ddlNuevoEstado.Items.Add(new ListItem("Abierto", "Abierto"));
            ddlNuevoEstado.Items.Add(new ListItem("En revisión", "EnRevision"));
            ddlNuevoEstado.Items.Add(new ListItem("Respondido", "Respondido"));
            ddlNuevoEstado.Items.Add(new ListItem("Cerrado", "Cerrado"));
        }

        private void CargarListado()
        {
            MostrarSolo(pnlListado);

            var tickets = _bllTicket.ListarTicketsInternos(ddlFiltroEstado.SelectedValue, ddlFiltroCategoria.SelectedValue);
            pnlSinTickets.Visible = tickets.Count == 0;
            rptTickets.DataSource = tickets;
            rptTickets.DataBind();
        }

        private void CargarDetalle(int idTicket)
        {
            ResultadoOperacion<TicketCompleto> resultado = _bllTicket.ObtenerDetalleParaInterno(idTicket);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarListado();
                return;
            }

            MostrarSolo(pnlDetalle);

            Ticket ticket = resultado.Valor.Ticket;
            litAsuntoDetalle.Text = Server.HtmlEncode(ticket.Asunto);
            litUsuarioDetalle.Text = Server.HtmlEncode(ticket.NombreUsuarioExterno + " (" + ticket.CorreoUsuarioExterno + ")");
            litCategoriaDetalle.Text = Server.HtmlEncode(ticket.Categoria);
            litEstadoDetalle.Text = ObtenerTextoEstado(ticket.Estado);
            pnlEstadoDetalle.CssClass = "ticket-badge " + ObtenerClaseEstado(ticket.Estado);

            // Contexto de la reserva asociada, para responder sabiendo de qué
            // servicio contratado se trata.
            pnlReservaAsociada.Visible = ticket.TieneReservaAsociada;
            if (ticket.TieneReservaAsociada)
            {
                litIdReserva.Text = ticket.IdReservaAsociada.Value.ToString(CultureInfo.InvariantCulture);
                litEspacioReserva.Text = Server.HtmlEncode(ticket.NombreEspacioReserva ?? "-");
                litFechaReserva.Text = Server.HtmlEncode(DescribirFechaHorario(ticket));
                litEstadoReserva.Text = Server.HtmlEncode(ticket.EstadoReserva ?? "-");
            }

            rptMensajes.DataSource = resultado.Valor.Mensajes;
            rptMensajes.DataBind();

            bool cerrado = ticket.Estado == "Cerrado";
            pnlResponder.Visible = !cerrado;
            pnlCerrado.Visible = cerrado;
            btnTomarTicket.Visible = !ticket.IdUsuarioInternoAsignado.HasValue;

            if (ddlNuevoEstado.Items.FindByValue(ticket.Estado) != null)
            {
                ddlNuevoEstado.SelectedValue = ticket.Estado;
            }

            txtRespuestaInterna.Text = string.Empty;
        }

        private void MostrarSolo(Panel panelVisible)
        {
            pnlListado.Visible = ReferenceEquals(panelVisible, pnlListado);
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

        protected static string DescribirReserva(Ticket ticket)
        {
            string texto = string.IsNullOrEmpty(ticket.NombreEspacioReserva) ? "Reserva" : ticket.NombreEspacioReserva;
            texto += " · " + DescribirFechaHorario(ticket);
            if (!string.IsNullOrEmpty(ticket.EstadoReserva))
            {
                texto += " (" + ticket.EstadoReserva + ")";
            }
            return texto;
        }

        private static string DescribirFechaHorario(Ticket ticket)
        {
            if (!ticket.FechaReserva.HasValue)
            {
                return "-";
            }

            string texto = ticket.FechaReserva.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (ticket.MinutoDesdeReserva.HasValue && ticket.MinutoHastaReserva.HasValue)
            {
                texto += " " + FormatearHora(ticket.MinutoDesdeReserva.Value) + " a " + FormatearHora(ticket.MinutoHastaReserva.Value);
            }
            return texto;
        }

        private static string FormatearHora(int minutos)
        {
            return (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        protected static string ObtenerTextoAsignado(object nombre)
        {
            return nombre == null ? "Sin asignar" : "Asignado a " + nombre;
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            if (!GestorDeSesion.TienePermisoInterno("GESTIONAR_SOPORTE"))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
