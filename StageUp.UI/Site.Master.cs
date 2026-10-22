using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using StageUp.BE.Entidades;
using StageUp.BE.Enumerados;
using StageUp.BLL;
using StageUp.Seguridad;
using StageUp.UI.Infraestructura;

namespace StageUp.UI
{
    public partial class SiteMaster : MasterPageMultidioma
    {
        private readonly BLL_Notificacion _bllNotificacion = new BLL_Notificacion();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InicializarMultidioma(ddlIdioma, hdnDiccionarioIdioma, HtmlRoot, LanguageSelector);


            if (!IsPostBack)
            {
                OrigenDeDifusion.Capturar(Context);
            }



            int? idUsuario = GestorDeSesion.ObtenerIdUsuarioActual();
            if (idUsuario.HasValue && GestorDeSesion.CorrespondeVerificarCuenta())
            {
                string perfilActual;
                if (!new BLL_UsuarioExterno().CuentaSigueActiva(idUsuario.Value, GestorDeSesion.ObtenerFechaAltaCuentaActual(), out perfilActual))
                {
                    GestorDeSesion.CerrarSesion();
                    Response.Redirect("~/Default.aspx?cuenta=baja", true);
                    return;
                }



                if (perfilActual != null && perfilActual != GestorDeSesion.ObtenerPerfilActual())
                {
                    GestorDeSesion.ActualizarPerfilEnSesion(perfilActual);
                }

                GestorDeSesion.MarcarCuentaVerificada();
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool autenticado = GestorDeSesion.EstaAutenticado();

            PublicActions.Visible = !autenticado;
            AuthenticatedTools.Visible = autenticado;
            AuthenticatedSidebar.Visible = autenticado;

            if (autenticado)
            {
                UserSummary.InnerText = GestorDeSesion.ObtenerNombreCompletoActual();



                pnlAvisoRestauracion.Visible = GestorDeSesion.TomarAvisoRestauracion();





                bool esGestorEspacios = GestorDeSesion.ObtenerPerfilActual() == PerfilUsuarioExterno.GestorEspacios.ToString();
                MisActividadesLink.Visible = esGestorEspacios;
                PreguntasRecibidasLink.Visible = esGestorEspacios;



                if (!esGestorEspacios)
                {
                    MisEspaciosLink.InnerText = "Ofrecer espacio";
                    MisEspaciosLink.HRef = "~/OfrecerEspacio.aspx";
                    MisEspaciosLink.Attributes.Remove("data-i18n");
                }







                if (BLL_Reserva.CorrespondeGenerarRecordatorios())
                {
                    try
                    {
                        System.Web.Hosting.HostingEnvironment.QueueBackgroundWorkItem(
                            cancelacion => new BLL_Reserva().GenerarRecordatorios24hsSiCorresponde());
                    }
                    catch (InvalidOperationException)
                    {


                        new BLL_Reserva().GenerarRecordatorios24hsSiCorresponde();
                    }
                }

                CargarNotificaciones();
                CargarIndicadorSoporte();
                CargarIndicadorMensajesReserva(esGestorEspacios);
            }
        }



        private void CargarIndicadorSoporte()
        {
            int novedades = new BLL_Ticket().ContarNovedadesUsuario(GestorDeSesion.ObtenerIdUsuarioActual().Value);
            SoporteLink.InnerHtml = novedades > 0
                ? "Soporte <span class=\"sidebar-badge\" title=\"Solicitudes de soporte con novedades\">" + novedades + "</span>"
                : "Soporte";


            if (PreguntasRecibidasLink.Visible)
            {
                int pendientes = new BLL_PreguntaEspacio().ContarPendientesGestor(GestorDeSesion.ObtenerIdUsuarioActual().Value);
                PreguntasRecibidasLink.InnerHtml = pendientes > 0
                    ? "Preguntas recibidas <span class=\"sidebar-badge\" title=\"Preguntas sin responder\">" + pendientes + "</span>"
                    : "Preguntas recibidas";
            }
        }





        private void CargarIndicadorMensajesReserva(bool esGestorEspacios)
        {
            List<MensajesNoLeidosReserva> noLeidos = new BLL_MensajeReserva().NoLeidos(GestorDeSesion.ObtenerIdUsuarioActual().Value);
            int comoSolicitante = noLeidos.Where(n => n.Rol == MensajesNoLeidosReserva.RolSolicitante).Sum(n => n.Cantidad);
            int comoGestor = noLeidos.Where(n => n.Rol == MensajesNoLeidosReserva.RolGestor).Sum(n => n.Cantidad);

            AgregarContador(MisReservasLink, comoSolicitante, "Mensajes sin leer en tus reservas");
            if (esGestorEspacios)
            {
                AgregarContador(MisEspaciosLink, comoGestor, "Mensajes sin leer en solicitudes recibidas");
            }
        }

        private static void AgregarContador(System.Web.UI.HtmlControls.HtmlAnchor enlace, int cantidad, string titulo)
        {
            if (cantidad <= 0)
            {
                return;
            }

            string clave = enlace.Attributes["data-i18n"];
            string texto = HttpUtility.HtmlEncode(enlace.InnerText);
            enlace.Attributes.Remove("data-i18n");
            enlace.InnerHtml = (string.IsNullOrEmpty(clave) ? texto : "<span data-i18n=\"" + HttpUtility.HtmlAttributeEncode(clave) + "\">" + texto + "</span>") +
                " <span class=\"sidebar-badge\" title=\"" + HttpUtility.HtmlAttributeEncode(titulo) + "\">" + cantidad + "</span>";
        }

        protected void lnkCerrarSesion_Click(object sender, EventArgs e)
        {
            GestorDeSesion.CerrarSesion();
            Response.Redirect("~/Default.aspx");
        }

        protected void rptNotificaciones_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Ir")
            {
                return;
            }




            int idNotificacion;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idNotificacion))
            {
                return;
            }

            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            string error;
            string destino = AperturaNotificacion.ObtenerDestino(idNotificacion, idUsuarioExterno, out error);
            Response.Redirect(destino != null
                ? ResolveUrl(destino)
                : "~/Notificaciones.aspx?no_disponible=" + idNotificacion, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lnkMarcarTodasLeidas_Click(object sender, EventArgs e)
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;
            _bllNotificacion.MarcarTodasLeidas(idUsuarioExterno);
            CargarNotificaciones();
        }



        public void ActualizarNotificaciones()
        {
            if (GestorDeSesion.EstaAutenticado())
            {
                CargarNotificaciones();
            }
        }

        private void CargarNotificaciones()
        {
            int idUsuarioExterno = GestorDeSesion.ObtenerIdUsuarioActual().Value;

            List<Notificacion> notificaciones = _bllNotificacion.ListarPorUsuario(idUsuarioExterno);
            int noLeidas = _bllNotificacion.ContarNoLeidas(idUsuarioExterno);

            rptNotificaciones.DataSource = notificaciones;
            rptNotificaciones.DataBind();

            pnlSinNotificaciones.Visible = notificaciones.Count == 0;

            litNotificacionesCount.Text = noLeidas > 0
                ? "<span class=\"notifications-count\" aria-label=\"" + noLeidas + " notificaciones sin leer\">" + noLeidas + "</span>"
                : string.Empty;
        }

        protected string FormatearFechaNotificacion(DateTime fecha)
        {
            if (fecha.Date == DateTime.Today)
            {
                return "Hoy " + fecha.ToString("HH:mm");
            }

            if (fecha.Date == DateTime.Today.AddDays(-1))
            {
                return "Ayer";
            }

            return fecha.ToString("dd/MM/yyyy");
        }
    }
}
