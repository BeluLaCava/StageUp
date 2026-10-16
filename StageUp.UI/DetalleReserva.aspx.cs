using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI
{
    // CU-001-005: "Detalle de reserva" (A8) para el solicitante y para el
    // gestor del espacio, con las acciones que corresponden según el estado y
    // la participación del usuario: cancelar (A13 a A16), aceptar o rechazar
    // (A9 a A12), pagar, calificar y contactar soporte. Cada acción pide una
    // confirmación antes de ejecutarse.
    // También muestra los mensajes privados de la reserva entre el
    // solicitante y el gestor (BLL_MensajeReserva).
    public partial class DetalleReserva : Page
    {
        private const string AccionCancelar = "Cancelar";
        private const string AccionAceptar = "Aceptar";
        private const string AccionRechazar = "Rechazar";

        private readonly BLL_Reserva _bllReserva = new BLL_Reserva();
        private readonly BLL_MensajeReserva _bllMensajes = new BLL_MensajeReserva();

        // Para pintar los mensajes: quién mira y cuáles no había leído todavía.
        private int _idUsuarioActual;
        private bool _actualEsSolicitante;
        private HashSet<int> _mensajesNuevos = new HashSet<int>();

        private int IdReserva
        {
            get { return ViewState["IdReserva"] is int ? (int)ViewState["IdReserva"] : 0; }
            set { ViewState["IdReserva"] = value; }
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
                int id;
                if (!int.TryParse(Request.QueryString["id"], NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id <= 0)
                {
                    MostrarNoEncontrada("No indicaste qué reserva querés ver.");
                    return;
                }

                IdReserva = id;
                Cargar();

                if (Request.QueryString["enviado"] == "1" && pnlDetalle.Visible)
                {
                    MostrarMensajeChat("Mensaje enviado.", false);
                }
            }
        }

        protected void btnEnviarMensaje_Click(object sender, EventArgs e)
        {
            ResultadoOperacion<int> resultado = _bllMensajes.Enviar(IdReserva, GestorDeSesion.ObtenerIdUsuarioActual().Value, txtMensaje.Text);
            if (resultado.Exitoso)
            {
                // Se vuelve a pedir la página para que actualizar no reenvíe el mensaje.
                Response.Redirect("~/DetalleReserva.aspx?id=" + IdReserva.ToString(CultureInfo.InvariantCulture) + "&enviado=1", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            Cargar();
            MostrarMensajeChat(resultado.Mensaje, true);
        }

        protected void btnCancelar_Click(object sender, EventArgs e)
        {
            Reserva reserva = CargarReserva();
            if (reserva == null)
            {
                return;
            }

            PedirConfirmacion(AccionCancelar, _bllReserva.ObtenerAvisoCancelacion(reserva),
                reserva.EstadoReserva == EstadoReserva.Pendiente.ToString() ? "Sí, cancelar la solicitud" : "Sí, cancelar la reserva", false);
        }

        protected void btnAceptar_Click(object sender, EventArgs e)
        {
            PedirConfirmacion(AccionAceptar,
                "¿Querés aceptar esta solicitud? El horario queda bloqueado para esta reserva y se le avisa al solicitante.",
                "Sí, aceptar", true);
        }

        protected void btnRechazar_Click(object sender, EventArgs e)
        {
            PedirConfirmacion(AccionRechazar,
                "¿Querés rechazar esta solicitud? Se le avisa al solicitante y no se puede deshacer.",
                "Sí, rechazar", true);
        }

        protected void lnkVolver_Click(object sender, EventArgs e)
        {
            pnlConfirmacion.Visible = false;
            hfAccion.Value = string.Empty;
            txtComentarioGestor.Text = string.Empty;
            Cargar();
        }

        protected void btnConfirmar_Click(object sender, EventArgs e)
        {
            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            ResultadoOperacion resultado;

            switch (hfAccion.Value)
            {
                case AccionCancelar:
                    resultado = _bllReserva.Cancelar(IdReserva, idUsuario);
                    break;
                case AccionAceptar:
                    resultado = _bllReserva.Aceptar(IdReserva, idUsuario, txtComentarioGestor.Text);
                    break;
                case AccionRechazar:
                    resultado = _bllReserva.Rechazar(IdReserva, idUsuario, txtComentarioGestor.Text);
                    break;
                default:
                    return;
            }

            pnlConfirmacion.Visible = false;
            hfAccion.Value = string.Empty;
            txtComentarioGestor.Text = string.Empty;

            // Con éxito o sin él (A11, A12), se vuelve a mostrar el detalle con
            // la información actualizada.
            Cargar();
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
        }

        private void PedirConfirmacion(string accion, string texto, string textoBoton, bool conComentario)
        {
            hfAccion.Value = accion;
            litConfirmacion.Text = texto;
            btnConfirmar.Text = textoBoton;
            pnlComentarioGestor.Visible = conComentario;
            pnlConfirmacion.Visible = true;
            pnlMensaje.Visible = false;
        }

        private Reserva CargarReserva()
        {
            ResultadoOperacion<Reserva> resultado = _bllReserva.ObtenerDetalle(IdReserva, GestorDeSesion.ObtenerIdUsuarioActual().Value);
            if (!resultado.Exitoso)
            {
                MostrarNoEncontrada(resultado.Mensaje);
                return null;
            }

            return resultado.Valor;
        }

        private void Cargar()
        {
            Reserva reserva = CargarReserva();
            if (reserva == null)
            {
                return;
            }

            int idUsuario = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            bool esSolicitante = reserva.IdUsuarioExternoSolicitante == idUsuario;
            bool esGestor = reserva.IdUsuarioGestor == idUsuario;
            string estado = reserva.EstadoReserva;

            pnlDetalle.Visible = true;
            pnlNoEncontrada.Visible = false;
            pnlEncabezado.Visible = true;

            litNumero.Text = reserva.IdReserva.ToString(CultureInfo.InvariantCulture);
            lblEstado.Text = Server.HtmlEncode(estado);
            lblEstado.CssClass = "dr-estado dr-estado-" + (estado ?? string.Empty).ToLowerInvariant();

            lnkEspacio.Text = Server.HtmlEncode(reserva.NombreEspacio);
            lnkEspacio.NavigateUrl = "~/Explorar/DetalleEspacio.aspx?id=" + reserva.IdEspacioArtistico.ToString(CultureInfo.InvariantCulture);
            litTipoEspacio.Text = ValorOGuion(reserva.TipoEspacio);
            litUbicacion.Text = ArmarUbicacion(reserva);
            litFecha.Text = reserva.FechaSolicitada.ToString("dddd d 'de' MMMM 'de' yyyy", new CultureInfo("es-AR"));
            litHorario.Text = reserva.MinutoDesde.HasValue && reserva.MinutoHasta.HasValue
                ? FormatearHora(reserva.MinutoDesde.Value) + " a " + FormatearHora(reserva.MinutoHasta.Value) + " hs"
                : "Sin horario indicado";
            litValor.Text = reserva.ImporteEstimado.HasValue
                ? BLL_CuentaCorriente.FormatearImporte(reserva.ImporteEstimado.Value, reserva.Moneda ?? "ARS") +
                  (reserva.PrecioHoraPactado.HasValue
                      ? " (" + BLL_CuentaCorriente.FormatearImporte(reserva.PrecioHoraPactado.Value, reserva.Moneda ?? "ARS") + " por hora)"
                      : string.Empty)
                : "A coordinar con el gestor";
            litPago.Text = ValorOGuion(BLL_Pago.DescribirEstadoPago(reserva, esGestor && !esSolicitante));
            litFechaSolicitud.Text = reserva.FechaCreacion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

            phResolucion.Visible = reserva.FechaResolucion.HasValue;
            if (reserva.FechaResolucion.HasValue)
            {
                litFechaResolucion.Text = reserva.FechaResolucion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }

            phCancelacion.Visible = estado == EstadoReserva.Cancelada.ToString();
            if (phCancelacion.Visible)
            {
                litFechaCancelacion.Text = reserva.FechaCancelacion.HasValue
                    ? reserva.FechaCancelacion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                    : "-";
                litCargoCancelacion.Text = reserva.ComisionAplicada && reserva.ImporteComision.HasValue
                    ? BLL_CuentaCorriente.FormatearImporte(reserva.ImporteComision.Value, reserva.Moneda ?? "ARS")
                    : "Sin cargo";
            }

            pnlComentarioSolicitante.Visible = !string.IsNullOrWhiteSpace(reserva.ComentarioSolicitante);
            litComentarioSolicitante.Text = reserva.ComentarioSolicitante;
            pnlComentarioResolucion.Visible = !string.IsNullOrWhiteSpace(reserva.ComentarioResolucion);
            litComentarioResolucion.Text = reserva.ComentarioResolucion;
            pnlComentarios.Visible = pnlComentarioSolicitante.Visible || pnlComentarioResolucion.Visible;

            litSolicitante.Text = esSolicitante ? reserva.NombreSolicitante + " (vos)" : reserva.NombreSolicitante;
            lblCorreoSolicitante.Text = Server.HtmlEncode(reserva.CorreoSolicitante);
            lblCorreoSolicitante.Visible = esGestor;
            litGestor.Text = esGestor ? reserva.NombreGestor + " (vos)" : reserva.NombreGestor;

            litCapacidad.Text = reserva.CapacidadMaxima.HasValue
                ? reserva.CapacidadMaxima.Value.ToString(CultureInfo.InvariantCulture) + " personas"
                : "-";
            litTipoPiso.Text = ValorOGuion(reserva.TipoPiso);
            litEquipamiento.Text = ValorOGuion(reserva.DetalleEquipamiento);

            PoliticaCancelacion politica = _bllReserva.ObtenerPoliticaCancelacion();
            litPolitica.Text = BLL_Reserva.DescribirPoliticaCancelacion(politica);
            bool cancelable = estado == EstadoReserva.Pendiente.ToString() || estado == EstadoReserva.Aceptada.ToString();
            pnlPoliticaAhora.Visible = esSolicitante && cancelable;
            if (pnlPoliticaAhora.Visible)
            {
                litPoliticaAhora.Text = DescribirCancelacionAhora(reserva);
            }

            rptSeguimiento.DataSource = esSolicitante
                ? BLL_Reserva.ConstruirSeguimiento(reserva)
                : BLL_Reserva.ConstruirSeguimientoGestor(reserva);
            rptSeguimiento.DataBind();

            ConfigurarAcciones(reserva, esSolicitante, esGestor);
            CargarMensajes(reserva, idUsuario, esSolicitante);

            lnkVolverListado.Text = esSolicitante ? "← Volver a Mis reservas" : "← Volver a Solicitudes recibidas";
            lnkVolverListado.NavigateUrl = esSolicitante ? "~/MisReservas.aspx" : "~/SolicitudesRecibidas.aspx";
        }

        private void ConfigurarAcciones(Reserva reserva, bool esSolicitante, bool esGestor)
        {
            string estado = reserva.EstadoReserva;
            bool pendiente = estado == EstadoReserva.Pendiente.ToString();
            bool aceptada = estado == EstadoReserva.Aceptada.ToString();
            bool finalizada = estado == EstadoReserva.Finalizada.ToString();

            lnkPagar.Visible = esSolicitante && BLL_Pago.EsperaPago(reserva);
            lnkPagar.NavigateUrl = "~/Pagar.aspx?reserva=" + reserva.IdReserva.ToString(CultureInfo.InvariantCulture);

            // CU-002-001: el detalle del pago y su factura simulada.
            lnkVerPago.Visible = (esSolicitante || esGestor) &&
                (reserva.EstadoPago == BLL_Pago.EstadoPagado || reserva.EstadoPago == BLL_Pago.EstadoDevuelto);
            lnkVerPago.NavigateUrl = "~/DetallePago.aspx?reserva=" + reserva.IdReserva.ToString(CultureInfo.InvariantCulture);

            btnCancelar.Visible = esSolicitante && (pendiente || aceptada);
            btnCancelar.Text = pendiente ? "Cancelar solicitud" : "Cancelar reserva";

            btnAceptar.Visible = esGestor && pendiente;
            btnRechazar.Visible = esGestor && pendiente;

            if (esSolicitante && finalizada && !reserva.CalificacionEspacioRealizada)
            {
                lnkCalificar.Visible = true;
                lnkCalificar.Text = "Calificar el espacio";
                lnkCalificar.NavigateUrl = "~/MisReservas.aspx";
            }
            else if (esGestor && finalizada && !reserva.CalificacionSolicitanteRealizada)
            {
                lnkCalificar.Visible = true;
                lnkCalificar.Text = "Calificar al solicitante";
                lnkCalificar.NavigateUrl = "~/SolicitudesRecibidas.aspx";
            }
            else
            {
                lnkCalificar.Visible = false;
            }

            lnkSoporte.Visible = esSolicitante;
            lnkSoporte.NavigateUrl = "~/Soporte.aspx?reserva=" + reserva.IdReserva.ToString(CultureInfo.InvariantCulture);

            bool hayAcciones = lnkPagar.Visible || lnkVerPago.Visible || btnCancelar.Visible || btnAceptar.Visible || btnRechazar.Visible ||
                lnkCalificar.Visible || lnkSoporte.Visible;
            pnlAcciones.Visible = hayAcciones;
            pnlSinAcciones.Visible = !hayAcciones;
        }

        // Mensajería privada de la reserva: se listan los mensajes, se marcan
        // como nuevos los que el usuario no había leído y después se marcan
        // como leídos.
        private void CargarMensajes(Reserva reserva, int idUsuario, bool esSolicitante)
        {
            _idUsuarioActual = idUsuario;
            _actualEsSolicitante = esSolicitante;

            litMensajesAyuda.Text = esSolicitante
                ? "Conversación privada con el gestor del espacio (" + reserva.NombreGestor + "). Solo ustedes dos ven estos mensajes. Para problemas con la plataforma, usá Contactar soporte."
                : "Conversación privada con el solicitante (" + reserva.NombreSolicitante + "). Solo ustedes dos ven estos mensajes.";

            ResultadoOperacion<List<MensajeReserva>> mensajes = _bllMensajes.Listar(reserva, idUsuario);
            List<MensajeReserva> lista = mensajes.Exitoso ? mensajes.Valor : new List<MensajeReserva>();
            _mensajesNuevos = new HashSet<int>(lista
                .Where(m => m.IdUsuarioExternoEmisor != idUsuario && !m.Leido)
                .Select(m => m.IdMensajeReserva));

            rptMensajes.DataSource = lista;
            rptMensajes.DataBind();
            pnlSinMensajes.Visible = lista.Count == 0;
            if (!mensajes.Exitoso)
            {
                MostrarMensajeChat(mensajes.Mensaje, true);
            }

            if (_mensajesNuevos.Count > 0)
            {
                _bllMensajes.MarcarLeidos(reserva, idUsuario);
            }

            bool admite = BLL_MensajeReserva.AdmiteMensajes(reserva);
            pnlEscribirMensaje.Visible = admite;
            pnlMensajesCerrados.Visible = !admite;
            litMensajesCerrados.Text = "La reserva está " + (reserva.EstadoReserva ?? string.Empty).ToLowerInvariant() +
                ": la conversación queda como registro, pero ya no se pueden enviar mensajes.";
        }

        protected string ClaseMensaje(object dato)
        {
            MensajeReserva mensaje = (MensajeReserva)dato;
            bool propio = mensaje.IdUsuarioExternoEmisor == _idUsuarioActual;
            return "dr-chat-mensaje " + (propio ? "dr-chat-propio" : "dr-chat-ajeno") +
                (_mensajesNuevos.Contains(mensaje.IdMensajeReserva) ? " dr-chat-nuevo" : string.Empty);
        }

        protected string AutorMensaje(object dato)
        {
            MensajeReserva mensaje = (MensajeReserva)dato;
            if (mensaje.IdUsuarioExternoEmisor == _idUsuarioActual)
            {
                return "Vos";
            }

            return mensaje.NombreCompletoEmisor + (_actualEsSolicitante ? " · Gestor" : " · Solicitante");
        }

        protected string EstadoLectura(object dato)
        {
            MensajeReserva mensaje = (MensajeReserva)dato;
            if (mensaje.IdUsuarioExternoEmisor == _idUsuarioActual)
            {
                return mensaje.Leido ? " · Leído" : " · Enviado";
            }

            return _mensajesNuevos.Contains(mensaje.IdMensajeReserva) ? " · Nuevo" : string.Empty;
        }

        private void MostrarMensajeChat(string mensaje, bool esError)
        {
            if (string.IsNullOrEmpty(mensaje))
            {
                return;
            }

            litMensajeChat.Text = mensaje;
            pnlMensajeChat.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensajeChat.Visible = true;
        }

        private string DescribirCancelacionAhora(Reserva reserva)
        {
            if (reserva.EstadoReserva == EstadoReserva.Pendiente.ToString())
            {
                return "Si cancelás ahora: sin cargo, porque la solicitud todavía no fue aceptada.";
            }

            if (reserva.EstadoPago == BLL_Pago.EstadoPendiente)
            {
                return "Si cancelás ahora: sin cargo, porque todavía no la pagaste.";
            }

            decimal cargo = _bllReserva.CalcularPenalidadCancelacion(reserva);
            if (cargo <= 0)
            {
                return "Si cancelás ahora: sin cargo.";
            }

            return "Si cancelás ahora: cargo del " +
                _bllReserva.ObtenerPorcentajeCargoCancelacion(reserva).ToString("0.##", CultureInfo.InvariantCulture) + " % (" +
                BLL_CuentaCorriente.FormatearImporte(cargo, reserva.Moneda ?? "ARS") + ").";
        }

        private static string ArmarUbicacion(Reserva reserva)
        {
            string ubicacion = string.Join(", ", new[] { reserva.DireccionEspacio, reserva.CiudadEspacio, reserva.ProvinciaEspacio }
                .Where(parte => !string.IsNullOrWhiteSpace(parte)));
            return string.IsNullOrEmpty(ubicacion) ? "-" : ubicacion;
        }

        private static string ValorOGuion(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor;
        }

        private static string FormatearHora(int minutos)
        {
            return (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private void MostrarNoEncontrada(string mensaje)
        {
            pnlDetalle.Visible = false;
            pnlEncabezado.Visible = false;
            pnlNoEncontrada.Visible = true;
            litNoEncontrada.Text = mensaje;
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            if (string.IsNullOrEmpty(mensaje))
            {
                return;
            }

            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = "form-message " + (esError ? "form-message-error" : "form-message-success");
            pnlMensaje.Visible = true;
        }
    }
}
