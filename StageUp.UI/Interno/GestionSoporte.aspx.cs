using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BLL;
using StageUp.Seguridad;

namespace StageUp.UI.Interno
{
    // CU-003-003 Gestionar solicitudes de soporte internamente, más el ítem
    // 12 (conversación, leído / no leído, actualización periódica y
    // asignación de responsables).
    //   GestionSoporte.aspx?[filtros]  -> "Solicitudes de soporte" (pasos 3 a 8, A2 a A4)
    //   GestionSoporte.aspx?ver={id}   -> "Detalle de solicitud de soporte" (pasos 9 a 26, A5 a A12)
    // Los filtros viajan en la query string (estado, func, resp, desde, hasta,
    // usuario, caso, noLeidas, pag) para que la actualización periódica y el
    // "Volver al listado" los conserven.
    public partial class GestionSoporte : Page
    {
        private const string ClaveFiltros = "GestionSoporte.Filtros";
        private readonly BLL_Ticket _bll = new BLL_Ticket();

        protected string UrlNovedades { get; private set; }
        protected string MarcaNovedades { get; private set; }

        private int IdUsuarioInterno
        {
            get { return GestorDeSesion.ObtenerIdUsuarioInternoActual().Value; }
        }

        private bool PuedeReabrir
        {
            get { return GestorDeSesion.TienePermisoInterno(BLL_Ticket.PermisoReabrir); }
        }

        private int? IdTicketActual
        {
            get
            {
                int id;
                return int.TryParse(Request.QueryString["ver"], out id) ? id : (int?)null;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Paso 2 y A1.
            if (!TieneAcceso() || IsPostBack)
            {
                return;
            }

            if (IdTicketActual.HasValue)
            {
                CargarDetalle(IdTicketActual.Value);
            }
            else
            {
                CargarListado();
            }
        }

        // ------------------------------------------------------------------
        // Listado (pasos 4 a 8, A2, A3 y A4)
        // ------------------------------------------------------------------

        protected void btnAplicarFiltros_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso())
            {
                return;
            }

            List<string> partes = new List<string>();
            Agregar(partes, "estado", ddlEstado.SelectedValue);
            Agregar(partes, "func", ddlFuncionalidad.SelectedValue);
            Agregar(partes, "resp", ddlResponsable.SelectedValue);
            Agregar(partes, "desde", txtDesde.Text);
            Agregar(partes, "hasta", txtHasta.Text);
            Agregar(partes, "usuario", txtUsuario.Text);
            Agregar(partes, "caso", txtCaso.Text);
            if (chkNoLeidas.Checked)
            {
                partes.Add("noLeidas=1");
            }

            Response.Redirect("~/Interno/GestionSoporte.aspx" + (partes.Count > 0 ? "?" + string.Join("&", partes) : string.Empty), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void CargarListado()
        {
            MostrarSolo(pnlListado);
            CargarOpcionesFiltros();

            ResultadoOperacion<ResumenSoporte> resumen = _bll.ObtenerResumen();
            if (!resumen.Exitoso)
            {
                MostrarMensaje(resumen.Mensaje, true);
                return;
            }

            litTotal.Text = resumen.Valor.Total.ToString(CultureInfo.InvariantCulture);
            litPendientes.Text = resumen.Valor.Pendientes.ToString(CultureInfo.InvariantCulture);
            litEnProceso.Text = resumen.Valor.EnProceso.ToString(CultureInfo.InvariantCulture);
            litResueltas.Text = resumen.Valor.Resueltas.ToString(CultureInfo.InvariantCulture);
            litCerradas.Text = resumen.Valor.Cerradas.ToString(CultureInfo.InvariantCulture);
            litSinLeer.Text = resumen.Valor.SinLeer.ToString(CultureInfo.InvariantCulture);
            litSinAsignar.Text = resumen.Valor.SinAsignar.ToString(CultureInfo.InvariantCulture);

            ActivarNovedadesPeriodicas("interno-listado", null, _bll.ObtenerMarcaListado(null));
            Session[ClaveFiltros] = Request.Url.Query;

            // A2: no hay ninguna solicitud.
            if (resumen.Valor.Total == 0)
            {
                pnlSinSolicitudes.Visible = true;
                rptSolicitudes.Visible = false;
                return;
            }

            FiltroTicket filtro = LeerFiltro();
            ResultadoOperacion<PaginaTickets> pagina = _bll.ListarInterno(filtro);
            if (!pagina.Exitoso)
            {
                MostrarMensaje(pagina.Mensaje, true);
                rptSolicitudes.Visible = false;
                return;
            }

            // A4: hay solicitudes pero ninguna cumple los filtros.
            pnlSinResultados.Visible = pagina.Valor.Total == 0;
            rptSolicitudes.Visible = pagina.Valor.Total > 0;
            rptSolicitudes.DataSource = pagina.Valor.Tickets;
            rptSolicitudes.DataBind();

            litCantidad.Text = pagina.Valor.Total == 0
                ? string.Empty
                : (pagina.Valor.Total == 1 ? "1 solicitud" : pagina.Valor.Total + " solicitudes") +
                  (filtro.TieneCriterios ? " con los filtros aplicados." : " registradas.") +
                  " Primero las que tienen mensajes sin leer.";

            pnlPaginado.Visible = pagina.Valor.CantidadPaginas > 1;
            litPagina.Text = "Página " + pagina.Valor.Pagina + " de " + pagina.Valor.CantidadPaginas;
            lnkAnterior.Enabled = pagina.Valor.Pagina > 1;
            lnkAnterior.NavigateUrl = UrlPagina(pagina.Valor.Pagina - 1);
            lnkSiguiente.Enabled = pagina.Valor.Pagina < pagina.Valor.CantidadPaginas;
            lnkSiguiente.NavigateUrl = UrlPagina(pagina.Valor.Pagina + 1);
        }

        private void CargarOpcionesFiltros()
        {
            ddlEstado.Items.Clear();
            ddlEstado.Items.Add(new ListItem("Todos", string.Empty));
            foreach (string estado in BLL_Ticket.Estados)
            {
                ddlEstado.Items.Add(new ListItem(BLL_Ticket.TextoEstado(estado), estado));
            }

            ddlFuncionalidad.Items.Clear();
            ddlFuncionalidad.Items.Add(new ListItem("Todas", string.Empty));
            foreach (string funcionalidad in BLL_Ticket.Funcionalidades)
            {
                ddlFuncionalidad.Items.Add(new ListItem(BLL_Ticket.TextoFuncionalidad(funcionalidad), funcionalidad));
            }

            ddlResponsable.Items.Clear();
            ddlResponsable.Items.Add(new ListItem("Todos", string.Empty));
            ddlResponsable.Items.Add(new ListItem("Asignadas a mí", "mias"));
            ddlResponsable.Items.Add(new ListItem("Sin responsable", "sin"));
            foreach (UsuarioInterno usuario in _bll.ListarResponsables())
            {
                ddlResponsable.Items.Add(new ListItem(usuario.Nombre + " " + usuario.Apellido, usuario.IdUsuarioInterno.ToString(CultureInfo.InvariantCulture)));
            }

            Seleccionar(ddlEstado, Request.QueryString["estado"]);
            Seleccionar(ddlFuncionalidad, Request.QueryString["func"]);
            Seleccionar(ddlResponsable, Request.QueryString["resp"]);
            txtDesde.Text = FechaValida(Request.QueryString["desde"]) ? Request.QueryString["desde"] : string.Empty;
            txtHasta.Text = FechaValida(Request.QueryString["hasta"]) ? Request.QueryString["hasta"] : string.Empty;
            txtUsuario.Text = Request.QueryString["usuario"] ?? string.Empty;
            txtCaso.Text = Request.QueryString["caso"] ?? string.Empty;
            chkNoLeidas.Checked = Request.QueryString["noLeidas"] == "1";
        }

        private FiltroTicket LeerFiltro()
        {
            FiltroTicket filtro = new FiltroTicket
            {
                Estado = Vacio(Request.QueryString["estado"]),
                FuncionalidadVinculada = Vacio(Request.QueryString["func"]),
                Desde = LeerFecha(Request.QueryString["desde"]),
                Hasta = LeerFecha(Request.QueryString["hasta"]),
                Usuario = Vacio(Request.QueryString["usuario"]),
                SoloNoLeidas = Request.QueryString["noLeidas"] == "1"
            };

            // N.º de caso: "SOP-000123", "123" o "#123".
            string caso = Vacio(Request.QueryString["caso"]);
            if (caso != null)
            {
                string numero = new string(caso.Where(char.IsDigit).ToArray());
                int idCaso;
                filtro.IdTicket = int.TryParse(numero, NumberStyles.Integer, CultureInfo.InvariantCulture, out idCaso) ? idCaso : -1;
            }

            string responsable = Request.QueryString["resp"];
            int idResponsable;
            if (responsable == "sin")
            {
                filtro.Asignacion = FiltroTicket.AsignacionSinAsignar;
            }
            else if (responsable == "mias")
            {
                filtro.Asignacion = FiltroTicket.AsignacionUsuario;
                filtro.IdUsuarioInternoAsignado = IdUsuarioInterno;
            }
            else if (int.TryParse(responsable, NumberStyles.Integer, CultureInfo.InvariantCulture, out idResponsable))
            {
                filtro.Asignacion = FiltroTicket.AsignacionUsuario;
                filtro.IdUsuarioInternoAsignado = idResponsable;
            }

            int numeroPagina;
            filtro.Pagina = int.TryParse(Request.QueryString["pag"], out numeroPagina) ? numeroPagina : 1;
            return filtro;
        }

        private string UrlPagina(int pagina)
        {
            NameValueCollectionCopia consulta = new NameValueCollectionCopia(Request.QueryString);
            consulta.Poner("pag", pagina.ToString(CultureInfo.InvariantCulture));
            return "~/Interno/GestionSoporte.aspx?" + consulta;
        }

        // ------------------------------------------------------------------
        // Detalle y gestión (pasos 9 a 26, A5 a A12)
        // ------------------------------------------------------------------

        protected void btnTomar_Click(object sender, EventArgs e)
        {
            Ejecutar(id => _bll.Tomar(id, IdUsuarioInterno));
        }

        protected void btnAsignar_Click(object sender, EventArgs e)
        {
            int idAsignado;
            int? asignado = int.TryParse(ddlAsignarA.SelectedValue, out idAsignado) ? idAsignado : (int?)null;
            Ejecutar(id => _bll.Asignar(id, asignado, IdUsuarioInterno));
        }

        protected void btnGuardarRespuesta_Click(object sender, EventArgs e)
        {
            string estadoVisto = Convert.ToString(ViewState["EstadoVisto"]);
            Ejecutar(id => _bll.RegistrarRespuesta(id, IdUsuarioInterno, txtRespuesta.Text, chkMarcarResuelta.Checked, estadoVisto), true);
        }

        // A8 pasos 1 a 4: se elige el estado y se pide confirmación.
        protected void btnCambiarEstado_Click(object sender, EventArgs e)
        {
            PedirConfirmacion(ddlNuevoEstado.SelectedValue);
        }

        // A11 paso 4.
        protected void btnCerrar_Click(object sender, EventArgs e)
        {
            PedirConfirmacion(BLL_Ticket.EstadoCerrada);
        }

        // A10 (con permiso de reapertura).
        protected void btnReabrir_Click(object sender, EventArgs e)
        {
            PedirConfirmacion(BLL_Ticket.EstadoEnProceso);
        }

        // A8 paso 5 / A11 paso 5.
        protected void btnConfirmarEstado_Click(object sender, EventArgs e)
        {
            string estadoNuevo = Convert.ToString(ViewState["EstadoAConfirmar"]);
            string estadoVisto = Convert.ToString(ViewState["EstadoVisto"]);
            ViewState.Remove("EstadoAConfirmar");
            Ejecutar(id => _bll.CambiarEstado(id, estadoNuevo, IdUsuarioInterno, PuedeReabrir, estadoVisto));
        }

        // A8: si se cancela, no se modifica nada y se vuelve al detalle.
        protected void btnCancelarEstado_Click(object sender, EventArgs e)
        {
            if (!TieneAcceso() || !IdTicketActual.HasValue)
            {
                return;
            }

            ViewState.Remove("EstadoAConfirmar");
            CargarDetalle(IdTicketActual.Value);
        }

        private void PedirConfirmacion(string estadoNuevo)
        {
            if (!TieneAcceso() || !IdTicketActual.HasValue)
            {
                return;
            }

            if (!BLL_Ticket.Estados.Contains(estadoNuevo))
            {
                MostrarMensaje("Elegí uno de los estados disponibles.", true);
                CargarDetalle(IdTicketActual.Value);
                return;
            }

            CargarDetalle(IdTicketActual.Value);
            string estadoActual = Convert.ToString(ViewState["EstadoVisto"]);
            ViewState["EstadoAConfirmar"] = estadoNuevo;
            litConfirmar.Text = estadoNuevo == BLL_Ticket.EstadoCerrada
                ? "¿Confirmás que querés cerrar la solicitud? Una solicitud cerrada solo se puede consultar."
                : "¿Confirmás el cambio de estado de \"" + BLL_Ticket.TextoEstado(estadoActual) + "\" a \"" + BLL_Ticket.TextoEstado(estadoNuevo) + "\"? Se le avisa al usuario.";
            pnlConfirmar.Visible = true;
        }

        private void Ejecutar(Func<int, ResultadoOperacion> accion, bool esRespuesta = false)
        {
            if (!TieneAcceso())
            {
                return;
            }

            if (!IdTicketActual.HasValue)
            {
                Response.Redirect("~/Interno/GestionSoporte.aspx");
                return;
            }

            ResultadoOperacion resultado = accion(IdTicketActual.Value);
            MostrarMensaje(resultado.Mensaje, !resultado.Exitoso);
            CargarDetalle(IdTicketActual.Value);
            if (esRespuesta && resultado.Exitoso)
            {
                txtRespuesta.Text = string.Empty;
                chkMarcarResuelta.Checked = false;
            }
        }

        private void CargarDetalle(int idTicket)
        {
            ResultadoOperacion<TicketCompleto> resultado = _bll.ObtenerDetalleInterno(idTicket, PuedeReabrir);
            if (!resultado.Exitoso)
            {
                MostrarMensaje(resultado.Mensaje, true);
                CargarListado();
                return;
            }

            MostrarSolo(pnlDetalle);
            litTitulo.Text = "Detalle de solicitud de soporte";
            Title = "Detalle de solicitud de soporte | StageUp";
            string filtros = Session[ClaveFiltros] as string;
            lnkVolver.NavigateUrl = "~/Interno/GestionSoporte.aspx" + (string.IsNullOrEmpty(filtros) ? string.Empty : filtros);

            TicketCompleto detalle = resultado.Valor;
            Ticket ticket = detalle.Ticket;
            ViewState["EstadoVisto"] = ticket.Estado;

            litCodigo.Text = ticket.Codigo;
            litMotivo.Text = ticket.Motivo;
            lblEstado.Text = HttpUtility.HtmlEncode(BLL_Ticket.TextoEstado(ticket.Estado));
            lblEstado.CssClass = "ticket-badge " + BLL_Ticket.ClaseEstado(ticket.Estado);

            pnlNovedades.Visible = detalle.Novedades > 0 && ticket.Estado != BLL_Ticket.EstadoCerrada;
            litNovedades.Text = !ticket.FechaUltimaLecturaSoporte.HasValue
                ? "Solicitud nueva: es la primera vez que alguien del equipo la abre."
                : detalle.Novedades == 1
                    ? "El usuario escribió 1 mensaje nuevo (marcado como \"Sin leer\")."
                    : "El usuario escribió " + detalle.Novedades + " mensajes nuevos (marcados como \"Sin leer\").";

            litUsuario.Text = ticket.NombreUsuarioExterno + " (" + ticket.CorreoUsuarioExterno + ")";
            litFechaRegistro.Text = ticket.FechaCreacion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs";
            litFuncionalidad.Text = BLL_Ticket.TextoFuncionalidad(ticket.FuncionalidadVinculada);
            litOrigen.Text = BLL_Ticket.TextoOrigen(ticket.Origen);
            litResponsable.Text = ticket.NombreUsuarioInternoAsignado ?? "Sin asignar";
            litUltimaActividad.Text = ticket.FechaUltimaActividad.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs";
            phResolucion.Visible = ticket.FechaResolucion.HasValue && (ticket.Estado == BLL_Ticket.EstadoResuelta || ticket.Estado == BLL_Ticket.EstadoCerrada);
            litFechaResolucion.Text = ticket.FechaResolucion.HasValue ? ticket.FechaResolucion.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs" : string.Empty;
            phCierre.Visible = ticket.FechaCierre.HasValue && ticket.Estado == BLL_Ticket.EstadoCerrada;
            litFechaCierre.Text = ticket.FechaCierre.HasValue ? ticket.FechaCierre.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " hs" : string.Empty;

            pnlReserva.Visible = ticket.TieneReservaAsociada;
            if (ticket.TieneReservaAsociada)
            {
                litIdReserva.Text = ticket.IdReservaAsociada.Value.ToString(CultureInfo.InvariantCulture);
                litReserva.Text = DescribirReserva(ticket);
            }

            litDescripcion.Text = ticket.Descripcion;
            phInformacion.Visible = !string.IsNullOrEmpty(ticket.InformacionAdicional);
            litInformacion.Text = ticket.InformacionAdicional;

            pnlSinMensajes.Visible = detalle.Mensajes.Count == 0;
            rptMensajes.DataSource = detalle.Mensajes;
            rptMensajes.DataBind();
            rptActualizaciones.DataSource = detalle.Actualizaciones;
            rptActualizaciones.DataBind();

            bool cerrada = ticket.Estado == BLL_Ticket.EstadoCerrada;
            bool resuelta = ticket.Estado == BLL_Ticket.EstadoResuelta;

            // A10 y A11.
            pnlEstadoAviso.Visible = cerrada || resuelta;
            litEstadoAviso.Text = cerrada
                ? "Solicitud cerrada: se muestra la información histórica del caso. No admite respuestas ni cambios de estado" +
                  (PuedeReabrir ? "; con tu permiso podés reabrirla." : ".")
                : "Solicitud resuelta: el usuario ya tiene la respuesta de resolución. Podés cerrarla o, si hace falta, volver a ponerla en proceso.";

            pnlAsignacion.Visible = !cerrada;
            pnlResponder.Visible = !cerrada;
            pnlCambiarEstado.Visible = !cerrada;
            pnlReabrir.Visible = cerrada && PuedeReabrir;
            btnCerrar.Visible = resuelta;
            pnlConfirmar.Visible = false;

            if (!cerrada)
            {
                ddlAsignarA.Items.Clear();
                ddlAsignarA.Items.Add(new ListItem("Sin responsable", string.Empty));
                List<UsuarioInterno> responsables = _bll.ListarResponsables();
                foreach (UsuarioInterno usuario in responsables)
                {
                    ddlAsignarA.Items.Add(new ListItem(usuario.Nombre + " " + usuario.Apellido, usuario.IdUsuarioInterno.ToString(CultureInfo.InvariantCulture)));
                }

                Seleccionar(ddlAsignarA, ticket.IdUsuarioInternoAsignado.HasValue ? ticket.IdUsuarioInternoAsignado.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
                btnTomar.Visible = ticket.IdUsuarioInternoAsignado != IdUsuarioInterno;

                // Paso 19: los cuatro estados; la validación (A9) se hace al confirmar.
                ddlNuevoEstado.Items.Clear();
                foreach (string estado in BLL_Ticket.Estados.Where(e => e != ticket.Estado))
                {
                    ddlNuevoEstado.Items.Add(new ListItem(BLL_Ticket.TextoEstado(estado), estado));
                }

                if (detalle.EstadosPermitidos.Count > 0)
                {
                    Seleccionar(ddlNuevoEstado, detalle.EstadosPermitidos[0]);
                }

                litTransiciones.Text = "Estado actual: " + BLL_Ticket.TextoEstado(ticket.Estado) + ". " +
                    (detalle.EstadosPermitidos.Count == 0
                        ? "Para avanzar, primero registrá una respuesta."
                        : "Desde acá se puede pasar a: " + string.Join(", ", detalle.EstadosPermitidos.Select(BLL_Ticket.TextoEstado)) + ".");
            }

            ActivarNovedadesPeriodicas("interno-detalle", ticket.IdTicket, _bll.ObtenerMarcaSolicitud(ticket.IdTicket, null));
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
            pnlDetalle.Visible = ReferenceEquals(panel, pnlDetalle);
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            litMensaje.Text = mensaje;
            pnlMensaje.CssClass = esError ? "form-message form-message-error" : "form-message form-message-success";
            pnlMensaje.Visible = !string.IsNullOrEmpty(mensaje);
        }

        protected static bool TieneSinLeer(object item)
        {
            Ticket ticket = (Ticket)item;
            return ticket.NovedadesSoporte > 0 && ticket.Estado != BLL_Ticket.EstadoCerrada;
        }

        protected static string TextoSinLeer(object item)
        {
            Ticket ticket = (Ticket)item;
            if (!ticket.FechaUltimaLecturaSoporte.HasValue)
            {
                return "Nueva";
            }

            return ticket.NovedadesSoporte == 1 ? "1 sin leer" : ticket.NovedadesSoporte + " sin leer";
        }

        protected static string TextoTipoActualizacion(object tipo)
        {
            switch (Convert.ToString(tipo))
            {
                case "Alta": return "Registro";
                case "CambioEstado": return "Cambio de estado";
                case "Asignacion": return "Asignación";
                default: return Convert.ToString(tipo);
            }
        }

        private static string DescribirReserva(Ticket ticket)
        {
            string texto = string.IsNullOrEmpty(ticket.NombreEspacioReserva) ? "Reserva" : ticket.NombreEspacioReserva;
            if (ticket.FechaReserva.HasValue)
            {
                texto += " · " + ticket.FechaReserva.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (ticket.MinutoDesdeReserva.HasValue && ticket.MinutoHastaReserva.HasValue)
                {
                    texto += " " + FormatearHora(ticket.MinutoDesdeReserva.Value) + " a " + FormatearHora(ticket.MinutoHastaReserva.Value);
                }
            }

            if (!string.IsNullOrEmpty(ticket.EstadoReserva))
            {
                texto += " (estado de la reserva: " + ticket.EstadoReserva + ")";
            }

            return texto;
        }

        private static string FormatearHora(int minutos)
        {
            return (minutos / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (minutos % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static void Seleccionar(DropDownList lista, string valor)
        {
            if (valor != null && lista.Items.FindByValue(valor) != null)
            {
                lista.SelectedValue = valor;
            }
        }

        private static void Agregar(List<string> partes, string clave, string valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                partes.Add(clave + "=" + HttpUtility.UrlEncode(valor.Trim()));
            }
        }

        private static string Vacio(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static bool FechaValida(string valor)
        {
            return LeerFecha(valor).HasValue;
        }

        private static DateTime? LeerFecha(string valor)
        {
            DateTime fecha;
            return DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                ? fecha
                : (DateTime?)null;
        }

        // Copia editable de la query string, para armar los enlaces del paginado.
        private sealed class NameValueCollectionCopia
        {
            private readonly List<KeyValuePair<string, string>> _pares = new List<KeyValuePair<string, string>>();

            public NameValueCollectionCopia(System.Collections.Specialized.NameValueCollection origen)
            {
                foreach (string clave in origen.AllKeys.Where(k => k != null))
                {
                    _pares.Add(new KeyValuePair<string, string>(clave, origen[clave]));
                }
            }

            public void Poner(string clave, string valor)
            {
                _pares.RemoveAll(p => p.Key == clave);
                _pares.Add(new KeyValuePair<string, string>(clave, valor));
            }

            public override string ToString()
            {
                return string.Join("&", _pares.Select(p => HttpUtility.UrlEncode(p.Key) + "=" + HttpUtility.UrlEncode(p.Value)));
            }
        }

        private bool TieneAcceso()
        {
            if (!GestorDeSesion.EstaAutenticadoComoInterno())
            {
                Response.Redirect("~/IniciarSesion.aspx");
                return false;
            }

            // A1: sin permiso no se muestra el listado.
            if (!GestorDeSesion.TienePermisoInterno(BLL_Ticket.PermisoGestionar))
            {
                Response.Redirect("~/Interno/PanelAdministrador.aspx?acceso=denegado");
                return false;
            }

            return true;
        }
    }
}
