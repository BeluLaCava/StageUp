using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // CU-003-002 Registrar y realizar seguimiento de solicitudes de soporte
    // (ítem 12: conversación, leído / no leído y actualización periódica).
    // Tres pantallas en una página, según la query string:
    //   Soporte.aspx                    -> "Mis solicitudes de soporte" (A5, A6)
    //   Soporte.aspx?nueva=1[&origen=CentroAyuda|Asistente][&consulta=...]
    //   Soporte.aspx?reserva={id}       -> "Nueva solicitud de soporte" (pasos 2 a 16,
    //                                      A1 a A4), con la reserva ya elegida
    //   Soporte.aspx?ver={id}           -> "Detalle de solicitud de soporte" (A5 a A9)
    public partial class Soporte : Page
    {
        private readonly BLL_Ticket _bll = new BLL_Ticket();

        protected string UrlNovedades { get; private set; }
        protected string MarcaNovedades { get; private set; }

        private int IdUsuario
        {
            get { return GestorDeSesion.ObtenerIdUsuarioActual().Value; }
        }

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

            int idVer;
            if (int.TryParse(Request.QueryString["ver"], out idVer))
            {
                MostrarRegistroExitoso();
                CargarDetalle(idVer);
            }
            else if (Request.QueryString["nueva"] == "1" || !string.IsNullOrEmpty(Request.QueryString["reserva"]))
            {
                PrepararFormulario();
            }
            else
            {
                CargarListado();
            }
        }

        // ------------------------------------------------------------------
        // Nueva solicitud
        // ------------------------------------------------------------------

        protected void btnEnviar_Click(object sender, EventArgs e)
        {
            int idReserva;
            ResultadoOperacion<int> resultado = _bll.RegistrarSolicitud(new SolicitudSoporteNueva
            {
                IdUsuarioExterno = IdUsuario,
                Motivo = txtMotivo.Text,
                FuncionalidadVinculada = ddlFuncionalidad.SelectedValue,
                Descripcion = txtDescripcion.Text,
                InformacionAdicional = txtInformacionAdicional.Text,
                IdReservaAsociada = int.TryParse(ddlReservaAsociada.SelectedValue, out idReserva) ? idReserva : (int?)null,
                Origen = Convert.ToString(ViewState["Origen"])
            });

            if (!resultado.Exitoso)
            {
                // A2, A3 y A4: el formulario conserva lo cargado.
                MostrarMensaje(resultado.Mensaje, true);
                MostrarSolo(pnlNueva);
                AjustarTitulo("Nueva solicitud de soporte", "Completá los datos para que el equipo pueda analizar el caso.");
                return;
            }

            // Paso 16: mensaje de éxito en el detalle de la solicitud creada.
            Session["SoporteRegistroExitoso"] = resultado.Mensaje;
            Response.Redirect("~/Soporte.aspx?ver=" + resultado.Valor.ToString(CultureInfo.InvariantCulture), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void PrepararFormulario()
        {
            MostrarSolo(pnlNueva);
            AjustarTitulo("Nueva solicitud de soporte", "Completá los datos para que el equipo pueda analizar el caso.");

            ddlFuncionalidad.Items.Clear();
            ddlFuncionalidad.Items.Add(new ListItem("Elegí una opción", string.Empty));
            foreach (string funcionalidad in BLL_Ticket.Funcionalidades)
            {
                ddlFuncionalidad.Items.Add(new ListItem(BLL_Ticket.TextoFuncionalidad(funcionalidad), funcionalidad));
            }

            ddlReservaAsociada.Items.Clear();
            ddlReservaAsociada.Items.Add(new ListItem("Ninguna en particular", string.Empty));
            foreach (Reserva reserva in _bll.ListarReservasParaAsociar(IdUsuario))
            {
                ddlReservaAsociada.Items.Add(new ListItem(
                    "#" + reserva.IdReserva + " · " + DescribirReserva(reserva.NombreEspacio, reserva.FechaSolicitada, reserva.MinutoDesde, reserva.MinutoHasta, reserva.EstadoReserva),
                    reserva.IdReserva.ToString(CultureInfo.InvariantCulture)));
            }

            // A1 y accesos desde una reserva: origen, datos precargados y a
            // dónde vuelve "Cancelar".
            string origen = Request.QueryString["origen"];
            string idReservaTexto = Request.QueryString["reserva"];
            string volver = "~/Soporte.aspx";
            if (!string.IsNullOrEmpty(idReservaTexto) && ddlReservaAsociada.Items.FindByValue(idReservaTexto) != null)
            {
                ddlReservaAsociada.SelectedValue = idReservaTexto;
                ddlFuncionalidad.SelectedValue = "Reserva";
                origen = BLL_Ticket.OrigenReserva;
                volver = "~/DetalleReserva.aspx?id=" + idReservaTexto;
            }
            else if (origen == BLL_Ticket.OrigenAsistente || origen == BLL_Ticket.OrigenCentroAyuda)
            {
                volver = "~/Ayuda/CentroAyuda.aspx";
            }
            else
            {
                origen = BLL_Ticket.OrigenSoporte;
            }

            ViewState["Origen"] = origen;
            lnkCancelar.NavigateUrl = volver;

            string consulta = (Request.QueryString["consulta"] ?? string.Empty).Trim();
            if (origen == BLL_Ticket.OrigenAsistente && consulta.Length > 0)
            {
                txtMotivo.Text = consulta.Length > BLL_Ticket.LongitudMaximaMotivo ? consulta.Substring(0, BLL_Ticket.LongitudMaximaMotivo) : consulta;
                pnlOrigenAsistente.Visible = true;
            }

            // A9: casos que siguen en curso.
            ResultadoOperacion<List<Ticket>> propias = _bll.ListarDelUsuario(IdUsuario);
            List<Ticket> enCurso = propias.Exitoso
                ? propias.Valor.Where(t => t.Estado == BLL_Ticket.EstadoPendiente || t.Estado == BLL_Ticket.EstadoEnProceso).Take(3).ToList()
                : new List<Ticket>();
            pnlEnCurso.Visible = enCurso.Count > 0;
            rptEnCurso.DataSource = enCurso;
            rptEnCurso.DataBind();
        }

        // ------------------------------------------------------------------
        // Mis solicitudes (A5 y A6)
        // ------------------------------------------------------------------

        private void CargarListado()
        {
            MostrarSolo(pnlListado);

            ResultadoOperacion<List<Ticket>> resultado = _bll.ListarDelUsuario(IdUsuario);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                return;
            }

            pnlSinSolicitudes.Visible = resultado.Valor.Count == 0;
            rptSolicitudes.Visible = resultado.Valor.Count > 0;
            rptSolicitudes.DataSource = resultado.Valor;
            rptSolicitudes.DataBind();

            ActivarNovedadesPeriodicas("usuario-listado", null, _bll.ObtenerMarcaListado(IdUsuario));
        }

        // ------------------------------------------------------------------
        // Detalle (A5 pasos 8 a 10, A7, A8 y A9)
        // ------------------------------------------------------------------

        protected void btnEnviarMensaje_Click(object sender, EventArgs e)
        {
            int idTicket;
            if (!int.TryParse(Request.QueryString["ver"], out idTicket))
            {
                Response.Redirect("~/Soporte.aspx");
                return;
            }

            ResultadoOperacion resultado = _bll.AgregarMensajeUsuario(idTicket, IdUsuario, txtNuevoMensaje.Text);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarDetalle(idTicket);
            if (!resultado.Exitoso)
            {
                return;
            }

            txtNuevoMensaje.Text = string.Empty;
        }

        private void CargarDetalle(int idTicket)
        {
            ResultadoOperacion<TicketCompleto> resultado = _bll.ObtenerSeguimiento(idTicket, IdUsuario);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarListado();
                return;
            }

            MostrarSolo(pnlDetalle);
            AjustarTitulo("Detalle de solicitud de soporte", "Seguí el avance de tu solicitud: estado, respuestas del equipo y actualizaciones.");

            TicketCompleto detalle = resultado.Valor;
            Ticket ticket = detalle.Ticket;
            litCodigo.Text = ticket.Codigo;
            litMotivo.Text = ticket.Motivo;
            lblEstado.Text = Server.HtmlEncode(BLL_Ticket.TextoEstado(ticket.Estado));
            lblEstado.CssClass = "ticket-badge " + BLL_Ticket.ClaseEstado(ticket.Estado);

            pnlNovedades.Visible = detalle.Novedades > 0;
            litNovedades.Text = detalle.Novedades == 1
                ? "Tu solicitud tiene 1 actualización nueva desde tu última visita (marcada como \"Nuevo\")."
                : "Tu solicitud tiene " + detalle.Novedades + " actualizaciones nuevas desde tu última visita (marcadas como \"Nuevo\").";

            litEstadoAviso.Text = BLL_Ticket.MensajeSegunEstado(ticket.Estado);
            pnlEstadoAviso.CssClass = "ticket-estado-aviso ticket-estado-aviso-" + ticket.Estado;

            litFechaRegistro.Text = ticket.FechaCreacion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs";
            litFuncionalidad.Text = BLL_Ticket.TextoFuncionalidad(ticket.FuncionalidadVinculada);
            litUltimaActividad.Text = ticket.FechaUltimaActividad.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs";
            phResolucion.Visible = ticket.FechaResolucion.HasValue && (ticket.Estado == BLL_Ticket.EstadoResuelta || ticket.Estado == BLL_Ticket.EstadoCerrada);
            litFechaResolucion.Text = ticket.FechaResolucion.HasValue ? ticket.FechaResolucion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs" : string.Empty;
            phCierre.Visible = ticket.FechaCierre.HasValue && ticket.Estado == BLL_Ticket.EstadoCerrada;
            litFechaCierre.Text = ticket.FechaCierre.HasValue ? ticket.FechaCierre.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs" : string.Empty;
            phReserva.Visible = ticket.TieneReservaAsociada;
            litReserva.Text = ticket.TieneReservaAsociada
                ? "#" + ticket.IdReservaAsociada.Value + " · " + DescribirReserva(ticket.NombreEspacioReserva, ticket.FechaReserva, ticket.MinutoDesdeReserva, ticket.MinutoHastaReserva, ticket.EstadoReserva)
                : string.Empty;

            litDescripcion.Text = ticket.Descripcion;
            phInformacion.Visible = !string.IsNullOrEmpty(ticket.InformacionAdicional);
            litInformacion.Text = ticket.InformacionAdicional;

            pnlSinMensajes.Visible = detalle.Mensajes.Count == 0;
            rptMensajes.DataSource = detalle.Mensajes;
            rptMensajes.DataBind();

            rptActualizaciones.DataSource = detalle.Actualizaciones;
            rptActualizaciones.DataBind();

            bool cerrada = ticket.Estado == BLL_Ticket.EstadoCerrada;
            pnlResponder.Visible = !cerrada;
            pnlCerrada.Visible = cerrada;

            ActivarNovedadesPeriodicas("usuario-detalle", ticket.IdTicket, _bll.ObtenerMarcaSolicitud(ticket.IdTicket, IdUsuario));
        }

        private void MostrarRegistroExitoso()
        {
            string mensaje = Session["SoporteRegistroExitoso"] as string;
            if (mensaje != null)
            {
                Session.Remove("SoporteRegistroExitoso");
                MostrarMensaje(mensaje, false);
            }
        }

        // ------------------------------------------------------------------
        // Auxiliares
        // ------------------------------------------------------------------

        private void ActivarNovedadesPeriodicas(string vista, int? idTicket, string marca)
        {
            if (marca == null)
            {
                return;
            }

            UrlNovedades = ResolveUrl("~/SoporteNovedades.ashx") + "?vista=" + vista +
                (idTicket.HasValue ? "&id=" + idTicket.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            MarcaNovedades = marca;
            pnlNovedadesPeriodicas.Visible = true;
        }

        private void MostrarSolo(Panel panel)
        {
            pnlListado.Visible = ReferenceEquals(panel, pnlListado);
            pnlNueva.Visible = ReferenceEquals(panel, pnlNueva);
            pnlDetalle.Visible = ReferenceEquals(panel, pnlDetalle);
        }

        private void AjustarTitulo(string titulo, string subtitulo)
        {
            litTitulo.Text = Server.HtmlEncode(titulo);
            litSubtitulo.Text = Server.HtmlEncode(subtitulo);
            Title = titulo + " | StageUp";
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        protected static string TextoNovedades(object cantidad)
        {
            int n = Convert.ToInt32(cantidad);
            return n == 1 ? "1 novedad" : n + " novedades";
        }

        // Texto de cada entrada del historial, visto por el usuario (no se
        // muestran los nombres del equipo interno).
        protected static string DescribirActualizacion(TicketActualizacion actualizacion)
        {
            switch (actualizacion.TipoActualizacion)
            {
                case "Alta":
                    return "Registraste la solicitud. Estado inicial: " + BLL_Ticket.TextoEstado(actualizacion.EstadoNuevo) + ".";
                case "Asignacion":
                    return actualizacion.Detalle != null && actualizacion.Detalle.StartsWith("Se quitó", StringComparison.Ordinal)
                        ? "La solicitud volvió a la bandeja del equipo de soporte."
                        : "Un integrante del equipo de soporte quedó a cargo de tu solicitud.";
                default:
                    string quien = actualizacion.EsInterno ? "El equipo de soporte" : "Vos";
                    string texto = "Estado: " + BLL_Ticket.TextoEstado(actualizacion.EstadoAnterior) + " → " + BLL_Ticket.TextoEstado(actualizacion.EstadoNuevo) + " (" + quien + ").";
                    return string.IsNullOrEmpty(actualizacion.Detalle) ? texto : texto + " " + actualizacion.Detalle;
            }
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
    }
}
